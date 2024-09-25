namespace SpreadsheetGUI;

public partial class ManagerPage : ContentPage
{
    SpreadsheetPage SprdSht = new SpreadsheetPage();

	public ManagerPage()
	{   
		InitializeComponent();
	}

    async void GoToStatsPage(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new StatisticsPage());
    }

    async void GoToCustomizationPage(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CustomizationPage());
    }

    async void GoToSpreadsheetPage(object sender, EventArgs e)
    {
        SprdSht = new SpreadsheetPage();
        if (!SprdSht.GetIDList())
            await DisplayAlert("User ID Spreadsheet Error", "There was an error checking the student List file, \n please make sure the file is closed and try again.", "Ok");
        else
            await Navigation.PushAsync(SprdSht);
    }

    async void ChangeSaveLocation(object sender, EventArgs e)
    {

    }

    async void GoToHomePage(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
        return;
    }
}