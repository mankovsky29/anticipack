# Anticipack Development Agent

You are a specialized development agent for the **Anticipack** packing checklist application — a multi-project .NET 9 solution.

## Solution Structure

| Project | SDK | Type | Purpose |
|---|---|---|---|
| `Anticipack` | `Microsoft.NET.Sdk.Razor` | .NET MAUI Blazor Hybrid | Main mobile/desktop app (Android, iOS, macOS, Windows) |
| `Anticipack.API` | `Microsoft.NET.Sdk.Web` | ASP.NET Core Web API | Backend REST API with JWT auth, in-memory repositories (dev) |
| `Anticipack.Workers` | `Microsoft.NET.Sdk.Worker` | .NET Worker Service | Telegram bot hosted service using `BackgroundService` |

All three projects target **.NET 9** with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`.

---

## Anticipack (MAUI Blazor Hybrid)

### Technology Stack
- **UI**: Blazor Hybrid — Razor components rendered in a MAUI WebView
- **Data**: SQLite via `sqlite-net-pcl` (async), models in `Storage/`
- **Localization**: `IStringLocalizer<AppResources>` with `.resx` files (`en`, `es`, `ru`) under `Resources/Localization/`
- **AI**: Gemini API via `GeminiSuggestionService` (implements `IAiSuggestionService`)
- **Payments**: In-app billing (`Plugin.InAppBilling`), PayPal integration
- **Icons**: Font Awesome — `<i class="fa fa-icon-name" aria-hidden="true"></i>`

### Architecture (SOLID)

**Repository Layer** (`Storage/Repositories/`):
- Three focused interfaces (ISP): `IPackingActivityRepository`, `IPackingItemRepository`, `IPackingHistoryRepository`
- `IDatabaseConnectionFactory` abstracts SQLite connection creation (DIP)
- Legacy `IPackingRepository` (marked `[Obsolete]`) extends all three — prefer focused interfaces in new code

**Service Layer** (`Services/`):
- `IPackingActivityService` / `PackingActivityService` — activity & item business logic (SRP)
- `IPackingHistoryService` / `PackingHistoryService` — history recording & statistics
- `IPackingStatisticsService` / `PackingStatisticsService` — aggregated stats
- `ICategoryIconProvider` / `CategoryIconProvider` — category icons (OCP: dictionary-based)
- `IAiSuggestionService` / `GeminiSuggestionService` — AI item suggestions
- `ILocalizationService` / `LocalizationService` — culture management with `Preferences` persistence
- `IKeyboardService` — platform-specific keyboard visibility (Android/iOS/Windows implementations)
- `ISyncService`, `IPremiumService` — cloud sync & premium subscription (premium-gated)
- `IPaymentService`, `IStoreService`, `IPayPalService` — payment processing
- `IItemSuggestionService` / `ItemSuggestionService` — item suggestions

**DI Registration** (`MauiProgram.cs`):
- Platform-specific services use `#if ANDROID / IOS / WINDOWS` conditionals
- `AddSingleton` for repositories, platform services, and configuration
- `AddScoped` for per-page business services and UI services (`IDialogService`, `IToastService`)

### Domain Model

**Core Entities** (SQLite, in `Storage/`):
- `PackingActivity` — Id (GUID string PK), Name, LastPacked, RunCount, IsShared, IsArchived, IsFinished, IsRecurring
- `PackingItem` — Id (GUID string PK), ActivityId (FK), Name, IsPacked, Category (string matching `PackingCategory` enum), Notes, SortOrder
- `PackingHistoryEntry` — Id (GUID string PK), ActivityId (FK), CompletedDate, TotalItems, PackedItems, DurationSeconds, StartTime, EndTime

**Category Enum** (`Packing/PackingCategory.cs`):
`Clothing`, `Shoes`, `Toiletries`, `Electronics`, `Documents`, `Health`, `Accessories`, `Outdoor`, `Food`, `Entertainment`, `Miscellaneous`

### Component Organization

