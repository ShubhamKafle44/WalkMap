using WalkMapMobile.Models;
using WalkMapMobile.Services;

namespace WalkMapMobile.ViewModels;

public class RegisterViewModel : BaseViewModel
{
    private readonly WalkMapApiService _api;
    private readonly AuthStateService _auth;

    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Email { get => _email; set => SetProperty(ref _email, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }

    public RelayCommand RegisterCommand { get; }
    public RelayCommand GoToLoginCommand { get; }

    public RegisterViewModel(WalkMapApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;
        RegisterCommand = new RelayCommand(async () => await RegisterAsync());
        GoToLoginCommand = new RelayCommand(async () =>
            await Shell.Current.GoToAsync("//login"));
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "All fields are required.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        var (success, error) = await _api.RegisterAsync(new RegisterRequest
        {
            Username = Username,
            Email = Email,
            Password = Password
        });

        IsBusy = false;

        if (success)
        {
            // Auto-login after registration
            var (loginSuccess, data, loginError) = await _api.LoginAsync(new LoginRequest
            {
                Email = Email,
                Password = Password
            });

            if (loginSuccess && data != null)
            {
                await _auth.SaveAsync(data.Token, data.Username);
                _api.SetToken(data.Token);
                await Shell.Current.GoToAsync("//history");
            }
            else
            {
                await Shell.Current.GoToAsync("//login");
            }
        }
        else
        {
            ErrorMessage = "Registration failed. Email may already be in use.";
        }
    }
}