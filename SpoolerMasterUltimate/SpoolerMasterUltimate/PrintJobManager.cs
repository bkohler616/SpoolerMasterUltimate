using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
//Aliases
using PrintJobFlags = SpoolerMasterUltimate.PrinterStatusFlagInfo.PrintJobStatusFlags;
using PrinterFlags = SpoolerMasterUltimate.PrinterStatusFlagInfo.PrinterStatusFlags;


namespace SpoolerMasterUltimate
{
    internal class PrintJobManager
    {
        private readonly HistoryViewWindow _historyView;

        /// <summary>
        ///     Initiallize these objects...
        ///     PritnerWindow
        ///     BlockedUserWindow
        ///     _historyView
        ///     CollectedHistory(list)
        ///     BlockedUsers(list)
        /// </summary>
        public PrintJobManager() {
            PrinterWindow = new SelectPrinterWindow();
            GetNewPrinter();
            _historyView = new HistoryViewWindow();
            BlockedUserWindow = new BlockedUserViewWindow();
            CollectedHistory = new List<PrintJobData>();
            BlockedUsers = new List<PrintJobBlocker>();
        }

        public bool IsPrinterConnected { get; private set; }
        public SelectPrinterWindow PrinterWindow { get; }
        private BlockedUserViewWindow BlockedUserWindow { get; }
        private List<PrintJobData> CollectedHistory { get; }
        private List<PrintJobBlocker> BlockedUsers { get; }
        private List<PrintJobData> CurrentPrintJobs { get; set; }

        public bool IsPrinterListCollected { private get; set; }
        private ManagementObjectCollection PrinterCollection { get; set; }

        /// <summary>
        ///     Get the collection of printers from installed printers.
        ///     A few methods utilize this to get information about the printer.
        /// </summary>
        private void GetPrinterCollection() {
            var logBuild = LogManager.LogSectionSeperator("Get Printer Attempt");
            try {
                var searchQuery = "SELECT * FROM Win32_Printer";
                var searchPrinters = new ManagementObjectSearcher(searchQuery);
                PrinterCollection = searchPrinters.Get();
                IsPrinterListCollected = true;
            }
            catch (Exception ex) {
                LogManager.AppendLog(logBuild + "\r\n" + LogManager.LogErrorSection +
                                     "\r\nCritical error when getting printer! " +
                                     ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace);
            }
            LogManager.AppendLog(logBuild);
        }

        /// <summary>
        ///     Get the collection of printers from SetPrinterSearch, and give the name information to the PrinterWindow
        /// </summary>
        public void GetNewPrinter() {
            var printerNameCollection = new StringCollection();
            if (!IsPrinterListCollected) {
                GetPrinterCollection();
            }
            foreach (var o in PrinterCollection) {
                var printer = (ManagementObject) o;
                printerNameCollection.Add(printer.Properties ["Name"].Value.ToString());
            }
            PrinterWindow.GetNewPrinters(printerNameCollection);
        }

