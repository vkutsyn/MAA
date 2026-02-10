# Quickstart: Eligibility Wizard UI

## Prerequisites
- .NET 9/10 SDK
- Node.js 22 LTS
- PostgreSQL 13+

## Backend (API)
1. Configure database connection in `backend/MAA.API/appsettings.Development.json`.
2. Apply migrations:
   ```bash
   cd backend/MAA.Infrastructure
   dotnet ef database update --startup-project ../MAA.API
   ```
3. Run the API:
   ```bash
   cd ../MAA.API
   dotnet run
   ```
4. Verify Swagger is available at http://localhost:5008/swagger

## Frontend (Wizard UI)
1. Once the frontend is scaffolded in `frontend/`, install dependencies:
   ```bash
   cd frontend
   npm install
   ```
2. Run the dev server:
   ```bash
   npm run dev
   ```
3. Open the Vite URL and start the wizard from the landing page.

## Tests
- Frontend (planned):
  ```bash
  cd frontend
  npm run test
  ```
- Backend:
  ```bash
  dotnet test
  ```
