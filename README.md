# UserTasks API

This project provides a small ASP.NET Core Web API that exposes an endpoint for seeding in-memory test data on demand.

## Endpoints

- `POST /api/seedTestData` &mdash; Calls `SeedInitialData()` on the singleton `InMemoryDataStore` to populate demo data. The method is not invoked automatically on application startup, so no test data is loaded until this endpoint is called. Subsequent calls return `409 Conflict` to indicate that the data has already been seeded.

## Running the project

1. Restore and build the solution:
   ```bash
   dotnet build
   ```
2. Run the API:
   ```bash
   dotnet run --project src/UserTasks.Api/UserTasks.Api.csproj
   ```
3. Seed the data by calling the endpoint:
   ```bash
   curl -X POST https://localhost:5001/api/seedTestData
   ```
