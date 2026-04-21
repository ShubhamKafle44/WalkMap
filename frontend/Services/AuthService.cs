using System.Net.Http.Json;
using System.Text.Json;
using WalkMapFrontend.Models;

namespace WalkMapFrontend.Services;

/// <summary>
/// Handles HTTP calls to the Auth API endpoints (login, register).
/// Distinct from AuthStateService which manages the in-memory/persisted token state.
/// </summary>
public class AuthService
{
    private readonly HttpClient _http;

    public AuthService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(AuthResponse? data, string? error)> LoginAsync(LoginRequest request)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/Auth/login", request);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var msg = TryExtractMessage(body) ?? $"Login failed ({(int)res.StatusCode}).";
                return (null, msg);
            }
            var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(AuthResponse? data, string? error)> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/Auth/register", request);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var msg = TryExtractMessage(body) ?? $"Registration failed ({(int)res.StatusCode}).";
                return (null, msg);
            }
            var data = await res.Content.ReadFromJsonAsync<AuthResponse>();
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    // Tries to pull a "message" field out of a JSON error body, e.g. { "message": "Invalid credentials" }
    private static string? TryExtractMessage(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch { }
        return null;
    }
}