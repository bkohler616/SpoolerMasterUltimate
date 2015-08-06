﻿using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SpoolerMasterUltimate {
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow {
		private static string _path;
		private readonly About _aboutWindow;
		private readonly PrintJobManager _printManager;
		private readonly SettingsWindow _settingsWindowAccess;
		private readonly Timer _updateTime;
		private DateTime _currentDateTime;

		public MainWindow() {
			InitializeComponent();
			_updateTime = new Timer {
				Interval = 1000
			};
			_currentDateTime = new DateTime();
			_aboutWindow = new About();
			_updateTime.Elapsed += UpdateTime_Elapsed;
			_updateTime.Start();

			_path = (new FileInfo(Assembly.GetEntryAssembly().Location)).Directory + "//SMU_Settings.xml";

			//If settings save exists, deserialize it to SettingsWindow
			if (File.Exists(_path)) {
				var reader = new XmlSerializer(typeof (SettingsInfo));
				var file = new StreamReader(_path);
				_settingsWindowAccess = new SettingsWindow((SettingsInfo) reader.Deserialize(file));
			}
			else _settingsWindowAccess = new SettingsWindow();
			_settingsWindowAccess.Settings.CloseApplication = false;
			_settingsWindowAccess.Hide();

			_printManager = new PrintJobManager();
		}

		/// <summary>
		///     After every update interval, stop the timer, get the current date, and update the main form via dispatcher.
		///     Most visual updates will be placed within here so it is updated properly via the dispatcher to avoid
		///     getting errors about not having the ability to edit another thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="elapsedEventArgs"></param>
		private void UpdateTime_Elapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
			_updateTime.Stop();
			_currentDateTime = DateTime.Now;
			//Invoke another thread to input content
			Application.Current.Dispatcher.BeginInvoke((Action) delegate {
				SettingsUpdate();
				PrinterUpdate();
			});
			_updateTime.Start();
		}

		private void SettingsUpdate() {
			LblDate.Foreground =
				(SolidColorBrush) new BrushConverter().ConvertFrom("#" + _settingsWindowAccess.Settings.DateTextColor);
			LblTime.Foreground =
				(SolidColorBrush) new BrushConverter().ConvertFrom("#" + _settingsWindowAccess.Settings.TimeTextColor);
			LblDate.Content = _currentDateTime.DayOfWeek + ", " + DateTime.Now.ToString("MMMM") + " (" + _currentDateTime.Month +
			                  "/" + _currentDateTime.Day + "/" +
			                  _currentDateTime.Year + ")";
			LblTime.Content = _currentDateTime.ToString("hh:mm:ss tt");
			LblTime.FontSize = _settingsWindowAccess.Settings.TimeFontSize;
			LblDate.FontSize = _settingsWindowAccess.Settings.DateFontSize;
			BrdrBackground.Background.Opacity = (_settingsWindowAccess.Settings.WindowOpacityPercentage / 100.0);
			WinMainWindowHandler.IsHitTestVisible = _settingsWindowAccess.Settings.ClickThrough;
		}

		private void PrinterUpdate() {
			if (_printManager.PrinterWindow.PrinterGet) {
				_printManager.PrinterWindow.PrinterGet = false;
				_printManager.UpdatePrintQueue();
			}
			else if (_printManager.PrinterConnection) SetPrintStatus();

			lblPrinterStatus.Content = _printManager.CurrentPrinterStatus();
		}

		/// <summary>
		///     On mouse down, move the whole window. (does not work if hit-click check is disabled)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left) DragMove();
		}

		/// <summary>
		///     In context-menu item
		///     On click, shut down the application.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CloseOverlay_Click(object sender, RoutedEventArgs e) {
			_updateTime.Stop();
			Application.Current.Shutdown();
		}

		private void LblDate_OnLoaded(object sender, RoutedEventArgs e) {
			LblDate.Content = "Getting Date...";
		}

		private void LblTime_OnLoaded(object sender, RoutedEventArgs e) {
			LblTime.Content = "Getting Time...";
		}

		/// <summary>
		///     In context-menu item
		///     On click, show the settings window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ShowSettings_Click(object sender, RoutedEventArgs e) {
			_settingsWindowAccess.Show();
		}

		/// <summary>
		///     In context-menu item
		///     On click, show the about window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ShowAbout_Click(object sender, RoutedEventArgs e) {
			_aboutWindow.Show();
		}

		/// <summary>
		///     Try to save the application on close.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
			try {
				var saveSettings = _settingsWindowAccess.Settings;
				var writer = new XmlSerializer(typeof (SettingsInfo));
				var file = File.Create(_path);
				writer.Serialize(file, saveSettings);
				file.Close();
			}
			catch (Exception ex) {
				MessageBox.Show("Error writing settings to document:" + ex.Message + "\n\n\n" + ex.StackTrace);
			}

			_settingsWindowAccess.Settings.CloseApplication = true;
		}

		private void SetPrinter_OnClick(object sender, RoutedEventArgs e) {
			_printManager.GetNewPrinter();
			_printManager.PrinterWindow.Show();
		}

		private void PrintJobDelete_OnClick(object sender, RoutedEventArgs e) {
			_printManager.DeletePrintQueues(dgPrintMonitor.SelectedItems);
		}

		private void PrintJobPause_OnClick(object sender, RoutedEventArgs e) {
			_printManager.PausePrinteQueues(dgPrintMonitor.SelectedItems);
		}

		private void SetPrintStatus() {
			var printData = _printManager.GetPrintData();
			dgPrintMonitor.ItemsSource = printData;
		}
	}
}