using WalkMapMobile.Services;

namespace WalkMapMobile;

public partial class App : Application
{
	private readonly AuthStateService _auth;
	private readonly WalkMapApiService _api;
	private bool _initialized;

	public App(AuthStateService auth, WalkMapApiService api)
	{
		InitializeComponent();
		_auth = auth;
		_api = api;
		MainPage = new AppShell();
	}

	protected override async void OnResume()
	{
		base.OnResume();
		if (!_initialized)
		{
			_initialized = true;
			await InitializeAuthAsync();
		}
	}

	protected override async void OnStart()
	{
		base.OnStart();
		if (!_initialized)
		{
			_initialized = true;
			await InitializeAuthAsync();
		}
	}

	private async Task InitializeAuthAsync()
	{
		await _auth.InitializeAsync();

		if (_auth.IsLoggedIn)
		{
			_api.SetToken(_auth.Token!);
			await Shell.Current.GoToAsync("//history");
		}
		else
		{
			await Shell.Current.GoToAsync("//login");
		}
	}
}