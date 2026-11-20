using CareFlow.Application.Interfaces;
using CareFlow.Application.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CareFlow.Desktop.ViewModels;

public partial class NewBookingViewModel : ObservableObject
{
    private readonly IBookingService _service;

    public ObservableCollection<PatientItem> Patients { get; } = new();
    public ObservableCollection<ServiceChoiceViewModel> Services { get; } = new();

    [ObservableProperty]
    private PatientItem? selectedPatient;

    [ObservableProperty]
    private DateTimeOffset bookingDate = DateTimeOffset.Now;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool isBusy;

    [ObservableProperty]
    private string? validationMessage;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isError;

    public NewBookingViewModel(IBookingService service)
    {
        _service = service;
        ValidationMessage = ComputeValidation();
    }

    partial void OnSelectedPatientChanged(PatientItem? value) => UpdateValidation();

    partial void OnBookingDateChanged(DateTimeOffset value)
    {
        UpdateValidation();
        _ = RefreshAvailabilityAsync();
    }

    private string? ComputeValidation()
    {
        if (SelectedPatient is null) return "Select a patient.";
        if (BookingDate.Date < DateTime.Today) return "The booking date cannot be earlier than today.";
        var count = Services.Count(s => s.IsSelected);
        if (count < 1) return "Select at least 1 service.";
        if (count > 3) return "Select no more than 3 services.";
        return null;
    }

    private void UpdateValidation()
    {
        ValidationMessage = ComputeValidation();
        SubmitCommand.NotifyCanExecuteChanged();
    }

    private bool CanSubmit() => !IsBusy && ComputeValidation() is null;

    public async Task LoadAsync()
    {
        try
        {
            if (Patients.Count == 0)
            {
                foreach (var p in await _service.GetPatientsAsync())
                    Patients.Add(p);
            }
            await RefreshAvailabilityAsync();
        }
        catch (Exception)
        {
            Report("Could not load data from the database.", true);
        }
    }

    private async Task RefreshAvailabilityAsync()
    {
        try
        {
            var date = DateOnly.FromDateTime(BookingDate.Date);
            var list = await _service.GetAvailabilityAsync(date);

            foreach (var a in list)
            {
                var existing = Services.FirstOrDefault(s => s.ServiceId == a.ServiceId);
                if (existing is null)
                {
                    var choice = new ServiceChoiceViewModel(a.ServiceId, a.Name, a.Remaining);
                    choice.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(ServiceChoiceViewModel.IsSelected))
                            UpdateValidation();
                    };
                    Services.Add(choice);
                }
                else
                {
                    existing.Remaining = a.Remaining;
                }
            }
            UpdateValidation();
        }
        catch (Exception)
        {
            Report("Could not load availability from the database.", true);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        IsBusy = true;
        try
        {
            var ids = Services.Where(s => s.IsSelected).Select(s => s.ServiceId).ToList();
            var request = new CreateBookingRequest(
                SelectedPatient!.Id, DateOnly.FromDateTime(BookingDate.Date), ids);

            var result = await _service.CreateBookingAsync(request);

            if (result.Success)
            {
                foreach (var s in Services) s.IsSelected = false;
                Report(result.Message, false);
            }
            else
            {
                Report(result.Message, true);
            }
        }
        finally
        {
            IsBusy = false;
        }

        // Always show the real, current capacity after an attempt (also after a rejection).
        await RefreshAvailabilityAsync();
    }

    private void Report(string message, bool error)
    {
        StatusMessage = message;
        IsError = error;
    }
}
