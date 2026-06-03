using System;
using System.Globalization;
using Avalonia.Data.Converters;
using TaxManager.Domain;

namespace TaxManager.App.Support;

/// <summary>Converte i valori enum di dominio in etichette italiane per la UI.</summary>
public sealed class EnumLabelConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        TaxpayerCategory c => Fmt.Category(c),
        ForfettarioContributionScheme s => Fmt.Scheme(s),
        EmploymentSector.Pubblico => "Settore pubblico",
        EmploymentSector.Privato => "Settore privato",
        null => string.Empty,
        _ => value.ToString(),
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
