using CareFlow.Business;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CareFlow.ViewModels;

public partial class ServiceOptionViewModel : ObservableObject
{
    public ServiceOptionViewModel(ServiceAvailability a) => Availability = a;

    public ServiceAvailability Availability { get; }
    public int ServiceId => Availability.ServiceId;
    public string Label => Availability.Remaining > 0
        ? $"{Availability.Name}  ({Availability.Remaining} of {Availability.DailyCapacity} slots left)"
        : $"{Availability.Name}  (FULL)";

    [ObservableProperty] private bool _isSelected;
}
