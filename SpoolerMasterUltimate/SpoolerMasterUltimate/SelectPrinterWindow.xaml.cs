using System.ComponentModel;
using System.Drawing.Printing;
using System.Windows;

namespace SpoolerMasterUltimate {
	/// <summary>
	///     Interaction logic for SelectPrinterWindow.xaml
	/// </summary>
	public partial class SelectPrinterWindow {
		public SelectPrinterWindow() {
			InitializeComponent();
			tbDeleteLimit.Text = DefaultDeletePrintLimit.ToString();
			tbPauseLimit.Text = DefaultPausePrintLimit.ToString();
		}

		public bool PrinterGet { get; set; }
		public string PrinterSelection { get; private set; }
		  public int PausePrintLimit { get; set; }
		  public int DeletePrintLimit { get; set; }
		private const int DefaultPausePrintLimit = 10;
		private const int DefaultDeletePrintLimit = 20;

		/// <summary>
		///     Clear the combobox of items, then populate it with currently printer gathered by external source.
		/// </summary>
		/// <param name="printers"></param>
		  public void GetNewPrinters(PrinterSettings.StringCollection printers) {
			cbPrinterSelection.Items.Clear();
			foreach (string printer in printers) cbPrinterSelection.Items.Add(printer);
			if (cbPrinterSelection.Items.Count < 1) MessageBox.Show("Error! No printers installed!");
			else cbPrinterSelection.SelectedIndex = 0;
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
			PrinterSelection = cbPrinterSelection.Text;
			bool settingsError = false;
			string errorText = "";
			try {
				PausePrintLimit = int.Parse(tbPauseLimit.Text);
			}
			catch {
				PausePrintLimit = DefaultPausePrintLimit;
				errorText += "\n-Pause Print Limit invalid input. Using Default";
				settingsError = true;
			}

			try {
				DeletePrintLimit = int.Parse(tbDeleteLimit.Text);
			}
			catch {
				DeletePrintLimit = DefaultDeletePrintLimit;
				errorText += "\n-Delete Print Limit invalid input. Using Default";
				settingsError = true;
				}

			if (settingsError) {
				MessageBox.Show("Error(s):" + errorText);
			}
			PrinterGet = true;
		}
	}
}