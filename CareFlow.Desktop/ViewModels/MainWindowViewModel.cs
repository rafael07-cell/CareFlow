using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace CareFlow.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public AvailabilityViewModel AvailabilityViewModel { get; }
    public NewBookingViewModel NewBookingViewModel { get; }
    public BookingsViewModel BookingsViewModel { get; }

    [ObservableProperty]
    private object? currentView;

    public MainWindowViewModel(
        AvailabilityViewModel availabilityViewModel,
        NewBookingViewModel newBookingViewModel,
        BookingsViewModel bookingsViewModel)
    {
        AvailabilityViewModel = availabilityViewModel;
        NewBookingViewModel = newBookingViewModel;
        BookingsViewModel = bookingsViewModel;

        CurrentView = AvailabilityViewModel;
        _ = AvailabilityViewModel.LoadAsync();
    }

    [RelayCommand]
    private async Task ShowAvailabilityAsync()
    {
        await AvailabilityViewModel.LoadAsync();
        CurrentView = AvailabilityViewModel;
    }

    [RelayCommand]
    private async Task ShowNewBookingAsync()
    {
        await NewBookingViewModel.LoadAsync();
        CurrentView = NewBookingViewModel;
    }

    [RelayCommand]
    private async Task ShowBookingsAsync()
    {
        await BookingsViewModel.LoadAsync();
        CurrentView = BookingsViewModel;
    }
}
