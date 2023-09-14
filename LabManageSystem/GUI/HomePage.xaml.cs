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
		if (UIDEntry.Text.Length == 8)
		{
			SprdSht.LoginUser(UIDEntry.Text, "C:\\Users\\batma\\source\\repos\\LabManageSystem\\LabManageSystem\\GUI\\LogDemo.sprd");
            UIDEntry.Text = "";
        }
		
	}
}