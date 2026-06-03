using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TaxManager.App.Support;
using TaxManager.Application.Abstractions;
using TaxManager.Application.Models;
using TaxManager.Domain;
using TaxManager.Domain.Common;
using TaxManager.Domain.Inputs;

namespace TaxManager.App.ViewModels;

/// <summary>
/// Form lavoratore dipendente. Le addizionali si ricavano dalla residenza, scelta a cascata
/// Regione → Provincia → Comune (tutti i comuni italiani). Fallback manuale per casi particolari.
/// </summary>
public partial class EmployeeFormViewModel : ObservableObject
{
    private readonly ISurtaxProvider _surtax;
    private int _year;

    [ObservableProperty] private decimal _grossAnnualSalary = 30_000m;
    [ObservableProperty] private int _employmentDays = 365;
    [ObservableProperty] private decimal _deductibleCharges;
    [ObservableProperty] private decimal _otherTaxCredits;
    [ObservableProperty] private decimal _pensionContribution;

    // Inserimento alternativo del reddito: lordo mensile x mensilita (la RAL include gia 13a/14a,
    // quindi la mensilita e' un DIVISORE; il default 12 e' un passthrough sicuro).
    [ObservableProperty] private bool _useMonthlyInput;
    [ObservableProperty] private decimal _monthlyGross = 2_000m;
    [ObservableProperty] private int _mensilita = 12;
    [ObservableProperty] private string _annualDerivedText = "—";

    [ObservableProperty] private RegionSurtax? _selectedRegion;
    [ObservableProperty] private string? _selectedProvince;
    [ObservableProperty] private MunicipalitySurtax? _selectedMunicipality;
    [ObservableProperty] private decimal _manualMunicipalPercent = 0.60m;

    [ObservableProperty] private string _regionalRateText = "—";
    [ObservableProperty] private string _municipalRateText = "—";

    public ObservableCollection<RegionSurtax> Regions { get; } = new();
    public ObservableCollection<string> Provinces { get; } = new();
    public ObservableCollection<MunicipalitySurtax> Municipalities { get; } = new();

    public int[] MensilitaOptions { get; } = { 12, 13, 14, 15 };

    public EmployeeFormViewModel(ISurtaxProvider surtax) => _surtax = surtax;

    public bool IsManualMunicipality => SelectedMunicipality is null || SelectedMunicipality.IsManualEntry;

    public void ApplyDefaults(int year)
    {
        _year = year;
        var previousRegion = SelectedRegion?.Region;
        Regions.Clear();
        foreach (var r in _surtax.GetRegions(year)) Regions.Add(r);
        SelectedRegion = Regions.FirstOrDefault(r => r.Region == previousRegion) ?? Regions.FirstOrDefault();
    }

    partial void OnSelectedRegionChanged(RegionSurtax? value)
    {
        ReloadProvinces(SelectedProvince);
        UpdateRegionalRateText();
    }

    partial void OnSelectedProvinceChanged(string? value) => ReloadMunicipalities(SelectedMunicipality?.Name);

    partial void OnSelectedMunicipalityChanged(MunicipalitySurtax? value)
    {
        OnPropertyChanged(nameof(IsManualMunicipality));
        UpdateMunicipalRateText();
    }

    partial void OnManualMunicipalPercentChanged(decimal value) => UpdateMunicipalRateText();

    partial void OnGrossAnnualSalaryChanged(decimal value)
    {
        UpdateRegionalRateText();
        UpdateDerivedText();
    }

    partial void OnUseMonthlyInputChanged(bool value)
    {
        // Entrando in modalita mensile, deriva il mensile dalla RAL corrente, poi riallinea la RAL.
        if (value && Mensilita > 0 && GrossAnnualSalary > 0)
            MonthlyGross = Money.Cents(GrossAnnualSalary / Mensilita);
        if (value) RecomputeRal();
        UpdateDerivedText();
    }

    partial void OnMonthlyGrossChanged(decimal value)
    {
        if (UseMonthlyInput) RecomputeRal();
    }

    partial void OnMensilitaChanged(int value)
    {
        if (UseMonthlyInput) RecomputeRal();
        UpdateDerivedText();
    }

    private void RecomputeRal()
    {
        if (Mensilita > 0) GrossAnnualSalary = Money.Cents(MonthlyGross * Mensilita);
    }

    private void UpdateDerivedText() => AnnualDerivedText = $"RAL stimata: {Fmt.Currency(GrossAnnualSalary)}";

    private void ReloadProvinces(string? keepName)
    {
        Provinces.Clear();
        if (SelectedRegion is not null)
            foreach (var p in _surtax.GetProvinces(_year, SelectedRegion.Region)) Provinces.Add(p);
        SelectedProvince = Provinces.FirstOrDefault(p => p == keepName) ?? Provinces.FirstOrDefault();
    }

    private void ReloadMunicipalities(string? keepName)
    {
        Municipalities.Clear();
        if (SelectedRegion is not null && SelectedProvince is not null)
            foreach (var m in _surtax.GetMunicipalities(_year, SelectedRegion.Region, SelectedProvince))
                Municipalities.Add(m);
        Municipalities.Add(MunicipalitySurtax.Manual);

        SelectedMunicipality =
            Municipalities.FirstOrDefault(m => !m.IsManualEntry && m.Name == keepName)
            ?? Municipalities.FirstOrDefault(m => !m.IsManualEntry)
            ?? MunicipalitySurtax.Manual;
    }

