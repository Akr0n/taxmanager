namespace TaxManager.Domain;

/// <summary>Categoria fiscale del contribuente gestita dall'applicazione.</summary>
public enum TaxpayerCategory
{
    /// <summary>Lavoratore dipendente del settore privato.</summary>
    DipendentePrivato,

    /// <summary>Lavoratore dipendente del settore pubblico (gestione ex-INPDAP).</summary>
    DipendentePubblico,

    /// <summary>Titolare di partita IVA in regime forfettario.</summary>
    Forfettario
}

/// <summary>Settore di impiego del lavoratore dipendente (determina l'aliquota contributiva).</summary>
public enum EmploymentSector
{
    Privato,
    Pubblico
}

/// <summary>Gestione previdenziale del forfettario.</summary>
public enum ForfettarioContributionScheme
{
    /// <summary>Gestione Separata INPS (professionisti senza cassa).</summary>
    GestioneSeparata,

    /// <summary>Gestione artigiani INPS (minimale + percentuale).</summary>
    Artigiani,

    /// <summary>Gestione commercianti INPS (minimale + percentuale).</summary>
    Commercianti,

    /// <summary>Cassa professionale privata (aliquota configurabile).</summary>
    CassaProfessionale
}
