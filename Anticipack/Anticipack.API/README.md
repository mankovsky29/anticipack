# Anticipack API

A RESTful API for the Anticipack packing list application built with ASP.NET Core 9.0.

## Features

- ? **JWT Authentication** with Google Sign-In and Apple Sign-In
- ? **User Management** (profile, settings)
- ? **Packing Activities** (create, update, delete, copy)
- ? **Packing Items** (CRUD operations)
- ? **User Settings** (notifications, themes, preferences)
- ? **CORS** enabled for mobile apps
- ? **OpenAPI/Swagger** documentation

## Architecture

### Current Implementation
- **In-Memory Repositories** - Development only, data is not persisted
- **JWT Token Authentication**
- **Google & Apple OAuth Integration**

### TODO for Production
- [ ] Replace in-memory repositories with actual database (Cosmos DB, SQL Server, or PostgreSQL)
- [ ] Implement proper Apple Sign-In token validation with Apple public keys
- [ ] Use Redis or database for refresh token storage
- [ ] Add rate limiting and API throttling
- [ ] Implement proper logging and monitoring
- [ ] Add API versioning
- [ ] Implement comprehensive error handling and validation
- [ ] Add unit and integration tests
- [ ] Configure secure secrets management (Azure Key Vault, AWS Secrets Manager)

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Google OAuth 2.0 Client ID
- Apple Sign In Service ID (optional)

### Configuration

1. Update `appsettings.json`:

```json
{
  "Jwt": {
    "Key": "your-secret-key-must-be-at-least-32-characters-long",
    "Issuer": "anticipack-api",
    "Audience": "anticipack-app"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com"
    },
    "Apple": {
      "ClientId": "your.apple.service.id",
      "TeamId": "your-apple-team-id"
    }
  }
}
```

2. For production, use **User Secrets** or environment variables:

```bash
dotnet user-secrets set "Jwt:Key" "your-production-secret-key"
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
```

### Run the API

```bash
cd Anticipack.API
dotnet restore
dotnet run
```

The API will be available at `http://localhost:5275`

### Test Endpoints

Use the included `Anticipack.API.http` file with Visual Studio's HTTP client or use tools like Postman.

## API Endpoints

### Authentication
- `POST /api/auth/login` - Login with Google/Apple
- `POST /api/auth/refresh` - Refresh access token

### Users
- `GET /api/users/me` - Get current user
- `PUT /api/users/me` - Update current user
- `DELETE /api/users/me` - Delete current user

### Activities
- `GET /api/activities` - Get all activities
- `GET /api/activities/{id}` - Get activity by ID
- `POST /api/activities` - Create new activity
- `PUT /api/activities/{id}` - Update activity
- `DELETE /api/activities/{id}` - Delete activity
- `POST /api/activities/{id}/copy` - Copy activity
- `POST /api/activities/{id}/start` - Start packing session

### Packing Items
- `GET /api/activities/{activityId}/items` - Get all items
- `POST /api/activities/{activityId}/items` - Create item
- `PUT /api/activities/{activityId}/items/{itemId}` - Update item
- `DELETE /api/activities/{activityId}/items/{itemId}` - Delete item

### Settings
- `GET /api/settings` - Get user settings
- `PUT /api/settings` - Update settings
- `POST /api/settings/reset` - Reset to defaults

## Authentication Flow

### Google Sign-In Flow
1. Mobile app obtains Google ID token using Google Sign-In SDK
2. Mobile app sends ID token to `/api/auth/login` with provider "Google"
3. API validates token with Google
4. API returns JWT access token and refresh token
5. Mobile app uses JWT token for subsequent API calls

### Apple Sign-In Flow
1. Mobile app obtains Apple ID token using Apple Sign-In
2. Mobile app sends ID token to `/api/auth/login` with provider "Apple"
3. API validates token (?? simplified validation - needs full implementation)
4. API returns JWT access token and refresh token
5. Mobile app uses JWT token for subsequent API calls

## Security Notes

?? **Important for Production:**

1. **JWT Secret Key**: Use a strong, randomly generated key (min 32 characters)
2. **HTTPS Only**: Always use HTTPS in production
3. **Refresh Token Storage**: Move from in-memory to Redis or database with encryption
4. **Apple Token Validation**: Implement full validation using Apple's public keys
5. **Rate Limiting**: Implement to prevent abuse
6. **CORS**: Restrict to specific origins in production
7. **Input Validation**: Add comprehensive validation using FluentValidation or Data Annotations

## Database Migration Guide

To replace in-memory repositories with a real database:

1. Choose a database (Cosmos DB, SQL Server, PostgreSQL)
2. Create DbContext and configure Entity Framework Core
3. Implement repository classes using EF Core
4. Update DI registration in `Program.cs`
5. Run migrations and seed data

Example for Entity Framework Core:
```csharp
builder.Services.AddDbContext<AnticipaqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<IActivityRepository, EfActivityRepository>();
// ... other repositories
```

## License

[Your License Here]


### How to get sha 1 fingerprint visual studio

To get the SHA-1 fingerprint in Visual Studio for Android projects (Xamarin, .NET MAUI, etc.), you typically use the Java Development Kit's (JDK) keytool utility via the command line or the Visual Studio terminal. 
Method 1: Using the Command Line (keytool utility)
This method involves running a command to list the fingerprints of your keystore file. 
Locate keytool.exe: The keytool.exe file is usually located in the bin directory of your installed JDK. A typical path might be C:\Program Files\Java\jdk-xx.x.x\bin or within the Xamarin-specific JDK path: C:\Program Files\Android\Android Studio\jre\bin.
Tip for Visual Studio/Xamarin users: You can find the exact JDK path used by Visual Studio in Tools > Options > Xamarin > Android Settings.
Open Command Prompt/Terminal:
Navigate to the directory where keytool.exe is located using the cd command.
Alternatively, open the folder in Windows Explorer, hold Shift, right-click in an empty space, and select Open command window here or Open PowerShell window here.
Run the keytool command: Use the following command to get the debug certificate fingerprint (the default password is android):
bash
keytool -list -v -keystore "%USERPROFILE%\.android\debug.keystore" -alias androiddebugkey -storepass android -keypass android
Use code with caution.

For a release keystore, replace "%USERPROFILE%\.android\debug.keystore" with the path to your production keystore file and androiddebugkey with your key's alias.