using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TaxManager.App.ViewModels;

namespace TaxManager.App.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private async void OnExportText(object? sender, RoutedEventArgs e) => await ExportAsync(asJson: false);

    private async void OnExportJson(object? sender, RoutedEventArgs e) => await ExportAsync(asJson: true);

    private async Task ExportAsync(bool asJson)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.CanExport)
            return;

        var top = GetTopLevel(this);
        if (top is null) return;

        var fileType = asJson
            ? new FilePickerFileType("File JSON") { Patterns = new[] { "*.json" } }
            : new FilePickerFileType("File di testo") { Patterns = new[] { "*.txt" } };

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Esporta prospetto di calcolo",
            SuggestedFileName = asJson ? "prospetto-taxmanager.json" : "prospetto-taxmanager.txt",
            DefaultExtension = asJson ? "json" : "txt",
            FileTypeChoices = new List<FilePickerFileType> { fileType },
        });

        if (file is null) return;
        await vm.ExportAsync(file.Path.LocalPath, asJson);
    }
}
