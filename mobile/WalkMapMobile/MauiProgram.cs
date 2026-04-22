using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using WalkMapMobile.Services;
using WalkMapMobile.ViewModels;
using WalkMapMobile.Views;

namespace WalkMapMobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.UseMauiMaps()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<WalkMapApiService>();
		builder.Services.AddSingleton<AuthStateService>();

		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<WalkHistoryViewModel>();
		builder.Services.AddTransient<ActiveWalkViewModel>();
		builder.Services.AddTransient<WalkDetailViewModel>();
		builder.Services.AddTransient<GenerateRouteViewModel>();

		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<WalkHistoryPage>();
		builder.Services.AddTransient<ActiveWalkPage>();
		builder.Services.AddTransient<WalkDetailPage>();
		builder.Services.AddTransient<GenerateRoutePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}