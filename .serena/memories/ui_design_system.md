# UI design system (GitHub-dark, stile "dbmigrator")

La GUI Avalonia adotta lo stesso standard visivo del progetto **dbmigrator** (`D:/_repositories/dbmigrator`).

- `App.axaml`: `RequestedThemeVariant="Dark"`, FluentTheme + DataGrid Fluent + converter `EnumLabel`, e il DESIGN SYSTEM come stili globali (`<Application.Styles>`).
- Palette: page `#0d1117`, surface/card `#161b22`, alt `#1c2128`, border `#30363d`, testo `#c9d1d9`, testo forte `#e6edf3`, muted `#8b949e`, accent `#58a6ff`, primary `#1f6feb`, secondary `#21262d`, success `#3fb950`, danger `#f85149`, attention `#d29922`.
- Classi: `Border.card/.statCard/.altPanel/.warning/.chrome`, `TextBlock.pageTitle/.sectionTitle/.cardTitle/.fieldLabel/.muted/.body/.warningText/.accent`, `Button.primary/.secondary`; input (TextBox/ComboBox/NumericUpDown/AutoCompleteBox) e Tab/DataGrid stilizzati globalmente. NB: esiste un reset per il TextBox interno di NumericUpDown e AutoCompleteBox (`/template/ TextBox#PART_TextBox`) per evitare il doppio bordo.
- Layout `MainWindow`: header con logo+titolo (`Border.chrome`), `TabControl` (Calcolo/Storico/Profili/Informazioni), status bar; sezioni in `Border.card`. La ricerca del comune usa `AutoCompleteBox` (type-ahead) con fallback "Altro comune".
- Logo/icona: generati da `tools/IconGenerator` (SkiaSharp) → `src/TaxManager.App/Assets/logo.ico` (multi-size) e `logo.png`; badge accent con `€` e accento tricolore. `Window.Icon` = `avares://TaxManager/Assets/logo.ico`; `<ApplicationIcon>` nel csproj; header usa `logo.png`.

REGOLA: nuove view/controlli usano le classi e la palette qui sopra; niente colori chiari/stili ad-hoc. MVVM con CommunityToolkit.Mvvm + compiled bindings (DataGrid/ComboBox-enum/AutoCompleteBox con `x:CompileBindings=False`). Vedi `mem:core`, `mem:addizionali`.