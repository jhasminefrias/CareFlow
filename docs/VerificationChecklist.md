# CareFlow - Verification Checklist

Fill in **Status**, **Date**, and **Verified by** after you actually run each check.
Unit tests run the real service against a real (in-memory) SQLite database, not mocks.

| ID | Scenario | How to verify | Status | Date | Verified by |
|---|---|---|---|---|---|
| AC-01 | Availability from persisted data | Test `Multi_service_booking_is_saved_and_reduces_capacity`; manually: book, switch dates, compare counts | | | |
| AC-02 | Multi-service booking saved | Same test; manually: book CBC+Urinalysis+ECG, check **Confirmed Bookings** tab | | | |
| AC-03 | Full service / invalid input rejected | Tests `Full_service_rejects_entire_booking`, `Invalid_service_selection_is_rejected`, `Past_date_is_rejected`; manually: book ECG twice, third request with ECG fails | | | |
| AC-04 | Stale availability re-checked | Test `Stale_availability_is_rechecked_on_confirm` | | | |
| AC-05 | DB failure leaves no partial data | Test `Failure_mid_transaction_leaves_nothing_behind`; source: `BookingService.CreateBookingAsync` transaction boundary | | | |
| AC-06 | Persistence after restart | Create booking, close app, reopen, check **Confirmed Bookings** | | | |

Run tests: `dotnet test`

Incomplete items / notes:
- 
