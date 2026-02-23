# Copilot Instructions for Anticipack

## Solution Overview
Anticipack is a **packing checklist app** built as a multi-project .NET 9 solution:

| Project | Type | Purpose |
|---------|------|---------|
| `Anticipack` | .NET MAUI Blazor Hybrid | Main mobile/desktop app (Android, iOS, macOS, Windows) |
| `Anticipack.API` | ASP.NET Core Web API | Backend REST API with JWT auth, in-memory repositories (dev) |
| `Anticipack.Workers` | .NET Worker Service | Telegram bot hosted service using `BackgroundService` |

### Key Technologies & Libraries
- **UI**: Blazor Hybrid (Razor components rendered in MAUI WebView), Font Awesome icons (`fa fa-*`)
- **Data**: SQLite via `sqlite-net-pcl` (async), models in `Anticipack\Storage\`
- **Localization**: `IStringLocalizer<AppResources>` with `.resx` files (`en`, `es`, `ru`) under `Resources/Localization/`
- **AI**: Gemini API via `GeminiSuggestionService` (implements `IAiSuggestionService`)
- **Payments**: In-app billing (`Plugin.InAppBilling`), PayPal integration
- **Telegram**: `Telegram.Bot` library in Workers project
- **API Auth**: JWT Bearer tokens, Google sign-in (`Google.Apis.Auth`)

---

## Architecture & SOLID Principles
The codebase follows SOLID principles (documented in `Anticipack/SOLID-REFACTORING.md`):

### Repository Layer (`Anticipack\Storage\Repositories\`)
- **ISP**: Three focused repository interfaces instead of one monolith:
  - `IPackingActivityRepository` — Activity CRUD
  - `IPackingItemRepository` — Item CRUD
  - `IPackingHistoryRepository` — History CRUD
- **Legacy**: `IPackingRepository` (marked `[Obsolete]`) extends all three for backward compatibility. Prefer the focused interfaces in new code.
- **DIP**: `IDatabaseConnectionFactory` abstracts SQLite connection creation (`SqliteDatabaseConnectionFactory` implementation).

### Service Layer (`Anticipack\Services\`)
- **SRP**: Business logic lives in dedicated services, not in Razor components:
  - `IPackingActivityService` / `PackingActivityService` — activity & item business operations
  - `IPackingHistoryService` / `PackingHistoryService` — history recording & statistics
  - `IPackingStatisticsService` / `PackingStatisticsService` — aggregated stats
  - `ICategoryIconProvider` / `CategoryIconProvider` — category icons (OCP: dictionary-based, extensible)
  - `IAiSuggestionService` / `GeminiSuggestionService` — AI item suggestions
  - `ILocalizationService` / `LocalizationService` — culture management with `Preferences` persistence
  - `IKeyboardService` — platform-specific keyboard visibility tracking (Android/iOS/Windows implementations)
  - `ISyncService`, `IPremiumService` — cloud sync & premium subscription (premium-gated)
  - `IPaymentService`, `IStoreService`, `IPayPalService` — payment processing

### DI Registration
All services are registered in `MauiProgram.cs`. Platform-specific services use `#if ANDROID / IOS / WINDOWS` conditionals. Use `AddSingleton` for repositories/platform services, `AddScoped` for per-page business services and UI services like `IDialogService`, `IToastService`.

---

## Component Organization

### Feature-Based Folder Structure
Components are organized by feature under `Components/Features/`:
```
Components/
├── Features/
│   ├── Packing/         # PackingActivities, EditPacking, PlayPacking, PackingDialog
│   ├── Settings/        # UserSettings
│   ├── Statistics/      # PackingStatistics
│   └── Support/         # Support, Donate
├── Shared/              # Reusable: DialogComponent, ToastComponent, NavigationHeaderComponent, SidebarTextComponent
└── Layout/              # MainLayout, NavMenu
```

### Feature `_Imports.razor` Files
Each feature folder has its own `_Imports.razor` with feature-specific `@using` directives. When adding a new feature folder, create a `_Imports.razor` with the required namespaces. The root `Components/_Imports.razor` has only base framework usings.

