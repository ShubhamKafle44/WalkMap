using System.Collections.ObjectModel;
using WalkMapMobile.Models;
using WalkMapMobile.Services;

namespace WalkMapMobile.ViewModels;

public class WalkHistoryViewModel : BaseViewModel
{
    private readonly WalkMapApiService _api;
    private readonly AuthStateService _auth;

    public ObservableCollection<WalkSummary> Walks { get; } = new();

    private string _username = string.Empty;
    public string Username { get => _username; set => SetProperty(ref _username, value); }

    private bool _isEmpty;
    public bool IsEmpty { get => _isEmpty; set => SetProperty(ref _isEmpty, value); }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand StartNewWalkCommand { get; }
    public RelayCommand GoToGenerateRouteCommand { get; }
    public RelayCommand LogoutCommand { get; }
    public RelayCommand<WalkSummary> ViewWalkCommand { get; }
    public RelayCommand<WalkSummary> DeleteWalkCommand { get; }

    public WalkHistoryViewModel(WalkMapApiService api, AuthStateService auth)
    {
        _api = api;
        _auth = auth;

        RefreshCommand = new RelayCommand(async () => await LoadWalksAsync());
        StartNewWalkCommand = new RelayCommand(async () =>
            await Shell.Current.GoToAsync("activewalk"));

        GoToGenerateRouteCommand = new RelayCommand(async () =>
            await Shell.Current.GoToAsync("generateroute"));
        LogoutCommand = new RelayCommand(async () => await LogoutAsync());
        ViewWalkCommand = new RelayCommand<WalkSummary>(async (walk) =>
        {
            if (walk != null)
                await Shell.Current.GoToAsync($"walkdetail?walkId={walk.Id}");
        });
        DeleteWalkCommand = new RelayCommand<WalkSummary>(async (walk) =>
        {
            if (walk == null) return;
            bool confirm = await Shell.Current.DisplayAlert(
                "Delete Walk", $"Delete \"{walk.Title}\"?", "Delete", "Cancel");
            if (confirm)
            {
                await _api.DeleteWalkAsync(walk.Id);
                await LoadWalksAsync();
            }
        });
    }

    public async Task InitializeAsync()
    {
        Username = _auth.Username ?? "Walker";
        await LoadWalksAsync();
    }

    private async Task LoadWalksAsync()
    {
        IsBusy = true;
        var walks = await _api.GetWalksAsync();
        Walks.Clear();
        foreach (var w in walks.OrderByDescending(w => w.StartedAt))
            Walks.Add(w);
        IsEmpty = Walks.Count == 0;
        IsBusy = false;
    }

    private async Task LogoutAsync()
    {
        await _auth.ClearAsync();
        _api.ClearToken();
        await Shell.Current.GoToAsync("//login");
    }
}