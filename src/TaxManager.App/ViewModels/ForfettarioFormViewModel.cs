using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TaxManager.Application.Models;
using TaxManager.Domain;
using TaxManager.Domain.Inputs;
using TaxManager.Domain.Rules;

namespace TaxManager.App.ViewModels;

/// <summary>Dati del form per il regime forfettario.</summary>
public partial class ForfettarioFormViewModel : ObservableObject
{
    [ObservableProperty] private decimal _revenue = 50_000m;
    [ObservableProperty] private ForfettarioCoefficient? _selectedCoefficient;
    [ObservableProperty] private bool _isStartup;
    [ObservableProperty] private ForfettarioContributionScheme _scheme = ForfettarioContributionScheme.GestioneSeparata;
    [ObservableProperty] private bool _applyReduction;
    [ObservableProperty] private decimal _cassaPercent = 4m;

    public ObservableCollection<ForfettarioCoefficient> Coefficients { get; } = new();

    public ForfettarioContributionScheme[] Schemes { get; } = Enum.GetValues<ForfettarioContributionScheme>();

    public void ApplyDefaults(TaxYearRuleset r)
    {
        var previous = SelectedCoefficient?.Coefficient;
        Coefficients.Clear();
        foreach (var c in r.Forfettario.Coefficients) Coefficients.Add(c);
        SelectedCoefficient =
            Coefficients.FirstOrDefault(c => c.Coefficient == previous)
            ?? Coefficients.FirstOrDefault(c => c.Coefficient == 0.78m)
            ?? Coefficients.FirstOrDefault();
    }

    public ForfettarioIncomeInput ToInput() => new()
    {
        Revenue = Revenue,
        Coefficient = SelectedCoefficient?.Coefficient ?? 0.78m,
        IsStartup = IsStartup,
        Scheme = Scheme,
        ApplyArtigianiCommerciantiReduction = ApplyReduction,
        CassaRate = CassaPercent / 100m,
    };

    public void LoadFrom(SavedProfile p, TaxYearRuleset r)
    {
        ApplyDefaults(r);
        Revenue = p.Revenue;
        IsStartup = p.IsStartup;
        Scheme = p.Scheme;
        ApplyReduction = p.ApplyContributionReduction;
        CassaPercent = p.CassaRate * 100m;
        SelectedCoefficient = Coefficients.FirstOrDefault(c => c.Coefficient == p.Coefficient) ?? SelectedCoefficient;
    }

    public void WriteTo(SavedProfile p)
    {
        p.Revenue = Revenue;
        p.Coefficient = SelectedCoefficient?.Coefficient ?? 0.78m;
        p.IsStartup = IsStartup;
        p.Scheme = Scheme;
        p.ApplyContributionReduction = ApplyReduction;
        p.CassaRate = CassaPercent / 100m;
    }
}
