# Task Rotation API

This ASP.NET Core Web API manages users and tasks that automatically rotate between available users while respecting capacity limits.

## Getting started

1. Install the .NET 8 SDK.
2. Restore dependencies and run the application from the repository root:

   ```bash
   dotnet restore TaskRotationApi
   dotnet run --project TaskRotationApi
   ```

3. The API is available immediately at the printed HTTP/HTTPS URLs. Swagger UI is always enabled at `https://localhost:7091/swagger` (adjust the port to match the console output).

## Configuration

The rotation interval is configured via `appsettings.json`:

```json
{
  "TaskRotation": {
    "IntervalSeconds": 120
  }
}
```

Override the value in `appsettings.Development.json` or with the `TaskRotation__IntervalSeconds` environment variable to speed up local testing.

## API endpoints

All endpoints are available under the `/api` prefix.

### Users

- `GET /api/users` – Returns all users with current assignment statistics.
- `GET /api/users/{id}` – Returns a single user or 404 when missing.
- `POST /api/users` – Creates a user. Body: `{ "name": "Alice" }`. Validates length (1-50 chars) and uniqueness.
- `DELETE /api/users/{id}` – Deletes a user, releases their tasks back to the waiting pool and clears previous assignee references.

### Tasks

- `GET /api/tasks` – Returns all tasks with status, assignee details, visit count and assignment history.
- `GET /api/tasks/{id}` – Returns a single task or 404 when missing.
- `POST /api/tasks` – Creates a task. Body: `{ "title": "Write documentation" }`. Titles must be unique and 1-100 chars long.

### Example requests

```bash
curl -X POST https://localhost:7091/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Alice"}'

curl -X POST https://localhost:7091/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title":"Prepare report"}'

curl https://localhost:7091/api/tasks
```

## Rotation rules

- Each user can hold at most three active tasks at a time.
- When a task is created it is immediately assigned to a random eligible user, if one exists. Otherwise the task stays in the `Waiting` state.
- Every rotation cycle (default every 120 seconds) all non-completed tasks are considered:
  - A new assignee must not be the current or previous user, and must still have free capacity.
  - If no candidates exist, the task returns to the `Waiting` state until capacity frees up.
  - Each assignment appends to the task's history.
- Once a task has visited every current user at least once it is marked as `Completed` and will no longer rotate, even if new users are added later.
- When a user is deleted, their active tasks return to `Waiting`, previous-user references are cleared, and the service immediately attempts to reassign waiting tasks.

Run `dotnet run --project TaskRotationApi` to experience the behaviour out-of-the-box.
