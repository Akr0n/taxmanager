using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaxManager.Application.Abstractions;
using TaxManager.Application.Models;
using TaxManager.Application.Services;
using TaxManager.Domain;
using TaxManager.Domain.Results;

namespace TaxManager.App.ViewModels;

/// <summary>
/// ViewModel principale: coordina selezione anno/categoria, i form, il calcolo, la persistenza
/// (storico su DB + profili su disco) e l'esportazione del prospetto.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly ITaxCalculationService _calc;
    private readonly ITaxComputationRepository _repo;
    private readonly IProfileStore _profileStore;
    private readonly IReportExporter _exporter;

    private Guid? _editingProfileId;
    private TaxResult? _lastResult;

    [ObservableProperty] private int _selectedYear;
    [ObservableProperty] private TaxpayerCategory _selectedCategory = TaxpayerCategory.DipendentePrivato;
    [ObservableProperty] private bool _hasResult;
    [ObservableProperty] private string _profileName = string.Empty;
    [ObservableProperty] private SavedProfile? _selectedProfile;
    [ObservableProperty] private HistoryRowViewModel? _selectedHistoryRow;
    [ObservableProperty] private string _statusMessage = "Pronto.";

    public ObservableCollection<int> Years { get; }
    public TaxpayerCategory[] Categories { get; } = Enum.GetValues<TaxpayerCategory>();
    public EmployeeFormViewModel EmployeeForm { get; }
    public ForfettarioFormViewModel ForfettarioForm { get; } = new();
    public ResultViewModel Result { get; } = new();
    public ObservableCollection<SavedProfile> Profiles { get; } = new();
    public ObservableCollection<HistoryRowViewModel> History { get; } = new();

    public bool IsEmployee => SelectedCategory != TaxpayerCategory.Forfettario;
    public bool IsForfettario => SelectedCategory == TaxpayerCategory.Forfettario;
    public string ProfilesPath => _profileStore.StorePath;

    public MainWindowViewModel(
        ITaxCalculationService calc,
        ITaxComputationRepository repo,
        IProfileStore profileStore,
        IReportExporter exporter,
        ISurtaxProvider surtax)
    {
        _calc = calc;
        _repo = repo;
        _profileStore = profileStore;
        _exporter = exporter;

        EmployeeForm = new EmployeeFormViewModel(surtax);
        Years = new ObservableCollection<int>(calc.AvailableYears);
        _selectedYear = Years.Count > 0 ? Years[^1] : 2025;
        ApplyYearDefaults();
    }

    public async Task InitializeAsync()
    {
        await RefreshProfilesAsync();
        await RefreshHistoryAsync();
    }

    partial void OnSelectedYearChanged(int value) => ApplyYearDefaults();

    partial void OnSelectedCategoryChanged(TaxpayerCategory value)
    {
        OnPropertyChanged(nameof(IsEmployee));
        OnPropertyChanged(nameof(IsForfettario));
    }

    private void ApplyYearDefaults()
    {
        if (!_calc.AvailableYears.Contains(SelectedYear)) return;
        var rs = _calc.GetRuleset(SelectedYear);
        EmployeeForm.ApplyDefaults(SelectedYear);
        ForfettarioForm.ApplyDefaults(rs);
    }

    [RelayCommand]
    private async Task CalculateAsync()
    {
        try
        {
            TaxResult result;
            object snapshot;

            if (SelectedCategory == TaxpayerCategory.Forfettario)
            {
                var input = ForfettarioForm.ToInput();
                result = _calc.Calculate(input, SelectedYear);
                snapshot = input;
            }
            else
            {
                if (EmployeeForm.SelectedRegion is null)
                {
                    StatusMessage = "Seleziona una regione valida prima di calcolare.";
                    return;
                }
                var sector = SelectedCategory == TaxpayerCategory.DipendentePubblico
                    ? EmploymentSector.Pubblico
                    : EmploymentSector.Privato;
                var input = EmployeeForm.ToInput(sector);
                result = _calc.Calculate(input, SelectedYear);
                snapshot = input;
            }

            Result.Populate(result);
            _lastResult = result;
            HasResult = true;

            var record = ComputationRecordFactory.Create(
                result,
                string.IsNullOrWhiteSpace(ProfileName) ? null : ProfileName,
                snapshot);
            await _repo.AddAsync(record);
            await RefreshHistoryAsync();

            StatusMessage = $"Calcolo eseguito: netto annuo {Result.NetIncomeText}. Salvato nello storico.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Errore nel calcolo: " + ex.Message;
        }
    }

    // ---------- Profili (persistenza su disco) ----------

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        var profile = new SavedProfile
        {
            Id = _editingProfileId ?? Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(ProfileName) ? "Profilo senza nome" : ProfileName.Trim(),
            Category = SelectedCategory,
            Year = SelectedYear,
        };

        if (SelectedCategory == TaxpayerCategory.Forfettario)
            ForfettarioForm.WriteTo(profile);
        else
            EmployeeForm.WriteTo(profile);

        await _profileStore.SaveAsync(profile);
        _editingProfileId = profile.Id;
        await RefreshProfilesAsync();
        StatusMessage = $"Profilo \"{profile.Name}\" salvato su disco.";
    }

    [RelayCommand]
    private void LoadProfile(SavedProfile? profile)
    {
        profile ??= SelectedProfile;
        if (profile is null) return;

        _editingProfileId = profile.Id;
        ProfileName = profile.Name;
        if (profile.Year != 0 && Years.Contains(profile.Year))
            SelectedYear = profile.Year;
        SelectedCategory = profile.Category;

        if (profile.Category == TaxpayerCategory.Forfettario)
            ForfettarioForm.LoadFrom(profile, _calc.GetRuleset(SelectedYear));
        else
            EmployeeForm.LoadFrom(profile, SelectedYear);

        StatusMessage = $"Profilo \"{profile.Name}\" caricato.";
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(SavedProfile? profile)
    {
        profile ??= SelectedProfile;
        if (profile is null) return;
        await _profileStore.DeleteAsync(profile.Id);
        if (_editingProfileId == profile.Id) _editingProfileId = null;
        await RefreshProfilesAsync();
        StatusMessage = $"Profilo \"{profile.Name}\" eliminato.";
    }

    [RelayCommand]
    private void NewProfile()
    {
        _editingProfileId = null;
        ProfileName = string.Empty;
        SelectedProfile = null;
        ApplyYearDefaults();
        StatusMessage = "Nuovo profilo: compila i campi e salva.";
    }

    private async Task RefreshProfilesAsync()
    {
        var all = await _profileStore.LoadAllAsync();
        Profiles.Clear();
        foreach (var p in all.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
            Profiles.Add(p);
    }

    // ---------- Storico (persistenza relazionale) ----------

    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        var recent = await _repo.GetRecentAsync(200);
        History.Clear();
        foreach (var r in recent)
            History.Add(new HistoryRowViewModel(r));
    }

    [RelayCommand]
    private async Task DeleteHistoryAsync(HistoryRowViewModel? row)
    {
        row ??= SelectedHistoryRow;
        if (row is null) return;
        await _repo.DeleteAsync(row.Id);
        await RefreshHistoryAsync();
        StatusMessage = "Voce dello storico eliminata.";
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _repo.ClearAsync();
        await RefreshHistoryAsync();
        StatusMessage = "Storico svuotato.";
    }

    [RelayCommand]
    private void OpenHistoryDetail(HistoryRowViewModel? row)
    {
        row ??= SelectedHistoryRow;
        if (row is null) return;
        var result = ComputationRecordFactory.ReadResult(row.Record);
        if (result is null) return;
        Result.Populate(result);
        _lastResult = result;
        HasResult = true;
        StatusMessage = "Dettaglio caricato dallo storico.";
    }

    // ---------- Esportazione (chiamata dal code-behind con il percorso scelto) ----------

    public bool CanExport => _lastResult is not null;

    public async Task<bool> ExportAsync(string filePath, bool asJson)
    {
        if (_lastResult is null) return false;
        if (asJson) await _exporter.ExportJsonAsync(_lastResult, filePath);
        else await _exporter.ExportTextAsync(_lastResult, filePath);
        StatusMessage = "Prospetto esportato: " + filePath;
        return true;
    }
}
