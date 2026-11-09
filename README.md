**Author**: Prudence Dithole
**Module**: Programming 2B (PROG6212)  
**Year**: 2026  
**Institution**: ROSEBANK INTERNATIONAL


# RaceDay - Event Management System

### part 3 CI/CD
- GitHub Actions workflow validates repository structure
- ✅ Green build:


## Overview
RaceDay is a web-based event management system designed specifically for the South African road running, walking, and cycling community. The platform allows Event Organisers to create and manage events, categories, and participant results, while Participants can browse upcoming events, enter events, and track their personal performance history. The system supports role-based access with distinct interfaces for Organisers and Participants.

The project was built in three parts:

| Part | Content | Evidence |
| 1 | ERD, API endpoint plan, SQL script | `docs/` (see `docs/READMEpart1.md`) |
| 2 | ASP.NET Core Web API, EF Core, session auth, Swagger, unit tests, CI | `RaceDay.API/`, `RaceDay.API.Tests/`, `docs/Part2-implemented-endpoints.md` |
| 3 | ASP.NET Core MVC front end, Azure Blob Storage, Docker | `RaceDay.MVC/`, `Dockerfile`, `docker-compose.yml` |

## System roles

**Organiser**
- Registers with an organiser invite code plus organisation name and phone number.
- Creates, edits and deletes their own events (name, description, date, location, distance, type) and uploads an event banner image.
- Adds age/distance categories to each event.
- Views all enrolments for their events and updates the status (Pending, Confirmed, Cancelled).
- Captures finish times and finishing positions for enrolled participants.

**Participant**
- Registers with personal details and date of birth.
- Browses and filters upcoming events and views event details and categories.
- Enters an event by selecting a category.
- Views their own enrolments with status, and their own results (finish time and position).
- Views and updates their profile and uploads a profile picture.

Role-based access is enforced by the API (Part 2). The MVC application reads the role from the session to show a different navigation menu and pages to each role, and redirects unauthenticated or wrong-role users to the login page.

## Solution structure

```text
RaceDay.API.sln
RaceDay.API/            Part 2 REST API (+ Azure Blob upload endpoints)
RaceDay.API.Tests/      API unit tests
RaceDay.MVC/            Part 3 ASP.NET Core MVC application
  Controllers/          Page and form actions
  Models/               View models and API response models
  Services/             RaceDayApiClient (server-side API client)
  Views/                Razor views and shared layout
  wwwroot/css/          RaceDay theme
Dockerfile              Builds the MVC application image
RaceDay.API/Dockerfile  Builds the API image
docker-compose.yml      SQL Server + API + MVC
.env.example            Template for Docker environment variables
.github/workflows/      GitHub Actions workflow
docs/                   Part 1 and Part 2 planning and evidence
```

## Architecture

The MVC project contains no database access and no business logic. Every data operation is an HTTP call to the API through `RaceDayApiClient`.

- **Authentication:** registration and login are posted to the API. The API's authentication cookie is stored server-side in the MVC session and forwarded on later calls. The MVC session also stores the user's ID, role and display name.
- **Forgot password:** the login page links to `/Account/ForgotPassword`. The MVC app calls `POST /api/auth/forgot-password`; the API creates a signed token (ASP.NET Data Protection) that expires after 1 hour and stops working once the password changes, so nothing extra is stored in the database. The link is emailed when SMTP is configured (`Smtp:*` settings / `SMTP_*` in `.env`). With no SMTP server the link is shown on the page (demo mode, `PasswordReset:ShowLinkInResponse`); set that to `false` in production. The user then sets a new password at `/Account/ResetPassword`, which calls `POST /api/auth/reset-password`. The response is the same whether or not the email exists.
- **Default event banners:** events without an uploaded banner show a photo for their type (`wwwroot/img/banner-run.jpg`, `banner-walk.jpg`, `banner-cycle.jpg`).
- **Azure Blob Storage:** the MVC app sends images to the API (`/api/storage/...`). The API validates them (JPG, PNG or WEBP, max 5 MB), uploads them to Azure Blob Storage and stores the URL in the database (`EventImages` for banners, `Participants.ProfilePictureURL` for profile pictures). The MVC app never contacts Azure directly.

## Run locally in Visual Studio

Requirements: Visual Studio 2022 (ASP.NET workload), .NET 8 SDK, SQL Server Express (or another SQL Server), and an Azure Storage account for image uploads.

1. Open `RaceDay.API.sln`.
2. In `RaceDay.API/appsettings.json` set `ConnectionStrings:DefaultConnection` for your SQL Server instance.
3. Set `OrganiserInviteCode` to a value of your choice.
4. Set `AzureBlobStorage:ConnectionString` using user secrets or the environment variable `AzureBlobStorage__ConnectionString` (never commit it).
5. `RaceDay.MVC/appsettings.json` expects the API at `http://localhost:5285/`. Change `ApiSettings:BaseUrl` if yours differs.
6. Right-click the solution → **Configure Startup Projects** → **Multiple startup projects** → set `RaceDay.API` and `RaceDay.MVC` to **Start**, then run.
7. Open the MVC site, register a Participant and log in. To register an Organiser, enter the invite code and organisation details.

The API creates the database on first run (`EnsureCreated()`). Swagger is available at `/swagger` when the API runs in Development.

## Run with Docker

Docker Desktop (or Docker Engine with the Compose plugin) is required.

1. Copy `.env.example` to `.env`.
2. Set `SA_PASSWORD` to a strong SQL Server password.
3. Optionally set `ORGANISER_INVITE_CODE` and `AZURE_BLOB_CONNECTION_STRING` (image uploads need the Azure connection string; the rest of the site works without it).
4. From the repository root run the single command:

## Troubleshooting

- **API returns 500 on start:** check `SA_PASSWORD` in `.env` meets SQL Server's complexity rules.
- **Reset link not emailed:** leave `SMTP_HOST` empty and set `PASSWORD_RESET_SHOW_LINK=true` to see the link on the page.
- **Port already in use:** stop other apps on 5200/5285 or change the ports in `docker-compose.yml`.
- **Health check:** open `/health` on the API to confirm it is running.

```bash
docker compose up --build
```

5. Open the MVC site at http://localhost:8080 (the API is exposed at http://localhost:5285).
6. Stop with `docker compose down`. Add `-v` to also delete the SQL Server data volume.

## Build and test

```bash
dotnet restore RaceDay.API.sln
dotnet build RaceDay.API.sln --configuration Release
dotnet test RaceDay.API.sln --configuration Release
```

## Video Presentation
[youtube link](https://youtu.be/nC7OiqRuY3E?si=dczsjYThvwXNlgdM)

## AI assistance disclosure

AI tools (Claude) were used during Part 3 to help plan the MVC structure and to draft parts of the API client, the Azure Blob upload endpoints, the Docker configuration and this README. I reviewed, tested and adapted the generated work and can explain it in my video presentation.

## References
claude (AI tool)
