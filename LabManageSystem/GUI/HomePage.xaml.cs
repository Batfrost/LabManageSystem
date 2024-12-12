using SS;
using System.ComponentModel;
using Sett;

namespace SpreadsheetGUI;

public partial class HomePage : ContentPage
{
	public SpreadsheetPage SprdSht;
	public ManagerPage ManagerPg;
	private Settings Settings;
	static private readonly object lockObject = new object();

	public HomePage(Settings sett)
	{
		lock (lockObject)
		{
			InitializeComponent();
			SprdSht = new SpreadsheetPage();

			Settings = sett;
			SprdSht.LoadSettings();
			Dictionary<String, Object> agreementPageInfo = Settings.GetAgreementPageInfo();
			List<String> SpecialVisibleFields = (List<String>)agreementPageInfo["SpecialVisibleFields"];
			SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList());
			SprdSht.GetCurrentlyLoggedIn(SpecialVisibleFields);
			currentlyLoggedIn.Load(Settings.saveFileLocation + "currentlyLoggedIn.csv");
			try
			{
				if (!SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList()))
					DisplayAlert("User ID Spreadsheet Error", "There was an error checking the student List file, \n please make sure the file is closed and try again.", "Ok");
			}
			catch (Exception ex) { DisplayAlert("Error", "Problem loading the file with all Users ID's. Error: \n" + ex.Message, "OK."); }
		}
	}

	async void GoToManagerMode(object sender, EventArgs e)
	{

		if (Settings.TestPassword(ManagerPasswordEntry.Text))
			await Navigation.PushAsync(new ManagerPage());
		else
		{
			string response = await DisplayActionSheet("Error: Incorrect Password", "Ok", null, "Forgot Password?");
			if (response.Equals("Forgot Password?"))
				await Navigation.PushAsync(new ManagerPasswordPage());
		}
        ManagerPasswordEntry.Text = "";
    }


	void LoginUser(object sender, EventArgs e)
	{
		lock (lockObject)
		{
			SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList());
			currentlyLoggedIn.Load(Settings.saveFileLocation + "currentlyLoggedIn.csv");

			string userName = "";
			if (UIDEntry.Text.Length == 8)
			{
				try
				{
					string ID = UIDEntry.Text;
					Dictionary<String, Object> agreementPageInfo = Settings.GetAgreementPageInfo();
					List<String> SpecialHiddenFields = (List<String>)agreementPageInfo["SpecialHiddenFields"];
					userName = SprdSht.LoginUser(ID, SpecialHiddenFields);

					System.Timers.Timer timer = new System.Timers.Timer();
					timer.Elapsed += new System.Timers.ElapsedEventHandler((sender, e) => AutomaticallyLogOutUser(sender, e, ID));
					timer.Interval = 7200000;
					timer.Enabled = true;
				}
				catch
				{
					DisplayAlert("Failure", "There was a problem attempting to edit the log file. \n Make sure all spreadsheet files in the log folder are closed and try again.", "Ok");
					UIDEntry.Text = "";
					return;
				}


				if (!userName.Equals("NOT FOUND"))
				{
					StudentFindability.Text = userName + DateTime.Now.ToShortTimeString();
				}
				else
					UserAgreementSigning(sender, e);
				UIDEntry.Text = "";
			}
		}
    }

	//After 2 hours, the user will automatically "logout" in that the currentlyLoggedIn sheet won't show them anymore.
	private void AutomaticallyLogOutUser(object sender, EventArgs e, string ID)
	{
		lock (lockObject)
		{
			System.Timers.Timer timer = (System.Timers.Timer)sender;
			timer.Stop();
			SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList());
			SprdSht.LogoutUserFromCurrentlyLoggedInSheet(ID);
			currentlyLoggedIn.Load(Settings.saveFileLocation + "currentlyLoggedIn.csv");
		}
    }


	async void UserAgreementSigning(object sender, EventArgs e)
	{
		UserAgreementPage SigningPage = new UserAgreementPage(Settings);
		await Navigation.PushAsync(SigningPage);
	}
}