        /// <summary>
        ///     Check if the printer name is connectable.
        /// </summary>
        public void CheckPrinterConnection() {
            var logBuild = LogManager.LogSectionSeperator("Update Print Queue");
            try {
                if (!IsPrinterListCollected) {
                    GetPrinterCollection();
                }

                foreach (var o in PrinterCollection) {
                    var printer = (ManagementObject) o;
                    if (printer.Properties ["Name"].Value.ToString() == PrinterWindow.PrinterSelection) {
                        //MessageBox.Show(printer.Properties["Location"].Value.ToString());
                        IsPrinterConnected = true;
                    }
                }
                LogManager.AppendLog(logBuild);
            }
            catch (Exception ex) {
                LogManager.AppendLog(logBuild + "\r\n" + LogManager.LogErrorSection +
                                     "\r\nCritical error when updating printer info! " +
                                     ex.Message + "\r\n" + ex.InnerException + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        ///     Retrive the printerStatus of the printer selected, if not selected, return "Not Connected"
        /// </summary>
        /// <returns> The printer status.</returns>
        public string CurrentPrinterStatus() {
            RemovedExhaustedPause();

            BlockedUserWindow.BlockedUsers = BlockedUsers;
            if (!IsPrinterListCollected) {
                GetPrinterCollection();
            }
            foreach (var o in PrinterCollection) {
                var printer = (ManagementObject) o;
                if (printer.Properties ["Name"].Value.ToString() == PrinterWindow.PrinterSelection)
                    return GetCurrentStatus(printer.Properties ["ExtendedPrinterStatus"].Value.ToString(), false);
            }
            return "Printer Not Connected";
        }


        /// <summary>
        ///     For every job in the list given, delete each and every one if possible.
        /// </summary>
        /// <param name="printData">A list of print job(s).</param>
        public void DeletePrintJobs(IList printData) {
            var logBuild = LogManager.LogSectionSeperator("Delete Print(s) Attempt");
            try {
                var searchQuery = "SELECT * FROM Win32_PrintJob";
                var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
                var printJobCollection = searchPrintJobs.Get();

                //Start deleting
                foreach (var o in printJobCollection) {
                    var printJob = (ManagementObject) o;
                    var jobId = printJob.Properties ["JobId"].Value.ToString();
                    foreach (PrintJobData jobEdit in printData) {
                        if (jobEdit.JobId.ToString() == jobId) {
                            printJob.Delete();
                            DeleteBlockedJob(jobEdit);
                            logBuild += "\r\nDelete event handled for " + jobEdit.JobId + " : " +
                                        jobEdit.MachineName;
                        }
                    }
                }
            }
            catch (Exception ex) {
                logBuild += "Error on delete: " + ex.Message + "\r\n\r\n" + ex.StackTrace;
            }
            LogManager.AppendLog(logBuild);
        }

        /// <summary>
        ///     For each job in the list, pause every one if possible.
        /// </summary>
        /// <param name="printData">A list of print job(s)</param>
        public void PausePrintJobs(IList printData) {
            var logBuild = LogManager.LogSectionSeperator("Pause Print(s) Attempt");
            try {
                var searchQuery = "SELECT * FROM Win32_PrintJob";
                var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
                var printJobCollection = searchPrintJobs.Get();

                //Start pausing
                foreach (var o in printJobCollection) {
                    var printJob = (ManagementObject) o;
                    var jobId = printJob.Properties ["JobId"].Value.ToString();
                    foreach (PrintJobData jobEdit in printData) {
                        if (jobEdit.JobId.ToString() == jobId) {
                            printJob.InvokeMethod(
                                                  (Convert.ToUInt32(printJob.Properties ["StatusMask"].Value.ToString()) &
                                                   PrintJobFlags.Paused) != 0
                                                      ? "Resume"
                                                      : "Pause", null);
                            logBuild += "\r\nPause/Resume event handled for " + jobEdit.JobId + " : " +
                                        jobEdit.MachineName;
                        }
                    }
                }
            }
            catch (Exception ex) {
                logBuild += "Error on delete: " + ex.Message + "\r\n\r\n" + ex.StackTrace;
            }
            LogManager.AppendLog(logBuild);
        }

        /// <summary>
        ///     The multithreaded version of GetPrintData.
        ///     Searches through Win32_PrintJob to create a collection of printJobs.
        ///     Using the collection, dynamically create threads to take every job
        ///     (in their own thread) to run ThreadedPrintJob([MangementObject]) with
        ///     that threads given Win32_PrintJob ManagementObject.
        /// </summary>
        /// <returns>A list of PrintJobData to be used in a GridView.</returns>
        public List<PrintJobData> GetPrintDataMultithreaded() {
            var logBuilder = LogManager.LogSectionSeperator("Get Print Data [MT]");
            try {
                var searchQuery = "SELECT * FROM Win32_PrintJob";
                var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
                var printJobCollection = searchPrintJobs.Get();
                CurrentPrintJobs = new List<PrintJobData>();
                var printJobTasks = new List<Task>();

                //Start Threading.
                foreach (var o in printJobCollection) {
                    var printJob = (ManagementObject) o;
                    printJobTasks.Add(Task.Factory.StartNew(() => { ThreadedPrintJob(printJob); }));
                }
                Task.WaitAll(printJobTasks.ToArray());
                LogManager.AppendLog(logBuilder);
                return CurrentPrintJobs;
            }
            catch (Exception ex) {
                MessageBox.Show("Critical error on updating printer information: " + ex.Message + "\r\n\r\n" +
                                ex.StackTrace +
                                "\r\n\r\n" +
                                ex.InnerException + "\r\n\r\nList of items: ");
            }
            LogManager.AppendLog(logBuilder);
            return new List<PrintJobData>();
        }

        /// <summary>
        ///     Method that can be used in a single thread.
        ///     Based on the printJob given, build a PrintJobData object,
        ///     run automations on the PrintJobData based on BlockedUsers and autoDelete.
        ///     When finished automating, add it to a list of CurrentPrintJobs.
        /// </summary>
        /// <param name="printJob"> A managementObject of type Win32_PrintJob. </param>
        private void ThreadedPrintJob(ManagementObject printJob) {
            var logBuilder = "";
            var pages = int.Parse(printJob.Properties ["TotalPages"].Value.ToString());
            var jobDataBuilder = new PrintJobData {
                                                      JobId = int.Parse(printJob.Properties ["JobId"].Value.ToString()),
                                                      Size = int.Parse(printJob.Properties ["Size"].Value.ToString()),
                                                      Pages = pages == 0 ? 1 : pages,
                                                      Status = GetCurrentStatus(printJob.Properties ["StatusMask"].Value.ToString(), true),
                                                      TimeStarted = printJob.Properties ["TimeSubmitted"].Value.ToString(),
                                                      User = printJob.Properties ["Owner"].Value.ToString(),
                                                      DocumentName = printJob.Properties ["Document"].Value.ToString(),
                                                      MachineName = printJob.Properties ["HostPrintQueue"].Value.ToString()
                                                  };
            //Set the proper time to a legible fasion.
            var hour = Convert.ToInt32(jobDataBuilder.TimeStarted.Substring(8, 2));
            var isPm = hour%13 < hour;
            hour = isPm ? (hour%13) + 1 : hour;
            var min = jobDataBuilder.TimeStarted.Substring(10, 2);
            var sec = jobDataBuilder.TimeStarted.Substring(12, 2);
            var day = jobDataBuilder.TimeStarted.Substring(6, 2);
            var mon = jobDataBuilder.TimeStarted.Substring(4, 2);
            var year = jobDataBuilder.TimeStarted.Substring(0, 4);
            jobDataBuilder.TimeStarted = hour + ":" + min + ":" + sec + " " + (isPm ? "PM" : "AM") + " - (" + mon + "/" + day + "/" + year + ")";

            //Check for autoPause
            var autoPause = CheckBlockedList(jobDataBuilder);
            if (jobDataBuilder.Pages > PrinterWindow.DeletePrintLimit) {
                try {
                    printJob.Properties ["StatusMask"].Value = (uint) printJob.Properties ["StatusMask"].Value + PrintJobFlags.AutoDelete;
                    jobDataBuilder.Status = GetCurrentStatus(printJob.Properties ["StatusMask"].Value.ToString(), true);
                    printJob.Delete();
                    DeleteBlockedJob(jobDataBuilder);
                }
                catch (Exception ex) {
                    logBuilder += "Error on auto delete for job " + jobDataBuilder.JobId + ": " + ex.Message +
                                  "\r\n\r\n" + ex.StackTrace;
                }
            }
            else if (autoPause || jobDataBuilder.Pages > PrinterWindow.PausePrintLimit) {
                try {
                    printJob.InvokeMethod("Pause", null);
                    printJob.Properties ["StatusMask"].Value = (uint) printJob.Properties ["StatusMask"].Value + PrintJobFlags.AutoPause;
                    jobDataBuilder.Status = GetCurrentStatus(printJob.Properties["StatusMask"].Value.ToString(), true);
                }
                catch (Exception ex) {
                    logBuilder += "Error on auto pause for job " + jobDataBuilder.JobId + ": " + ex.Message +
                                  "\r\n\r\n" + ex.StackTrace;
                }
            }
            logBuilder += "\r\n Job parsed: " + jobDataBuilder.JobId + " : " + jobDataBuilder.MachineName + " : " +
                          (autoPause ? "Paused" : "Allowed");
            LogManager.AppendLog(logBuilder);

            CheckPrintHistory(jobDataBuilder);
            lock (CurrentPrintJobs)
                CurrentPrintJobs.Add(jobDataBuilder);
        }

        /// <summary>
        ///     Deprecated.
        ///     Collect every print job in the currently selected printer.
        ///     This has automation features for autopause and autodelete
        /// </summary>
        /// <returns>A list of jobs in PrintJobData object format. To be used in a UI.</returns>
        public List<PrintJobData> GetPrintData() {
            var logBuilder = LogManager.LogSectionSeperator("Get Print Data");
            try {
                var searchQuery = "SELECT * FROM Win32_PrintJob";
                var searchPrintJobs = new ManagementObjectSearcher(searchQuery);
                var printJobCollection = searchPrintJobs.Get();
                var printJobs = new List<PrintJobData>();
                foreach (var o in printJobCollection) {
                    //Start Threading.
                    var printJob = (ManagementObject) o;
                    var pages = int.Parse(printJob.Properties ["TotalPages"].Value.ToString());
                    var jobDataBuilder = new PrintJobData {
                                                              JobId = int.Parse(printJob.Properties ["JobId"].Value.ToString()),
                                                              Size = int.Parse(printJob.Properties ["Size"].Value.ToString()),
                                                              Pages = pages == 0 ? 1 : pages,
                                                              Status = GetCurrentStatus(printJob.Properties ["StatusMask"].Value.ToString(), true),
                                                              TimeStarted = printJob.Properties ["TimeSubmitted"].Value.ToString(),
                                                              User = printJob.Properties ["Owner"].Value.ToString(),
                                                              DocumentName = printJob.Properties ["Document"].Value.ToString(),
                                                              MachineName = printJob.Properties ["HostPrintQueue"].Value.ToString()
                                                          };
                    
                    var autoPause = CheckBlockedList(jobDataBuilder);
                    if (jobDataBuilder.Pages > PrinterWindow.DeletePrintLimit) {
                        try {
                            printJob.Properties ["StatusMask"].Value = (uint) printJob.Properties ["StatusMask"].Value + PrintJobFlags.AutoDelete;
                            jobDataBuilder.Status = GetCurrentStatus(printJob.Properties ["StatusMask"].Value.ToString(), true);
                            printJob.Delete();
                            DeleteBlockedJob(jobDataBuilder);
                        }
                        catch (Exception ex) {
                            logBuilder += "Error on auto delete for job " + jobDataBuilder.JobId + ": " + ex.Message +
                                          "\r\n\r\n" + ex.StackTrace;
                        }
                    }
                    else if (autoPause || jobDataBuilder.Pages > PrinterWindow.PausePrintLimit) {
                        try {
                            printJob.InvokeMethod("Pause", null);
                        }
                        catch (Exception ex) {
                            logBuilder += "Error on auto pause for job " + jobDataBuilder.JobId + ": " + ex.Message +
                                          "\r\n\r\n" + ex.StackTrace;
                        }
                    }
                    logBuilder += "\r\n Job parsed: " + jobDataBuilder.JobId + " : " + jobDataBuilder.MachineName + " : " +
                                  (autoPause ? "Paused" : "Allowed");
                    CheckPrintHistory(jobDataBuilder);
                    printJobs.Add(jobDataBuilder);
                }
                LogManager.AppendLog(logBuilder);
                return printJobs;
            }
            catch (Exception ex) {
                MessageBox.Show("Critical error on updating printer information: " + ex.Message + "\r\n\r\n" +
                                ex.StackTrace +
                                "\r\n\r\n" +
                                ex.InnerException + "\r\n\r\nList of items: ");
            }
            LogManager.AppendLog(logBuilder);
            return new List<PrintJobData>();
        }

        /// <summary>
        ///     Search the history. If the job is new, add it to the list. If not, see if the old history is updateable.
        /// </summary>
        /// <param name="newJob">PrintJobData of the job that needs to be checked for history references.</param>
        private void CheckPrintHistory(PrintJobData newJob) {
            lock (CollectedHistory) {
                var oldJobId = false;
                foreach (var oldData in CollectedHistory.Where(oldData => oldData.JobId == newJob.JobId)) {
                    oldJobId = true;
                    oldData.CheckFilledData(newJob);
                }
                if (!oldJobId) CollectedHistory.Add(newJob);
            }
        }

        /// <summary>
        ///     Search the block list. If the job is new, add it to the list. If not, update the user's current record.
        /// </summary>
        /// <param name="newJob">PrintJobData of the job that needs to be added to the block list.</param>
        /// <returns>A boolean if the job needs to be paused or not.</returns>
        private bool CheckBlockedList(PrintJobData newJob) {
            var oldUserName = false;
            foreach (var oldData in BlockedUsers.Where(oldData => (oldData.MachineName == newJob.MachineName) && (oldData.UserName == newJob.User))) {
                oldUserName = true;
                oldData.UpdateBlocker(newJob);
            }
            if (!oldUserName) {
                BlockedUsers.Add(new PrintJobBlocker(newJob, PrinterWindow.PauseComputerPrintTime,
                                                     PrinterWindow.PausePrintLimit));
            }
            foreach (var oldData in BlockedUsers) {
                if ((oldData.MachineName == newJob.MachineName) && (oldData.UserName == newJob.User))
                    return oldData.Paused;
            }
            return false;
        }

        /// <summary>
        ///     Delete the selected job from the blocked users list. This removes that job's allocation. Keeps previous blocked
        ///     allocation.
        /// </summary>
        /// <param name="deleteJob">PrintJobData that needs to be cleared from the list.</param>
        private void DeleteBlockedJob(PrintJobData deleteJob) {
            foreach (var data in BlockedUsers) {
                if (data.MachineName == deleteJob.MachineName)
                    data.DeleteJob(deleteJob);
            }
        }

        /// <summary>
        ///     Blocked users that have reached the time exauhstion will be deleted.
        /// </summary>
        private void RemovedExhaustedPause() {
            var tempStore = BlockedUsers.ToArray();
            foreach (var blockedUser in tempStore) {
                if (blockedUser.TimeExhausted)
                    BlockedUsers.Remove(blockedUser);
            }
            tempStore = BlockedUsers.ToArray();
            BlockedUserWindow.RefreshUsers();
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

        /// <summary>
        ///     Pause the printer queue. This is not a hardware level pause, as the printer itself will
        ///     not report that it is paused, only the driver will.
        /// </summary>
        public void PausePrinter() {
            if (!IsPrinterListCollected) {
                GetPrinterCollection();
            }
            foreach (var o in PrinterCollection) {
                var printer = (ManagementObject) o;
                if (printer.Properties ["Name"].Value.ToString() == PrinterWindow.PrinterSelection) {
                    printer.InvokeMethod(printer.Properties ["ExtendedPrinterStatus"].Value.ToString() ==
                                         PrinterFlags.Paused.ToString()
                                             ? "Resume"
                                             : "Pause", null);
                }
            }
        }

        /// <summary>
        ///     Display the history.
        /// </summary>
        public void ShowHistory() { _historyView.ShowHistory(CollectedHistory); }

        /// <summary>
        ///     Get the current status of the job or printer.
        /// </summary>
        /// <param name="numInfo">An unparsed string containing a uint according to what parameters the job/printer has.</param>
        /// <param name="printJobOrPrinter">True for print job, false for printer.</param>
        /// <returns></returns>
        private string GetCurrentStatus(string numInfo, bool printJobOrPrinter) {
            var status = uint.Parse(numInfo);
            var statusBuilder = "";
            if (printJobOrPrinter) {
                if ((status & PrintJobFlags.Error) != 0) statusBuilder += "Error - ";
                if ((status & PrintJobFlags.Restart) != 0) statusBuilder += "Restart - ";
                if ((status & PrintJobFlags.UserInterventionReq) != 0) statusBuilder += "Printer Needs Love - ";
                if ((status & PrintJobFlags.BlockedDevQ) != 0) statusBuilder += "The gate is barred - ";
                if ((status & PrintJobFlags.Deleted) != 0) statusBuilder += "Deleted - ";
                if ((status & PrintJobFlags.Printed) != 0) statusBuilder += "Printed - ";
                if ((status & PrintJobFlags.Paperout) != 0) statusBuilder += "No Paper - ";
                if ((status & PrintJobFlags.Offline) != 0) statusBuilder += "Offline - ";
                if ((status & PrintJobFlags.Printing) != 0) statusBuilder += "Printing - ";
                if ((status & PrintJobFlags.Spooling) != 0) statusBuilder += "Spooling - ";
                if ((status & PrintJobFlags.Deleting) != 0) statusBuilder += "Deleting - ";
                if ((status & PrintJobFlags.Paused) != 0) statusBuilder += "Paused - ";
                if ((status & PrintJobFlags.AutoPause) != 0) statusBuilder += "AutoPaused - ";
                if ((status & PrintJobFlags.AutoDelete) != 0) statusBuilder += "Auto Deleted";
            }
            else {
                if (PrinterWindow.AltStatusText) {
                    if (status == PrinterFlags.ManualFeed) statusBuilder += "Reload Paper. Insert Deeply.";
                    if (status == PrinterFlags.IoActive) statusBuilder += "Wow IOActive!";
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
                    if (status == PrinterFlags.IoActive) statusBuilder += "Printer IO Active";
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

        /// <summary>
        ///     Display the blocked users window.
        /// </summary>
        public void ShowBlockedUsers() { BlockedUserWindow.ShowBlockedUsers(BlockedUsers); }

        /// <summary>
        ///     Clear the blocked users.
        /// </summary>
        public void PurgeBlockedUsers() { BlockedUsers.Clear(); }
    }
}