```
Components/
├── Features/
│   ├── Packing/      # PackingActivities, EditPacking, PlayPacking, PackingDialog
│   ├── Settings/     # UserSettings
│   ├── Statistics/   # PackingStatistics
│   └── Support/      # Support, Donate
├── Shared/           # DialogComponent, ToastComponent, NavigationHeaderComponent, SidebarTextComponent
└── Layout/           # MainLayout, NavMenu
```

**Code-Behind Pattern** (mandatory):
- Markup: `Component.razor` — only `@page` directive and HTML/Razor, **no `@code` block**
- Logic: `Component.razor.cs` — partial class with `[Inject]`, `[Parameter]`, fields, methods
- Styles: `Component.razor.css` — scoped CSS isolation
- JS: `Component.razor.js` — co-located JS module loaded via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Features/…/Component.razor.js")`

**Feature `_Imports.razor`**: Each feature folder has its own `_Imports.razor` with feature-specific `@using` directives. Create one when adding a new feature folder.

**View Model**: `PackingItemView` wraps `PackingItem` with `IsChecked` and `IsAnimating` for UI. Always use in packing components, not raw `PackingItem`.

**DI in Components**: `[Inject] private IServiceName ServiceName { get; set; } = default!;`

### CSS & Styling Rules

**CSS Custom Properties (REQUIRED)**: All new components must use variables from `wwwroot/css/app.css`:
- Colors: `--primary`, `--accent`, `--danger`, `--warning`, `--success`, `--background`, `--surface`, `--text-primary`, `--text-secondary`, `--border-color`
- Typography: `--font-size-xs` through `--font-size-xl`
- Spacing: `--space-xs` through `--space-xxxl`
- Borders: `--border-radius-sm`, `--border-radius`, `--border-radius-lg`
- Shadows: `--shadow-sm`, `--shadow`, `--shadow-md`, `--shadow-lg`
- Transitions: `--transition-fast`, `--transition`
- Component: `--input-bg`, `--input-border`, `--overlay-bg`, `--modal-bg`

**Dark Theme**: Light (`[data-theme="light"]`) and dark (`[data-theme="dark"]`) — all variables have dark overrides. Never hard-code colors.

**Responsive & Accessibility**:
- Breakpoints: `@media (max-width: 480px)`, `@media (max-width: 360px)`
- Include `@media (prefers-reduced-motion: reduce)` and `@media (prefers-contrast: high)`
- Use `role`, `aria-label`, `aria-hidden="true"` on icons, `title` on interactive elements

### JavaScript Rules

- Component JS goes in co-located `.razor.js` files. Only global helpers in `wwwroot/js/site.js`.
- Use `export function` for all public functions.
- Create `DotNetObjectReference<T>` for JS→Blazor callbacks; dispose in `IAsyncDisposable`.
- **Click-outside menus**: Use document-level capture-phase listener (`addEventListener('click', handler, true)`), not CSS overlay. Wrap in `requestAnimationFrame`. Flip with `open-up` class if near viewport bottom.
- **Full-screen/chat pages**: JS measures container top offset, sets height to `window.innerHeight - top` with `resize` listener. Container uses `overflow: hidden`.
- **Keyboard helpers**: Always pass actual keyboard height from `KeyboardService`.

### Blazor Component Behavior

- **EditPacking**: Click outside `add-item-form` = cancel. Click outside `item-edit-container` = auto-save including notes.
- **PlayPacking**: `PackingItemView.IsAnimating` for vanishing animation (350ms). Hide-checked via `.content-area.hide-checked`. Tracks history via `IPackingHistoryService`.
- **Page layout**: Root `<div class="page-container with-fixed-navbar">`. Header via `NavigationHeaderService.SetText(...)` in `OnInitialized`/`OnAfterRenderAsync`.
- **Keyboard**: Subscribe to `KeyboardService.KeyboardVisibilityChanged` in `OnInitialized`, initialize in `OnAfterRenderAsync`.
- **Dispose**: `IAsyncDisposable` must unregister JS modules, event handlers, and `DotNetObjectReference`.

### Localization

