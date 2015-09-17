using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management;
using System.Windows;
//Aliases
using PrintJobFlags = SpoolerMasterUltimate.PrinterStatusFlagInfo.PrintJobStatusFlags;
using PrinterFlags = SpoolerMasterUltimate.PrinterStatusFlagInfo.PrinterStatusFlags;

namespace SpoolerMasterUltimate {
    internal class PrintJobManager {
        private readonly HistoryViewWindow _historyView;

        public PrintJobManager() {
            PrinterWindow = new SelectPrinterWindow();
            GetNewPrinter();
            _historyView = new HistoryViewWindow();
            BlockedUserWindow = new BlockedUserViewWindow();
            CollectedHistory = new List<PrintJobData>();
            BlockedUsers = new List<PrintJobBlocker>();
        }

        public bool PrinterConnection { get; private set; }
        public SelectPrinterWindow PrinterWindow { get; }
        private BlockedUserViewWindow BlockedUserWindow { get; }
        private List<PrintJobData> CollectedHistory { get; }
        private List<PrintJobBlocker> BlockedUsers { get; }

        public void GetNewPrinter() {
            var printerNameCollection = new StringCollection();
            var searchQuery = "SELECT * FROM Win32_Printer";
            var searchPrinters =
                new ManagementObjectSearcher(searchQuery);
            var printerCollection = searchPrinters.Get();
            foreach (var o in printerCollection) {
                var printer = (ManagementObject) o;
                printerNameCollection.Add(printer.Properties["Name"].Value.ToString());
            }
            PrinterWindow.GetNewPrinters(printerNameCollection);
        }

        public void UpdatePrintQueue() {
            var logBuild = LogManager.LogSectionSeperator("Get Printer Attempt");
            try {
                var searchQuery = "SELECT * FROM Win32_Printer";
                var searchPrinters = new ManagementObjectSearcher(searchQuery);
                var printerCollection = searchPrinters.Get();
                foreach (var o in printerCollection) {
                    var printer = (ManagementObject) o;
                    if (printer.Properties["Name"].Value.ToString() == PrinterWindow.PrinterSelection) {
                        //MessageBox.Show(printer.Properties["Location"].Value.ToString());
                        PrinterConnection = true;
                    }
                }
                LogManager.AppendLog(logBuild);
            }
            catch (Exception ex) {
                LogManager.AppendLog(logBuild + "\n" + LogManager.LogErrorSection +
                                     "\nCritical error when getting printer! " +
                                     ex.Message + "\n" + ex.InnerException + "\n" + ex.StackTrace);
            }
        }

        public string CurrentPrinterStatus() {
            RemovedExhaustedPause();
            var searchQuery = "SELECT * FROM Win32_Printer";
            var searchPrinters = new ManagementObjectSearcher(searchQuery);
            var printerCollection = searchPrinters.Get();
            foreach (var o in printerCollection) {
                var printer = (ManagementObject) o;
                if (printer.Properties["Name"].Value.ToString() == PrinterWindow.PrinterSelection) 
                    return GetCurrentStatus(printer.Properties["ExtendedPrinterStatus"].Value.ToString(), false);
            }
            return "Printer Not Connected";
        }


        public void DeletePrintJobs(IList printData) {
            var logBuild = LogManager.LogSectionSeperator("Delete Print(s) Attempt");
            try {
                var searchQuery = "SELECT * FROM Win32_PrintJob";
                var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
                var printJobCollection = searchPrintJobs.Get();
                foreach (var o in printJobCollection) {
                    var printJob = (ManagementObject) o;
                    var jobId = printJob.Properties["JobId"].Value.ToString();
                    foreach (PrintJobData jobEdit in printData) {
                        if (jobEdit.JobId.ToString() == jobId) {
                            printJob.Delete();
                            logBuild += "\nDelete event handled for " + jobEdit.JobId + " : " +
                                        jobEdit.MachineName;
                        }
                    }
                }
            }
            catch (Exception ex) {
                logBuild += "Error on delete: " + ex.Message + "\n\n" + ex.StackTrace;
            }
            LogManager.AppendLog(logBuild);
        }

