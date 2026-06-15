using System;
using System.IO;
using TaxManager.App.ViewModels;
using TaxManager.Domain;
using TaxManager.Infrastructure.Surtaxes;

namespace TaxManager.App.Tests;

/// <summary>Logica del form dipendente: input mensile→RAL e produzione dell'input di dominio.</summary>
public class EmployeeFormViewModelTests
{
    private static EmployeeFormViewModel NewForm()
    {
        var surtax = new SurtaxJsonProvider(Path.Combine(AppContext.BaseDirectory, "addizionali"));
        var vm = new EmployeeFormViewModel(surtax);
        vm.ApplyDefaults(2025);
        return vm;
    }

    [Fact]
    public void Input_mensile_calcola_la_ral_come_mensile_per_mensilita()
    {
        var vm = NewForm();

        vm.UseMonthlyInput = true;
        vm.Mensilita = 13;
        vm.MonthlyGross = 2_000m;

        Assert.Equal(26_000m, vm.GrossAnnualSalary); // 2.000 × 13
    }

    [Fact]
    public void ToInput_riporta_la_previdenza_complementare_e_la_regione()
    {
        var vm = NewForm();
        vm.GrossAnnualSalary = 30_000m;
        vm.PensionContribution = 1_500m;

        var input = vm.ToInput(EmploymentSector.Privato);

        Assert.Equal(30_000m, input.GrossAnnualSalary);
        Assert.Equal(1_500m, input.ComplementaryPensionContribution);
        Assert.NotNull(input.RegionalSurtaxSchedule); // la regione di default fornisce gli scaglioni
    }
}
