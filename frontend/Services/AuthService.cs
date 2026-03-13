using System.Net.Http.Json;
using WalkMapFrontend.Models;

namespace WalkMapFrontend.Services;

public class AuthService
{
    private readonly HttpClient _http;
    public AuthService(HttpClient http) => _http = http;

    public async Task<(AuthResponse? Data, string? Error)> RegisterAsync(RegisterRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/Auth/register", req);
            if (res.IsSuccessStatusCode)
                return (await res.Content.ReadFromJsonAsync<AuthResponse>(), null);
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            return (null, err?.Message ?? "Registration failed");
        }
        catch (Exception ex) { return (null, ex.Message); }
    }

    public async Task<(AuthResponse? Data, string? Error)> LoginAsync(LoginRequest req)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("/api/Auth/login", req);
            if (res.IsSuccessStatusCode)
                return (await res.Content.ReadFromJsonAsync<AuthResponse>(), null);
            var err = await res.Content.ReadFromJsonAsync<ApiError>();
            return (null, err?.Message ?? "Invalid email or password");
        }
        catch (Exception ex) { return (null, ex.Message); }
    }
}
