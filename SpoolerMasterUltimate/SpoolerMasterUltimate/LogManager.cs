using System;
using System.IO;
using System.Reflection;

namespace SpoolerMasterUltimate {
    public static class LogManager {
        private const string LogPath = "SMU_PrintLog.log";
        private static string _previousInfo = "";

        public const string LogErrorSection =
            "\r\n##################******************!!!!!!!!!!!!!!!!!!******************##################";

        public static void SetupLog() {
            if (File.Exists(LogPath)) return;
            File.Create(LogPath).Dispose();
            AppendLog("Printer log for SpoolerMasterUltimate.\r\nDate created: " + DateTime.Now +
                      "\r\nVersion that log file was created: " + Assembly.GetExecutingAssembly().GetName().Version +
                      "\r\nFormat of print data info: ::JobID - Pages - Size - User - Action Taken::" +
                      "\r\n********************Beginning log********************");
        }

        public static void AppendLog(string addition) {
            try {
                _previousInfo += addition;
                var logFileInfo = new FileInfo(LogPath);
                if (logFileInfo.Length > 10000000) {
                    File.Delete(LogPath);
                    SetupLog();
                    AppendLog("!!!--Log was purged--!!!");
                }
                using (var sw = File.AppendText(LogPath)) sw.WriteLine(_previousInfo);
                _previousInfo = "";
            }
            catch (IOException ex) {
                _previousInfo += LogErrorSection + "\r\nIOException: " + ex.Message;
            }
        }

        public static string LogSectionSeperator(string sectionAction) {
            return "---\r\n" + sectionAction + " - " + DateTime.Now;
        }
    }
}