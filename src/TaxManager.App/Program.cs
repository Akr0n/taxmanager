using System;
using System.Globalization;
using Avalonia;

namespace TaxManager.App;

internal static class Program
{
    // Punto di ingresso. Inizializzazione minima e avvio del lifetime desktop.
    [STAThread]
    public static void Main(string[] args)
    {
        // L'app è rivolta a utenti italiani: formattazione di valuta/date in it-IT.
        var it = CultureInfo.GetCultureInfo("it-IT");
        CultureInfo.DefaultThreadCurrentCulture = it;
        CultureInfo.DefaultThreadCurrentUICulture = it;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
