namespace WalkMapMobile.Services;

public class AuthStateService
{
    private const string TokenKey = "walkmap_jwt";
    private const string UsernameKey = "walkmap_username";

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

    public async Task InitializeAsync()
    {
        Token = await SecureStorage.GetAsync(TokenKey);
        Username = await SecureStorage.GetAsync(UsernameKey);
    }

    public async Task SaveAsync(string token, string username)
    {
        Token = token;
        Username = username;
        await SecureStorage.SetAsync(TokenKey, token);
        await SecureStorage.SetAsync(UsernameKey, username);
    }

    public async Task ClearAsync()
    {
        Token = null;
        Username = null;
        SecureStorage.Remove(TokenKey);
        SecureStorage.Remove(UsernameKey);
        await Task.CompletedTask;
    }
}