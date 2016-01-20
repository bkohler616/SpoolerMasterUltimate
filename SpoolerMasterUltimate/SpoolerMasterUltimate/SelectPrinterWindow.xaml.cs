using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;


namespace SpoolerMasterUltimate
{
    /// <summary>
    ///     Interaction logic for SelectPrinterWindow.xaml
    /// </summary>
    public partial class SelectPrinterWindow
    {
        private const int DefaultPausePrintLimit = 10;
        private const int DefaultDeletePrintLimit = 20;
        private const int DefaultPauseComputerPrintTime = 300;

        public SelectPrinterWindow() {
            InitializeComponent();
            TbDeleteLimit.Text = DefaultDeletePrintLimit.ToString();
            TbPauseLimit.Text = DefaultPausePrintLimit.ToString();
            TbPauseComputerLimit.Text = DefaultPauseComputerPrintTime.ToString();
        }

        public bool PrinterGet { get; set; }
        public string PrinterSelection { get; private set; }
        public int PausePrintLimit { get; set; }
        public int DeletePrintLimit { get; set; }
        public int PauseComputerPrintTime { get; set; }
        public bool AltStatusText { get; set; }

        /// <summary>
        ///     Clear the combobox of items, then populate it with currently printer gathered by external source.
        /// </summary>
        /// <param name="printers"></param>
        public void GetNewPrinters(StringCollection printers) {
            CbPrinterSelection.Items.Clear();
            foreach (var printer in printers) CbPrinterSelection.Items.Add(printer);
            if (CbPrinterSelection.Items.Count < 1) MessageBox.Show("Error! No printers installed!");
            else CbPrinterSelection.SelectedIndex = 0;
        }

        /// <summary>
        ///     If the window is closed, hide instead to save resources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectPrinterWindow_OnClosing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            Hide();
        }

        /// <summary>
        ///     Hide the window, then set the printer selection that was obtained,
        ///     and inform objects watching printer selection that is has gathered a new printer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptButton_OnClick(object sender, RoutedEventArgs e) {
            Hide();
            PrinterSelection = CbPrinterSelection.Text;
            var settingsError = false;
            var errorText = "";
            try {
                PausePrintLimit = int.Parse(TbPauseLimit.Text);
            }
            catch {
                PausePrintLimit = DefaultPausePrintLimit;
                errorText += "\n-Pause Print Limit invalid input. Using Default";
                settingsError = true;
            }

            try {
                DeletePrintLimit = int.Parse(TbDeleteLimit.Text);
            }
            catch {
                DeletePrintLimit = DefaultDeletePrintLimit;
                errorText += "\n-Delete Print Limit invalid input. Using Default";
                settingsError = true;
            }

            try {
                PauseComputerPrintTime = int.Parse(TbPauseComputerLimit.Text);
            }
            catch {
                PauseComputerPrintTime = DefaultPauseComputerPrintTime;
                errorText += "\n-Pause Computer Prints Limit invalid input. Using default";
                settingsError = true;
            }

            AltStatusText = CbAltStatText.IsChecked.Value;
            if (settingsError) MessageBox.Show("Error(s):" + errorText);
            PrinterGet = true;
        }
    }
}