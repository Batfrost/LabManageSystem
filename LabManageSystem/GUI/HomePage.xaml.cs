using SS;
using System.ComponentModel;
using Sett;

namespace SpreadsheetGUI;

public partial class HomePage : ContentPage
{
	public SpreadsheetPage SprdSht;
	public ManagerPage ManagerPg;
	private Settings Settings;

	public HomePage(Settings sett)
	{
        InitializeComponent();
        SprdSht = new SpreadsheetPage();
		
		this.Settings = sett;
		Dictionary<String, Object> agreementPageInfo = Settings.GetAgreementPageInfo();
		List<String> SpecialFieldsList = (List<String>)agreementPageInfo["SpecialFieldsList"];
        SprdSht.GetCurrentlyLoggedIn(SpecialFieldsList);
        currentlyLoggedIn.Load(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\currentlyLoggedIn.csv");
        try
		{
			if (!SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList()))
				DisplayAlert("User ID Spreadsheet Error", "There was an error checking the student List file, \n please make sure the file is closed and try again.", "Ok");
		} catch (Exception ex){ DisplayAlert("Error", "Problem loading the file with all Users ID's. Error: \n" + ex.Message, "OK."); }
	}



	async void GoToManagerMode(object sender, EventArgs e)
	{
        if (Settings.TestPassword(ManagerPasswordEntry.Text))
			await Navigation.PushAsync(new ManagerPage());
		else await DisplayAlert("Error", "Incorrect Password", "Ok");
		ManagerPasswordEntry.Text = "";
	}


	void LoginUser(object sender, EventArgs e)
	{
		SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList());
        currentlyLoggedIn.Load(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\currentlyLoggedIn.csv");

        string userName = "";
		if (UIDEntry.Text.Length == 8)
		{
			try
			{
                userName = SprdSht.LoginUser(UIDEntry.Text);
            }
			catch
			{
				DisplayAlert("Failure", "There was a problem attempting to edit the log file. \n Make sure all spreadsheet files in the log folder are closed and try again.", "Ok");
				UIDEntry.Text = "";
				return;
			}
			
            UIDEntry.Text = "";
			if (!userName.Equals("NOT FOUND"))
				StudentFindability.Text = userName + DateTime.Now.ToShortTimeString();
            else
				UserAgreementSigning(sender, e);
        }

		//currentlyLoggedIn = new CurrentOccupancyGrid();


    }

	async void UserAgreementSigning(object sender, EventArgs e)
	{
		UserAgreementPage SigningPage = new UserAgreementPage(Settings);
		await Navigation.PushAsync(SigningPage);
	}
}