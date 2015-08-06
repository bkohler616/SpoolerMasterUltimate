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
		public bool PrinterConnection { get; private set; }
		public SelectPrinterWindow PrinterWindow { get; }

		/// <summary>
		///     Get the currently installed printers, and populate the PrinterWindow with the printers, and display the window.
		/// </summary>
		public void GetNewPrinter() {
			var printers = PrinterSettings.InstalledPrinters;
			PrinterWindow.GetNewPrinters(printers);
		}

		/// <summary>
		///     Update MainPrintQueue by first determining if the printer is network located or local,
		///     building the server and printer location according to that information, then grabbing
		///     the printer desired by searching through the server for the printer name.
		/// </summary>
		public void UpdatePrintQueue() {
			PrinterConnection = false;
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

			//Iterate through print queues until desired print queue is found.
			var pqc = MainPrintServer.GetPrintQueues();
			foreach (var pq in pqc) {
				if (pq.FullName == PrinterWindow.PrinterSelection) {
					_mainPrintQueue = pq;
					PrinterConnection = true;
				}
			}
		}

		/// <summary>
		///     Delete the print jobs according to job id.
		/// </summary>
		/// <param name="printData"></param>
		public void DeletePrintJobs(IList printData) {
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

		/// <summary>
		///     Pause or unpause print jobs according to job id.
		/// </summary>
		/// <param name="printData"></param>
		public void PausePrintJobs(IList printData) {
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

		/// <summary>
		///     Get print jobs from MainPrintQueue, and build the job's information in a format to be placed in a DataGrid.
		/// </summary>
		/// <returns>A List(PrintJobData) of print jobs currently being sent to the printer.</returns>
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

		/// <summary>
		///     Iterate through the MainPrintQueue's status's, selecting the one that is most important,
		///     and returning that information back to the main window.
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		///     A (foolish) attempt to properly dispose of the print queue and server so admin rights can be re-obtained on net
		///     execution.
		/// </summary>
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