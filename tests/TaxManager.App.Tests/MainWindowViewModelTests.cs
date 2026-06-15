using System;
using System.IO;
using System.Threading.Tasks;
using TaxManager.Domain;
using TaxManager.Infrastructure.Persistence;

namespace TaxManager.App.Tests;

/// <summary>
/// Test del ViewModel principale senza runtime Avalonia: i VM sono CommunityToolkit.Mvvm puri.
/// Copre il flusso di calcolo e — soprattutto — la robustezza dell'esportazione.
/// </summary>
public class MainWindowViewModelTests
{
    [Fact]
    public async Task Export_su_destinazione_in_errore_non_crasha_e_segnala_lo_stato()
    {
        var vm = VmFactory.Create(new ThrowingReportExporter(), out _);
        vm.SelectedCategory = TaxpayerCategory.Forfettario; // il forfettario non richiede la regione
        await vm.CalculateCommand.ExecuteAsync(null);
        Assert.True(vm.CanExport);

        // L'esportazione lancia internamente: NON deve propagare (era un async void → crash potenziale).
        var ok = await vm.ExportAsync(Path.Combine(Path.GetTempPath(), "tm-ko.txt"), asJson: false);

        Assert.False(ok);
        Assert.Contains("Errore nell'esportazione", vm.StatusMessage);
    }

    [Fact]
    public async Task Export_su_percorso_valido_scrive_il_prospetto()
    {
        var vm = VmFactory.Create(new FileReportExporter(), out _);
        vm.SelectedCategory = TaxpayerCategory.Forfettario;
        await vm.CalculateCommand.ExecuteAsync(null);

        var path = Path.Combine(Path.GetTempPath(), $"tm-export-{Guid.NewGuid():N}.txt");
        try
        {
            var ok = await vm.ExportAsync(path, asJson: false);

            Assert.True(ok);
            Assert.True(File.Exists(path));
            Assert.Contains("TaxManager", await File.ReadAllTextAsync(path));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Calcolo_forfettario_popola_il_risultato_e_lo_storico()
    {
        var vm = VmFactory.Create(new FileReportExporter(), out var repo);
        vm.SelectedCategory = TaxpayerCategory.Forfettario;
        Assert.False(vm.HasResult);

        await vm.CalculateCommand.ExecuteAsync(null);

        Assert.True(vm.HasResult);
        Assert.NotEqual("—", vm.Result.NetIncomeText);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task Calcolo_dipendente_usa_la_regione_di_default_e_produce_un_esito()
    {
        var vm = VmFactory.Create(new FileReportExporter(), out _);
        vm.SelectedCategory = TaxpayerCategory.DipendentePrivato;

        // ApplyDefaults (in costruzione) deve aver popolato e selezionato una regione dai dati addizionali.
        Assert.NotNull(vm.EmployeeForm.SelectedRegion);

        await vm.CalculateCommand.ExecuteAsync(null);

        Assert.True(vm.HasResult);
    }

    [Fact]
    public async Task Calcolo_dipendente_senza_regione_non_calcola_e_avvisa()
    {
        var vm = VmFactory.Create(new FileReportExporter(), out var repo);
        vm.SelectedCategory = TaxpayerCategory.DipendentePrivato;
        vm.EmployeeForm.SelectedRegion = null;

        await vm.CalculateCommand.ExecuteAsync(null);

        Assert.False(vm.HasResult);
        Assert.Empty(repo.Items);
        Assert.Contains("regione", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }
}
