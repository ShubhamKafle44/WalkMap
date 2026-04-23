# WalkMap Backend — Setup & API Reference

**Live Project Link:** https://walkmap-front.azurewebsites.net/

## Stack
- **ASP.NET Core 8** (C#)
- **Entity Framework Core 8** with SQL Server / LocalDB
- **JWT Bearer Authentication**
- **BCrypt** password hashing
- **Swagger UI** at `/swagger`

---

## Quick Start

```bash
# 1. Restore packages
dotnet restore

# 2. Apply EF Core migrations (creates DB automatically on first run)
dotnet ef migrations add InitialCreate
dotnet ef database update

# 3. Run the API
dotnet run
```

---

## Project Structure

```
WalkMap.Api/
├── Controllers/
│   ├── AuthController.cs       # POST /api/auth/register, /login
│   └── WalksController.cs      # Full walks CRUD + route generation
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext
├── DTOs/
│   ├── AuthDTOs.cs             # Request/response records for auth
│   └── WalkDTOs.cs             # Request/response records for walks
├── Middleware/
│   └── GlobalExceptionMiddleware.cs
├── Models/
│   ├── User.cs
│   ├── Walk.cs
│   └── WalkPoint.cs            # Individual GPS coordinate
├── Services/
│   ├── IAuthService.cs / AuthService.cs
│   └── IWalkService.cs / WalkService.cs
├── appsettings.json
└── Program.cs                  # App startup & DI config

## API Endpoints

### Auth
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | Create account |
| POST | `/api/auth/login` | Get JWT token |

### Walks (all require `Authorization: Bearer <token>`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/walks` | Get walk history (summaries) |
| GET | `/api/walks/{id}` | Get single walk with GPS route |
| POST | `/api/walks/start` | Start a new walk session |
| PUT | `/api/walks/{id}/end` | End walk, save GPS + step data |
| DELETE | `/api/walks/{id}` | Delete a walk |
| POST | `/api/walks/generate-route` | Generate circular route suggestion |

---

## Example Requests

### Register
```json
POST /api/auth/register
{
  "username": "shubham",
  "email": "shubham@example.com",
  "password": "securePass123"
}
```

### Start Walk
```json
POST /api/walks/start
Authorization: Bearer <token>
{
  "title": "Morning Walk"
}
```

### End Walk (with GPS data)
```json
PUT /api/walks/1/end
Authorization: Bearer <token>
{
  "stepCount": 3500,
  "routePoints": [
    { "latitude": 35.8456, "longitude": -86.3902, "timestamp": "2026-02-23T08:00:00Z", "sequenceOrder": 0 },
    { "latitude": 35.8460, "longitude": -86.3910, "timestamp": "2026-02-23T08:01:00Z", "sequenceOrder": 1 }
  ]
}
```

### Generate a 2km Route
```json
POST /api/walks/generate-route
Authorization: Bearer <token>
{
  "startLat": 35.8456,
  "startLng": -86.3902,
  "targetDistanceKm": 2.0
}
```

---

## Distance Calculation
Distance is calculated server-side using the **Haversine formula** (`WalkService.HaversineMeters`), which gives the great-circle distance between two GPS coordinates. This is applied cumulatively across all route points when a walk ends.

---

## Database Schema

```
Users          Walks               WalkPoints
──────         ──────────────      ────────────────
Id             Id                  Id
Username       UserId (FK)         WalkId (FK)
Email          Title               Latitude
PasswordHash   TotalDistanceMeters Longitude
CreatedAt      StepCount           Timestamp
               StartedAt           SequenceOrder
               EndedAt
```
