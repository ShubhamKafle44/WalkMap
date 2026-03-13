using Blazored.LocalStorage;
using WalkMapFrontend.Models;

namespace WalkMapFrontend.Services;

public class AuthStateService
{
    private readonly ILocalStorageService _storage;
    public event Action? OnChange;

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? Email { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public AuthStateService(ILocalStorageService storage) => _storage = storage;

    public async Task InitializeAsync()
    {
        Token = await _storage.GetItemAsync<string>("wm_token");
        Username = await _storage.GetItemAsync<string>("wm_username");
        Email = await _storage.GetItemAsync<string>("wm_email");
        OnChange?.Invoke();
    }

    public async Task SetAuthAsync(AuthResponse auth)
    {
        Token = auth.Token;
        Username = auth.Username;
        Email = auth.Email;
        await _storage.SetItemAsync("wm_token", auth.Token);
        await _storage.SetItemAsync("wm_username", auth.Username);
        await _storage.SetItemAsync("wm_email", auth.Email);
        OnChange?.Invoke();
    }

    public async Task ClearAsync()
    {
        Token = null; Username = null; Email = null;
        await _storage.RemoveItemAsync("wm_token");
        await _storage.RemoveItemAsync("wm_username");
        await _storage.RemoveItemAsync("wm_email");
        OnChange?.Invoke();
    }
}
