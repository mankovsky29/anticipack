# Anticipack API - Quick Reference

## ?? Project Structure

```
Anticipack.API/
??? Controllers/
?   ??? AuthController.cs          # Google/Apple login, token refresh
?   ??? UsersController.cs         # User profile management
?   ??? ActivitiesController.cs    # Packing activities & items CRUD
?   ??? SettingsController.cs      # User preferences & settings
??? Models/
?   ??? User.cs                    # User entity with auth provider
?   ??? PackingActivity.cs         # Activity & Item entities
?   ??? UserSettings.cs            # User preferences model
??? DTOs/
?   ??? ApiDtos.cs                 # Request/Response DTOs for all endpoints
??? Services/
?   ??? IAuthService.cs            # Auth service interface
?   ??? AuthService.cs             # JWT & OAuth token validation
??? Repositories/
?   ??? IRepositories.cs           # Repository interfaces
?   ??? InMemoryRepositories.cs    # In-memory implementations (dev only)
??? Program.cs                      # App configuration & DI setup
??? appsettings.json               # Configuration (JWT, OAuth)
??? Anticipack.API.http            # HTTP endpoint examples
??? README.md                       # Full documentation
??? MOBILE_INTEGRATION.md          # MAUI integration guide
```

## ?? Quick Start

1. **Install packages:**
   ```bash
   dotnet restore
   ```

2. **Configure authentication** in `appsettings.json`:
   ```json
   {
     "Jwt": {
       "Key": "your-32-char-secret-key"
     },
     "Authentication": {
       "Google": {
         "ClientId": "your-google-client-id"
       }
     }
   }
   ```

3. **Run the API:**
   ```bash
   dotnet run
   ```

4. **Test endpoints** using `Anticipack.API.http`

## ?? Authentication Flow

```
Mobile App
    ? (1) Google/Apple Sign-In
[Google/Apple]
    ? (2) ID Token
Mobile App
    ? (3) POST /api/auth/login {idToken, provider}
API Server
    ? (4) Validate with Google/Apple
    ? (5) Create/Find User
    ? (6) Generate JWT + Refresh Token
Mobile App
    ? (7) Store tokens securely
    ? (8) Use JWT for all API calls
API Server (Protected Endpoints)
```

## ?? API Endpoints Summary

### Authentication (No auth required)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login with Google/Apple ID token |
| POST | `/api/auth/refresh` | Refresh access token |

### Users (Auth required)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users/me` | Get current user profile |
| PUT | `/api/users/me` | Update user profile |
| DELETE | `/api/users/me` | Delete user account |

### Activities (Auth required)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/activities` | Get all user activities |
| GET | `/api/activities/{id}` | Get activity by ID |
| POST | `/api/activities` | Create new activity |
| PUT | `/api/activities/{id}` | Update activity |
| DELETE | `/api/activities/{id}` | Delete activity |
| POST | `/api/activities/{id}/copy` | Duplicate activity |
| POST | `/api/activities/{id}/start` | Start packing (reset items) |

### Items (Auth required)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/activities/{id}/items` | Get all items for activity |
| POST | `/api/activities/{id}/items` | Add item to activity |
| PUT | `/api/activities/{id}/items/{itemId}` | Update item |
| DELETE | `/api/activities/{id}/items/{itemId}` | Delete item |

### Settings (Auth required)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/settings` | Get user settings |
| PUT | `/api/settings` | Update settings (partial) |
| POST | `/api/settings/reset` | Reset to defaults |

## ?? Configuration Keys

### Required for Production
- `Jwt:Key` - Secret key for JWT signing (min 32 chars)
- `Jwt:Issuer` - Token issuer identifier
- `Jwt:Audience` - Token audience identifier
- `Authentication:Google:ClientId` - Google OAuth client ID
- `Authentication:Apple:ClientId` - Apple service ID
- `Authentication:Apple:TeamId` - Apple developer team ID

### Store Securely
Use **User Secrets** (dev) or **Azure Key Vault** (prod):
```bash
dotnet user-secrets set "Jwt:Key" "your-secret-key"
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
```

## ?? Technology Stack

- **Framework:** ASP.NET Core 9.0
- **Authentication:** JWT Bearer + OAuth 2.0 (Google, Apple)
- **Serialization:** System.Text.Json
- **Data Storage:** In-memory (dev) ? Replace with Cosmos DB/SQL
- **Documentation:** OpenAPI/Swagger

## ?? NuGet Packages

```xml
<PackageReference Include="Google.Apis.Auth" Version="1.69.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.3.1" />
```

## ?? Production Checklist

Before deploying to production:

- [ ] Replace in-memory repositories with actual database
- [ ] Configure proper CORS policy (restrict origins)
- [ ] Implement proper Apple token validation
- [ ] Move refresh tokens to Redis or database
- [ ] Enable HTTPS only
- [ ] Add rate limiting
- [ ] Implement comprehensive logging
- [ ] Add health check endpoints
- [ ] Configure proper error handling
- [ ] Set up CI/CD pipeline
- [ ] Add API versioning
- [ ] Implement caching strategy
- [ ] Add monitoring and alerting
- [ ] Write unit and integration tests

## ?? Next Steps

1. **Choose a database:**
   - Cosmos DB (for global distribution)
   - SQL Server/PostgreSQL (for relational needs)
   - Combine both (SQLite offline + API for sync)

2. **Implement sync logic in MAUI app:**
   - See `MOBILE_INTEGRATION.md` for complete guide
   - Keep SQLite for offline-first approach
   - Sync with API when online

3. **Deploy API:**
   - Azure App Service
   - AWS Elastic Beanstalk
   - Docker container (Kubernetes, ECS)

## ?? Documentation

- **Full API Docs:** `README.md`
- **Mobile Integration:** `MOBILE_INTEGRATION.md`
- **Test Endpoints:** `Anticipack.API.http`

## ?? Support

For issues or questions:
1. Check the README.md
2. Review endpoint examples in .http file
3. Test locally using provided examples
4. Review mobile integration guide

---

**Version:** 1.0.0  
**Last Updated:** 2024  
**Target Framework:** .NET 9.0
