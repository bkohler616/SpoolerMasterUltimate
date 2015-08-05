using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace SpoolerMasterUltimate {
	/// <summary>
	///     Interaction logic for About.xaml
	/// </summary>
	public partial class About : Window {
		public About() {
			InitializeComponent();
			Hide();
			PreNameText.Text = "Application Developer: ";
			AppDevName.Text = "Benjamin Kohler";
			VersionNumber.Text = "Version: " + Assembly.GetExecutingAssembly().GetName().Version;
			GithubLinkTimeOverlay.Text = "See the TimeOverlay base project on my github!";
			GithubLinkSpoolerMasterUltimate.Text = "See this SpoolerMasterUltimate project on my github!";
			ApplicationDesc.Text =
				"This spooler project is for use on spooling printers! The project is licensed under the GNU GPL 2 License. Please only use this application and the source code as the license states.";
		}

		/// <summary>
		///     On GitHub Link Click
		///     Start process to open browser to the TimeOverlay project on Github.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GithubLinkTimeOverlay_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
			Process.Start("http://github.com/riku12124/TimeOverlay");
		}

		/// <summary>
		///     On GitHub Link Click
		///     Start process to open browser to the SpoolerMasterUltimate project on Github.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GithubLinkSpoolerMasterUltimate_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
			Process.Start("http://github.com/riku12124/SpoolerMasterUltimate");
		}

		/// <summary>
		///     On Window Closing Hide window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void About_OnClosing(object sender, CancelEventArgs e) {
			e.Cancel = true;
			Hide();
		}

		/// <summary>
		///     On Developer Name Click Open link to his website.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BenWebsite_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
			Process.Start("http://BenKohler.com");
		}
	}
}