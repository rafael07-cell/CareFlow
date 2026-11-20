using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CareFlow.Application.Interfaces;
using CareFlow.Application.Services;
using CareFlow.Desktop.ViewModels;
using CareFlow.Desktop.Views;
using CareFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace CareFlow.Desktop;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Persistence (PostgreSQL + EF Core) is registered inside the Infrastructure layer.
        services.AddPersistence();

        services.AddSingleton(TimeProvider.System);
        services.AddTransient<IBookingService, BookingService>();

        services.AddSingleton<AvailabilityViewModel>();
        services.AddSingleton<NewBookingViewModel>();
        services.AddSingleton<BookingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        // Apply pending migrations and seed sample data (only if the database is empty).
        Task.Run(() => Services.InitializeDatabaseAsync()).GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

