# TaxManager

Applicazione desktop (Windows/Linux/macOS via **Avalonia**) per la **stima delle imposte annue italiane**, rivolta a:

- **Lavoratori dipendenti privati**
- **Lavoratori dipendenti pubblici**
- **Titolari di Partita IVA in regime forfettario**

L'utente inserisce i propri dati e ottiene un **prospetto dettagliato e auditabile** (voce per voce), con riepilogo di netto annuo, imposte, contributi e pressione fiscale. I calcoli vengono salvati in uno **storico** ed è possibile salvare **profili** riutilizzabili ed **esportare** il prospetto.

> ⚠️ **Avviso**: i risultati sono una **stima semplificata** a scopo informativo e **non sostituiscono** il calcolo di un commercialista o del CAF. Aliquote e soglie vanno sempre verificate sulle fonti ufficiali (Agenzia delle Entrate, INPS) per l'anno di riferimento.

---

## Architettura

Soluzione `.slnx` a strati (Clean Architecture):

```
src/
  TaxManager.Domain          # Dominio puro: regole fiscali come DATI per anno, calcolatori, esito auditabile
  TaxManager.Application      # Casi d'uso + astrazioni di persistenza (interfacce)
  TaxManager.Infrastructure   # EF Core SQLite + store JSON su disco + caricatore ruleset
  TaxManager.App             # UI Avalonia (MVVM con CommunityToolkit.Mvvm)
tests/
  TaxManager.Domain.Tests    # xUnit: engine, mapping, caricamento ruleset, non-divergenza JSON↔codice
data/
  rulesets/2024.json, 2025.json   # Parametri fiscali editabili (sorgente di verità a runtime)
```

Principi chiave:

- **`decimal` per il denaro** ovunque; arrotondamenti centralizzati in `Domain.Common.Money` (all'euro per le imposte).
- **Regole come dati versionati per anno** (`TaxYearRuleset`): caricabili da JSON, con fallback in codice (`SampleRulesets`). I metodi di calcolo vivono sui tipi, così un ruleset deserializzato conserva la propria logica.
- **Esito = scomposizione passo-passo** (`TaxResult` con voci `TaxLineItem`), non un solo numero.
- **Dominio senza dipendenze esterne**; la persistenza è solo dietro le astrazioni di `Application`.

## Cosa calcola il motore

**Dipendenti (privato/pubblico):** contributi previdenziali a carico del lavoratore (9,19% privato / 8,80% pubblico, con +1% oltre la prima fascia), imponibile IRPEF, IRPEF a scaglioni (23/35/43%), detrazione da lavoro dipendente, addizionali regionale/comunale, trattamento integrativo e — **dal 2025** — il **cuneo fiscale** (somma integrativa non imponibile + ulteriore detrazione per redditi medi).

**Forfettario:** reddito forfettario (ricavi × coefficiente di redditività per gruppo ATECO), contributi previdenziali deducibili (Gestione Separata / artigiani / commercianti con riduzione 35% / cassa professionale), **imposta sostitutiva** 15% (o 5% start-up). Nessuna IRPEF/addizionale/IVA.

### Addizionali per residenza (tutti i comuni)
L'addizionale **regionale** (a scaglioni dove previsto) e quella **comunale** non si inseriscono a mano: si scelgono **Regione → Provincia → Comune**. I dati coprono tutte le regioni/province e ~7.900 comuni italiani, con aliquote a scaglioni e soglie di esenzione. `data/addizionali/comuni-<anno>.json` è **generato** da `tools/AddizionaliImporter` (elenco comuni + tabella MEF) — vedi `tools/README.md`. Per i rari comuni non in elenco resta l'inserimento manuale dell'aliquota.

### Semplificazioni note (vedi anche le `Note` nel prospetto)
Non sono modellati nel dettaglio: conguagli, familiari a carico (assegno unico), agevolazioni/detrazioni regionali condizionate, esonero contributivo 2024, condizioni puntuali del bonus/trattamento di fascia B e dei requisiti start-up. I valori dei ruleset sono frutto di ricerca verificata ma **da confermare sulle fonti ufficiali**.

## Persistenza

- **Database relazionale (SQLite, EF Core):** storico dei calcoli in `%APPDATA%/TaxManager/taxmanager.db`.
- **Disco (JSON):** profili contribuente in `%APPDATA%/TaxManager/profiles.json`; export del prospetto in TXT/JSON nel percorso scelto.

## Requisiti

- .NET SDK **10.0.x**

## Build / Test / Avvio

```powershell
# dalla root del repo
dotnet build TaxManager.slnx
dotnet test  tests/TaxManager.Domain.Tests/TaxManager.Domain.Tests.csproj
dotnet run   --project src/TaxManager.App
```

## Distribuzione (publish self-contained)

Eseguibile unico Windows, senza .NET preinstallato:

```powershell
dotnet publish src/TaxManager.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true
```

Output in `src/TaxManager.App/bin/Release/net10.0/win-x64/publish/`: `TaxManager.exe` (~150 MB) con accanto le cartelle `rulesets/` e `addizionali/`. L'icona e il logo (`Assets/logo.ico`, `logo.png`) sono generati da `tools/IconGenerator` (vedi `tools/README.md`).

## Aggiornare i parametri di un anno

1. Modificare/creare `data/rulesets/<anno>.json`.
2. Allineare il metodo corrispondente in `src/TaxManager.Domain/SampleRulesets.cs` (un test verifica che JSON e codice diano gli stessi risultati).
3. `dotnet test`.
