using TaxManager.Application.Models;

namespace TaxManager.Application.Abstractions;

/// <summary>
/// Fornisce le addizionali regionali (per anno) e comunali (per anno/regione/provincia) da dati pubblici
/// (MEF + elenco comuni), coprendo tutte le regioni, province e comuni italiani.
/// </summary>
public interface ISurtaxProvider
{
    /// <summary>Regioni con la rispettiva addizionale regionale dell'anno indicato.</summary>
    IReadOnlyList<RegionSurtax> GetRegions(int year);

    /// <summary>Province (nomi) presenti nella regione indicata.</summary>
    IReadOnlyList<string> GetProvinces(int year, string region);

    /// <summary>Comuni della provincia indicata, con addizionale comunale.</summary>
    IReadOnlyList<MunicipalitySurtax> GetMunicipalities(int year, string region, string province);
}
