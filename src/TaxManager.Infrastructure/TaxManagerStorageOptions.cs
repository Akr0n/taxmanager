namespace TaxManager.Infrastructure;

/// <summary>Percorsi di archiviazione su disco usati dall'app.</summary>
public sealed class TaxManagerStorageOptions
{
    /// <summary>Cartella base dei dati utente (DB + profili). Default: %APPDATA%\TaxManager.</summary>
    public string DataDirectory { get; set; } = string.Empty;

    /// <summary>Cartella dei ruleset JSON. Default: &lt;cartella app&gt;\rulesets.</summary>
    public string RulesetsDirectory { get; set; } = string.Empty;

    /// <summary>Cartella dei dati addizionali regionali/comunali. Default: &lt;cartella app&gt;\addizionali.</summary>
    public string AddizionaliDirectory { get; set; } = string.Empty;

    /// <summary>File SQLite dello storico calcoli.</summary>
    public string DatabasePath => Path.Combine(DataDirectory, "taxmanager.db");

    /// <summary>File JSON dei profili contribuente.</summary>
    public string ProfilesPath => Path.Combine(DataDirectory, "profiles.json");

    /// <summary>Costruisce le opzioni di default basate sulle cartelle standard del sistema.</summary>
    public static TaxManagerStorageOptions CreateDefault()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return new TaxManagerStorageOptions
        {
            DataDirectory = Path.Combine(appData, "TaxManager"),
            RulesetsDirectory = Path.Combine(AppContext.BaseDirectory, "rulesets"),
            AddizionaliDirectory = Path.Combine(AppContext.BaseDirectory, "addizionali"),
        };
    }
}
