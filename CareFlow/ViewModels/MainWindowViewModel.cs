using System.Collections.ObjectModel;
using CareFlow.Business;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IBookingService _bookings;
    private readonly IDatabaseInitializer _initializer;

    public MainWindowViewModel(IBookingService bookings, IDatabaseInitializer initializer, IClock clock)
    {
        _bookings = bookings;
        _initializer = initializer;
        _bookingDate = new DateTimeOffset(clock.Today.ToDateTime(TimeOnly.MinValue)); // field: no change event yet
    }

    public ObservableCollection<PatientItem> Patients { get; } = new();
    public ObservableCollection<ServiceOptionViewModel> Services { get; } = new();
    public ObservableCollection<BookingSummary> Bookings { get; } = new();

    [ObservableProperty] private PatientItem? _selectedPatient;
    [ObservableProperty] private DateTimeOffset? _bookingDate;
    [ObservableProperty] private BookingSummary? _selectedBooking;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _isError;

    [NotifyCanExecuteChangedFor(nameof(SubmitBookingCommand))]
    [ObservableProperty] private bool _isBusy;

    partial void OnBookingDateChanged(DateTimeOffset? value) => _ = RefreshAvailabilityAsync();

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await _initializer.InitializeAsync();
            foreach (var p in await _bookings.GetPatientsAsync()) Patients.Add(p);
            await RefreshAvailabilityAsync();
            await LoadBookingsAsync();
        }
        catch (Exception)
        {
            SetStatus("The database could not be opened. Check the README and run the migrations.", true);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RefreshAvailabilityAsync()
    {
        if (BookingDate is null) return;
        var date = DateOnly.FromDateTime(BookingDate.Value.DateTime);
        try
        {
            var list = await _bookings.GetAvailabilityAsync(date);
            var selected = Services.Where(s => s.IsSelected).Select(s => s.ServiceId).ToHashSet();
            Services.Clear();
            foreach (var a in list)
                Services.Add(new ServiceOptionViewModel(a) { IsSelected = selected.Contains(a.ServiceId) });
        }
        catch (Exception) { SetStatus("Could not load service availability.", true); }
    }

    [RelayCommand]
    private async Task LoadBookingsAsync()
    {
        try
        {
            var list = await _bookings.GetBookingsAsync();
            Bookings.Clear();
            foreach (var b in list) Bookings.Add(b);
        }
        catch (Exception) { SetStatus("Could not load bookings.", true); }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitBookingAsync()
    {
        if (SelectedPatient is null) { SetStatus("Please select a patient.", true); return; }
        if (BookingDate is null)     { SetStatus("Please choose a booking date.", true); return; }

        var date = DateOnly.FromDateTime(BookingDate.Value.DateTime);
        var ids = Services.Where(s => s.IsSelected).Select(s => s.ServiceId).ToList();

        IsBusy = true;
        try
        {
            var result = await _bookings.CreateBookingAsync(SelectedPatient.Id, date, ids);
            if (result.Success)
            {
                foreach (var s in Services) s.IsSelected = false;
                SetStatus($"Booking #{result.BookingId} confirmed.", false);
                await LoadBookingsAsync();
            }
            else
            {
                SetStatus(string.Join(" ", result.Errors), true);
            }
            await RefreshAvailabilityAsync();   // show the latest persisted capacity either way
        }
        finally { IsBusy = false; }
    }

    private bool CanSubmit() => !IsBusy;

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }
}
