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
			studentFound = SprdSht.LoginUser(UIDEntry.Text);
            UIDEntry.Text = "";
			if (studentFound)
				StudentFindability.Text = "Student Found";
			else
				StudentFindability.Text = "Student Not Found, sign User Agreement.";
        }
		
	}
}