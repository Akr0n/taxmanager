# Addizionali regionali/comunali + anni fiscali supportati

Le addizionali si ricavano dalla residenza a cascata **Regione → Provincia → Comune** (tutti i ~7.904 comuni). Astrazione `ISurtaxProvider`; modelli `RegionSurtax`/`MunicipalitySurtax` con `ProgressiveSchedule` (scaglioni) + `ExemptionThreshold`; impl. `Infrastructure.Surtaxes.SurtaxJsonProvider` (cache per anno). Engine: regionale/comunale via schedule marginale o flat (fallback manuale); comunale azzerata se imponibile ≤ soglia esenzione (cliff). UI: 3 `AutoCompleteBox` (type-ahead) con auto-correzione su LostFocus (code-behind `EmployeeFormView.axaml.cs`) + voce "Altro comune".

## Anni supportati: 2024, 2025, 2026
- Ruleset statali: `data/rulesets/<anno>.json` + fallback in codice `SampleRulesets.Year2024/2025/2026` (`AvailableYears`). **2026** = Legge di Bilancio 2026 (L. 199/2025): IRPEF centrale **33%** (era 35%), soglie INPS 56.224/122.295, forfettario minimale 18.808. ATTENZIONE: la 3ª banda della detrazione lavoro va codificata `base:0, taper:1910, taperFrom:28000, taperTo:50000` (NON base:1910 — la formula del motore è base + taper·(taperTo−reddito)/span). Un test verifica `2026.json` ≡ `Year2026` (no-drift) e l'IRPEF 33%.

## Dati addizionali — generati da `tools/AddizionaliImporter` (Serena-friendly)
- Genera `regioni-<anno>.json` e `comuni-<anno>.json` per 2024/2025/2026 da: elenco comuni (matteocontrini/comuni-json) + MEF `mef-<anno>.csv` (comunale) e `mef-reg-<anno>.csv` (regionale). Join sul codice catastale.
- **Proroga fallback comuni**: per ogni comune si usa l'anno richiesto; se l'entry MEF dell'anno è assente O **senza aliquota** (delibera non ancora pubblicata, top rate 0 e nessuna esenzione) si ripiega su anno-1, poi anno-2 (ultima aliquota effettivamente deliberata). I comuni a 0% reale (Trento/Bolzano) restano 0. Questo è il motivo per cui es. Roma 2026 mostra 0,9% (proroga 2025) e non 0.
- Rigenerare: `dotnet run --project tools/AddizionaliImporter -c Release`. Per nuovi anni: scaricare `mef-<anno>.csv` e `mef-reg-<anno>.csv` dal MEF (download.php?anno= / ?tipo=reg&anno=), aggiungere l'anno al loop dell'importer e a `SampleRulesets`/`data/rulesets`.

## Limiti noti
- Agevolazioni/detrazioni regionali condizionate non modellate; +1% INPS oltre la prima fascia e massimali ante-1996 non distinti; forfettario senza riduzione 50% (riservata a chi ha aperto nel 2025).
Vedi `mem:core`, `mem:ui_design_system`, `mem:conventions`.