        public void PausePrintJobs(IList printData) {
            var logBuild = LogManager.LogSectionSeperator("Pause Print(s) Attempt");
            try {
                var searchQuery = "SELECT * FROM Win32_PrintJob";
                var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
                var printJobCollection = searchPrintJobs.Get();
                foreach (var o in printJobCollection) {
                    var printJob = (ManagementObject) o;
                    var jobId = printJob.Properties["JobId"].Value.ToString();
                    foreach (PrintJobData jobEdit in printData) {
                        if (jobEdit.JobId.ToString() == jobId) {
                            printJob.InvokeMethod(
                                (Convert.ToUInt32(printJob.Properties["StatusMask"].Value.ToString()) & PrintJobFlags.Paused) != 0 ? "Resume" : "Pause", null);
                            logBuild += "\nPause/Resume event handled for " + jobEdit.JobId + " : " +
                                        jobEdit.MachineName;
                            
                        }
                    }
                }
            }
            catch (Exception ex) {
                logBuild += ("Error on delete: " + ex.Message + "\n\n" + ex.StackTrace);
            }
            LogManager.AppendLog(logBuild);
        }

        public List<PrintJobData> GetPrintData() {
            var logBuilder = LogManager.LogSectionSeperator("Get Print Data");
            try {
                var searchQuery = "SELECT * FROM Win32_PrintJob";
                var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
                var printJobCollection = searchPrintJobs.Get();
                var printJobs = new List<PrintJobData>();
                foreach (var o in printJobCollection) {
                    var printJob = (ManagementObject) o;
                    int pages = int.Parse(printJob.Properties["TotalPages"].Value.ToString());
                    var jobDataBuilder = new PrintJobData {
                        JobId = int.Parse(printJob.Properties["JobId"].Value.ToString()),
                        Size = int.Parse(printJob.Properties["Size"].Value.ToString()),
                        Pages = pages == 0 ? 1 : pages,
                        //Status = GetCurrentStatus(printJob.Properties["StatusMask"].Value.ToString()),
                        Status = GetCurrentStatus(printJob.Properties["StatusMask"].Value.ToString(), true),
                        TimeStarted = printJob.Properties["TimeSubmitted"].Value.ToString(),
                        User = printJob.Properties["Owner"].Value.ToString(),
                        DocumentName = printJob.Properties["Document"].Value.ToString(),
                        MachineName = printJob.Properties["HostPrintQueue"].Value.ToString()
                    };

                    var autoPause = CheckBlockedList(jobDataBuilder);
                    if (jobDataBuilder.Pages >= PrinterWindow.DeletePrintLimit) {
                        try {
                            printJob.Delete();
                        }
                        catch (Exception ex) {
                            logBuilder += ("Error on auto delete for job " + jobDataBuilder.JobId + ": " + ex.Message +
                                           "\n\n" + ex.StackTrace);
                        }
                    }
                    else if (autoPause || jobDataBuilder.Pages > PrinterWindow.PausePrintLimit) {
                        try {
                            printJob.InvokeMethod("Pause", null);
                        }
                        catch (Exception ex) {
                            logBuilder += ("Error on auto pause for job " + jobDataBuilder.JobId + ": " + ex.Message +
                                           "\n\n" + ex.StackTrace);
                        }
                    }
                    logBuilder += "\n Job parsed: " + jobDataBuilder.JobId + " : " + jobDataBuilder.MachineName + " : " +
                                  (autoPause
                                      ? "Paused"
                                      : "Allowed");
                    CheckPrintHistory(jobDataBuilder);
                    printJobs.Add(jobDataBuilder);
                }
                LogManager.AppendLog(logBuilder);
                return printJobs;
            }
            catch (Exception ex) {
                MessageBox.Show("Critical error on updating printer information: " + ex.Message + "\n\n" + ex.StackTrace +
                                "\n\n" +
                                ex.InnerException + "\n\nList of items: ");
            }
            LogManager.AppendLog(logBuilder);
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

        private bool CheckBlockedList(PrintJobData newJob) {
            var oldUserName = false;
            foreach (var oldData in BlockedUsers.Where(oldData => oldData.MachineName == newJob.MachineName)) {
                oldUserName = true;
                oldData.UpdateBlocker(newJob);
            }
            if (!oldUserName) {
                BlockedUsers.Add(new PrintJobBlocker(newJob, PrinterWindow.PauseComputerPrintTime,
                    PrinterWindow.PausePrintLimit));
            }
            foreach (var oldData in BlockedUsers) {
                if (oldData.MachineName == newJob.MachineName)
                    return oldData.Paused;
            }
            return false;
        }

        private void RemovedExhaustedPause() {
            var tempStore = BlockedUsers.ToArray();
            foreach (var blockedUser in tempStore) {
                if (blockedUser.TimeExhausted)
                    BlockedUsers.Remove(blockedUser);
            }
            tempStore = BlockedUsers.ToArray();
            var tempStoreBlockCancels = BlockedUserWindow.BlockedUsers.ToArray();
            if (!BlockedUserWindow.RemoveBlocked) return;
            foreach (var blockedUser in tempStore) {
                foreach (var deleteBlock in tempStoreBlockCancels) {
                    if (blockedUser.MachineName != deleteBlock.MachineName) continue;
                    if (deleteBlock.CancelBlocking)
                        BlockedUsers.Remove(blockedUser);
                }
            }
            BlockedUserWindow.RemoveBlocked = false;
        }

        public void PausePrinter() {
            var searchQuery = "SELECT * FROM Win32_Printer";
            var searchPrinters = new ManagementObjectSearcher(searchQuery);
            var printerCollection = searchPrinters.Get();
            foreach (var o in printerCollection) {
                var printer = (ManagementObject) o;
                if (printer.Properties["Name"].Value.ToString() == PrinterWindow.PrinterSelection) {
                    printer.InvokeMethod(printer.Properties["ExtendedPrinterStatus"].Value.ToString() ==
                        PrinterFlags.Paused.ToString()
                            ? "Resume"
                            : "Pause", null);
                }
            }
        }

        public void ShowHistory() {
            _historyView.ShowHistory(CollectedHistory);
        }

        /// <summary>
        /// </summary>
        /// <param name="numInfo">an unparsed string containing a uint</param>
        /// <param name="printJobOrPrinter"> true for print job, false for printer.</param>
        /// <returns></returns>
        private string GetCurrentStatus(string numInfo, bool printJobOrPrinter) {
            var status = uint.Parse(numInfo);
            string statusBuilder = "";
            if (printJobOrPrinter) {
                if ((status & PrintJobFlags.Error) != 0) statusBuilder += "Error - ";
                if ((status & PrintJobFlags.Restart) != 0) statusBuilder += "Restart - ";
                if ((status & PrintJobFlags.User_Intervention_Req) != 0) statusBuilder += "Printer Needs Love - ";
                if ((status & PrintJobFlags.Blocked_DevQ) != 0) statusBuilder += "The gate is barred - ";
                if ((status & PrintJobFlags.Deleted) != 0) statusBuilder += "Deleted - ";
                if ((status & PrintJobFlags.Printed) != 0) statusBuilder += "Printed - ";
                if ((status & PrintJobFlags.Paperout) != 0) statusBuilder += "No Paper - ";
                if ((status & PrintJobFlags.Offline) != 0) statusBuilder += "Offline - ";
                if ((status & PrintJobFlags.Printing) != 0) statusBuilder += "Printing - ";
                if ((status & PrintJobFlags.Spooling) != 0) statusBuilder += "Spooling - ";
                if ((status & PrintJobFlags.Deleting) != 0) statusBuilder += "Deleting - ";
                if ((status & PrintJobFlags.Paused) != 0) statusBuilder += "Paused";
            }
            else {
                if (PrinterWindow.AltStatusText) {
                    if (status == PrinterFlags.ManualFeed) statusBuilder += "Reload Paper. Insert Deeply.";
                    if (status == PrinterFlags.IOActive) statusBuilder += "Wow IOActive!";
                    if (status == PrinterFlags.PendingDeletion) statusBuilder += "Job on the chopping block";
                    if (status == PrinterFlags.PowerSave) statusBuilder += "Counting electric sheep";
                    if (status == PrinterFlags.Initalization) statusBuilder += "Preparing for the uprising...";
                    if (status == PrinterFlags.Processing) statusBuilder += "Sharpening knives...";
                    if (status == PrinterFlags.Waiting) statusBuilder += "SmokeDayErryWeed";
                    if (status == PrinterFlags.NotAvailable) statusBuilder += "Printer has left this plane of existance";
                    if (status == PrinterFlags.Busy) statusBuilder += "Thinking... and thinking.... Fried!";
                    if (status == PrinterFlags.Error) statusBuilder += "Printer has ascended";
                    if (status == PrinterFlags.Paused) statusBuilder += "Here there be dragons!";
                    if (status == PrinterFlags.Offline) statusBuilder += "Printer has ragequit";
                    if (status == PrinterFlags.StoppedPrinting) statusBuilder += "The ride is comming to a stop";
                    if (status == PrinterFlags.WarmingUp) statusBuilder += "Gassing the chambers";
                    if (status == PrinterFlags.Printing) statusBuilder += "I finally got a job guys!";
                    if (status == PrinterFlags.Idle) statusBuilder += "Call IT. They can do it.";
                    if (status == PrinterFlags.Unknown) statusBuilder += "Doing my \"job\"";
                    if (status == PrinterFlags.Other) statusBuilder += "People don't think it be like it is, but it do.";
                }
                else {
                    if (status == PrinterFlags.ManualFeed) statusBuilder += "Printer Manual Feed Required";
                    if (status == PrinterFlags.IOActive) statusBuilder += "Printer IO Active";
                    if (status == PrinterFlags.PendingDeletion) statusBuilder += "Printer is Deleting Stuff";
                    if (status == PrinterFlags.PowerSave) statusBuilder += "Printer In Power Save Mode";
                    if (status == PrinterFlags.Initalization) statusBuilder += "Printer Initializing...";
                    if (status == PrinterFlags.Processing) statusBuilder += "Printer is Processing";
                    if (status == PrinterFlags.Waiting) statusBuilder += "Printer Waiting";
                    if (status == PrinterFlags.NotAvailable) statusBuilder += "Printer not available";
                    if (status == PrinterFlags.Busy) statusBuilder += "Printer Busy";
                    if (status == PrinterFlags.Error) statusBuilder += "Printer Error!";
                    if (status == PrinterFlags.Paused) statusBuilder += "Printer is Paused";
                    if (status == PrinterFlags.Offline) statusBuilder += "Printer is Offline";
                    if (status == PrinterFlags.StoppedPrinting) statusBuilder += "Printer stopped printing!";
                    if (status == PrinterFlags.WarmingUp) statusBuilder += "Printer warming up.";
                    if (status == PrinterFlags.Printing) statusBuilder += "Printer is Printing.";
                    if (status == PrinterFlags.Idle) statusBuilder += "Printer Idle.";
                    if (status == PrinterFlags.Unknown) statusBuilder += "Connected: No status reported.";
                    if (status == PrinterFlags.Other) statusBuilder += "Printer Other Issue!";
                }
            }
            return statusBuilder;
        }
    
        public void ShowBlockedUsers() {
            BlockedUserWindow.ShowBlockedUsers(BlockedUsers);
        }

        public void PurgeBlockedUsers() {
            BlockedUsers.Clear();
        }
    }
}