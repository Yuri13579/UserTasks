using Microsoft.AspNetCore.Mvc;
using UserTasks.Api.Services;

namespace UserTasks.Api.Controllers;

[ApiController]
[Route("api")]
public class TestDataController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;

    public TestDataController(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpPost("seedTestData")]
    public IActionResult SeedTestData()
    {
        var seeded = _dataStore.SeedInitialData();

        if (!seeded)
        {
            return Conflict(new { message = "Test data has already been seeded." });
        }

        return Ok(new { message = "Test data seeded successfully." });
    }
}
