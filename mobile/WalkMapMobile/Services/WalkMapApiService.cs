using System.Net.Http.Headers;
using System.Net.Http.Json;
using WalkMapMobile.Models;

namespace WalkMapMobile.Services;

public class WalkMapApiService
{
    private readonly HttpClient _http;


    private const string BaseUrl = "http://192.168.1.77:5195";

    public WalkMapApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void SetToken(string token)
    {
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _http.DefaultRequestHeaders.Authorization = null;
    }

    // ── AUTH ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, LoginResponse? Data, string Error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return (true, data, string.Empty);
            }
            var error = await response.Content.ReadAsStringAsync();
            return (false, null, error);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Success, string Error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register", request);
            if (response.IsSuccessStatusCode)
                return (true, string.Empty);
            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── WALKS ─────────────────────────────────────────────────────────────

    public async Task<List<WalkSummary>> GetWalksAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<WalkSummary>>("/api/walks")
                   ?? new List<WalkSummary>();
        }
        catch
        {
            return new List<WalkSummary>();
        }
    }

    public async Task<WalkDetail?> GetWalkAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<WalkDetail>($"/api/walks/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<WalkSummary?> StartWalkAsync(string title)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/walks/start",
                new StartWalkRequest { Title = title });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WalkSummary>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> EndWalkAsync(int walkId, int stepCount, List<WalkPoint> routePoints)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/walks/{walkId}/end",
                new EndWalkRequest { StepCount = stepCount, RoutePoints = routePoints });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteWalkAsync(int walkId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/walks/{walkId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<GenerateRouteResponse?> GenerateRouteAsync(GenerateRouteRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/walks/generate-route", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GenerateRouteResponse>();
        }
        catch
        {
            return null;
        }
    }
}