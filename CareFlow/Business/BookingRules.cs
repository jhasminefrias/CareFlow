namespace CareFlow.Business;

/// <summary>Pure business rules - no UI, no database. Covers BR-02, BR-03, BR-06.</summary>
public static class BookingRules
{
    public const int MinServices = 1;
    public const int MaxServices = 3;

    public static IReadOnlyList<string> ValidateRequest(DateOnly date, DateOnly today, IReadOnlyCollection<int> serviceIds)
    {
        var errors = new List<string>();

        if (date < today)
            errors.Add("The booking date cannot be earlier than today.");                       // BR-03

        if (serviceIds.Count < MinServices || serviceIds.Count > MaxServices)
            errors.Add($"Select between {MinServices} and {MaxServices} diagnostic services.");  // BR-02

        if (serviceIds.Distinct().Count() != serviceIds.Count)
            errors.Add("The same service cannot be selected more than once.");                   // BR-06

        return errors;
    }
}
