using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TaxManager.App.Support;
using TaxManager.Domain.Results;

namespace TaxManager.App.ViewModels;

/// <summary>Riga formattata del prospetto (per il DataGrid).</summary>
public sealed record TaxLineRow(string Label, string Amount, string Kind);

/// <summary>Stato del pannello risultati.</summary>
public partial class ResultViewModel : ObservableObject
{
    [ObservableProperty] private string _title = "Nessun calcolo eseguito";
    [ObservableProperty] private string _netIncomeText = "—";
    [ObservableProperty] private string _totalTaxesText = "—";
    [ObservableProperty] private string _contributionsText = "—";
    [ObservableProperty] private string _effectiveRateText = "—";
    [ObservableProperty] private string _burdenRateText = "—";

    public ObservableCollection<TaxLineRow> Lines { get; } = new();
    public ObservableCollection<string> Notes { get; } = new();

    public void Populate(TaxResult r)
    {
        Title = $"{Fmt.Category(r.Category)} — Anno {r.Year}";
        NetIncomeText = Fmt.Currency(r.NetAnnualIncome);
        TotalTaxesText = Fmt.Currency(r.TotalTaxes);
        ContributionsText = Fmt.Currency(r.SocialContributions);
        EffectiveRateText = Fmt.Percent(r.EffectiveTaxRate);
        BurdenRateText = Fmt.Percent(r.TotalBurdenRate);

        Lines.Clear();
        foreach (var l in r.Lines)
            Lines.Add(new TaxLineRow(l.Label, Fmt.Currency(l.Amount), l.Kind.ToString()));

        Notes.Clear();
        foreach (var n in r.Notes) Notes.Add(n);
    }
}
