using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CareFlow.Business;
using CareFlow.Data;
using CareFlow.ViewModels;
using CareFlow.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow;

public partial class App : Application
{
    private ServiceProvider? _provider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<CareFlowDbContext>(o => o.UseSqlite(DatabasePaths.ConnectionString));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IBookingService, BookingService>();
        services.AddSingleton<MainWindowViewModel>();
        _provider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _provider.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            _ = vm.InitializeAsync();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
