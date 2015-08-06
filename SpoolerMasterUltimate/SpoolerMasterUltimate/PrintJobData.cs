namespace SpoolerMasterUltimate {
	/// <summary>
	///     PrintJobData is a class that's like PrintSystemJobInfo, but much more slimmed down. This way the dgPrintManager
	///     grid gets populated with only what we want to see.
	/// </summary>
	internal class PrintJobData {
		public int JobId { get; set; }
		public string DocumentName { get; set; }
		public string Status { get; set; }
		public string Pages { get; set; }
		public string Size { get; set; }
		public string User { get; set; }
	}
}