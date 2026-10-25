using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using PrintHTML.Core.Helpers;
using PrintHTML.Core.Services;
using Ui = Wpf.Ui.Controls;

namespace FluentHTMLReceiptPrinter;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly PrinterService _printerService = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadPrinters();
        BindKeyboardShortcuts();
    }

    /// <summary>
    /// Set the character per line value
    /// </summary>
    private int CharsPerLine => (int)(CharsPerLineBox.Value ?? 42);

    /// <summary>
    /// Load the available printers on the system
    /// </summary>
    private void LoadPrinters()
    {
        var printers = PrinterInfo.GetPrinterNames().ToList();
        PrintersComboBox.ItemsSource = printers;
        if (printers.Count > 0)
            PrintersComboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// Generate the preview given the HTML input
    /// </summary>
    private async void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        var htmlInput = InputHtmlTextBox.Text;
        if (string.IsNullOrWhiteSpace(htmlInput))
        {
            OutputPreviewViewer.Document = MakePlaceholderDocument("Please enter HTML content to preview.");
            return;
        }

        var selectedPrinter = PrintersComboBox.SelectedItem as string ?? string.Empty;
        try
        {
            var previewDocument = _printerService.GeneratePreview(htmlInput, selectedPrinter, CharsPerLine);
            OutputPreviewViewer.Document = previewDocument;
        }
        catch (Exception exception)
        {
            OutputPreviewViewer.Document =
                MakePlaceholderDocument("Failed to generate preview. Please check the HTML content and try again.");
            await ShowDialogAsync("Error",
                $"An error occurred while generating the preview: {exception.Message}", Ui.SymbolRegular.ErrorCircle24);
        }
    }

    private async void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var content = InputHtmlTextBox.Text;
        var selectedPrinter = PrintersComboBox.SelectedItem as string;

        if (string.IsNullOrWhiteSpace(selectedPrinter))
        {
            await ShowDialogAsync("Warning", "Please select a printer.", Ui.SymbolRegular.Warning24);
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            await ShowDialogAsync("Warning", "The content to be printed cannot be empty.", Ui.SymbolRegular.Warning24);
            return;
        }

        PrintButton.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;

        var dialogTitle = "Information";
        var dialogMessage = "The printing process was completed successfully.";
        var dialogIcon = Ui.SymbolRegular.CheckmarkCircle24;

        try
        {
            AsyncPrintTask.Exec(true, () => _printerService.DoPrint(content, selectedPrinter, CharsPerLine));
        }
        catch (Exception ex)
        {
            dialogTitle = "Error";
            dialogMessage = $"An error occurred during printing: {ex.Message}";
            dialogIcon = Ui.SymbolRegular.ErrorCircle24;
        }
        finally
        {
            PrintButton.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }

        await ShowDialogAsync(dialogTitle, dialogMessage, dialogIcon);
    }

    private Task<Ui.ContentDialogResult> ShowDialogAsync(string title, string message, Ui.SymbolRegular icon)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(new Ui.SymbolIcon
        {
            Symbol = icon,
            FontSize = 28,
            Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Top
        });
        content.Children.Add(new ScrollViewer
        {
            MaxHeight = 260,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 360
            }
        });

        var dialog = new Ui.ContentDialog(DialogHost)
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            CloseButtonAppearance = Ui.ControlAppearance.Primary,
            DefaultButton = Ui.ContentDialogButton.Close
        };

        return dialog.ShowAsync();
    }

    private void GridSplitter_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is GridSplitter splitter)
            splitter.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
    }

    private void GridSplitter_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is GridSplitter splitter)
            splitter.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    }

    private static FlowDocument MakePlaceholderDocument(string message)
    {
        return new FlowDocument(new Paragraph(new Run(message)));
    }

    private void BindKeyboardShortcuts()
    {
        // Bind Ctrl+S to Save Input
        var saveCommand = new RoutedCommand();
        saveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
        CommandBindings.Add(new CommandBinding(saveCommand, SaveInput_Click));
    }

    private async void SaveInput_Click(object sender, RoutedEventArgs e)
    {
        var inputContent = InputHtmlTextBox.Text;

        if (string.IsNullOrWhiteSpace(inputContent))
        {
            await ShowDialogAsync("Warning", "There is no content to save.", Ui.SymbolRegular.Warning24);
            return;
        }

        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|HTML Files (*.html)|*.html|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            Title = "Save HTML Input"
        };

        if (saveFileDialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllText(saveFileDialog.FileName, inputContent);
        }
        catch (Exception exception)
        {
            await ShowDialogAsync("Error", $"An error occurred while saving the file: {exception.Message}",
                Ui.SymbolRegular.ErrorCircle24);
            return;
        }

        await ShowDialogAsync("Success", $"File saved successfully to:\n{saveFileDialog.FileName}",
            Ui.SymbolRegular.CheckmarkCircle24);
    }

    private void ClearInput_Click(object sender, RoutedEventArgs e)
    {
        InputHtmlTextBox.Clear();
        OutputPreviewViewer.Document =
            MakePlaceholderDocument("Preview will appear here after you enter HTML and click Preview.");
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        InputHtmlTextBox.SelectAll();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (InputHtmlTextBox.SelectedText.Length > 0)
            Clipboard.SetText(InputHtmlTextBox.SelectedText);
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
            InputHtmlTextBox.Paste();
    }

    private async void About_Click(object sender, RoutedEventArgs e)
    {
        await ShowDialogAsync(
            "About Fluent HTML Receipt Printer",
            "Fluent HTML Receipt Printer v1.0\n\n" +
            "A fluent interface for printing HTML receipts to thermal printers.\n\n" +
            "© 2026",
            Ui.SymbolRegular.Info24);
    }
}