using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace SpoolerMasterUltimate {
    /// <summary>
    ///     Interaction logic for BlockedUserViewWindow.xaml
    /// </summary>
    public partial class BlockedUserViewWindow : Window {
        public BlockedUserViewWindow() {
            InitializeComponent();
            BlockedUsers = new List<PrintJobBlocker>();
        }

        public bool RemoveBlocked { get; set; }
        public List<PrintJobBlocker> BlockedUsers { get; private set; }

        public void ShowBlockedUsers(List<PrintJobBlocker> newBlockedUsers) {
            Visibility = Visibility.Visible;
            BlockedUsers = newBlockedUsers;
            lvBlockedUsers.ItemsSource = BlockedUsers;
            try {
                var view = (CollectionView) CollectionViewSource.GetDefaultView(lvBlockedUsers.ItemsSource);
                view.SortDescriptions.Add(new SortDescription("ComputerName", ListSortDirection.Descending));
            }
            catch (NullReferenceException) {
                LogManager.AppendLog(LogManager.LogErrorSection + "\nNo blocked users to sort");
            }
        }

        private void RemoveBlock_OnClick(object sender, RoutedEventArgs e) {
            RemoveBlocked = true;
            foreach (PrintJobBlocker blockedUser in lvBlockedUsers.SelectedItems) blockedUser.CancelBlocking = true;
        }

        private void BlockedUserViewWindow_OnClosing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            Visibility = Visibility.Hidden;
        }
    }
}