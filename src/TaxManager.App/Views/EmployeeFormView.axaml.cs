using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TaxManager.App.Views;

public partial class EmployeeFormView : UserControl
{
    public EmployeeFormView() => InitializeComponent();

    /// <summary>
    /// Rende "auto-correttiva" la ricerca: alla perdita di focus, il testo digitato viene risolto
    /// in una voce della lista (match esatto o, se univoco, parziale). Se non c'è corrispondenza,
    /// il testo torna alla selezione corrente. Evita selezioni "stantie" tipiche dell'AutoCompleteBox.
    /// </summary>
    private void OnSearchLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not AutoCompleteBox box) return;

        var text = box.Text?.Trim() ?? string.Empty;
        var items = (box.ItemsSource as IEnumerable)?.Cast<object?>()
            .Where(x => x is not null).Cast<object>().ToList() ?? new();

        if (text.Length == 0)
        {
            box.Text = box.SelectedItem?.ToString() ?? string.Empty;
            return;
        }

        if (box.SelectedItem is not null &&
            string.Equals(box.SelectedItem.ToString(), text, StringComparison.CurrentCultureIgnoreCase))
            return; // già coerente

        var exact = items.FirstOrDefault(x =>
            string.Equals(x.ToString(), text, StringComparison.CurrentCultureIgnoreCase));
        if (exact is not null) { box.SelectedItem = exact; return; }

        var matches = items.Where(x =>
            (x.ToString() ?? string.Empty).Contains(text, StringComparison.CurrentCultureIgnoreCase)).ToList();
        if (matches.Count == 1) { box.SelectedItem = matches[0]; return; }

        // ambiguo o nessun match: ripristina il testo della selezione corrente
        box.Text = box.SelectedItem?.ToString() ?? string.Empty;
    }
}
