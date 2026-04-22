using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using WalkMapMobile.ViewModels;

namespace WalkMapMobile.Views;

public partial class GenerateRoutePage : ContentPage
{
    private readonly GenerateRouteViewModel _vm;

    public GenerateRoutePage(GenerateRouteViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(GenerateRouteViewModel.RouteLocations))
                DrawRoute();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Center map on user's current location
        try
        {
            var loc = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(5)
            });
            if (loc != null)
                RouteMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(loc.Latitude, loc.Longitude),
                    Distance.FromKilometers(2)));
        }
        catch { /* location unavailable */ }
    }

    private void DrawRoute()
    {
        RouteMap.MapElements.Clear();
        RouteMap.Pins.Clear();

        if (_vm.RouteLocations.Count < 2) return;

        var polyline = new Polyline
        {
            StrokeColor = Color.FromArgb("#4CAF50"),
            StrokeWidth = 5
        };

        foreach (var loc in _vm.RouteLocations)
            polyline.Geopath.Add(loc);

        RouteMap.MapElements.Add(polyline);

        var start = _vm.RouteLocations.First();
        RouteMap.Pins.Add(new Pin
        {
            Label = "Start / End",
            Location = start,
            Type = PinType.Place
        });

        RouteMap.MoveToRegion(MapSpan.FromCenterAndRadius(start, Distance.FromKilometers(2)));
    }
}
