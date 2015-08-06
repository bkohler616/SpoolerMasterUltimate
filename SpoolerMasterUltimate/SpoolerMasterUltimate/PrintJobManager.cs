using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.Printing;
using System.Windows;

namespace SpoolerMasterUltimate {
	internal class PrintJobManager {
		private PrintQueue _mainPrintQueue;

		public PrintJobManager() {
			PrinterWindow = new SelectPrinterWindow();
			GetNewPrinter();
		}

		private PrintServer MainPrintServer { get; set; }
		public bool PrinterConnection { get; set; }
		public SelectPrinterWindow PrinterWindow { get; }

		public void GetNewPrinter() {
			var printers = PrinterSettings.InstalledPrinters;
			PrinterWindow.GetNewPrinters(printers);
		}

		public void UpdatePrintQueue() {
			MessageBox.Show("Method: UpdatePrintQueue");

			//Determine if the printer is network located or local.
			var counter = 0;
			bool isNetwork = false, networkInfo = true;
			string serverName = "", printerLocation = "";
			foreach (var character in PrinterWindow.PrinterSelection) {
				if (networkInfo) {
					if (character.Equals('\\')) counter++;
					else if (counter < 2)
						networkInfo = false;
					if (counter == 2)
						isNetwork = true;
					if (counter > 2) {
						printerLocation += character;
						networkInfo = false;
					}
					else if (networkInfo == false) printerLocation += character;
					else serverName += character;
				}
				else printerLocation += character;
			}
			MessageBox.Show("Connection attempt:\nServer name: " + serverName + "\nPrtiner Location: " + printerLocation);
			try {
				MainPrintServer = isNetwork
					? new PrintServer(serverName, PrintSystemDesiredAccess.AdministratePrinter)
					: new PrintServer(serverName + printerLocation, PrintSystemDesiredAccess.AdministratePrinter);
			}
			catch (PrintServerException ex) {
				MessageBox.Show("Error. " + ex.Message + ".. Attempting to connect in user mode.");
				MainPrintServer = isNetwork
					? new PrintServer(serverName)
					: new PrintServer(serverName + printerLocation);
			}

			MessageBox.Show("Main Print Server connection established");
			//_mainPrintQueue = mainPrintServer.GetPrintQueue(PrinterWindow.PrinterSelection); //This sadly doesn't work at all. Printer name is invalid.
			/*_mainPrintQueue =
				mainPrintServer.GetPrintQueue(PrinterSettings.InstalledPrinters[PrinterWindow.PrinterSelectionIndex]);*/ // 

			//The following works for local printer connections just fine.
			/*
					 Network Error: mainPrintServer null reference exception.
					 Dig deeper: main Print server object initialization failure. Cannot find spcified file
				*/
			var pqc = MainPrintServer.GetPrintQueues();
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

				try {
					_mainPrintQueue.GetJob(jobId).Cancel();
				}
				catch (Exception ex) {
					MessageBox.Show("Error on delete attempt. " + ex.Message);
				}
			}
		}

		public void PausePrinteQueues(IList printData) {
			foreach (DataRowView row in printData) {
				var jobId = int.Parse(row["JobId"].ToString());
				if (_mainPrintQueue.GetJob(jobId).IsPaused) {
					try {
						_mainPrintQueue.GetJob(jobId).Resume();
					}
					catch (Exception ex) {
						MessageBox.Show("Error on unpause attempt. " + ex.Message);
					}
				}

				else {
					try {
						_mainPrintQueue.GetJob(jobId).Pause();
					}
					catch (Exception ex) {
						MessageBox.Show("Error on pause attempt. " + ex.Message);
					}
				}
			}
		}

		public List<PrintJobData> GetPrintData() {
			var printJobs = new List<PrintJobData>();
			foreach (var job in _mainPrintQueue.GetPrintJobInfoCollection()) {
				var jobDataBuilder = new PrintJobData {
					DocumentName = job.JobName,
					JobId = job.JobIdentifier,
					Size = job.JobSize.ToString(),
					Status = job.JobStatus.ToString(),
					Pages = job.NumberOfPages.ToString(),
					User = job.Submitter
				};
				if (job.NumberOfPages > 10)
					job.Pause();

				printJobs.Add(jobDataBuilder);
			}
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
			if (_mainPrintQueue.IsTonerLow)
				return "Printer toner low";
			if (_mainPrintQueue.NeedUserIntervention)
				return "Printer needs love.";
			return _mainPrintQueue.NumberOfJobs + " jobs, " +
			       (_mainPrintQueue.QueueStatus.ToString() == "None" ? "No Print Issues" : _mainPrintQueue.QueueStatus.ToString());
		}

		public void Dispose() {
			try {
				_mainPrintQueue.Dispose();
			}
			catch (NullReferenceException) {
				MessageBox.Show("Unable to dispose of print queue properly.");
			}
			try {
				MainPrintServer.Dispose();
			}
			catch (NullReferenceException) {
				MessageBox.Show("Unable to dispose of server handle properly.");
			}
		}
	}
}