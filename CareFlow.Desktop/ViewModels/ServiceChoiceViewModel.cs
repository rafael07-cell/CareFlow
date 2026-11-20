using CommunityToolkit.Mvvm.ComponentModel;

namespace CareFlow.Desktop.ViewModels;

public partial class ServiceChoiceViewModel : ObservableObject
{
    public int ServiceId { get; }
    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Label))]
    private int remaining;

    [ObservableProperty]
    private bool isSelected;

    public string Label => Remaining > 0 ? $"{Name}  ({Remaining} left)" : $"{Name}  (FULL)";

    public ServiceChoiceViewModel(int serviceId, string name, int remainingSlots)
    {
        ServiceId = serviceId;
        Name = name;
        Remaining = remainingSlots;
    }
}
