using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Windows;
using System.Windows.Controls;

namespace SpoolerMasterUltimate
{
	 /// <summary>
	 /// Interaction logic for SelectPrinterWindow.xaml
	 /// </summary>
	 public partial class SelectPrinterWindow : Window {
		 public bool PrinterGet { get; set; } = false;
		 public SelectPrinterWindow() {
				InitializeComponent();
		 }

		 public void GetNewPrinters(PrinterSettings.StringCollection printers) {
			 cbPrinterSelection.Items.Clear();
			 foreach (string printer in printers)
				{
					 cbPrinterSelection.Items.Add(printer);
				}
			 if (cbPrinterSelection.Items.Count < 1) {
				 MessageBox.Show("Error!");
			 }
			 else {
				 cbPrinterSelection.SelectedIndex = 1;
			 }
		 }

		  public string PrinterSelection { get; set; }

		 private void SelectPrinterWindow_OnClosing(object sender, CancelEventArgs e) {
				e.Cancel = true;
				this.Hide();
		 }

		 private void AcceptButton_OnClick(object sender, RoutedEventArgs e) {
				this.Hide();
				PrinterSelection = cbPrinterSelection.Text;
				MessageBox.Show(PrinterSelection);
			 PrinterGet = true;
		 }
	 }
}
