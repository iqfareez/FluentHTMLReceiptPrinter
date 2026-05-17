using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
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
        var previewDocument = _printerService.GeneratePreview(htmlInput, selectedPrinter, CharsPerLine);

        OutputPreviewViewer.Document = previewDocument;
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
}