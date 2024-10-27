using System.Diagnostics;

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
        await Navigation.PushAsync(SprdSht);
    }

    void OpenSaveLocation(object sender, EventArgs e)
    {
        var psi = new ProcessStartInfo();
        psi.FileName = @"c:\windows\explorer.exe";
        psi.Arguments = @"C:\ProgramData\TWLogging";
        Process.Start(psi);
    }

    async void GoToHomePage(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
        return;
    }

    private async void LookupUserButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LookupUserPage());
    }
}