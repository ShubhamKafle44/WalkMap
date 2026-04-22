using Microsoft.Maui.Maps;
using WalkMapMobile.Models;
using WalkMapMobile.Services;

namespace WalkMapMobile.ViewModels;

[QueryProperty(nameof(WalkId), "walkId")]
public class WalkDetailViewModel : BaseViewModel
{
    private readonly WalkMapApiService _api;

    private int _walkId;
    private WalkDetail? _walk;

    public int WalkId
    {
        get => _walkId;
        set
        {
            _walkId = value;
            _ = LoadAsync();
        }
    }

    public WalkDetail? Walk { get => _walk; set => SetProperty(ref _walk, value); }

    private List<Location> _routeLocations = new();
    public List<Location> RouteLocations
    {
        get => _routeLocations;
        set => SetProperty(ref _routeLocations, value);
    }

    private MapSpan? _mapRegion;
    public MapSpan? MapRegion { get => _mapRegion; set => SetProperty(ref _mapRegion, value); }

    public RelayCommand BackCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public WalkDetailViewModel(WalkMapApiService api)
    {
        _api = api;
        BackCommand = new RelayCommand(async () => await Shell.Current.GoToAsync(".."));
        DeleteCommand = new RelayCommand(async () => await DeleteAsync());
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        Walk = await _api.GetWalkAsync(_walkId);
        IsBusy = false;

        if (Walk?.RoutePoints?.Count > 0)
        {
            RouteLocations = Walk.RoutePoints
                .OrderBy(p => p.SequenceOrder)
                .Select(p => new Location(p.Latitude, p.Longitude))
                .ToList();

            var centerLat = RouteLocations.Average(l => l.Latitude);
            var centerLon = RouteLocations.Average(l => l.Longitude);
            MapRegion = MapSpan.FromCenterAndRadius(
                new Location(centerLat, centerLon),
                Distance.FromKilometers(1));
        }
    }

    private async Task DeleteAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Delete Walk", "Delete this walk permanently?", "Delete", "Cancel");
        if (!confirm) return;

        await _api.DeleteWalkAsync(_walkId);
        await Shell.Current.GoToAsync("..");
    }
}