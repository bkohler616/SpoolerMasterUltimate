using System;
using System.IO;
using System.Reflection;

namespace SpoolerMasterUltimate {
	public static class LogManager {
		private const string LogPath = "SMU_PrintLog.log";

		  public const string LogErrorSection =
			  "\n##################******************!!!!!!!!!!!!!!!!!!******************##################";

		  public static void SetupLog() {
			if (File.Exists(LogPath)) return;
			File.Create(LogPath).Dispose();
		  AppendLog("Printer log for SpoolerMasterUltimate.\nDate created: " + DateTime.Now +
					 "\nVersion that log file was created: " + Assembly.GetExecutingAssembly().GetName().Version +
					 "\nFormat of print data info: ::JobID - Pages - Size - User - Action Taken::" +
					 "\n********************Beginning log********************");
		  }

		public static void AppendLog(string addition) {
				FileInfo logFileInfo = new FileInfo(LogPath);
				if (logFileInfo.Length > 50000000)
				{
					 File.Delete(LogPath);
					 SetupLog();
					 AppendLog("!!!--Log was purged--!!!");
				}
				using (var sw = File.AppendText(LogPath)) sw.WriteLine(addition);
		}

		public static string LogSectionSeperator(string sectionAction) {
				
			return "---\n" + sectionAction + " - " + DateTime.Now;
		}
	}
}