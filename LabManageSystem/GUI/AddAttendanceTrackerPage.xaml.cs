using Sett;
using SS;
using System.Diagnostics.Metrics;
using Windows.Globalization.DateTimeFormatting;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        System.String students = StudentList.Text.Replace("\r", string.Empty).Replace(" ", "");
        students = students.Trim(' ').Trim('\n');
        if ((students.Length) % 8 != 0)
        {
            await DisplayAlert("Error", "Combined Count of UID's chars aren't divisible by 8,\nSome of the IDs may be wrong.", "Ok");
            return;
        }

        //Check days of week, all this info will be added to a list which will be added to a spreadsheet later
        List<int> DaysOfWeekInfo = new List<int>();
        if (MondayBox.IsChecked)
            DaysOfWeekInfo.Add(1);
        if (TuesdayBox.IsChecked)
            DaysOfWeekInfo.Add(2);
        if (WednesdayBox.IsChecked)
            DaysOfWeekInfo.Add(3);
        if (ThursdayBox.IsChecked)
            DaysOfWeekInfo.Add(4);
        if (FridayBox.IsChecked)
            DaysOfWeekInfo.Add(5);

        //Next we'll go through the student list and separate each ID into a student.
        List<string> theClass = new List<string>();
        for (int i = 0; i < students.Length; i+=8)
        {
            theClass.Add(students.Substring(i, 8));
        }
        Spreadsheet TrackerSheet = new Spreadsheet();
        //On the TrackerSheet, the First Row will contain the name of this class/module, Number of current absences per student, all the dates within the chosen days of the week between the specified To and From date.
        TrackerSheet.SetContentsOfCell("A1", TrackerName.Text);
        TrackerSheet.SetContentsOfCell("B1", "Absences");

        //First figure out the dates to be added to the Sheet based on days of week and the from and to dates.
        string cellLetter = "C";
        DateTime fromDate = FromDate.Date;
        DateTime toDate = ToDate.Date;
        int year = fromDate.Year;
        int month = fromDate.Month;
        int day = fromDate.Day;
        bool doubleLettersReached = false;
        int weekNum = 0;
        for (; year <= toDate.Year; year++)
        {
            int tillMonth = 12;
            if (toDate.Year == year)
            {
                tillMonth = toDate.Month;
            }
            for (; month <= tillMonth; month++)
            {
                int tillDay = DateTime.DaysInMonth(year, month);
                if (month == toDate.Month)
                    tillDay = toDate.Day;
                
                for (; day <= tillDay; day++)
                {
                    DateTime date = new DateTime(year, month, day);
                    if (date.DayOfWeek == DayOfWeek.Sunday)
                        weekNum++;
                    
                    for (int DayOfWeek = 0; DayOfWeek < DaysOfWeekInfo.Count; DayOfWeek++)
                        if ((int)date.DayOfWeek == DaysOfWeekInfo[DayOfWeek])
                        {
                            try
                            {
                                if (BiweeklyCheckBox.IsChecked)
                                {
                                    if (weekNum % 2 == 0)
                                        TrackerSheet.SetContentsOfCell(cellLetter + "1", date.ToString("MM/dd/yyyy"));
                                    else
                                    {
                                        continue;
                                    }
                                    
                                }
                                else TrackerSheet.SetContentsOfCell(cellLetter + "1", date.ToString("MM/dd/yyyy"));
                            } 
                            catch
                            {
                                await DisplayAlert("Error", "Too many Dates Selected. Spreadsheet only\n has capacity for 52 total days, sorry. Planned for \nsemesters which are ~15 weeks with usually 2-3 days class per week.", "Ok");
                                return;
                            }
                            if (cellLetter.First() <= 'Z' && !doubleLettersReached)
                                if (cellLetter.First() == 'Z')
                                {
                                    cellLetter = "AA";
                                    doubleLettersReached = true;
                                }
                                else cellLetter = ((char)(cellLetter[0] + 1)).ToString();
                            else
                            {
                                cellLetter = "A" + ((char)(cellLetter[1] + 1)).ToString();
                            }
                            break;
                        }
                }
                day = 1;
            }
            month = 1;
        }

        //Now to add the students/class to the A column
        for (int i = 0; i < theClass.Count; i++)
        {
            TrackerSheet.SetContentsOfCell("A" + (i+2).ToString(), "u" + theClass[i][1..]);
            TrackerSheet.SetContentsOfCell("B" + (i + 2).ToString(), "0");
        }
        Settings s = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        s.AttendanceTrackers.Add(TrackerName.Text);
        s.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        TrackerSheet.Save(s.saveFileLocation + "AttendanceTrackers\\" + TrackerName.Text + ".csv");
        await DisplayAlert("Finished", "Attendance Tracker Finished! You can now view it through any \nSpreadsheet software, or on the previous page.\nTo view file location, you can open the file location on the Manager Menu Page.", "Ok");
        await Navigation.PopAsync();
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