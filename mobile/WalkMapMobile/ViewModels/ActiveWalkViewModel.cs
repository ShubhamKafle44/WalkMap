using Microsoft.Maui.Maps;
using WalkMapMobile.Models;
using WalkMapMobile.Services;

namespace WalkMapMobile.ViewModels;

public class ActiveWalkViewModel : BaseViewModel
{
    private readonly WalkMapApiService _api;

    private int _currentWalkId;
    private CancellationTokenSource _gpsCts = new();
    private readonly List<WalkPoint> _routePoints = new();
    private IDispatcherTimer? _timer;
    private DateTime _startTime;

    // ── Bindable State ──────────────────────────────────────────────────

    private string _walkTitle = "Morning Walk";
    private string _elapsedTime = "00:00";
    private string _distance = "0.00 m";
    private int _stepCount;
    private bool _isWalking;
    private Location? _currentLocation;

    public string WalkTitle { get => _walkTitle; set => SetProperty(ref _walkTitle, value); }
    public string ElapsedTime { get => _elapsedTime; set => SetProperty(ref _elapsedTime, value); }
    public string Distance { get => _distance; set => SetProperty(ref _distance, value); }
    public int StepCount { get => _stepCount; set => SetProperty(ref _stepCount, value); }
    public bool IsWalking { get => _isWalking; set => SetProperty(ref _isWalking, value); }
    public bool IsNotWalking => !IsWalking;
    public Location? CurrentLocation { get => _currentLocation; set => SetProperty(ref _currentLocation, value); }

    // Raised to tell the View to update the polyline
    public event Action<Location>? LocationUpdated;

    public RelayCommand StartWalkCommand { get; }
    public RelayCommand EndWalkCommand { get; }
    public RelayCommand CancelCommand { get; }

    public ActiveWalkViewModel(WalkMapApiService api)
    {
        _api = api;
        StartWalkCommand = new RelayCommand(async () => await StartWalkAsync());
        EndWalkCommand = new RelayCommand(async () => await EndWalkAsync());
        CancelCommand = new RelayCommand(async () =>
            await Shell.Current.GoToAsync(".."));
    }

    private async Task StartWalkAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        var walk = await _api.StartWalkAsync(WalkTitle);
        if (walk == null)
        {
            ErrorMessage = "Could not start walk. Check your connection.";
            IsBusy = false;
            return;
        }

        _currentWalkId = walk.Id;
        _routePoints.Clear();
        _startTime = DateTime.UtcNow;
        IsWalking = true;
        OnPropertyChanged(nameof(IsNotWalking));
        IsBusy = false;

        StartTimer();
        await StartGpsTrackingAsync();
    }

    private async Task StartGpsTrackingAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            ErrorMessage = "Location permission is required.";
            return;
        }

        _gpsCts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            int order = 0;
            while (!_gpsCts.Token.IsCancellationRequested)
            {
                try
                {
                    var loc = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Best,
                        Timeout = TimeSpan.FromSeconds(10)
                    }, _gpsCts.Token);

                    if (loc != null)
                    {
                        var point = new WalkPoint
                        {
                            Latitude = loc.Latitude,
                            Longitude = loc.Longitude,
                            Timestamp = DateTime.UtcNow,
                            SequenceOrder = order++
                        };

                        _routePoints.Add(point);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CurrentLocation = loc;
                            UpdateDistance();
                            LocationUpdated?.Invoke(loc);
                        });
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* swallow GPS errors, keep trying */ }

                await Task.Delay(5000, _gpsCts.Token).ContinueWith(_ => { });
            }
        });
    }

    private void StartTimer()
    {
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            var elapsed = DateTime.UtcNow - _startTime;
            ElapsedTime = elapsed.ToString(@"mm\:ss");
            // Simple step estimation: ~1.3 steps/second while walking
            StepCount = (int)(elapsed.TotalSeconds * 1.3);
        };
        _timer.Start();
    }

    private void UpdateDistance()
    {
        if (_routePoints.Count < 2) return;
        double totalMeters = 0;
        for (int i = 1; i < _routePoints.Count; i++)
        {
            totalMeters += HaversineMeters(
                _routePoints[i - 1].Latitude, _routePoints[i - 1].Longitude,
                _routePoints[i].Latitude, _routePoints[i].Longitude);
        }
        Distance = totalMeters >= 1000
            ? $"{totalMeters / 1000:F2} km"
            : $"{totalMeters:F0} m";
    }

    private async Task EndWalkAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "End Walk", "Are you sure you want to end this walk?", "End Walk", "Keep Going");
        if (!confirm) return;

        _gpsCts.Cancel();
        _timer?.Stop();

        IsBusy = true;
        bool saved = await _api.EndWalkAsync(_currentWalkId, StepCount, _routePoints);
        IsBusy = false;

        if (saved)
        {
            await Shell.Current.DisplayAlert("Walk Saved!", $"Great walk! {Distance} covered.", "OK");
            await Shell.Current.GoToAsync("//history");
        }
        else
        {
            ErrorMessage = "Failed to save walk. Try again.";
        }
    }

    // Same Haversine formula as the backend
    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}