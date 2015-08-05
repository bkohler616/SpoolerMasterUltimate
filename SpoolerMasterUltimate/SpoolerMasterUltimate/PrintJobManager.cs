using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.Printing;

namespace SpoolerMasterUltimate {
	internal class PrintJobManager {
		private readonly SelectPrinterWindow printerWindow;
		private PrintQueue mainPrintQueue;
		public bool PrinterConnection { get; set; } = false;

		public PrintJobManager() {
			printerWindow = new SelectPrinterWindow();
			GetNewPrinter();
		}

		public void GetNewPrinter() {
			var printers = PrinterSettings.InstalledPrinters;
			printerWindow.GetNewPrinters(printers);
		}

		public void PopulatePrinterInformation() {
				
			UpdatePrintQueue();
		}

		public SelectPrinterWindow PrinterWindow => printerWindow;

		public void UpdatePrintQueue() {
				PrintServer mainPrintServer = new PrintServer(printerWindow.PrinterSelection);
				PrintQueueCollection pqc = mainPrintServer.GetPrintQueues();
			foreach (PrintQueue pq in pqc) {
				if (pq.FullName == printerWindow.PrinterSelection)
					mainPrintQueue = pq;
			}
		}

		public void DeletePrintQueues(IList printData) {
			foreach (DataRowView row in printData) {
				int jobId = int.Parse( row["JobId"].ToString());
				
					mainPrintQueue.GetJob(jobId).Cancel();
			}
		}

		public void PausePrinteQueues(IList printData) {
			foreach (DataRowView row in printData) {
				int jobId = int.Parse(row["JobId"].ToString());
					if(mainPrintQueue.GetJob(jobId).IsPaused)
								mainPrintQueue.GetJob(jobId).Resume();
					else {
						mainPrintQueue.GetJob(jobId).Pause();
					}
			}
		}

		public List<PrintJobData> getPrintData() {
			List<PrintJobData> printJobs = new List<PrintJobData>();
			foreach (var job in mainPrintQueue.GetPrintJobInfoCollection()) {
				PrintJobData jobDataBuilder = new PrintJobData {
					DocumentName = job.JobName,
					JobId = job.JobIdentifier,
					Size = job.JobSize.ToString(),
					Status = job.JobStatus.ToString(),
					Pages = job.NumberOfPages.ToString()
				};
				printJobs.Add(jobDataBuilder);
			}
			return printJobs;
		} 
	}
}