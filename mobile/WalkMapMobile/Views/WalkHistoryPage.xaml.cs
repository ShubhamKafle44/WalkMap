using WalkMapMobile.ViewModels;

namespace WalkMapMobile.Views;

public partial class WalkHistoryPage : ContentPage
{
    private readonly WalkHistoryViewModel _vm;

    public WalkHistoryPage(WalkHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }
}
