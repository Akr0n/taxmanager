using System.Globalization;
using TaxManager.Domain;

namespace TaxManager.App.Support;

/// <summary>Formattazioni in cultura it-IT condivise dalla UI.</summary>
internal static class Fmt
{
    public static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    public static string Currency(decimal v) => v.ToString("C2", It);

    public static string Percent(decimal v) => v.ToString("P2", It);

    public static string Category(TaxpayerCategory c) => c switch
    {
        TaxpayerCategory.DipendentePrivato => "Dipendente privato",
        TaxpayerCategory.DipendentePubblico => "Dipendente pubblico",
        TaxpayerCategory.Forfettario => "Partita IVA forfettaria",
        _ => c.ToString(),
    };

    public static string Scheme(ForfettarioContributionScheme s) => s switch
    {
        ForfettarioContributionScheme.GestioneSeparata => "Gestione Separata INPS",
        ForfettarioContributionScheme.Artigiani => "Artigiani INPS",
        ForfettarioContributionScheme.Commercianti => "Commercianti INPS",
        ForfettarioContributionScheme.CassaProfessionale => "Cassa professionale",
        _ => s.ToString(),
    };
}
