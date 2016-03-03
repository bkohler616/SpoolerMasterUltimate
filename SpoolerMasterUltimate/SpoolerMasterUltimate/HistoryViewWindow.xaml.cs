using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;


namespace SpoolerMasterUltimate
{
    /// <summary>
    ///     Interaction logic for HistoryViewWindow.xaml
    /// </summary>
    public partial class HistoryViewWindow : Window
    {
        public HistoryViewWindow() { InitializeComponent(); }

        public void ShowHistory(List<PrintJobData> printInformation) {
            var tempInfo = printInformation.OrderByDescending(c => c.SortingTime);
            Visibility = Visibility.Visible;
            LvPrintHistory.ItemsSource = tempInfo;
        }

        private void HistoryViewWindow_OnClosing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }
    }
}