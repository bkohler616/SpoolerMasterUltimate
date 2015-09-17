namespace SpoolerMasterUltimate {
    /// <summary>
    ///     PrintJobData is a class that's like PrintSystemJobInfo, but much more slimmed down. This way the dgPrintManager
    ///     grid gets populated with only what we want to see.
    /// </summary>
    public class PrintJobData {
        public PrintJobData() {
            JobId = -5;
        }

        public int JobId { get; set; }
        public string Status { private get; set; }
        public int Pages { get; set; }
        public int Size { get; set; }
        public string User { get; set; }
        public string TimeElapsed { private get; set; }
        public string TimeStarted { get; set; }
        public string MachineName { get; set; }
        public string DocumentName { get; set; }

        public void CheckFilledData(PrintJobData newData) {
            if (TimeStarted != newData.TimeStarted) {
                Status = newData.Status;
                Pages = newData.Pages;
                Size = newData.Size;
                User = newData.User;
                MachineName = newData.MachineName;
                DocumentName = newData.DocumentName;
                TimeElapsed = newData.TimeElapsed;
                TimeStarted = newData.TimeStarted;
            }
            else {
                if (newData.Pages > Pages)
                    Pages = newData.Pages;
                if (newData.Size > Size)
                    Size = newData.Size;
                Status = newData.Status;
                TimeElapsed = newData.TimeElapsed;
            }
        }
    }
}