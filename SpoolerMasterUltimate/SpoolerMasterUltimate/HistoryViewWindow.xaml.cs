using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            Visibility = Visibility.Visible;
            LvPrintHistory.ItemsSource = printInformation;
            try {
                var view = (CollectionView) CollectionViewSource.GetDefaultView(LvPrintHistory.ItemsSource);
                view.SortDescriptions.Add(new SortDescription("TimeStarted", ListSortDirection.Descending));
            }
            catch (NullReferenceException) {
                LogManager.AppendLog(LogManager.LogErrorSection + "\nNo history to sort.");
            }
        }

        private void HistoryViewWindow_OnClosing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }
    }
}