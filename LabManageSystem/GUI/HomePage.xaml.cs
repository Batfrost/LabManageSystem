namespace SpreadsheetGUI;

public partial class HomePage : ContentPage
{
	public SpreadsheetPage SprdSht;
	public HomePage()
	{
		SprdSht = new SpreadsheetPage();
		
		InitializeComponent();
		if (!SprdSht.GetIDList())
			DisplayAlert("User ID Spreadsheet Error", "There was an error checking the student List file, \n please make sure the file is closed and try again.", "Ok");
	}

	async void GoToSpreadsheet(object sender, EventArgs e)
	{
		SprdSht = new SpreadsheetPage();
        if (!SprdSht.GetIDList())
            await DisplayAlert("User ID Spreadsheet Error", "There was an error checking the student List file, \n please make sure the file is closed and try again.", "Ok");
		else
			await Navigation.PushAsync(SprdSht);
	}


	void LoginUser(object sender, EventArgs e)
	{
		bool studentFound = false;
		if (UIDEntry.Text.Length == 8)
		{
			try
			{
                studentFound = SprdSht.LoginUser(UIDEntry.Text);
            }
			catch
			{
				DisplayAlert("Failure", "There was a problem attempting to edit the log file. \n Make sure all spreadsheet files in the log folder are closed and try again.", "Ok");
				UIDEntry.Text = "";
				return;
			}
			
            UIDEntry.Text = "";
			if (studentFound)
				StudentFindability.Text = "Student Logged in: " + DateTime.Now.ToShortTimeString();
			else
				UserAgreementSigning(sender, e);
        }
		
	}

	async void UserAgreementSigning(object sender, EventArgs e)
	{
		UserAgreementPage SigningPage = new UserAgreementPage();
		await Navigation.PushAsync(SigningPage);
	}
}