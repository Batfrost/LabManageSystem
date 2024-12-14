namespace SpreadsheetGUI;

public partial class ViewAttendanceTrackersPage : ContentPage
{
	public ViewAttendanceTrackersPage()
	{
		InitializeComponent();
	}
    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}