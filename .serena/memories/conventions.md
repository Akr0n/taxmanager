# Convenzioni di codice

- Lingua: commenti, etichette UI e nomi di dominio in ITALIANO; nomi tecnici (tipi/membri) in inglese dove naturale.
- Denaro: `decimal` ovunque (mai double/float); arrotondare SOLO tramite `Domain.Common.Money` (`Euro` per le imposte, `Cents` per il resto).
- Regole fiscali come DATI versionati per anno (record in `Domain.Rules`), con i metodi di calcolo SUL TIPO (così sopravvivono alla deserializzazione JSON). Aggiungere un anno = nuovo `data/rulesets/<anno>.json` + metodo in `SampleRulesets`; tenerli allineati (un test verifica la non-divergenza JSON↔codice).
- Esito = `TaxResult` con voci `TaxLineItem` (prospetto auditabile) + totali sintetici + `Notes`. Importi delle voci in valore assoluto positivo; il segno logico è dato da `TaxLineKind`.
- Il dominio NON ha dipendenze esterne. La persistenza è solo dietro le astrazioni in `Application.Abstractions`; l'implementazione vive in `Infrastructure`.
- MVVM: nessuna logica di business nelle viste; i dialoghi file (StorageProvider) stanno nel code-behind della finestra.
- Aliquote nei form inserite in % (es. 1,23) e convertite in decimale (/100) nei metodi `ToInput()`.
- Per i nuovi file usare `Write`; per editare codice esistente preferire i tool simbolici di Serena.