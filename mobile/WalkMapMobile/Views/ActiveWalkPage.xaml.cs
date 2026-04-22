using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using WalkMapMobile.ViewModels;

namespace WalkMapMobile.Views;

public partial class ActiveWalkPage : ContentPage
{
    private readonly ActiveWalkViewModel _vm;
    private readonly Polyline _polyline;

    public ActiveWalkPage(ActiveWalkViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;

        // Set up the route polyline on the map
        _polyline = new Polyline
        {
            StrokeColor = Color.FromArgb("#4CAF50"),
            StrokeWidth = 5
        };
        WalkMap.MapElements.Add(_polyline);

        // Subscribe to GPS updates from the ViewModel
        vm.LocationUpdated += OnLocationUpdated;
    }

    private void OnLocationUpdated(Location loc)
    {
        // Add point to the polyline
        _polyline.Geopath.Add(loc);

        // Pan map to follow user
        WalkMap.MoveToRegion(MapSpan.FromCenterAndRadius(loc,
            Distance.FromMeters(200)));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.LocationUpdated -= OnLocationUpdated;
    }
}
