# NEXORA — Your next idea starts here

A modern, production-grade full-stack general-purpose AI assistant platform built with **React (Vite)**, **ASP.NET Core .NET 8 Web API**, **Entity Framework Core (MySQL)**, and **Google Gemini API**.

NEXORA provides next-generation intelligence to help users explore ideas, solve problems, write content, learn concepts, and build software. The application features user authentication, PBKDF2 password hashing, secure JWT with HttpOnly cookies, persistent multi-conversation management, strict per-user data isolation, light/dark themes, responsive mobile drawer navigation, Markdown rendering with copy-code blocks, and graceful AI service fallback.

---

## Features

- **User Authentication**:
  - Registration with client and server input validation and unique email enforcement.
  - Login with cryptographic password hashing (PBKDF2 via ASP.NET Core Identity `PasswordHasher<T>`).
  - Secure session handling using JWT Bearer tokens and HttpOnly cookies (`SameSite=None/Lax`, `Secure`).
  - Persistent login via `/api/auth/me` on application boot.
  - User profile display and logout functionality.
- **Persistent Conversations & Data Segregation**:
  - Users have isolated study conversations saved in a relational database.
  - Full CRUD: Create, read, rename, and delete conversations with confirmation modals.
  - Strict server-side authorization: User A cannot read, add messages to, rename, or delete User B's conversations.
  - Auto-titling: New sessions automatically adopt meaningful titles from the initial student question.
- **AI-Powered Study Guidance**:
  - Integrated with Google Gemini API (`gemini-3.5-flash` with automatic fallback to `gemini-3.5-flash-lite`).
  - Full Markdown rendering with syntax-highlighted code blocks, tables, bold, lists, and links.
  - Typing indicator during AI generation.
  - Graceful handling of Gemini 503 high-demand, 429 quota exhaustion, and safety filters without saving fabricated responses.
- **Responsive & Modern UI**:
  - Clean, accessible desktop and mobile drawer interface.
  - Actionable suggested prompts on empty session screens.
  - Built-in rate limiting to protect API endpoints.

---

## Technology Stack

### Frontend (`AIStudyAssistant.Client`)
- **React 19** with **Vite 8**
- **Vanilla CSS** with design tokens and modular stylesheets
- **Axios** with automatic token injection and 401 response interceptors
- **React Markdown** (`react-markdown` + `remark-gfm`)
- **React Icons** (`react-icons/fa`)

