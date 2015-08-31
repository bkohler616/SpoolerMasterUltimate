using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management;
using System.Windows;

namespace SpoolerMasterUltimate {
	internal class PrintJobManager {
		private readonly HistoryViewWindow _historyView;

		public PrintJobManager() {
			PrinterWindow = new SelectPrinterWindow();
			GetNewPrinter();
			_historyView = new HistoryViewWindow();
			CollectedHistory = new List<PrintJobData>();
		}

		public ManagementObject Printer { get; set; }
		public ManagementObject PrintQueue { get; set; }
		public bool PrinterConnection { get; private set; }
		public SelectPrinterWindow PrinterWindow { get; }
		private List<PrintJobData> CollectedHistory { get; }

		public void GetNewPrinter() {
			var printerNameCollection = new StringCollection();
			var searchQuery = "SELECT * FROM Win32_Printer";
			var searchPrinters =
				new ManagementObjectSearcher(searchQuery);
			var printerCollection = searchPrinters.Get();
			foreach (ManagementObject printer in printerCollection)
				printerNameCollection.Add(printer.Properties["Name"].Value.ToString());
			PrinterWindow.GetNewPrinters(printerNameCollection);
		}

		public void UpdatePrintQueue() {
			var logBuild = LogManager.LogSectionSeperator("Get Printer Attempt");
			try {
				var printerNameCollection = new StringCollection();
				var searchQuery = "SELECT * FROM Win32_Printer";
				var searchPrinters = new ManagementObjectSearcher(searchQuery);
				var printerCollection = searchPrinters.Get();
				foreach (ManagementObject printer in printerCollection) {
					if (printer.Properties["Name"].Value.ToString() == PrinterWindow.PrinterSelection) {
						//MessageBox.Show(printer.Properties["Location"].Value.ToString());
						Printer = printer;
						PrinterConnection = true;
					}
				}
				LogManager.AppendLog(logBuild);
			}
			catch (Exception ex) {
				LogManager.AppendLog(logBuild + "\n" + LogManager.LogErrorSection + "\nCritical error when getting printer! " +
				                     ex.Message + "\n" + ex.InnerException + "\n" + ex.StackTrace);
			}
		}

		public string CurrentPrinterStatus() {
			var searchQuery = "SELECT * FROM Win32_Printer";
			var searchPrinters = new ManagementObjectSearcher(searchQuery);
			var printerCollection = searchPrinters.Get();
			foreach (ManagementObject printer in printerCollection) {
				if (printer.Properties["Name"].Value.ToString() == PrinterWindow.PrinterSelection)
					return printer.Properties["Status"].Value.ToString();
			}
			return "Printer Not Connected";
		}

		public void Dispose() {}

