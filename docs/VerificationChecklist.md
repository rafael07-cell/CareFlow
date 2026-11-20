# Verification Checklist - CareFlow

Automated tests: `dotnet test tests\CareFlow.Tests` ran on 2026-11-19 with **11 of 11 passing**
(test database `careflow_test`, created from the real EF Core migrations).
Screenshots are in `docs/screenshots/`.

| ID | Scenario | How it was checked | Status |
|---|---|---|---|
| AC-01 | View services for a selected date; remaining capacity from persisted data | Test `AC01_Availability_IsCalculatedFromPersistedBookings`. UI: Availability screen showed full capacity before booking (`01-availability-before.png`) and reduced capacity after (`04-availability-after.png`) | Verified |
| AC-02 | Booking with multiple available services records one booking with every service | Test `AC02_MultiServiceBooking_RecordsOneBookingWithEverySelectedService`. UI: "Booking #1 confirmed" (`03-booking-confirmed.png`). Database: Bookings and BookingItems rows (`10-...`, `11-...`) | Verified |
| AC-03 | Fully booked service or invalid input is rejected, nothing saved | Tests `AC03_FullService_RejectsTheWholeRequest_NothingIsSaved`, `AC03_NoServicesSelected_IsRejected`, `AC03_MoreThanThreeServices_IsRejected`, `AC03_DuplicateService_IsRejected`, `AC03_PastDate_IsRejected`, `AC03_UnknownPatient_IsRejected`. UI: red "Fully booked ... No part of this booking was recorded." message (`05-fully-booked-rejected.png`) | Verified |
| AC-04 | Stale availability is revalidated and overbooking is prevented | Test `AC04_StaleAvailability_IsRevalidated_AndOverbookingIsBlocked`: availability read, capacity then filled, submit rejected, row counts unchanged. Not reproduced in the UI (controlled test scenario, as the task allows) | Verified (test) |
| AC-05 | Database failure before commit leaves no partial booking or capacity change | Test `AC05_DatabaseFailureBeforeCommit_LeavesNoPartialBooking`: writes happen inside the transaction, the commit is forced to fail, then 0 bookings, 0 items and unchanged capacity are asserted. Source: `BookingService.CreateBookingAsync` (try/catch returns a failed result) and `EfBookingUnitOfWork` (transaction boundary) | Verified (test and source) |
| AC-06 | Bookings remain after closing and restarting the application | Test `AC06_ConfirmedBookings_AreReadableFromBrandNewServiceInstances`. Manual: app closed and reopened, bookings #1 and #2 and their services were still listed (`07-restart-bookings-persist.png`) | Verified |

## Manual UI checks

- [x] Availability screen shows full capacity on an empty day (`01-availability-before.png`)
- [x] Confirm button is disabled until a patient, a valid date, and 1 to 3 services are chosen (`02-confirm-disabled.png`)
- [x] Booking with 3 services shows a "confirmed" message, and Availability decreases by one for each (`03`, `04`)
- [x] Booking a full service shows a red "Fully booked" message and no capacity changes (`05`)
- [x] Bookings screen shows the patient, date, and services in the detail panel (`06-bookings-detail.png`)
- [x] Restart check (AC-06) (`07-restart-bookings-persist.png`)
- [x] Data confirmed in PostgreSQL: 2 rows in Bookings, 4 rows in BookingItems (`10`, `11`)

## Incomplete or not covered

- AC-04 and AC-05 were verified with controlled tests and source evidence, not by causing a failure through the UI.
- No concurrent multi-user simulation was done (not required by the task).
- pgAdmin shows `CreatedAtUtc` in local time (UTC+8); the app shows it in UTC.
