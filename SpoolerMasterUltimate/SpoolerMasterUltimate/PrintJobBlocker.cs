using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace SpoolerMasterUltimate {
    public class PrintJobBlocker {
        public PrintJobBlocker(PrintJobData printInfo, int setTime, int prntLimit) {
            MachineName = printInfo.MachineName;
            UserName = printInfo.User;
            TimeRemaining = setTime;
            TimeAlloted = setTime;
            PagesAllocated = printInfo.Pages;
            PrintLimit = prntLimit;
            TimeExhausted = false;
            Paused = printInfo.Pages > PrintLimit;
            PreviousDocument = new List<int>();
            PreviousDocumentPageChange = new List<int>();
            PreviousDocument.Add(printInfo.JobId);
            PreviousDocumentPageChange.Add(printInfo.Pages);
            UpdateTimer = new Timer {Interval = 1000};
            UpdateTimer.Elapsed += UpdateTimerOnElapsed;
            UpdateTimer.Start();
        }

        public string MachineName { get; set; }
        public string UserName { get; set; }
        public bool Paused { get; set; }
        public bool TimeExhausted { get; set; }
        public bool CancelBlocking { get; set; } = false;
        public int TimeRemaining { get; set; }
        private int TimeAlloted { get; }
        public int PagesAllocated { get; set; }
        private Timer UpdateTimer { get; }
        public int PrintLimit { get; set; }
        public List<int> PreviousDocument { get; set; }
        public List<int> PreviousDocumentPageChange { get; set; }

        private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
            TimeRemaining += -1;
            if (TimeRemaining <= 0) TimeExhausted = true;
        }

        public void UpdateBlocker(PrintJobData newPrintInfo) {
            if (PreviousDocument.Any(pd => pd == newPrintInfo.JobId)) {
                var index = PreviousDocument.IndexOf(newPrintInfo.JobId);
                if (PreviousDocumentPageChange[index] < newPrintInfo.Pages) {
                    PreviousDocumentPageChange[index] = newPrintInfo.Pages;
                    PagesAllocated = PreviousDocumentPageChange.Sum();
                    Paused = PagesAllocated > PrintLimit;
                }
            }
            else {
                TimeRemaining = TimeAlloted;
                PreviousDocument.Add(newPrintInfo.JobId);
                PreviousDocumentPageChange.Add(newPrintInfo.Pages);
                PagesAllocated = PreviousDocumentPageChange.Sum();
                Paused = PagesAllocated > PrintLimit;
            }
        }

        public void DeleteJob(PrintJobData jobToDelete) {
            var index = PreviousDocument.IndexOf(jobToDelete.JobId);
            PreviousDocument.RemoveAt(index);
            PreviousDocumentPageChange.RemoveAt(index);
            PagesAllocated = PreviousDocumentPageChange.Sum();
            Paused = PagesAllocated > PrintLimit;
        }
    }
}