using SS;

namespace SpreadsheetGUI;

public partial class AddAttendanceTrackerPage : ContentPage
{
	public AddAttendanceTrackerPage()
	{
		InitializeComponent();
	}

    async public void Confirm_Clicked(object sender, EventArgs e)
    {
        string response = await DisplayActionSheet("Double Confirmation: Do you wish to fully confirm?", "No", null, "Yes, Confirm");
        if (response == "No")
            return;
        
        if (!MondayBox.IsChecked && !TuesdayBox.IsChecked && !WednesdayBox.IsChecked && !ThursdayBox.IsChecked && !FridayBox.IsChecked)
        {
            await DisplayAlert("Error", "At Least One Day Of the week needs to be checked.", "Ok");
            return;
        }

        if (TrackerName.Text == "")
        {
            await DisplayAlert("Error", "Please Enter A Name for this Tracker.", "Ok");
            return;
        }

        if (StudentList.Text == "")
        {
            await DisplayAlert("Error", "At Least Student Needs to be Included.", "Ok");
            return;
        }

        if (AbsenceCount.Text == "")
        {
            await DisplayAlert("Error", "Fill out Number of Absences Allowed", "Ok");
            return;
        }
        String students = StudentList.Text.Trim(' ');
        if (students.Length % 8 != 0)
        {
            await DisplayAlert("Error", "Combined Count of UID's chars aren't divisible by 8,\nSome of the IDs may be wrong.", "Ok");
            return;
        }

        //Check days of week, all this info will be added to a list which will be added to a spreadsheet later
        List<String> TrackerInfo = new List<String>();
        if (MondayBox.IsChecked)
            TrackerInfo.Add("Monday");
        if (TuesdayBox.IsChecked)
            TrackerInfo.Add("Tuesday");
        if (WednesdayBox.IsChecked)
            TrackerInfo.Add("Wednesday");
        if (ThursdayBox.IsChecked)
            TrackerInfo.Add("Thursday");
        if (FridayBox.IsChecked)
            TrackerInfo.Add("Friday");
        //Check from and to date
        TrackerInfo.Add(FromDate.Date.ToString());
        TrackerInfo.Add(ToDate.Date.ToString());
        //Number of Absences allowed
        TrackerInfo.Add(AbsenceCount.Text);

        //Next we'll go through the student list and separate each ID into a student.
        List<String> theClass = new List<String>();
        for (int i = 0; i < students.Length - 8; i+=8)
        {
            theClass.Add(students.Substring(i, i+8));
        }
        Spreadsheet TrackerSheet = new Spreadsheet();
        //On the TrackerSheet, the First Row will contain the name of this class/module, all the dates within the chosen days of the week between the specified To and From date.
        
    }

    async public void ReturnToMenu(object sender, EventArgs e)
    {
        string response = await DisplayActionSheet("Are you sure you wish to Cancel?", "No", null, "Yes, Cancel");
        if (response == "Yes, Cancel")
        {
            await Navigation.PopAsync();
        }
    }
}