    private void UpdateRegionalRateText()
    {
        if (SelectedRegion is null) { RegionalRateText = "—"; return; }
        if (SelectedRegion.IsProgressive)
        {
            var income = Math.Max(GrossAnnualSalary, 0m);
            var eff = income > 0 ? SelectedRegion.Schedule.ComputeTax(income) / income : 0m;
            RegionalRateText = $"a scaglioni (effettiva ~{eff.ToString("P2", Fmt.It)})";
        }
        else
        {
            RegionalRateText = SelectedRegion.RepresentativeRate.ToString("P2", Fmt.It);
        }
    }

    private void UpdateMunicipalRateText()
    {
        if (IsManualMunicipality)
        {
            MunicipalRateText = (ManualMunicipalPercent / 100m).ToString("P2", Fmt.It);
            return;
        }

        var m = SelectedMunicipality!;
        var baseText = m.IsProgressive
            ? $"a scaglioni (max {m.RepresentativeRate.ToString("P2", Fmt.It)})"
            : m.RepresentativeRate.ToString("P2", Fmt.It);
        MunicipalRateText = m.ExemptionThreshold > 0
            ? $"{baseText} — esente fino a {m.ExemptionThreshold.ToString("C0", Fmt.It)}"
            : baseText;
    }

    public EmploymentIncomeInput ToInput(EmploymentSector sector)
    {
        decimal municipalRate, municipalExemption;
        Domain.Rules.ProgressiveSchedule? municipalSchedule;
        if (IsManualMunicipality)
        {
            municipalRate = ManualMunicipalPercent / 100m;
            municipalExemption = 0m;
            municipalSchedule = null;
        }
        else
        {
            municipalRate = SelectedMunicipality!.RepresentativeRate;
            municipalExemption = SelectedMunicipality.ExemptionThreshold;
            municipalSchedule = SelectedMunicipality.Schedule;
        }

        return new EmploymentIncomeInput
        {
            GrossAnnualSalary = GrossAnnualSalary,
            Sector = sector,
            EmploymentDays = EmploymentDays,
            RegionalSurtaxSchedule = SelectedRegion?.Schedule,
            RegionalSurtaxRate = SelectedRegion?.RepresentativeRate ?? 0m,
            MunicipalSurtaxSchedule = municipalSchedule,
            MunicipalSurtaxRate = municipalRate,
            MunicipalExemptionThreshold = municipalExemption,
            DeductibleCharges = DeductibleCharges,
            OtherTaxCredits = OtherTaxCredits,
            ComplementaryPensionContribution = PensionContribution,
        };
    }

    public void LoadFrom(SavedProfile p, int year)
    {
        ApplyDefaults(year);
        UseMonthlyInput = false; // evita ricalcoli intermedi durante il caricamento
        GrossAnnualSalary = p.GrossAnnualSalary;
        Mensilita = p.Mensilita is >= 12 and <= 15 ? p.Mensilita : 12;
        MonthlyGross = p.MonthlyGross > 0 ? p.MonthlyGross : Money.Cents(GrossAnnualSalary / Mensilita);
        UseMonthlyInput = p.UseMonthlyInput;
        EmploymentDays = p.EmploymentDays <= 0 ? 365 : p.EmploymentDays;
        DeductibleCharges = p.DeductibleCharges;
        OtherTaxCredits = p.OtherTaxCredits;
        PensionContribution = p.PensionContribution;

        if (!string.IsNullOrWhiteSpace(p.Region))
            SelectedRegion = Regions.FirstOrDefault(r => r.Region == p.Region) ?? SelectedRegion;
        if (!string.IsNullOrWhiteSpace(p.Province))
            SelectedProvince = Provinces.FirstOrDefault(pr => pr == p.Province) ?? SelectedProvince;

        if (!string.IsNullOrWhiteSpace(p.Municipality))
        {
            var match = Municipalities.FirstOrDefault(m => !m.IsManualEntry && m.Name == p.Municipality);
            if (match is not null) { SelectedMunicipality = match; return; }
        }

        SelectedMunicipality = MunicipalitySurtax.Manual;
        if (p.MunicipalSurtaxRate > 0) ManualMunicipalPercent = p.MunicipalSurtaxRate * 100m;
    }

    public void WriteTo(SavedProfile p)
    {
        p.GrossAnnualSalary = GrossAnnualSalary;
        p.UseMonthlyInput = UseMonthlyInput;
        p.MonthlyGross = MonthlyGross;
        p.Mensilita = Mensilita;
        p.EmploymentDays = EmploymentDays;
        p.DeductibleCharges = DeductibleCharges;
        p.OtherTaxCredits = OtherTaxCredits;
        p.PensionContribution = PensionContribution;
        p.Region = SelectedRegion?.Region;
        p.Province = SelectedProvince;
        p.RegionalSurtaxRate = SelectedRegion?.RepresentativeRate ?? 0m;

        if (IsManualMunicipality)
        {
            p.Municipality = null;
            p.MunicipalSurtaxRate = ManualMunicipalPercent / 100m;
            p.MunicipalExemptionThreshold = 0m;
        }
        else
        {
            p.Municipality = SelectedMunicipality!.Name;
            p.MunicipalSurtaxRate = SelectedMunicipality.RepresentativeRate;
            p.MunicipalExemptionThreshold = SelectedMunicipality.ExemptionThreshold;
        }
    }
}
