using TaxManager.Domain;

namespace TaxManager.Application.Models;

/// <summary>
/// Profilo contribuente salvabile e ricaricabile dall'utente. È un'unione piatta dei campi
/// dei vari profili: i campi non pertinenti alla <see cref="Category"/> sono ignorati.
/// Usato sia per la persistenza su disco (JSON) sia come riga del DB relazionale.
/// </summary>
public sealed class SavedProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Nuovo profilo";
    public TaxpayerCategory Category { get; set; } = TaxpayerCategory.DipendentePrivato;
    public int Year { get; set; }
    public DateTime UpdatedUtc { get; set; }

    // --- Campi lavoratore dipendente ---
    public decimal GrossAnnualSalary { get; set; }
    public bool UseMonthlyInput { get; set; }
    public decimal MonthlyGross { get; set; }
    public int Mensilita { get; set; } = 12;
    public EmploymentSector Sector { get; set; } = EmploymentSector.Privato;
    public int EmploymentDays { get; set; } = 365;
    public string? Region { get; set; }
    public string? Province { get; set; }
    public string? Municipality { get; set; }
    public decimal RegionalSurtaxRate { get; set; }
    public decimal MunicipalSurtaxRate { get; set; }
    public decimal MunicipalExemptionThreshold { get; set; }
    public decimal DeductibleCharges { get; set; }
    public decimal OtherTaxCredits { get; set; }
    public decimal PensionContribution { get; set; }

    // --- Campi forfettario ---
    public decimal Revenue { get; set; }
    public decimal Coefficient { get; set; }
    public bool IsStartup { get; set; }
    public ForfettarioContributionScheme Scheme { get; set; } = ForfettarioContributionScheme.GestioneSeparata;
    public bool ApplyContributionReduction { get; set; }
    public decimal CassaRate { get; set; }
}