		public void DeletePrintJobs(IList printData) {
			var logBuild = LogManager.LogSectionSeperator("Delete Print(s) Attempt");
			try {
				var searchQuery = "SELECT * FROM Win32_PrintJob";
				var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
				var printJobCollection = searchPrintJobs.Get();
				foreach (ManagementObject printJob in printJobCollection) {
					var jobId = printJob.Properties["JobId"].Value.ToString();
					foreach (PrintJobData jobEdit in printData) {
						if (jobEdit.JobId.ToString() == jobId)
							printJob.Delete();
					}
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Error on delete: " + ex.Message + "\n\n" + ex.StackTrace);
			}
		}

		public void PausePrintJobs(IList printData) {
			var logBuild = LogManager.LogSectionSeperator("Delete Print(s) Attempt");
			try {
				var searchQuery = "SELECT * FROM Win32_PrintJob";
				var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
				var printJobCollection = searchPrintJobs.Get();
				foreach (ManagementObject printJob in printJobCollection) {
					var jobId = printJob.Properties["JobId"].Value.ToString();
					foreach (PrintJobData jobEdit in printData) {
						if (jobEdit.JobId.ToString() == jobId)
							printJob.InvokeMethod(printJob.Properties["StatusMask"].Value.ToString() == "1" ? "Pause" : "Resume", null);
					}
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Error on delete: " + ex.Message + "\n\n" + ex.StackTrace);
			}
		}

		public List<PrintJobData> GetPrintData() {
			var testItems = "";
			try {
				var searchQuery = "SELECT * FROM Win32_PrintJob";
				var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
				var printJobCollection = searchPrintJobs.Get();
				var printJobs = new List<PrintJobData>();
				foreach (ManagementObject printJob in printJobCollection) {
					var jobDataBuilder = new PrintJobData {
						JobId = int.Parse(printJob.Properties["JobId"].Value.ToString()),
						Size = int.Parse(printJob.Properties["Size"].Value.ToString()),
						Pages = int.Parse(printJob.Properties["TotalPages"].Value.ToString()),
						//Status = GetCurrentStatus(printJob.Properties["StatusMask"].Value.ToString()),
						Status = printJob.Properties["StatusMask"].Value.ToString(),
						TimeStarted = printJob.Properties["TimeSubmitted"].Value.ToString(),
						User = printJob.Properties["Owner"].Value.ToString(),
						DocumentName = printJob.Properties["Document"].Value.ToString(),
						MachineName = printJob.Properties["HostPrintQueue"].Value.ToString()
					};
					if (jobDataBuilder.Pages >= PrinterWindow.DeletePrintLimit) {
						try {
							printJob.Delete();
						}
						catch (Exception ex) {
							MessageBox.Show("Error on auto delete: " + ex.Message + "\n\n" + ex.StackTrace);
						}
					}
					else if (jobDataBuilder.Pages >= PrinterWindow.PausePrintLimit) {
						try {
							printJob.InvokeMethod("Pause", null);
						}
						catch (Exception ex) {
							MessageBox.Show("Error on auto pause: " + ex.Message + "\n\n" + ex.StackTrace);
						}
					}
					CheckPrintHistory(jobDataBuilder);
					printJobs.Add(jobDataBuilder);
				}
				return printJobs;
			}
			catch (Exception ex) {
				MessageBox.Show("Critical error on updating printer information: " + ex.Message + "\n\n" + ex.StackTrace + "\n\n" +
				                ex.InnerException + "\n\nList of items: " + testItems);
			}
			return new List<PrintJobData>();
		}

		private void CheckPrintHistory(PrintJobData newJob) {
			var oldJobId = false;
			foreach (var oldData in CollectedHistory.Where(oldData => oldData.JobId == newJob.JobId)) {
				oldJobId = true;
				oldData.CheckFilledData(newJob);
			}
			if (!oldJobId) CollectedHistory.Add(newJob);
		}

		public void Pause() {
			var searchQuery = "SELECT * FROM Win32_Printer";
			var searchPrinters = new ManagementObjectSearcher(searchQuery);
			var printerCollection = searchPrinters.Get();
			foreach (ManagementObject printer in printerCollection) {
				if (printer.Properties["Name"].Value.ToString() == PrinterWindow.PrinterSelection)
					printer.InvokeMethod(printer.Properties["PrinterState"].Value.ToString() == "1" ? "Pause" : "Resume", null);
			}
		}

		public void ShowHistory() {
			_historyView.ShowHistory(CollectedHistory);
		}

		private string GetCurrentStatus(string numInfo) {
			var status = uint.Parse(numInfo);
			switch (status) {
				case 1:
					return "Paused";
				case 2:
					return "Error";
				case 4:
					return "Deleting";
				case 8:
					return "Spooling";
				case 16:
					return "Printing";
				case 32:
					return "Offline";
				case 64:
					return "Paper Out";
				case 128:
					return "Printed";
				case 256:
					return "Deleted";
				case 512:
					return "Blocked_DevQ";
				case 1024:
					return "User_Intervention_Req";
				case 2048:
					return "Restart";
				default:
					return "Error";
			}
		}
	}
}