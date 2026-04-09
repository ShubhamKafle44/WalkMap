using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WalkMap.Api.DTOs;
using WalkMap.Api.Services;

namespace WalkMap.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WalksController : ControllerBase
{
    private readonly IWalkService _walks;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public WalksController(IWalkService walks, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _walks = walks;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WalkSummaryDto>>> GetHistory()
    {
        var walks = await _walks.GetWalkHistoryAsync(UserId);
        return Ok(walks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WalkDetailDto>> GetById(int id)
    {
        try { return Ok(await _walks.GetWalkByIdAsync(UserId, id)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("start")]
    public async Task<ActionResult<WalkSummaryDto>> Start([FromBody] StartWalkRequest request)
    {
        var walk = await _walks.StartWalkAsync(UserId, request);
        return CreatedAtAction(nameof(GetById), new { id = walk.Id }, walk);
    }

    [HttpPut("{id}/end")]
    public async Task<ActionResult<WalkDetailDto>> End(int id, [FromBody] EndWalkRequest request)
    {
        try { return Ok(await _walks.EndWalkAsync(UserId, id, request)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try { await _walks.DeleteWalkAsync(UserId, id); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // FIX: Removed [AllowAnonymous] — was a security hole exposing ORS API key to unauthenticated users.
    // FIX: Now passes TargetDistanceKm to ORS via a proper round-trip waypoint calculation.
    [HttpPost("generate-route")]
    public async Task<IActionResult> GenerateRoute([FromBody] GenerateRouteRequest req)
    {
        var apiKey = _config["OpenRouteService:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(503, new { message = "Route generation is not configured on this server." });

        // FIX: Use TargetDistanceKm to compute a sensible waypoint offset so the
        //      route actually matches the requested distance (~half the target out).
        double offsetDeg = (req.TargetDistanceKm / 2.0) / 111.0;

        var orsRequest = new
        {
            coordinates = new List<List<double>>
            {
                new() { req.StartLng, req.StartLat },
                new() { req.StartLng + offsetDeg, req.StartLat + offsetDeg },
                new() { req.StartLng, req.StartLat }   // return to start
            },
            instructions = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(orsRequest), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
        var res = await client.PostAsync(
            "https://api.openrouteservice.org/v2/directions/foot-walking/geojson", content);

        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            return StatusCode((int)res.StatusCode, new { message = $"Route service error: {body}" });

        return Content(body, "application/json");
    }
}
