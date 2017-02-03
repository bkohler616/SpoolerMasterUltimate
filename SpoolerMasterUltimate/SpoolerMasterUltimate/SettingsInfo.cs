namespace SpoolerMasterUltimate
{
    public class SettingsInfo
    {
        private const int TimeFontSizeDefault = 30;
        private const int DateFontSizeDefault = 15;
        private const int UpdateIntervalDefault = 500;
        private const string TimeTextColorDefault = "ffffff";
        private const string DateTextColorDefault = "808080";
        private const int WindowOpacityDefault = 50;
        private const bool ClickThroughDefault = true;

        public SettingsInfo() {
            RestoreDefault();
            CloseApplication = false;
        }

        public string TimeTextColor { get; set; }
        public int UpdateInterval { get; set; }
        public string DateTextColor { get; set; }
        public int TimeFontSize { get; set; }
        public int DateFontSize { get; set; }
        public bool CloseApplication { get; set; }
        public int WindowOpacityPercentage { get; set; }
        public bool ClickThrough { get; set; }
        public bool IsChangeMade { get; set; }

        public void DefaultTimeColor() { TimeTextColor = TimeTextColorDefault; }

        public void DefaultDateColor() { DateTextColor = DateTextColorDefault; }

        public void DefaultTimeFontSize() { TimeFontSize = TimeFontSizeDefault; }

        public void DefaultDateFontSize() { DateFontSize = DateFontSizeDefault; }

        public void DefaultOpacityPercentage() { WindowOpacityPercentage = WindowOpacityDefault; }

        public void DefaultUpdateInterval() { UpdateInterval = UpdateIntervalDefault; }

        private void DefaultClickThrough() { ClickThrough = ClickThroughDefault; }

        public void RestoreDefault() {
            DefaultDateColor();
            DefaultTimeColor();
            DefaultDateFontSize();
            DefaultTimeFontSize();
            DefaultOpacityPercentage();
            DefaultUpdateInterval();
            DefaultClickThrough();
        }
    }
}