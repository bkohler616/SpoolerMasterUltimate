using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Printing;
using System.Windows;

namespace SpoolerMasterUltimate {
	internal class PrintJobManager {
		private PrintQueue _mainPrintQueue;

		public PrintJobManager() {
			PrinterWindow = new SelectPrinterWindow();
			LogManager.SetupLog();
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
					_mainPrintQueue = new PrintQueue(MainPrintServer, PrinterWindow.PrinterSelection,
						PrintSystemDesiredAccess.AdministratePrinter);
					PrinterConnection = true;
					logBuild += "\nConnection complete. The print queue is a network printer.";
				}
				else {
					var pqc = MainPrintServer.GetPrintQueues();
					foreach (var pq in pqc) {
						if (pq.FullName == PrinterWindow.PrinterSelection) {
							_mainPrintQueue = pq;
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
						_mainPrintQueue.GetJob(printJob.JobId).Cancel();
						logBuild += logPrintDataBuilder(printJob, "Delete");
					}
					catch (Exception ex) {
						MessageBox.Show("Error on delete attempt. " + ex.Message + "\n\nThis has been placed into the log.");
						logBuild += logPrintDataBuilder(printJob, "Delete Failed", ex.Message + " " + ex.InnerException);
					}
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Error on delete attempt. " + ex.Message + "\n\nThis has been placed into the log.");
				logBuild += LogManager.LogErrorSection + "\nCritical delete method failure. Dump....\n" + ex.Message + " " +
				            ex.InnerException + LogManager.LogErrorSection;
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
					if (_mainPrintQueue.GetJob(printJob.JobId).IsPaused) {
						try {
							_mainPrintQueue.GetJob(printJob.JobId).Resume();
							logBuild += logPrintDataBuilder(printJob, "Resumed");
						}
						catch (Exception ex) {
							MessageBox.Show("Error on resume attempt. " + ex.Message + "\n\nThis has been placed into the log.");
							logBuild += logPrintDataBuilder(printJob, "Resume Failed", ex.Message + " " + ex.InnerException);
							break;
						}
					}

					else {
						try {
							_mainPrintQueue.GetJob(printJob.JobId).Pause();
							logBuild += logPrintDataBuilder(printJob, "Paused");
						}
						catch (Exception ex) {
							MessageBox.Show("Error on pause attempt. " + ex.Message + "\n\nThis has been placed into the log.");
							logBuild += logPrintDataBuilder(printJob, "Pause Failed", ex.Message + " " + ex.InnerException);
							break;
						}
					}
				}
			}
			catch (Exception ex) {
				MessageBox.Show("Critical error on pause attempt(s). Please check the log.");
				logBuild += LogManager.LogErrorSection + "\nCritical pause method failure. Dump....\n" + ex.Message + " " +
				            ex.InnerException + LogManager.LogErrorSection;
			}
			LogManager.AppendLog(logBuild);
		}

		/// <summary>
		///     Get print jobs from MainPrintQueue, and build the job's information in a format to be placed in a DataGrid.
		/// </summary>
		/// <returns>A List(PrintJobData) of print jobs currently being sent to the printer.</returns>
		public List<PrintJobData> GetPrintData() {
			try {
				_mainPrintQueue.Refresh();

				if (_mainPrintQueue.GetPrintJobInfoCollection().Any()) {
					var printJobs = new List<PrintJobData>();
					var logBuild = LogManager.LogSectionSeperator("Print Collection w/ Automation attempt");
					foreach (var job in _mainPrintQueue.GetPrintJobInfoCollection()) {
						var jobDataBuilder = new PrintJobData {
							DocumentName = job.JobName,
							JobId = job.JobIdentifier,
							Size = job.JobSize.ToString(),
							Status = job.JobStatus.ToString(),
							Pages = job.NumberOfPages.ToString(),
							User = job.Submitter
						};
						if (job.NumberOfPages >= PrinterWindow.DeletePrintLimit) {
							try {
								job.Cancel();
								logBuild += logPrintDataBuilder(jobDataBuilder, "Automated Delete");
							}
							catch (Exception ex) {
								logBuild += logPrintDataBuilder(jobDataBuilder, "Automated Delete Failed", ex.Message + " " + ex.InnerException);
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
								logBuild += logPrintDataBuilder(jobDataBuilder, "Automated Pause Failed", ex.Message + " " + ex.InnerException);
								MessageBox.Show("Error! a job could not be Auto-Paused! Please see the log to know why! jobId: " +
								                jobDataBuilder.JobId);
							}
						}
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
				                     "\n\n" + ex.InnerException);
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
			       (_mainPrintQueue.QueueStatus.ToString() == "None"
				       ? "No Print Issues"
				       : ("Print Queue " + _mainPrintQueue.QueueStatus));
		}

		/// <summary>
		///     A (foolish) attempt to properly dispose of the print queue and server so admin rights can be re-obtained on net
		///     execution.
		/// </summary>
		public void Dispose() {
			var logBuild = LogManager.LogSectionSeperator("Disposal of Queues");
			try {
				_mainPrintQueue.Dispose();
			}
			catch (NullReferenceException) {
				logBuild += "\n\tDispose queue called with null refrence. This usually happens with lack of setPrinter.";
			}
			catch (Exception ex) {
				MessageBox.Show("Error on disposing _mainPrintQueue! Please view log for more information!");
				logBuild += "\n\tError on disposing _mainPrintQueue!\n\n" + ex.Message + "\n\n" + ex.InnerException;
			}
			try {
				MainPrintServer.Dispose();
			}
			catch (NullReferenceException) {
				logBuild += "\n\tDispose server called with null refrence. This usually happens with lack of setPrinter.";
			}
			catch (Exception ex) {
				MessageBox.Show("Error on disposing MainPrintServer! Please view log for more information!");
				logBuild += "\n\tError on disposing MainPrintServer!\n\n" + ex.Message + "\n\n" + ex.InnerException;
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
	}
}