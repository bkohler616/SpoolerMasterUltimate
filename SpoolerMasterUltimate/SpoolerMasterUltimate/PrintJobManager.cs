using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.Printing;
using System.Printing.IndexedProperties;
using System.Windows;

namespace SpoolerMasterUltimate {
	internal class PrintJobManager {
		private PrintQueue _mainPrintQueue;

		public PrintJobManager() {
			PrinterWindow = new SelectPrinterWindow();
			GetNewPrinter();
		}

		public bool PrinterConnection { get; set; }
		public SelectPrinterWindow PrinterWindow { get; }

		public void GetNewPrinter() {
			var printers = PrinterSettings.InstalledPrinters;
			PrinterWindow.GetNewPrinters(printers);
		}

		public void UpdatePrintQueue() {
			MessageBox.Show("Method: UpdatePrintQueue");
			var mainPrintServer = new PrintServer(PrinterWindow.PrinterSelection);
			MessageBox.Show("Main Print Server connection established");
			//_mainPrintQueue = mainPrintServer.GetPrintQueue(PrinterWindow.PrinterSelection); //This sadly doesn't work at all. Printer name is invalid.
			/*_mainPrintQueue =
				mainPrintServer.GetPrintQueue(PrinterSettings.InstalledPrinters[PrinterWindow.PrinterSelectionIndex]);*/ // 

				//The following works for local printer connections just fine.
			var pqc = mainPrintServer.GetPrintQueues();
			MessageBox.Show("Print Queues recieved");
			var printQueues = "Print Queues Found:";
			foreach (var pq in pqc) {
				printQueues += "\n" + pq.Name;
				MessageBox.Show(pq.Name + " Found");
				if (pq.FullName == PrinterWindow.PrinterSelection) {
					MessageBox.Show(pq.Name + " is now Main Print Queue");
					_mainPrintQueue = pq;
					PrinterConnection = true;
				}
			}
			MessageBox.Show(printQueues);
		}

		public void DeletePrintQueues(IList printData) {
			foreach (DataRowView row in printData) {
				var jobId = int.Parse(row["JobId"].ToString());

				_mainPrintQueue.GetJob(jobId).Cancel();
			}
		}

		public void PausePrinteQueues(IList printData) {
			foreach (DataRowView row in printData) {
				var jobId = int.Parse(row["JobId"].ToString());
				if (_mainPrintQueue.GetJob(jobId).IsPaused)
					_mainPrintQueue.GetJob(jobId).Resume();
				else _mainPrintQueue.GetJob(jobId).Pause();
			}
		}

		public List<PrintJobData> GetPrintData() {
			MessageBox.Show("Method: GetPrintData()");
			var printJobs = new List<PrintJobData>();
			foreach (var job in _mainPrintQueue.GetPrintJobInfoCollection()) {
				var jobDataBuilder = new PrintJobData {
					DocumentName = job.JobName,
					JobId = job.JobIdentifier,
					Size = job.JobSize.ToString(),
					Status = job.JobStatus.ToString(),
					Pages = job.NumberOfPages.ToString()
				};
				printJobs.Add(jobDataBuilder);
			}
			MessageBox.Show("Leaving GetPrintData()");
			return printJobs;
		}

		public string CurrentPrinterStatus() {
			if (PrinterConnection == false)
				return "No connection established.";
			if (_mainPrintQueue.IsOffline)
				return "Printer Offline";
			if (_mainPrintQueue.IsDoorOpened)
				return "Printer Door Opened";
			if (_mainPrintQueue.IsPaperJammed)
				return "Printer Jammed";
			if (_mainPrintQueue.HasPaperProblem)
				return "Unknown paper problem";
			if (_mainPrintQueue.IsOutOfPaper)
				return "Printer out of paper";
			if (_mainPrintQueue.NeedUserIntervention)
				return "Printer needs love.";
			return _mainPrintQueue.NumberOfJobs + " jobs, " +
			       (_mainPrintQueue.QueueStatus.ToString() == "None" ? "No Print Issues" : _mainPrintQueue.QueueStatus.ToString());
		}
	}
}