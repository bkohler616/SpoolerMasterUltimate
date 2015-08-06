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
		}

		public bool PrinterGet { get; set; }
		public string PrinterSelection { get; private set; }

		public void GetNewPrinters(PrinterSettings.StringCollection printers) {
			cbPrinterSelection.Items.Clear();
			foreach (string printer in printers) cbPrinterSelection.Items.Add(printer);
			if (cbPrinterSelection.Items.Count < 1) MessageBox.Show("Error! No printers installed!");
			else cbPrinterSelection.SelectedIndex = 0;
		}

		private void SelectPrinterWindow_OnClosing(object sender, CancelEventArgs e) {
			e.Cancel = true;
			Hide();
		}

		private void AcceptButton_OnClick(object sender, RoutedEventArgs e) {
			Hide();
			PrinterSelection = cbPrinterSelection.Text;
			PrinterGet = true;
		}
	}
}