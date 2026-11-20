using CareFlow.Application.Interfaces;
using CareFlow.Application.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CareFlow.Desktop.ViewModels;

public partial class AvailabilityViewModel : ObservableObject
{
    private readonly IBookingService _service;

    public ObservableCollection<ServiceAvailability> Items { get; } = new();

    [ObservableProperty]
    private DateTimeOffset selectedDate = DateTimeOffset.Now;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    public AvailabilityViewModel(IBookingService service) => _service = service;

    partial void OnSelectedDateChanged(DateTimeOffset value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        try
        {
            var date = DateOnly.FromDateTime(SelectedDate.Date);
            var list = await _service.GetAvailabilityAsync(date);

            Items.Clear();
            foreach (var item in list)
                Items.Add(item);

            StatusMessage = $"Showing availability for {date:MMM d, yyyy}.";
            IsError = false;
        }
        catch (Exception)
        {
            StatusMessage = "Could not load availability from the database.";
            IsError = true;
        }
    }
}
