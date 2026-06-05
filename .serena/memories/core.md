# TaxManager — mappa del progetto

App desktop (Avalonia) per la stima delle imposte annue italiane: dipendenti privati, dipendenti pubblici, P.IVA forfettaria.

Architettura a strati (Clean Architecture), soluzione `TaxManager.slnx`:
- `src/TaxManager.Domain` — dominio puro, zero dipendenze esterne. Regole come DATI versionati per anno (`Rules/`), input (`Inputs/`), esito auditabile (`Results/TaxResult`), calcolatori (`Engine/`). `SampleRulesets.cs` = ruleset 2024/2025 in codice (fallback + base dei test). Namespace per cartella (`TaxManager.Domain.Rules`, `.Inputs`, `.Engine`, `.Results`, `.Common`); enum di categoria in `TaxManager.Domain`.
- `src/TaxManager.Application` — casi d'uso. `ITaxCalculationService`/`TaxCalculationService`, astrazioni di persistenza (`Abstractions/`: IRulesetProvider, ITaxComputationRepository, IProfileStore, IReportExporter), modelli persistiti (`Models/`: SavedProfile, ComputationRecord), `ProfileMapper`, `ComputationRecordFactory`. DI: `AddTaxManagerApplication`.
- `src/TaxManager.Infrastructure` — EF Core SQLite (storico calcoli, `Persistence/`), store profili JSON su disco, esportatore prospetto (TXT/JSON), `JsonRulesetProvider` (carica `data/rulesets/*.json`, fallback su SampleRulesets, file malformati ignorati). DI: `AddTaxManagerInfrastructure` + `EnsureTaxManagerDatabaseCreated`.
- `src/TaxManager.App` — Avalonia 12 MVVM. `Program`→`App`(DI)→`MainWindow` (tab Calcolo/Storico/Profili/Informazioni). ViewModels in `ViewModels/`, viste in `Views/`, helper in `Support/`.
- `tests/TaxManager.Domain.Tests` — xUnit; copre engine, mapper, caricamento JSON e NON-divergenza JSON↔codice.
- `data/rulesets/2024.json`,`2025.json` — sorgente editabile dei parametri fiscali (copiati nell'output di App e dei test).

Invarianti:
- Denaro SEMPRE `decimal`; arrotondamenti solo in `Domain.Common.Money` (Euro per le imposte, Cents per il resto).
- I parametri fiscali sono STIME verificate (ricerca 2024/2025) da confermare sulle fonti ufficiali; il 2025 include il cuneo fiscale (somma integrativa + ulteriore detrazione).
- Persistenza: storico su SQLite (`%APPDATA%/TaxManager/taxmanager.db`), profili su disco (`%APPDATA%/TaxManager/profiles.json`).

Vedi `mem:tech_stack`, `mem:conventions`, `mem:suggested_commands`, `mem:task_completion`.