### Code-Behind Pattern
All Razor pages use the **partial class code-behind** pattern:
- Markup: `Component.razor` (no `@code` block — only `@page` directive and HTML/Razor)
- Logic: `Component.razor.cs` (partial class with `[Inject]`, `[Parameter]`, fields, methods)
- Styles: `Component.razor.css` (scoped CSS isolation)
- JS: `Component.razor.js` (co-located JS module, loaded via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Features/…/Component.razor.js")`)

### Dependency Injection in Components
Use `[Inject] private IServiceName ServiceName { get; set; } = default!;` pattern for DI in code-behind files. Common injected services:
- `NavigationManager`, `IJSRuntime`
- `IDialogService`, `IToastService`, `INavigationHeaderService`
- `IStringLocalizer<AppResources>` (aliased as `Localizer` or `L`)
- `ILocalizationService`, `IKeyboardService`, `ICategoryIconProvider`
- `IPackingRepository` (legacy) or focused repository interfaces

### View Models
`PackingItemView` wraps `PackingItem` for UI display (shared by `EditPacking` and `PlayPacking`). It adds `IsChecked` and `IsAnimating` properties. Always use this view model in packing components, not raw `PackingItem`.

---

## Domain Model

### Core Entities (SQLite, in `Anticipack\Storage\`)
- `PackingActivity` — Id (GUID string PK), Name, LastPacked, RunCount, IsShared, IsArchived, IsFinished, IsRecurring
- `PackingItem` — Id (GUID string PK), ActivityId (FK), Name, IsPacked, Category (string matching `PackingCategory` enum), Notes, SortOrder
- `PackingHistoryEntry` — Id (GUID string PK), ActivityId (FK), CompletedDate, TotalItems, PackedItems, DurationSeconds, StartTime, EndTime

### Category Enum (`Anticipack\Packing\PackingCategory.cs`)
Valid categories: `Clothing`, `Shoes`, `Toiletries`, `Electronics`, `Documents`, `Health`, `Accessories`, `Outdoor`, `Food`, `Entertainment`, `Miscellaneous`

---

## UI & Styling Rules

### CSS Custom Properties (REQUIRED)
All new components and refactoring **must** use CSS custom properties defined in `wwwroot/css/app.css` instead of hard-coded values. Key variable groups:
- **Colors**: `--primary`, `--primary-light`, `--primary-dark`, `--primary-rgb`, `--accent`, `--danger`, `--warning`, `--success`, `--background`, `--surface`, `--text-primary`, `--text-secondary`, `--border-color`, `--hover-bg`
- **Typography**: `--font-family`, `--font-size-xs` through `--font-size-xl`
- **Spacing**: `--space-xs` through `--space-xxxl`
- **Borders**: `--border-radius-sm`, `--border-radius`, `--border-radius-lg`, `--border-radius-xl`
- **Shadows**: `--shadow-sm`, `--shadow`, `--shadow-md`, `--shadow-lg`
- **Transitions**: `--transition-fast`, `--transition`
- **Component-specific**: `--input-bg`, `--input-border`, `--input-focus-border`, `--overlay-bg`, `--modal-bg`, `--disabled-bg`, `--disabled-text`

If no existing variable covers a needed value, introduce a new CSS custom property in `app.css` under the appropriate section with **both light and dark theme variants**.

### Dark Theme Support
The app supports light (`[data-theme="light"]`) and dark (`[data-theme="dark"]`) themes. All CSS custom properties have dark-mode overrides in `app.css`. Always verify new styles look correct in both themes.

### Responsive & Accessibility
- Use `@media (max-width: 480px)` and `@media (max-width: 360px)` breakpoints for mobile adjustments.
- Include `@media (prefers-reduced-motion: reduce)` to disable animations.
- Include `@media (prefers-contrast: high)` for high-contrast borders when relevant.
- Use `role`, `aria-label`, `aria-hidden="true"` on icons, and `title` attributes on interactive elements.

---

## JavaScript Rules

### Co-located JS Modules (Blazor JS Isolation)
- JS code related to a specific component **must** go in a co-located `.razor.js` file next to the Razor page (e.g., `EditPacking.razor.js`). Only shared/global helpers go in `wwwroot/js/site.js`.
- Load modules via: `_jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./Components/Features/…/Component.razor.js");`
- Use `export function` for all public functions in `.razor.js` files.
- Create `DotNetObjectReference<T>` for JS-to-Blazor callbacks and dispose it in `IAsyncDisposable`.

### Click-Outside / Context Menu Pattern
Three-dot / context menus (dropdowns) must use a **document-level JS click handler** (capture phase via `addEventListener('click', handler, true)`) registered in the co-located `.razor.js` to close the menu when clicking outside. Do **not** use a CSS overlay div for click-outside detection — it fails due to CSS stacking contexts. Wrap registration in `requestAnimationFrame` so the opening click is not intercepted. When the menu has no room below the viewport, add an `open-up` CSS class to flip it upward.

### Chat-Style / Full-Screen Pages
Chat-style pages (where an input must always stay visible without page-level scrolling) should use JS in the co-located `.razor.js` to measure the container's top offset and set its height to `window.innerHeight - top`, with a `resize` listener for orientation changes. The container CSS should use `overflow: hidden` (no `min-height: 100vh`) so only the inner content area scrolls.

### Keyboard Handling
When calling global JS helpers that accept a keyboard height parameter (e.g., `scrollActiveElementIntoView`), **always** pass the actual keyboard height from `KeyboardService` — omitting it causes the function to assume the full viewport is visible.

---

## Blazor Component Behavior Rules

### EditPacking Page
- Clicking outside `add-item-form` should **cancel** (not auto-add).
- Clicking outside `item-edit-container` should **auto-save** changes including notes.

### PlayPacking Page
- Uses `PackingItemView.IsAnimating` for vanishing animation on checked items (350ms delay).
- Supports hide-checked-items filter via `.content-area.hide-checked` CSS class.
- Tracks packing history via `IPackingHistoryService`.

### Common Page Layout
- Pages use `<div class="page-container with-fixed-navbar">` as the root container.
- Navigation header text is set via `NavigationHeaderService.SetText(...)` in `OnInitialized` or `OnAfterRenderAsync`.
- Keyboard visibility is tracked via `KeyboardService.KeyboardVisibilityChanged` event subscription (subscribe in `OnInitialized`, initialize in `OnAfterRenderAsync`).
- Components implementing `IAsyncDisposable` must unregister JS modules, event handlers, and `DotNetObjectReference` in `DisposeAsync`.

---

## Localization Rules
- Use `IStringLocalizer<AppResources>` (injected as `Localizer` or `L`) for all user-facing strings.
- Resource files: `AppResources.resx` (English, default), `AppResources.es.resx` (Spanish), `AppResources.ru.resx` (Russian).
- Static access via `AppResources.PropertyName` is also used in code-behind (e.g., `AppResources.PackingActivity`).
- When adding new user-facing text, add entries to **all three** `.resx` files.
- Subscribe to `LocalizationService.CultureChanged` to refresh the UI on language change and unsubscribe on dispose.

---

## API Project (`Anticipack.API`)
- Controllers use `[ApiController]`, `[Route("api/[controller]")]`, `[Authorize]` attributes.
- User identification via `User.FindFirst(ClaimTypes.NameIdentifier)`.
- Repository interfaces are in `Anticipack.API.Repositories.IRepositories.cs` (separate from MAUI app repositories).
- DTOs are records in `Anticipack.API.DTOs.ApiDtos.cs`.
- Currently uses in-memory repositories — production will use a real database.

## Workers Project (`Anticipack.Workers`)
- `TelegramBotService` extends `BackgroundService` for the Telegram bot hosted service.
- `BotUpdateHandler` processes Telegram updates; `UserSessionManager` tracks user state.
- `AnticipackApiClient` communicates with the API via `HttpClient`.
- Configuration via `appsettings.json` (`Telegram:BotToken`, `Api:BaseUrl`).

---

## Coding Conventions
- **Nullable**: Enabled globally (`<Nullable>enable</Nullable>`).
- **Implicit usings**: Enabled.
- **Private fields**: Prefix with `_` (e.g., `_isLoading`, `_currentActivity`).
- **Async methods**: Suffix with `Async`.
- **XML doc comments**: Used on interfaces and service methods; implementations may omit them.
- **String IDs**: All entity primary keys are `string` (GUID via `Guid.NewGuid().ToString()`).
- **Enum-to-string categories**: `PackingItem.Category` is a `string` matching `PackingCategory` enum values; use `Enum.TryParse` for conversion.
- **Font Awesome**: Icons use `<i class="fa fa-icon-name" aria-hidden="true"></i>` pattern.
- **Transitions/animations**: Use CSS `cubic-bezier(0.4, 0, 0.2, 1)` for standard easing; respect `prefers-reduced-motion`.

## Project Guidelines
- In `EditPacking.razor`, clicking outside `add-item-form` should cancel (not auto-add). Clicking outside `item-edit-container` should auto-save changes including notes.
- JS code related to a specific component or form should be placed in a co-located `.razor.js` file next to the Razor page it belongs to (Blazor JS isolation pattern). Only shared/global JS functionality should go in `site.js`.
- All new components and refactoring must use CSS custom properties (variables) defined in `wwwroot/css/app.css` (e.g., `--primary`, `--border-radius`, `--space-md`, `--shadow`, `--font-size-sm`, etc.) instead of hard-coded values. If no existing variable covers the needed value, introduce a new CSS custom property in `app.css` under the appropriate section (with both light and dark theme variants when applicable) and reference it in the component styles.
- Three-dot / context menus (dropdowns) must use a **document-level JS click handler** (capture phase via `addEventListener('click', handler, true)`) registered in the co-located `.razor.js` to close the menu when clicking outside. Do **not** use a CSS overlay div for click-outside detection — it fails due to CSS stacking contexts. Wrap registration in `requestAnimationFrame` so the opening click is not intercepted. When the menu has no room below the viewport, add an `open-up` CSS class to flip it upward.
- Chat-style / full-screen pages (where an input must always stay visible without page-level scrolling) should use JS in the co-located `.razor.js` to measure the container's top offset and set its height to `window.innerHeight - top`, with a `resize` listener for orientation changes. The container CSS should use `overflow: hidden` (no `min-height: 100vh`) so only the inner content area scrolls. When calling global JS helpers that accept a keyboard height parameter (e.g., `scrollActiveElementIntoView`), always pass the actual keyboard height from the `KeyboardService` — omitting it causes the function to assume the full viewport is visible.- When calling global JS helpers that accept a keyboard height parameter (e.g., `scrollActiveElementIntoView`), always pass the actual keyboard height from the `KeyboardService` — omitting it causes the function to assume the full viewport is visible.