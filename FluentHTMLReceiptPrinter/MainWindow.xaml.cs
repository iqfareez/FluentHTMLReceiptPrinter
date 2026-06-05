using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using PrintHTML.Core.Helpers;
using PrintHTML.Core.Services;

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
    private void PreviewButton_Click(object sender, RoutedEventArgs e)
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
            MessageBox.Show($"An error occurred while generating the preview: {exception.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            OutputPreviewViewer.Document =
                MakePlaceholderDocument("Failed to generate preview. Please check the HTML content and try again.");
        }
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var content = InputHtmlTextBox.Text;
        var selectedPrinter = PrintersComboBox.SelectedItem as string;

        if (string.IsNullOrWhiteSpace(selectedPrinter))
        {
            MessageBox.Show("Please select a printer.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            MessageBox.Show("The content to be printed cannot be empty.", "Warning", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        PrintButton.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            AsyncPrintTask.Exec(true, () => _printerService.DoPrint(content, selectedPrinter, CharsPerLine));
            MessageBox.Show("The printing process was completed successfully.", "Information", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred during printing: {ex.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            PrintButton.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
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

    #region Menu Event Handlers

    private void BindKeyboardShortcuts()
    {
        // Bind Ctrl+S to Save Input
        var saveCommand = new RoutedCommand();
        saveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
        CommandBindings.Add(new CommandBinding(saveCommand, SaveInput_Click));
    }

    private void SaveInput_Click(object sender, RoutedEventArgs e)
    {
        var inputContent = InputHtmlTextBox.Text;

        if (string.IsNullOrWhiteSpace(inputContent))
        {
            MessageBox.Show("There is no content to save.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveFileDialog = new SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|HTML Files (*.html)|*.html|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            Title = "Save HTML Input"
        };

        File.WriteAllText(saveFileDialog.FileName, inputContent);
        MessageBox.Show($"File saved successfully to:\n{saveFileDialog.FileName}", "Success",
            MessageBoxButton.OK, MessageBoxImage.Information);
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

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Fluent HTML Receipt Printer v1.0\n\n" +
            "A fluent interface for printing HTML receipts to thermal printers.\n\n" +
            "© 2026",
            "About Fluent HTML Receipt Printer",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    #endregion
}