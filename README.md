# Task Rotation API

This repository contains a minimal ASP.NET Core Web API that manages users and tasks and enforces the rotation rules described in the assignment. Everything is kept in memory to keep the project easy to run and reason about.

## Running the API

1. Install the .NET 8 SDK.
2. Open `TaskRotationApi.sln` in Visual Studio (or your preferred IDE) to explore and run the project, or use the CLI commands below from the repository root.
3. Restore dependencies and run the application:

   ```bash
   dotnet restore TaskRotationApi
   dotnet run --project TaskRotationApi
   ```

4. The API listens on the default ASP.NET ports. When running locally you can browse the interactive Swagger UI at `https://localhost:7091/swagger` (or the HTTP port shown in the console output).

### Configuration

The automatic task rotation runs every two minutes by default. You can override the interval (for example during debugging) by adding the following setting to `appsettings.Development.json` or by setting an environment variable:

```json
{
  "TaskRotation": {
    "IntervalSeconds": 30
  }
}
```

## Available endpoints

All routes are prefixed with `/api`.

### Users

- `GET /api/users` – list all users with the number of currently assigned tasks.
- `GET /api/users/{id}` – get a single user.
- `POST /api/users` – create a user. Body: `{ "name": "Alice" }`.
- `DELETE /api/users/{id}` – delete a user. Any tasks currently assigned to the user move back to the waiting pool.

### Tasks

- `GET /api/tasks` – list all tasks including their state, assignee (if any) and history.
- `GET /api/tasks/{id}` – get a single task.
- `POST /api/tasks` – create a task. Body: `{ "title": "Write documentation" }`.

## Behaviour summary

- User names and task titles must be unique.
- Each user can work on up to three tasks at a time.
- Tasks are assigned automatically on creation if a suitable user exists. Otherwise, they remain in the waiting state.
- Every two minutes tasks are rotated to a different user respecting the rules defined in the brief. When no user is available the task returns to the waiting state.
- A task is marked as completed once it has been assigned to every existing user at least once. Completed tasks are no longer reassigned.

The code includes inline comments and logging explaining the key decisions.
