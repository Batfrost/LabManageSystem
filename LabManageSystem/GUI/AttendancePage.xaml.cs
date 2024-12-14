namespace SpreadsheetGUI;

public partial class AttendancePage : ContentPage
{
	public AttendancePage()
	{
		InitializeComponent();
	}

    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void AddAttendanceClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddAttendanceTrackerPage());
    }

    private async void ViewAttendanceTrackerButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ViewAttendanceTrackersPage());
    }
}