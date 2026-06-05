using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace TaxManager.App.Views;

public partial class EmployeeFormView : UserControl
{
    public EmployeeFormView() => InitializeComponent();

    /// <summary>
    /// Rende "auto-correttiva" la ricerca: alla perdita di focus, il testo digitato viene risolto
    /// in una voce della lista (match esatto o, se univoco, parziale). Se non c'è corrispondenza,
    /// il testo torna alla selezione corrente. Evita selezioni "stantie" tipiche dell'AutoCompleteBox.
    /// Subito dopo la normalizzazione chiude SEMPRE il popup dei suggerimenti (vedi CloseDropDown):
    /// senza questo, in Avalonia 12 le tendine dei tre AutoCompleteBox (Regione/Provincia/Comune)
    /// restavano tutte aperte dopo il TAB.
    /// </summary>
    private void OnSearchLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not AutoCompleteBox box) return;

        NormalizeSelection(box);
        CloseDropDown(box);
    }

    /// <summary>
    /// Risolve il testo digitato in una voce della lista (match esatto o parziale univoco); se non
    /// c'è corrispondenza, ripristina il testo della selezione corrente. Logica invariata rispetto
    /// all'originale: cambia solo il fatto di essere estratta così che la chiusura del popup avvenga
    /// in un unico punto, qualunque ramo termini la normalizzazione.
    /// </summary>
    private static void NormalizeSelection(AutoCompleteBox box)
    {
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

    /// <summary>
    /// Chiude in modo affidabile la tendina dei suggerimenti alla perdita del focus.
    ///
    /// In Avalonia 12 l'AutoCompleteBox prova già a chiudersi da solo (OnLostFocus → FocusChanged),
    /// ma solo se il FocusManager riflette già il nuovo elemento a fuoco; con il TAB quella condizione
    /// può non essere ancora soddisfatta, così la tendina resta aperta. Forziamo quindi la chiusura.
    /// Una chiusura sincrona basta nel caso comune (MinimumPopulateDelay=0 ⇒ popolamento sincrono);
    /// il secondo passaggio su Dispatcher a priorità Background è un'assicurazione contro l'ordine di
    /// aggiornamento del focus ed è no-op se nel frattempo il controllo ha riacquisito il focus
    /// (es. Shift+Tab immediato), così non interferisce con la normale riapertura durante la
    /// digitazione né con la selezione via mouse/tastiera (che avvengono mentre il controllo ha il focus).
    /// </summary>
    private static void CloseDropDown(AutoCompleteBox box)
    {
        box.IsDropDownOpen = false;

        Dispatcher.UIThread.Post(() =>
        {
            if (box.IsKeyboardFocusWithin) return;
            box.IsDropDownOpen = false;
        }, DispatcherPriority.Background);
    }
}
