namespace TaxManager.Infrastructure.Surtaxes;

// DTO che rispecchiano i file in data/addizionali.

internal sealed class RegioniFileDto
{
    public int Year { get; set; }
    public List<RegionDto> Regioni { get; set; } = new();
}

internal sealed class RegionDto
{
    public string Name { get; set; } = string.Empty;
    public List<SurtaxBracketDto> Brackets { get; set; } = new();
}

internal sealed class SurtaxBracketDto
{
    public decimal UpTo { get; set; }
    public decimal Rate { get; set; }
}

internal sealed class ComuniFileDto
{
    public int Year { get; set; }
    public List<ComuneDto> Comuni { get; set; } = new();
}

internal sealed class ComuneDto
{
    public string Region { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<SurtaxBracketDto> Brackets { get; set; } = new();
    public decimal ExemptionThreshold { get; set; }
}
