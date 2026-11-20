using CareFlow.Application.Interfaces;
using CareFlow.Application.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CareFlow.Desktop.ViewModels;

public partial class BookingsViewModel : ObservableObject
{
    private readonly IBookingService _service;

    public ObservableCollection<BookingSummary> Bookings { get; } = new();

    [ObservableProperty]
    private BookingSummary? selectedBooking;

    [ObservableProperty]
    private BookingDetail? detail;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    public BookingsViewModel(IBookingService service) => _service = service;

    partial void OnSelectedBookingChanged(BookingSummary? value) => _ = LoadDetailAsync(value);

    public async Task LoadAsync()
    {
        try
        {
            var list = await _service.GetBookingsAsync();
            Bookings.Clear();
            foreach (var b in list)
                Bookings.Add(b);

            StatusMessage = list.Count == 0
                ? "No confirmed bookings yet."
                : $"{list.Count} confirmed booking(s).";
            IsError = false;
        }
        catch (Exception)
        {
            StatusMessage = "Could not load bookings from the database.";
            IsError = true;
        }
    }

    private async Task LoadDetailAsync(BookingSummary? summary)
    {
        try
        {
            Detail = summary is null ? null : await _service.GetBookingDetailAsync(summary.Id);
        }
        catch (Exception)
        {
            Detail = null;
            StatusMessage = "Could not load the booking details.";
            IsError = true;
        }
    }
}
