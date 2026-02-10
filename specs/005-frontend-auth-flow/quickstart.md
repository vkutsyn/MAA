# Quickstart: Frontend Authentication Flow with Login/Registration

## Prerequisites

- Node.js 22 LTS
- .NET 10 SDK
- Backend API running locally

## Run Backend

```powershell
Set-Location D:\Programming\Langate\MedicaidApplicationAssistant\src\MAA.API

dotnet run
```

## Run Frontend

```powershell
Set-Location D:\Programming\Langate\MedicaidApplicationAssistant\frontend

npm install
npm run dev
```

## Verify Auth Flow

1. Open the frontend in the browser at /login.
2. Create a new account on the registration page.
3. Log in with the new credentials.
4. Confirm protected pages load after login.
5. Simulate an unauthorized response by stopping the backend, then refreshing; confirm you are routed to login without a loop.
6. Log out and confirm protected pages redirect to login.

## Testing Notes

- Use Vitest + React Testing Library for frontend auth components, hooks, and routing guards.
- Verify accessibility with keyboard-only navigation and screen reader checks on login and registration forms.
