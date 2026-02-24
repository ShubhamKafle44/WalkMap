using System.Security.Claims;
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

    public WalksController(IWalkService walks)
    {
        _walks = walks;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET api/walks
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WalkSummaryDto>>> GetHistory()
    {
        var walks = await _walks.GetWalkHistoryAsync(UserId);
        return Ok(walks);
    }

    // GET api/walks/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<WalkDetailDto>> GetById(int id)
    {
        try
        {
            var walk = await _walks.GetWalkByIdAsync(UserId, id);
            return Ok(walk);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // POST api/walks/start
    [HttpPost("start")]
    public async Task<ActionResult<WalkSummaryDto>> Start([FromBody] StartWalkRequest request)
    {
        var walk = await _walks.StartWalkAsync(UserId, request);
        return CreatedAtAction(nameof(GetById), new { id = walk.Id }, walk);
    }

    // PUT api/walks/{id}/end
    [HttpPut("{id}/end")]
    public async Task<ActionResult<WalkDetailDto>> End(int id, [FromBody] EndWalkRequest request)
    {
        try
        {
            var walk = await _walks.EndWalkAsync(UserId, id, request);
            return Ok(walk);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // DELETE api/walks/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _walks.DeleteWalkAsync(UserId, id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // POST api/walks/generate-route
    [HttpPost("generate-route")]
    public ActionResult<RouteGenerateResponse> GenerateRoute([FromBody] RouteGenerateRequest request)
    {
        var route = _walks.GenerateRoute(request);
        return Ok(route);
    }
}