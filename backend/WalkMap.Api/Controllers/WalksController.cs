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

    // ── Walk CRUD ──────────────────────────────────────────────────────────────

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

    // ── Circular Route Generation ──────────────────────────────────────────────
    //
    //  Strategy: place two intermediate waypoints at roughly 1/3 and 2/3 of the
    //  circumference of a circle of radius = targetDistanceKm / (2π).  Using two
    //  waypoints displaced in perpendicular directions (North-East and South-East)
    //  produces a more realistic loop than the previous single diagonal waypoint,
    //  and both ORS legs are constrained to real footpaths.
    //
    //  The final coordinate equals the first, guaranteeing a closed loop.

    [HttpPost("generate-route")]
    public async Task<IActionResult> GenerateRoute([FromBody] GenerateRouteRequest req)
    {
        var apiKey = _config["OpenRouteService:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(503, new { message = "Route generation is not configured on this server." });

        // Radius of a circle whose circumference equals the target distance
        double radiusKm = req.TargetDistanceKm / (2 * Math.PI);
        double radiusDeg = radiusKm / 111.0;   // ~111 km per degree of latitude

        // Longitude degrees per km varies with latitude
        double lngScale = 1.0 / Math.Cos(req.StartLat * Math.PI / 180.0);

        // Waypoint A: bearing 60° (roughly NE)
        double wA_lat = req.StartLat + radiusDeg * Math.Cos(60.0 * Math.PI / 180.0);
        double wA_lng = req.StartLng + radiusDeg * lngScale * Math.Sin(60.0 * Math.PI / 180.0);

        // Waypoint B: bearing 180° (South) — creates a proper loop rather than an out-and-back
        double wB_lat = req.StartLat + radiusDeg * Math.Cos(180.0 * Math.PI / 180.0);
        double wB_lng = req.StartLng + radiusDeg * lngScale * Math.Sin(180.0 * Math.PI / 180.0);

        var orsRequest = new
        {
            coordinates = new List<List<double>>
            {
                new() { req.StartLng, req.StartLat },   // origin
                new() { wA_lng,       wA_lat       },   // 1st waypoint
                new() { wB_lng,       wB_lat       },   // 2nd waypoint
                new() { req.StartLng, req.StartLat },   // back to origin — closes the loop
            },
            instructions = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(orsRequest),
            Encoding.UTF8,
            "application/json");

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