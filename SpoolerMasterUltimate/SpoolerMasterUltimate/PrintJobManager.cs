using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Printing;
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

		public PrintQueue MainPrintQueue { get; private set; }
		private PrintServer MainPrintServer { get; set; }
		public bool PrinterConnection { get; private set; }
		public SelectPrinterWindow PrinterWindow { get; }
		private List<PrintJobData> CollectedHistory { get; }

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
			var logBuild = LogManager.LogSectionSeperator("Get Printer attempt");
			try {
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
							isNetwork = true; //The printer is located on a network!
						if (counter > 2) {
							printerLocation += character;
							networkInfo = false;
						}
						else if (networkInfo == false) printerLocation += character;
						else serverName += character;
					}
					else printerLocation += character;
				}
				//Connect print server according to location breakdown.
				MainPrintServer = isNetwork
					? new PrintServer(serverName)
					: new PrintServer(serverName + printerLocation);

				//Iterate through print queues until desired print queue is found.
				if (isNetwork) {
					MainPrintQueue = new PrintQueue(MainPrintServer, PrinterWindow.PrinterSelection,
						PrintSystemDesiredAccess.AdministratePrinter);
					PrinterConnection = true;
					logBuild += "\nConnection complete. The print queue is a network printer.";
				}
				else {
					var pqc = MainPrintServer.GetPrintQueues();
					foreach (var pq in pqc) {
						if (pq.FullName == PrinterWindow.PrinterSelection) {
							MainPrintQueue = pq;
							PrinterConnection = true;
							logBuild += "\nConnection complete. The print queue is a local printer.";
						}
						if (PrinterConnection)
							return;
					}
					logBuild += "\nConnection could not be made, but no error exception. Posting print information gathered:" +
					            LogManager.LogErrorSection +
					            "\nSelected print queue not found: " + PrinterWindow.PrinterSelection +
					            "\n----------Print server info: \n" + MainPrintServer +
					            LogManager.LogErrorSection;
				}
				LogManager.AppendLog(logBuild);
			}
			catch (Exception ex) {
				LogManager.AppendLog("\n Critical error when getting printer! " + ex.Message);
			}
		}

		/// <summary>
		///     Delete the print jobs according to job id.
		/// </summary>
		/// <param name="printData"></param>
		public void DeletePrintJobs(IList printData) {
			var logBuild = LogManager.LogSectionSeperator("Delete Print(s) attempt");
			try {
				foreach (PrintJobData printJob in printData) {
					try {
						MainPrintQueue.GetJob(printJob.JobId).Cancel();
						logBuild += logPrintDataBuilder(printJob, "Delete");
					}
					catch (Exception ex) {
						MessageBox.Show("Error on delete attempt. " + ex.Message + "\n\nThis has been placed into the log.");
						logBuild += logPrintDataBuilder(printJob, "Delete Failed", ex.Message + " " + ex.StackTrace);
					}
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Error on delete attempt. " + ex.Message + "\n\nThis has been placed into the log.");
				logBuild += LogManager.LogErrorSection + "\nCritical delete method failure. Dump....\n" + ex.Message + " " +
				            ex.StackTrace + LogManager.LogErrorSection;
			}
			LogManager.AppendLog(logBuild);
		}

		/// <summary>
		///     Pause or unpause print jobs according to job id.
		/// </summary>
		/// <param name="printData"></param>
		public void PausePrintJobs(IList printData) {
			var logBuild = LogManager.LogSectionSeperator("Paused Print(s) attempt");
			try {
				foreach (PrintJobData printJob in printData) {
					if (MainPrintQueue.GetJob(printJob.JobId).IsPaused) {
						try {
							MainPrintQueue.GetJob(printJob.JobId).Resume();
							logBuild += logPrintDataBuilder(printJob, "Resumed");
						}
						catch (Exception ex) {
							MessageBox.Show("Error on resume attempt. " + ex.Message + "\n\nThis has been placed into the log.");
							logBuild += logPrintDataBuilder(printJob, "Resume Failed", ex.Message + " " + ex.StackTrace);
							break;
						}
					}

					else {
						try {
							MainPrintQueue.GetJob(printJob.JobId).Pause();
							logBuild += logPrintDataBuilder(printJob, "Paused");
						}
						catch (Exception ex) {
							MessageBox.Show("Error on pause attempt. " + ex.Message + "\n\nThis has been placed into the log.");
							logBuild += logPrintDataBuilder(printJob, "Pause Failed", ex.Message + " " + ex.StackTrace);
							break;
						}
					}
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Critical error on pause attempt(s). Please check the log.");
				logBuild += LogManager.LogErrorSection + "\nCritical pause method failure. Dump....\n" + ex.Message + " " +
				            ex.StackTrace + LogManager.LogErrorSection;
			}
			LogManager.AppendLog(logBuild);
		}

		/// <summary>
		///     Get print jobs from MainPrintQueue, and build the job's information in a format to be placed in a DataGrid.
		/// </summary>
		/// <returns>A List(PrintJobData) of print jobs currently being sent to the printer.</returns>
		public List<PrintJobData> GetPrintData() {
			try {
				MainPrintQueue.Refresh();

				if (MainPrintQueue.GetPrintJobInfoCollection().Any()) {
					var printJobs = new List<PrintJobData>();
					var logBuild = LogManager.LogSectionSeperator("Print Collection w/ Automation attempt");
					foreach (var job in MainPrintQueue.GetPrintJobInfoCollection()) {
						var jobDataBuilder = new PrintJobData {
							JobId = job.JobIdentifier,
							Size = job.JobSize,
							Status = job.JobStatus.ToString(),
							Pages = job.NumberOfPages,
							User = job.Submitter,
							TimeStarted = job.TimeJobSubmitted.ToString("G")
						};
						if (job.NumberOfPages >= PrinterWindow.DeletePrintLimit) {
							try {
								job.Cancel();
								logBuild += logPrintDataBuilder(jobDataBuilder, "Automated Delete");
							}
							catch (Exception ex) {
								logBuild += logPrintDataBuilder(jobDataBuilder, "Automated Delete Failed", ex.Message + " " + ex.StackTrace);
								MessageBox.Show("Error! a job could not be Auto-Deleted! Please see the log to know why! jobId: " +
								                jobDataBuilder.JobId);
							}
						}

						else if (job.NumberOfPages >= PrinterWindow.PausePrintLimit) {
							try {
								job.Pause();
								logBuild += logPrintDataBuilder(jobDataBuilder, "Automated Pause");
							}
							catch (Exception ex) {
								logBuild += logPrintDataBuilder(jobDataBuilder, "Automated Pause Failed", ex.Message + " " + ex.StackTrace);
								MessageBox.Show("Error! a job could not be Auto-Paused! Please see the log to know why! jobId: " +
								                jobDataBuilder.JobId);
							}
						}
						job.Refresh();
						jobDataBuilder.Status = job.JobStatus.ToString();
						CheckPrintHistory(jobDataBuilder);
						LogManager.AppendLog(logBuild);
						printJobs.Add(jobDataBuilder);
					}
					return printJobs;
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Critical error upon updating printer information. Please read the log for more details.");
				LogManager.AppendLog(LogManager.LogErrorSection + "\n" + DateTime.Now +
				                     "\nDumping Error Information from GetPrintData...\n" + ex.Message +
				                     "\n\n" + ex.StackTrace);
			}
			return new List<PrintJobData>();
		}

		/// <summary>
		///     Iterate through the MainPrintQueue's status's, selecting the one that is most important,
		///     and returning that information back to the main window.
		/// </summary>
		/// <returns></returns>
		public string CurrentPrinterStatus() {
			if (PrinterConnection == false)
				return "No connection established.";
			if (MainPrintQueue.IsOffline)
				return "Printer Offline";
			if (MainPrintQueue.IsDoorOpened)
				return "Printer Door Opened";
			if (MainPrintQueue.IsPaperJammed)
				return "Printer Jammed";
			if (MainPrintQueue.HasPaperProblem)
				return "Unknown paper problem";
			if (MainPrintQueue.IsOutOfPaper)
				return "Printer out of paper";
			if (MainPrintQueue.IsTonerLow)
				return "Printer toner low";
			if (MainPrintQueue.NeedUserIntervention)
				return "Printer needs love.";
			return MainPrintQueue.NumberOfJobs + " jobs, " +
			       (MainPrintQueue.QueueStatus.ToString() == "None"
				       ? "No Print Issues"
				       : ("Print Queue " + MainPrintQueue.QueueStatus));
		}

		/// <summary>
		///     A (foolish) attempt to properly dispose of the print queue and server so admin rights can be re-obtained on net
		///     execution.
		/// </summary>
		public void Dispose() {
			var logBuild = LogManager.LogSectionSeperator("Disposal of Queues");
			try {
				MainPrintQueue.Dispose();
			}
			catch (NullReferenceException) {
				logBuild += "\n\tDispose queue called with null refrence. This usually happens with lack of setPrinter.";
			}
			catch (Exception ex) {
				MessageBox.Show("Error on disposing _mainPrintQueue! Please view log for more information!");
				logBuild += "\n\tError on disposing _mainPrintQueue!\n\n" + ex.Message + "\n\n" + ex.StackTrace;
			}
			try {
				MainPrintServer.Dispose();
			}
			catch (NullReferenceException) {
				logBuild += "\n\tDispose server called with null refrence. This usually happens with lack of setPrinter.";
			}
			catch (Exception ex) {
				MessageBox.Show("Error on disposing MainPrintServer! Please view log for more information!");
				logBuild += "\n\tError on disposing MainPrintServer!\n\n" + ex.Message + "\n\n" + ex.StackTrace;
			}

			LogManager.AppendLog(logBuild);
		}

		private string logPrintDataBuilder(PrintJobData printJob, string actionTaken, string advError = "") {
			var logBuilder = "";
			logBuilder += "\n\t:: " + printJob.JobId + " - " + printJob.Pages + " - " + printJob.Size + " - " + printJob.User +
			              " - " +
			              actionTaken + "::";
			if (advError != "") logBuilder += LogManager.LogErrorSection + "\n\t\t-" + advError + LogManager.LogErrorSection;

			return logBuilder;
		}

		private void CheckPrintHistory(PrintJobData newJob) {
			var oldJobId = false;
			foreach (var oldData in CollectedHistory.Where(oldData => oldData.JobId == newJob.JobId)) {
				oldJobId = true;
				oldData.CheckFilledData(newJob);
			}
			if (!oldJobId) CollectedHistory.Add(newJob);
		}

		public void ShowHistory() {
			_historyView.ShowHistory(CollectedHistory);
		}
	}
}