- Use `IStringLocalizer<AppResources>` (aliased as `Localizer` or `L`) for all user-facing strings
- Resource files: `AppResources.resx` (en), `AppResources.es.resx` (es), `AppResources.ru.resx` (ru)
- When adding new user-facing text, add entries to **all three** `.resx` files
- Subscribe to `LocalizationService.CultureChanged` to refresh UI; unsubscribe on dispose

---

## Anticipack.API (ASP.NET Core Web API)

### Structure
- **Controllers**: `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]`
- **User ID**: `User.FindFirst(ClaimTypes.NameIdentifier)`
- **Auth**: JWT Bearer tokens, Google sign-in (`Google.Apis.Auth`), Telegram bot auth via `X-Bot-Api-Key` header
- **Repositories** (`Repositories/`): `IUserRepository`, `IActivityRepository`, `ISettingsRepository`, `IPackingItemRepository`, `IPackingHistoryRepository` — currently in-memory implementations
- **DTOs** (`DTOs/ApiDtos.cs`): Record types — `ActivityDto`, `PackingItemDto`, `LoginResponse`, `CreateActivityRequest`, etc.
- **Models** (`Models/`): `User`, `PackingActivity`, `PackingHistoryEntry`, `UserSettings`
- **Services**: `IAuthService` / `AuthService` for JWT token generation

### API Response Pattern
All API responses use `ApiResponse<T>` wrapper:
```csharp
record ApiResponse<T>(bool Success, T? Data, string? Message, List<string>? Errors);
```

---

## Anticipack.Workers (.NET Worker Service)

### Structure
- **`TelegramBotService`**: Extends `BackgroundService`, polls for Telegram updates via long-polling
- **`BotUpdateHandler`** (implements `IBotUpdateHandler`): Routes commands (`/start`, `/new`, `/lists`, `/add`, `/pack`, etc.) and callback queries
- **`UserSessionManager`** (implements `IUserSessionManager`): `ConcurrentDictionary`-based session store with JWT tokens (23h TTL)
- **`AnticipackApiClient`** (implements `IAnticipackApiClient`): `HttpClient`-based API client, authenticates via `/api/auth/telegram`
- **Models** (`Models/ApiModels.cs`): Mirrors API DTOs as records — `ActivityDto`, `PackingItemDto`, `TelegramLoginRequest`, etc.
- **Config**: `appsettings.json` — `Telegram:BotToken`, `Api:BaseUrl`, `Api:BotApiKey`

### DI Registration (`Program.cs`)
- `TelegramBotClient` as singleton
- `AnticipackApiClient` via `AddHttpClient<IAnticipackApiClient, AnticipackApiClient>`
- `UserSessionManager` and `BotUpdateHandler` as singletons
- `TelegramBotService` via `AddHostedService`

---

## Coding Conventions

- **Private fields**: `_camelCase` prefix
- **Async methods**: `Async` suffix
- **String IDs**: All entity PKs are `string` (`Guid.NewGuid().ToString()`)
- **XML doc comments**: On interfaces and service methods; implementations may omit them
- **Enum-to-string categories**: `PackingItem.Category` is `string` matching `PackingCategory` enum; use `Enum.TryParse` for conversion
- **Transitions/animations**: CSS `cubic-bezier(0.4, 0, 0.2, 1)`; respect `prefers-reduced-motion`
- **File-scoped namespaces**: Used in non-MAUI projects (API, Workers)
- **Nullable reference types**: Enabled globally — use `is null` / `is not null`

## When Modifying Code

1. **New Blazor components**: Use code-behind pattern. Create `_Imports.razor` for new feature folders. Use CSS variables, not hard-coded values.
2. **New services**: Define interface in separate file. Register in `MauiProgram.cs`. Use `AddSingleton` for stateless, `AddScoped` for per-page.
3. **New API endpoints**: Follow existing controller patterns with `[Authorize]`, `ApiResponse<T>` wrapper, and repository injection.
4. **New localization strings**: Add to all three `.resx` files (`en`, `es`, `ru`).
5. **New Workers commands**: Add to `BotUpdateHandler` command switch and update `SetMyCommands` in `TelegramBotService`.
6. **Prefer focused repository interfaces** (`IPackingActivityRepository`, etc.) over legacy `IPackingRepository`.
