using Microsoft.AspNetCore.Mvc;
using TaskRotationApi.Storage;

namespace TaskRotationApi.Controllers;

[ApiController]
[Route("api/seedTestData")]
public class SeedTestDataController(InMemoryDataStore dataStore) : ControllerBase
{
    /// <summary>
    ///     Seeds the in-memory data store with default test data.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Seed()
    {
        var seeded = dataStore.SeedInitialData();
        return Ok(new { seeded });
    }
}