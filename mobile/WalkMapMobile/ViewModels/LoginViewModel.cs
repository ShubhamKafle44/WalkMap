using WalkMapMobile.Models;
using WalkMapMobile.Services;
using WalkMapMobile.Views;

namespace WalkMapMobile.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly WalkMapApiService _api;
    private readonly AuthStateService _auth;

    private string _email = string.Empty;
    private string _password = string.Empty;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public RelayCommand LoginCommand { get; }
    public RelayCommand GoToRegisterCommand { get; }

    public LoginViewModel(WalkMapApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;

        LoginCommand = new RelayCommand(async () => await LoginAsync());
        GoToRegisterCommand = new RelayCommand(async () =>
            await Shell.Current.GoToAsync(nameof(RegisterPage)));
    }

    private async Task LoginAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter email and password.";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            var (success, data, error) = await _api.LoginAsync(new LoginRequest
            {
                Email = Email,
                Password = Password
            });

            if (!success || data == null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(error) ? "Invalid email or password." : error;
                return;
            }

            await _auth.SaveAsync(data.Token, data.Username);
            _api.SetToken(data.Token);

            await Shell.Current.GoToAsync(nameof(WalkHistoryPage));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}