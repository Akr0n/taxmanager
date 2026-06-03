# tools — generazione dati addizionali

`data/addizionali/comuni-<anno>.json` (tutti i ~7.900 comuni italiani con addizionale comunale a
scaglioni + soglia di esenzione) è **generato**, non scritto a mano, da `AddizionaliImporter`.

## Fonti dati (in questa cartella)

- `comuni-raw.json` — elenco ufficiale dei comuni italiani (regione, provincia, sigla, codice catastale).
  Fonte: dataset pubblico [matteocontrini/comuni-json](https://github.com/matteocontrini/comuni-json)
  (derivato dai codici ISTAT). 7.904 comuni.
- `mef-2024.csv`, `mef-2025.csv` — tabella ufficiale delle aliquote dell'addizionale comunale IRPEF.
  Fonte: MEF — Dipartimento delle Finanze, "Addizionale comunale all'IRPEF — Elenchi generali"
  (`download.php?anno=<anno>`). Formato `;`, chiave `CODICE_CATASTALE`, aliquote per fascia
  (`ALIQUOTA_n`/`FASCIA_n`) e `IMPORTO_ESENTE`.
- `mef-reg-2024.csv`, `mef-reg-2025.csv` — tabella ufficiale delle aliquote dell'addizionale
  REGIONALE IRPEF. Fonte: MEF — "Addizionale regionale all'IRPEF — Elenchi generali"
  (`download.php?tipo=reg&anno=<anno>`). Una riga per scaglione: `REGIONE`, `ALIQUOTA`, `FASCIA`.

## Rigenerare i dati

```powershell
# (ri)scaricare le fonti se necessario, poi:
dotnet run --project tools/AddizionaliImporter -c Release
```

L'importer unisce i due dataset sul **codice catastale**, normalizza i nomi regione
(Trentino-Alto Adige → "Provincia Autonoma di Trento/Bolzano"; Valle d'Aosta), trasforma le fasce
MEF in scaglioni (`ProgressiveBracket`) e scrive `data/addizionali/comuni-2024.json` e
`comuni-2025.json`. Anche le addizionali **regionali** (`regioni-<anno>.json`) sono generate dallo
stesso importer dalla tabella MEF regionale (`mef-reg-<anno>.csv`): i nomi regione vengono mappati
a quelli usati nei comuni (Trentino → PA Trento/Bolzano; Friuli-Venezia Giulia; Valle d'Aosta).

> I file CSV/JSON sorgente sono inclusi per riproducibilità offline; gli URL sopra permettono di
> aggiornarli per gli anni futuri.

---

## IconGenerator — logo e icona

`tools/IconGenerator` (SkiaSharp) genera `src/TaxManager.App/Assets/logo.ico` (multi-size,
PNG-embedded: 16/24/32/48/64/128/256) e `logo.png` 256. Design: badge arrotondato con gradiente
accent (`#1f6feb`→`#58a6ff`), simbolo `€` bianco centrato otticamente, bordo hairline `#30363d`
e accento tricolore verticale (size-gated ≥48px).

```powershell
dotnet run --project tools/IconGenerator -c Release
```

