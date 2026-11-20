# CareFlow - Diagnostic Booking Management

ITSD81 Performance Task 4. A desktop application for recording diagnostic bookings for existing patients.
A booking can contain 1 to 3 services for one date, and it is saved **all-or-nothing**: if any requested
service is fully booked, nothing is recorded.

**Team (pair):** Rafael Sofian Guipetacio and Deniele Bern M. Miasco

## Tech stack

C# / .NET 10, Avalonia UI 12 + XAML, MVVM (CommunityToolkit.Mvvm), Entity Framework Core 10,
PostgreSQL (Npgsql), dependency injection, EF Core migrations, xUnit.

## Requirements

- .NET 10 SDK
- PostgreSQL running locally on `localhost:5432` with the default `postgres` user
- The EF Core CLI tool: `dotnet tool install --global dotnet-ef`

## Setup and run

1. **Set the database password** as an environment variable (nothing is stored in the repository).
   In PowerShell, for the current window:
   ```powershell
   $env:CAREFLOW_DB_PASSWORD = "your_postgres_password"
   ```
   To keep it for future windows: `setx CAREFLOW_DB_PASSWORD "your_postgres_password"` (then open a new window).

2. **Apply the migrations** (creates the `CareFlow` database and its tables):
   ```powershell
   dotnet ef database update -p src\CareFlow.Infrastructure -s CareFlow.Desktop
   ```
   The application also applies any pending migrations automatically when it starts.

3. **Sample data.** On first start, if the database is empty, the app seeds fictional data:
   4 patients and 4 services with daily capacities: Complete Blood Count (3), Urinalysis (3), Chest X-Ray (2), ECG (2).
   No manual seeding is needed.

4. **Run the application:**
   ```powershell
   dotnet run --project CareFlow.Desktop
   ```

5. **Run the automated tests** (they use a separate database, `careflow_test`, which is dropped and recreated on each run):
   ```powershell
   dotnet test tests\CareFlow.Tests --logger "console;verbosity=detailed"
   ```

## Using the application

- **Availability:** choose a date to see each service's remaining slots (calculated from saved bookings).
- **New Booking:** choose a patient, a date, and 1 to 3 services, then Confirm. Messages explain any problem.
- **Bookings:** list of confirmed bookings; select one to see the patient, date, and services.

## Architecture

- Views and ViewModels only call `IBookingService`. They never touch the `DbContext`.
- `BookingService` (Application layer) holds the rules and coordinates the transaction.
- `EfBookingUnitOfWork` (Infrastructure) opens one database transaction (Serializable isolation) and
  exposes the data operations. Leaving it without `CommitAsync` rolls everything back.

### Project structure

```text
CareFlow.Desktop         Avalonia Views + ViewModels (no EF Core, no business rules)
src/CareFlow.Application Business rules, BookingService, interfaces, DTOs
src/CareFlow.Domain      Entities: Patient, DiagnosticService, Booking, BookingItem
src/CareFlow.Infrastructure  EF Core DbContext, migrations, seeding, transaction (unit of work), queries
tests/CareFlow.Tests     xUnit tests for the acceptance scenarios
```

### Where each business rule is enforced

| Rule | Enforced in |
|---|---|
| BR-01 existing patient | `BookingService` (inside the transaction) + foreign key |
| BR-02 one to three services | `BookingService` + form validation |
| BR-03 date not in the past | `BookingService` + form validation |
| BR-04 / BR-05 capacity | `BookingService`, counted from saved bookings |
| BR-06 no duplicate service | `BookingService` + unique index on (BookingId, ServiceId) |
| BR-07 commit together or none | One transaction, single commit, rollback on any failure |
| BR-08 recheck on confirm | Capacity is recounted **inside the transaction** at confirmation time |
| BR-09 persistence | PostgreSQL |
| BR-10 understandable feedback | `BookingResult` messages shown in the UI; failures are never shown as success |

## Documentation

- Relational design: [docs/RelationalDesign.md](docs/RelationalDesign.md)
- Verification checklist: [docs/VerificationChecklist.md](docs/VerificationChecklist.md)
- Screenshots (verification evidence): [docs/screenshots](docs/screenshots)

## Security

All patient data is fictional. No passwords or credentials are stored in the repository.

## AI assistance acknowledgment

An AI assistant (Claude by Anthropic) was used only for the following:

- **Coding:** helped write and debug parts of the source code and automated tests.
- **Architecture verification:** helped check that the layered design (Views/ViewModels, Application, Domain, Infrastructure) and the transaction boundary meet the task requirements.
- **Progress tracking:** helped keep a checklist of the deliverables and acceptance scenarios.

The team ran, tested, and reviewed the application and is responsible for understanding it.
