using Microsoft.Maui.Maps;
using WalkMapMobile.Models;
using WalkMapMobile.Services;

namespace WalkMapMobile.ViewModels;

public class GenerateRouteViewModel : BaseViewModel
{
    private readonly WalkMapApiService _api;

    private double _targetDistanceKm = 2.0;
    private List<Location> _routeLocations = new();
    private string _resultText = string.Empty;
    private bool _hasRoute;

    public double TargetDistanceKm
    {
        get => _targetDistanceKm;
        set => SetProperty(ref _targetDistanceKm, value);
    }

    public List<Location> RouteLocations
    {
        get => _routeLocations;
        set => SetProperty(ref _routeLocations, value);
    }

    public string ResultText { get => _resultText; set => SetProperty(ref _resultText, value); }
    public bool HasRoute { get => _hasRoute; set => SetProperty(ref _hasRoute, value); }

    public RelayCommand GenerateCommand { get; }
    public RelayCommand BackCommand { get; }

    public GenerateRouteViewModel(WalkMapApiService api)
    {
        _api = api;
        GenerateCommand = new RelayCommand(async () => await GenerateAsync());
        BackCommand = new RelayCommand(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task GenerateAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        HasRoute = false;

        var loc = await Geolocation.GetLocationAsync(new GeolocationRequest
        {
            DesiredAccuracy = GeolocationAccuracy.Medium,
            Timeout = TimeSpan.FromSeconds(10)
        });

        if (loc == null)
        {
            ErrorMessage = "Could not get your location.";
            IsBusy = false;
            return;
        }

        var result = await _api.GenerateRouteAsync(new GenerateRouteRequest
        {
            StartLat = loc.Latitude,
            StartLng = loc.Longitude,
            TargetDistanceKm = TargetDistanceKm
        });

        IsBusy = false;

        if (result != null && result.RoutePoints.Count > 0)
        {
            RouteLocations = result.RoutePoints
                .Select(p => new Location(p.Latitude, p.Longitude))
                .ToList();
            ResultText = $"~{result.EstimatedDistanceKm:F1} km circular route generated";
            HasRoute = true;
        }
        else
        {
            ErrorMessage = "Could not generate route. Try again.";
        }
    }
}