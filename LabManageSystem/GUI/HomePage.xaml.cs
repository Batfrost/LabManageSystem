namespace SpreadsheetGUI;

public partial class HomePage : ContentPage
{
	public SpreadsheetPage SprdSht;
	public HomePage()
	{
		SprdSht = new SpreadsheetPage();
		InitializeComponent();
	}

	async void GoToSpreadsheet(object sender, EventArgs e)
	{
		SprdSht = new SpreadsheetPage();
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