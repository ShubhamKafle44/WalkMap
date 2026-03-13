using System.Net.Http.Json;
using System.Net.Http.Headers;
using WalkMapFrontend.Models;
using Microsoft.Extensions.Configuration;
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

    private void Authorize()
    {
        if (!string.IsNullOrEmpty(_auth.Token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _auth.Token);
    }

    // Existing API methods
    public async Task<List<WalkSummaryDto>> GetAllAsync()
    {
        Authorize();
        return await _http.GetFromJsonAsync<List<WalkSummaryDto>>("/api/Walks") ?? new();
    }

    public async Task<WalkDetailDto?> GetByIdAsync(int id)
    {
        Authorize();
        return await _http.GetFromJsonAsync<WalkDetailDto>($"/api/Walks/{id}");
    }

    public async Task<WalkSummaryDto?> StartAsync(StartWalkRequest req)
    {
        Authorize();
        var res = await _http.PostAsJsonAsync("/api/Walks/start", req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WalkSummaryDto>();
    }

    public GenerateRouteResponse? CurrentRoute { get; set; }

    public async Task<WalkDetailDto?> EndAsync(int id, EndWalkRequest req)
    {
        Authorize();
        var res = await _http.PutAsJsonAsync($"/api/Walks/{id}/end", req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<WalkDetailDto>();
    }

    public async Task DeleteAsync(int id)
    {
        Authorize();
        var res = await _http.DeleteAsync($"/api/Walks/{id}");
        res.EnsureSuccessStatusCode();
    }

    // -----------------------
    // Generate a real walking route using OpenRouteService
    // -----------------------
    public async Task<GenerateRouteResponse?> GenerateRouteAsync(GenerateRouteRequest req)
    {
        Authorize();
        var res = await _http.PostAsJsonAsync("/api/Walks/generate-route", req);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Route error {res.StatusCode}: {body}");

        // Backend returns ORS JSON directly — deserialize as ORSResponse
        var data = JsonSerializer.Deserialize<ORSResponse>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data?.Features == null || !data.Features.Any())
            throw new Exception($"No route returned. Raw response: {body}");

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

