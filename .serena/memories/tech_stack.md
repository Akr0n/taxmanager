# Stack tecnologico

- .NET 10 (`net10.0`), C# LangVersion latest. Nullable + ImplicitUsings abilitati a livello di soluzione via `Directory.Build.props`.
- Central Package Management: TUTTE le versioni in `Directory.Packages.props` (NON mettere `Version` nei singoli .csproj).
- UI: Avalonia 12.0.4 (Desktop, Themes.Fluent, Fonts.Inter; Controls.DataGrid 12.0.0 = ultima pubblicata su NuGet; Avalonia.Diagnostics 11.3.17 solo in Debug, niente 12.x ma convive col core 12.x perche' richiede solo Avalonia >= 11.3.17). SkiaSharp 3.119.4 (richiesto da Avalonia.Skia 12). Compiled bindings ON (`AvaloniaUseCompiledBindingsByDefault=true`); i binding interni ai `DataGrid` usano `x:CompileBindings="False"`, così come le `ComboBox` con item-template su enum.
- MVVM: CommunityToolkit.Mvvm 8.4.2 (`[ObservableProperty]`, `[RelayCommand]`). I ViewModel sono `partial`.
- Persistenza relazionale: Microsoft.EntityFrameworkCore.Sqlite 10.0.8 (via `IDbContextFactory`, schema con `EnsureCreated`, niente migrazioni).
- DI: Microsoft.Extensions.DependencyInjection 10.0.8.
- Test: xUnit 2.9.3 (no FluentAssertions per evitare il cambio di licenza).
- Soluzione in formato `.slnx`. SDK atteso 10.0.x (presente anche WindowsDesktop runtime).