### Backend (`AIStudyAssistant.API`)
- **ASP.NET Core Web API** running on **.NET 8**
- **Entity Framework Core 8** with **Pomelo.EntityFrameworkCore.MySql**
- **JWT Bearer Authentication** (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **ASP.NET Core RateLimiter** (`System.Threading.RateLimiting`)
- **Swagger / OpenAPI** with Bearer authorization support

### Database
- **MySQL 8.0+** (Local MySQL Server 8.0 or Cloud-Hosted MySQL such as Aiven, Clever Cloud, Railway, PlanetScale, or AWS RDS)

### Testing (`AIStudyAssistant.Tests`)
- **xUnit** + **Microsoft.AspNetCore.Mvc.Testing**
- **Moq** for isolated mock AI services

---

## Architecture Overview

```
AIStudyAssistant/
├── AIStudyAssistant.sln
├── Dockerfile                      # Production Docker container definition for .NET 8 API
├── sample.env                      # Sample environment variables template
│
├── AIStudyAssistant.API/           # ASP.NET Core 8 Web API
│   ├── Controllers/
│   │   ├── AuthController.cs          # POST /api/auth/register, login, me, logout
│   │   ├── ConversationsController.cs # CRUD /api/conversations & /messages
│   │   └── ChatController.cs          # Backwards-compatible /api/chat
│   ├── Data/
│   │   ├── AppDbContext.cs            # EF Core DbContext with User, Conversation, Message
│   │   ├── Entities/                  # User, Conversation, Message entities
│   │   └── Migrations/                # EF Core migrations (InitialCreate)
│   ├── DTOs/                          # Request/response data transfer objects
│   ├── Services/
│   │   ├── IAuthService.cs / AuthService.cs
│   │   ├── IConversationService.cs / ConversationService.cs
│   │   ├── IChatService.cs / GeminiChatService.cs
│   └── Program.cs
│
├── AIStudyAssistant.Client/        # React + Vite Frontend
│   ├── src/
│   │   ├── components/
│   │   │   ├── auth/AuthModal.jsx     # Registration & Login modal
│   │   │   ├── Sidebar.jsx            # User profile, history, rename, delete
│   │   │   ├── ChatMessage.jsx        # Markdown rendering + typing indicator
│   │   │   ├── ChatInput.jsx          # Input textarea with shortcuts
│   │   │   ├── WelcomeScreen.jsx      # Suggested study prompts
│   │   │   └── ConfirmModal.jsx       # Delete confirmation dialog
│   │   ├── context/
│   │   │   ├── AuthContext.jsx        # Auth state provider
│   │   │   └── useAuth.js             # useAuth hook
│   │   ├── services/
│   │   │   ├── apiClient.js           # Axios instance with auth interceptor
│   │   │   ├── authService.js         # Register, login, me, logout
│   │   │   └── conversationService.js # Conversation & message APIs
│   │   └── App.jsx
│
└── AIStudyAssistant.Tests/         # xUnit Integration & Unit Tests
    ├── AuthTests.cs                   # Registration, login, duplicate email, me
    ├── ConversationIsolationTests.cs  # Strict User A vs User B data isolation
    ├── ConversationPersistenceTests.cs# Full lifecycle & message persistence
    └── GeminiErrorHandlingTests.cs    # 503, 429, safety filter, empty prompt
```

---

## Local Setup & Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v18+) and npm](https://nodejs.org/)
- Google Gemini API Key from [Google AI Studio](https://aistudio.google.com/)

---

### 1. Configure Development Secrets

In the backend project directory, configure your Gemini API Key using .NET User Secrets:

```bash
cd AIStudyAssistant.API
dotnet user-secrets set "Gemini:ApiKey" "YOUR_ACTUAL_GEMINI_API_KEY"
```

> **Note**: For local development, `appsettings.Development.json` automatically uses a local SQLite database (`aistudyassistant.db`), allowing you to develop immediately without running a local PostgreSQL instance.

---

### 2. Run the Backend API

From `AIStudyAssistant.API`:

```bash
dotnet run --urls "http://localhost:5000"
```

- API Base URL: `http://localhost:5000`
- Interactive Swagger UI: `http://localhost:5000/swagger`

---

### 3. Run the React Frontend

Open a new terminal in `AIStudyAssistant.Client`:

```bash
cd AIStudyAssistant.Client
npm install
npm run dev
```

Open your browser at:
```
http://localhost:5173
```

---

### 4. Running the Tests

To run the complete automated test suite (14 tests covering auth, data isolation, persistence, and error handling):

```bash
dotnet test
```

---

## Database Configuration & Migrations

### Database Schema
1. **Users**: `Id` (`char(36)` UUID PK), `DisplayName` (`varchar(100)`), `Email` (`varchar(256)` Unique index), `PasswordHash` (`varchar(500)`), `CreatedAt`, `UpdatedAt`.
2. **Conversations**: `Id` (`char(36)` UUID PK), `UserId` (`char(36)` FK -> Users.Id, Cascade delete, indexed), `Title` (`varchar(200)`), `CreatedAt`, `UpdatedAt` (indexed).
3. **Messages**: `Id` (`char(36)` UUID PK), `ConversationId` (`char(36)` FK -> Conversations.Id, Cascade delete, indexed), `Role` (`varchar(20)`: "user" or "assistant"), `Content` (`longtext`), `CreatedAt` (indexed).
All tables use the `utf8mb4` character set.

### Applying Migrations to MySQL

1. Set your MySQL connection string in User Secrets (or environment variables):
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=aistudyassistant;User=root;Password=your_password;"
   ```

2. Apply pending EF Core migrations:
   ```bash
   dotnet ef database update --project AIStudyAssistant.API
   ```

To generate a new migration after model changes:
```bash
dotnet ef migrations add <MigrationName> --project AIStudyAssistant.API
```

---

## Environment Variables Reference

Refer to [sample.env](file:///d:/AI%20study%20Assistence/AIStudyAssistant/sample.env) for full configuration placeholders.

### Backend (`AIStudyAssistant.API`)
| Variable | Description | Example |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ConnectionStrings__DefaultConnection` | MySQL connection string | `Server=...;Port=3306;Database=...;User=...;Password=...;` |
| `Database__Provider` | Relational provider (`MySQL`) | `MySQL` |
| `Jwt__SecretKey` | Cryptographic secret for signing tokens (>= 32 chars) | `your-32-character-secret-key-goes-here` |
| `Jwt__Issuer` | JWT issuer identifier | `AIStudyAssistant.API` |
| `Jwt__Audience` | JWT audience identifier | `AIStudyAssistant.Client` |
| `Jwt__ExpiryDays` | Token lifetime in days | `7` |
| `Gemini__ApiKey` | Google Gemini API Key | `AIzaSy...` |
| `Gemini__Model` | Primary Gemini model | `gemini-3.5-flash` |
| `Gemini__FallbackModel` | Backup model if primary has 503/404 | `gemini-3.5-flash-lite` |
| `Cors__AllowedOrigins__0` | Allowed frontend origin for CORS | `https://your-frontend.pages.dev` |

### Frontend (`AIStudyAssistant.Client`)
| Variable | Description | Example |
| :--- | :--- | :--- |
| `VITE_API_BASE_URL` | Public API URL of deployed backend | `https://api.yourdomain.com` |

---

## Free Deployment Guide

### 1. Database: Free Hosted MySQL
1. Create a MySQL database with a free tier provider:
   - [Aiven](https://aiven.io) (Free tier managed MySQL)
   - [Clever Cloud](https://www.clever-cloud.com) (Free 10MB MySQL plan)
   - [Railway](https://railway.com) (MySQL database service)
2. Copy the connection string details (Host, Port, Database name, Username, Password).
3. Set `ConnectionStrings__DefaultConnection` on your hosting provider.

### 2. Backend: Free Host (Render / Railway / Fly.io / Google Cloud Run)
Using the provided multi-stage [Dockerfile](file:///d:/AI%20study%20Assistence/AIStudyAssistant/Dockerfile):

1. Push your repository to GitHub.
2. In [Render](https://render.com), click **New Web Service** and select your GitHub repository.
3. Select **Docker** as the runtime.
4. Add the required Environment Variables (`ConnectionStrings__DefaultConnection`, `Database__Provider=MySQL`, `Jwt__SecretKey`, `Gemini__ApiKey`, `Cors__AllowedOrigins__0`).
5. Deploy. Render will provide a public URL like `https://aistudyassistant-api.onrender.com`.

### 3. Frontend: Free Static Host (Cloudflare Pages)
1. In [Cloudflare Dashboard](https://dash.cloudflare.com), navigate to **Workers & Pages** -> **Create Application** -> **Pages**.
2. Connect your GitHub repository.
3. Build settings:
   - Framework preset: `Vite`
   - Build command: `npm run build`
   - Build output directory: `dist`
   - Root directory: `AIStudyAssistant.Client`
4. Add environment variable:
   - `VITE_API_BASE_URL`: `https://aistudyassistant-api.onrender.com`
5. Deploy. Your application will be live at `https://your-project.pages.dev`.

---

## Security Considerations

1. **API Key Isolation**: The Google Gemini API key is strictly stored server-side in backend environment configuration and is never exposed to the frontend bundle or client network calls.
2. **Password Security**: Passwords are never stored in plaintext. They are salted and hashed with PBKDF2 (100,000+ HMAC-SHA512 iterations) via `Microsoft.AspNetCore.Identity.PasswordHasher<User>`.
3. **Data Segregation**: User identity is derived strictly from validated server-side JWT claims (`User.FindFirstValue(ClaimTypes.NameIdentifier)`), not from any client-supplied body or query parameters. All database queries enforce `UserId == currentUserId`.
4. **Rate Limiting**: Rate limiting middleware is active on all endpoints to prevent brute-force attacks and abuse of the Gemini API quota.
5. **Safe Error Handling**: Internal exception details, stack traces, and database connection strings are never returned in public HTTP responses.

---

## Known Limitations of Free Hosting

- **Free Tier Inactivity Sleep**: Free-tier web hosts (e.g., Render free tier) spin down instances after 15 minutes of inactivity. The initial request after sleep may take 30–50 seconds to respond ("cold start").
- **Database Limits**:
  - Neon free tier provides 0.5 GB storage and pauses inactive compute after 5 minutes (resumes within seconds).
  - Supabase free tier pauses projects after 7 days of inactivity.
- **Gemini API Free Tier Quota**: The Google Gemini free tier has rate limits (e.g. 15 requests per minute). The backend handles 429 quota exhaustion gracefully by alerting the user to wait a moment.
