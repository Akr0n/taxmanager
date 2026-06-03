using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.App.ViewModels;
using TaxManager.App.Views;
using TaxManager.Application;
using TaxManager.Infrastructure;

namespace TaxManager.App;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        collection.AddTaxManagerApplication();
        collection.AddTaxManagerInfrastructure();
        collection.AddSingleton<MainWindowViewModel>();

        _services = collection.BuildServiceProvider();

        // Crea il DB SQLite e lo schema se non esistono.
        _services.EnsureTaxManagerDatabaseCreated();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            desktop.ShutdownRequested += (_, _) => _services.Dispose();
            _ = vm.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
