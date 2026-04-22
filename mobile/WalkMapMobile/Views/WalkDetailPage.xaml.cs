using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using WalkMapMobile.ViewModels;

namespace WalkMapMobile.Views;

public partial class WalkDetailPage : ContentPage
{
    private readonly WalkDetailViewModel _vm;

    public WalkDetailPage(WalkDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Watch for when route locations are loaded, then draw them
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(WalkDetailViewModel.RouteLocations))
                DrawRoute();
            if (e.PropertyName == nameof(WalkDetailViewModel.MapRegion) && vm.MapRegion != null)
                DetailMap.MoveToRegion(vm.MapRegion);
        };
    }

    private void DrawRoute()
    {
        DetailMap.MapElements.Clear();

        if (_vm.RouteLocations.Count < 2) return;

        var polyline = new Polyline
        {
            StrokeColor = Color.FromArgb("#4CAF50"),
            StrokeWidth = 5
        };

        foreach (var loc in _vm.RouteLocations)
            polyline.Geopath.Add(loc);

        DetailMap.MapElements.Add(polyline);

        // Start pin
        DetailMap.Pins.Add(new Pin
        {
            Label = "Start",
            Location = _vm.RouteLocations.First(),
            Type = PinType.Place
        });

        // End pin
        DetailMap.Pins.Add(new Pin
        {
            Label = "End",
            Location = _vm.RouteLocations.Last(),
            Type = PinType.Place
        });
    }
}
