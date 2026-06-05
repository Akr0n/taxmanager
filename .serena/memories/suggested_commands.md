# Comandi (Windows / PowerShell pwsh)

Dalla root `D:\_repositories\taxmanager`:
- Build soluzione: `dotnet build TaxManager.slnx`
- Test: `dotnet test tests/TaxManager.Domain.Tests/TaxManager.Domain.Tests.csproj`
- Avvio app: `dotnet run --project src/TaxManager.App` (o eseguire `src/TaxManager.App/bin/Debug/net10.0/TaxManager.exe`)
- Publish self-contained single-file: `dotnet publish src/TaxManager.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true` → `src/TaxManager.App/bin/Release/net10.0/win-x64/publish/TaxManager.exe` (+ cartelle `rulesets/`, `addizionali/`).
- Rigenerare dati addizionali (comuni + regioni): `dotnet run --project tools/AddizionaliImporter -c Release`
- Rigenerare logo/icona: `dotnet run --project tools/IconGenerator -c Release`
- Versione SDK: `dotnet --version`

Note Windows:
- Dati utente in `%APPDATA%\TaxManager` (DB SQLite + profiles.json). Reset: `Remove-Item "$env:APPDATA\TaxManager" -Recurse -Force`.
- I tool in `tools/` NON sono in `TaxManager.slnx`; SkiaSharp è pinnato in `Directory.Packages.props` a 3.119.4 (richiesto da Avalonia.Skia 12) e usato anche da `tools/IconGenerator`, migrato alle API testo 3.x (SKFont). Versione unica per tutto il repo.
- Per buildare l'app va CHIUSA l'istanza in esecuzione (il file `.exe` resta bloccato): `Get-Process TaxManager | Stop-Process -Force`.
- Versioni NuGet: NuGet flat-container API.
- GIT: lo gestisce l'UTENTE — NON eseguire comandi git. Branch di sviluppo: `develop`.