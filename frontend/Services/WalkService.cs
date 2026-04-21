using System.Net.Http.Json;
using System.Net.Http.Headers;
using WalkMapFrontend.Models;
using System.Text.Json;

namespace WalkMapFrontend.Services;

public class WalkService
{
    private readonly HttpClient _http;
    private readonly AuthStateService _auth;

    public WalkService(HttpClient http, AuthStateService auth)
    {
        _http = http;
        _auth = auth;
    }

    // FIX: Use a per-request message instead of mutating DefaultRequestHeaders.
    //      Mutating shared headers is not thread-safe and can be stomped by
    //      concurrent requests (e.g. dashboard loading while a walk ends).
    private HttpRequestMessage AuthorizedRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_auth.Token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        return req;
    }

    // ── Walk CRUD ──────────────────────────────────────────────────────────────

    // FIX: GetFromJsonAsync silently returns null on 401/403/500.
    //      Using SendAsync + EnsureSuccessStatusCode surfaces auth errors
    //      instead of showing an empty dashboard.
    public async Task<List<WalkSummaryDto>> GetAllAsync()
    {
        var req = AuthorizedRequest(HttpMethod.Get, "/api/Walks");
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<List<WalkSummaryDto>>() ?? new();
    }

    public async Task<WalkDetailDto?> GetByIdAsync(int id)
    {
        var req = AuthorizedRequest(HttpMethod.Get, $"/api/Walks/{id}");
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WalkDetailDto>();
    }

    public async Task<WalkSummaryDto?> StartAsync(StartWalkRequest body)
    {
        var req = AuthorizedRequest(HttpMethod.Post, "/api/Walks/start");
        req.Content = JsonContent.Create(body);
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WalkSummaryDto>();
    }

    public async Task<WalkDetailDto?> EndAsync(int id, EndWalkRequest body)
    {
        var req = AuthorizedRequest(HttpMethod.Put, $"/api/Walks/{id}/end");
        req.Content = JsonContent.Create(body);
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WalkDetailDto>();
    }

    public async Task DeleteAsync(int id)
    {
        var req = AuthorizedRequest(HttpMethod.Delete, $"/api/Walks/{id}");
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    // ── Route Generation ───────────────────────────────────────────────────────

    public GenerateRouteResponse? CurrentRoute { get; set; }

    public async Task<GenerateRouteResponse?> GenerateRouteAsync(GenerateRouteRequest body)
    {
        var req = AuthorizedRequest(HttpMethod.Post, "/api/Walks/generate-route");
        req.Content = JsonContent.Create(body);

        var res = await _http.SendAsync(req);
        var rawBody = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Route error {res.StatusCode}: {rawBody}");

        var data = JsonSerializer.Deserialize<ORSResponse>(rawBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data?.Features == null || !data.Features.Any())
            throw new Exception($"No route returned. Raw response: {rawBody}");

        var feature = data.Features[0];
        var route = new GenerateRouteResponse
        {
            EstimatedDistanceKm = feature.Properties.Summary.Distance / 1000.0,
            Points = feature.Geometry.Coordinates
                .Select((c, i) => new WalkPointDto
                {
                    Longitude = c[0],
                    Latitude = c[1],
                    SequenceOrder = i,
                    Timestamp = DateTime.UtcNow
                }).ToList()
        };

        CurrentRoute = route;
        return route;
    }
}