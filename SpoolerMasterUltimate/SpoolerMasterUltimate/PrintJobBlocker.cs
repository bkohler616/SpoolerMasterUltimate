using System.Timers;

namespace SpoolerMasterUltimate {
    public class PrintJobBlocker {
        public PrintJobBlocker(PrintJobData printInfo, int setTime, int prntLimit) {
            ComputerName = printInfo.MachineName;
            UserName = printInfo.User;
            TimeRemaining = setTime;
            TimeAlloted = setTime;
            PagesAllocated = printInfo.Pages;
            PrintLimit = prntLimit;
            TimeExhausted = false;
            Paused = printInfo.Pages > PrintLimit;
            PreviousDocument = printInfo.JobId;
            UpdateTimer = new Timer {Interval = 1000};
            UpdateTimer.Elapsed += UpdateTimerOnElapsed;
            UpdateTimer.Start();
        }

        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public bool Paused { get; set; }
        public bool TimeExhausted { get; set; }
        public bool CancelBlocking { get; set; } = false;
        public int TimeRemaining { get; set; }
        private int TimeAlloted { get; }
        public int PagesAllocated { get; set; }
        private Timer UpdateTimer { get; }
        public int PrintLimit { get; set; }
        public int PreviousDocument { get; set; }
        public int PreviousDocumentPageChange { get; set; }

        private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
            TimeRemaining += -1;
            if (TimeRemaining <= 0) TimeExhausted = true;
        }

        public void UpdateBlocker(PrintJobData newPrintInfo) {
            if (PreviousDocument != newPrintInfo.JobId) {
                TimeRemaining = TimeAlloted;
                PagesAllocated += newPrintInfo.Pages;
                Paused = PagesAllocated > PrintLimit;
                PreviousDocument = newPrintInfo.JobId;
                PreviousDocumentPageChange = newPrintInfo.Pages;
            }
            else {
                if (PreviousDocumentPageChange > newPrintInfo.Pages) {
                    PagesAllocated = (PagesAllocated - PreviousDocumentPageChange) + newPrintInfo.Pages;
                    PreviousDocumentPageChange = newPrintInfo.Pages;
                }
            }
        }

        public void Refresh() {
            TimeRemaining = TimeAlloted;
        }
    }
}