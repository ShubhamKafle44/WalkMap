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

    [HttpPost("generate-route")]
    [AllowAnonymous] // temporary until auth is sorted
    public async Task<IActionResult> GenerateRoute([FromBody] GenerateRouteRequest req)
    {
        var orsRequest = new
        {
            coordinates = new List<List<double>>
        {
            new() { req.StartLng, req.StartLat },
            new() { req.StartLng + 0.01, req.StartLat + 0.01 }
        },
            instructions = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(orsRequest), Encoding.UTF8, "application/json");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _config["OpenRouteService:ApiKey"]);
        var res = await client.PostAsync(
            "https://api.openrouteservice.org/v2/directions/foot-walking/geojson", content);

        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            return StatusCode((int)res.StatusCode, $"ORS error: {body}");

        return Content(body, "application/json");
    }
}