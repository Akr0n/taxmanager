# Definizione di "fatto"

Prima di considerare conclusa una modifica:
1. `dotnet build TaxManager.slnx` → 0 errori, 0 warning.
2. `dotnet test tests/TaxManager.Domain.Tests/TaxManager.Domain.Tests.csproj` → tutti verdi. (Il build dei test compila anche App, quindi valida i compiled bindings XAML.)
3. Se sono stati toccati i parametri fiscali: aggiornare SIA `data/rulesets/<anno>.json` SIA `SampleRulesets` (il test di non-divergenza fallisce altrimenti).
4. Per modifiche a UI/avvio: smoke test avviando `TaxManager.exe` (deve restare attivo qualche secondo e creare/usare `%APPDATA%\TaxManager`).

NON eseguire comandi git (li gestisce l'utente).