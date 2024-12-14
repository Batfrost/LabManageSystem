namespace SpreadsheetGUI;

public partial class AddAttendanceTrackerPage : ContentPage
{
	public AddAttendanceTrackerPage()
	{
		InitializeComponent();
	}

    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}