using TaxManager.App.Support;
using TaxManager.Application.Models;

namespace TaxManager.App.ViewModels;

/// <summary>Riga formattata dello storico calcoli.</summary>
public sealed class HistoryRowViewModel
{
    public HistoryRowViewModel(ComputationRecord record)
    {
        Record = record;
        Id = record.Id;
        CreatedText = record.CreatedUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Fmt.It);
        CategoryText = Fmt.Category(record.Category);
        YearText = record.Year.ToString();
        ProfileName = string.IsNullOrWhiteSpace(record.ProfileName) ? "—" : record.ProfileName!;
        GrossText = Fmt.Currency(record.GrossIncome);
        TaxesText = Fmt.Currency(record.TotalTaxes);
        NetText = Fmt.Currency(record.NetAnnualIncome);
        RateText = Fmt.Percent(record.EffectiveTaxRate);
    }

    public ComputationRecord Record { get; }
    public int Id { get; }
    public string CreatedText { get; }
    public string CategoryText { get; }
    public string YearText { get; }
    public string ProfileName { get; }
    public string GrossText { get; }
    public string TaxesText { get; }
    public string NetText { get; }
    public string RateText { get; }
}
