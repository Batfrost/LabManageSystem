using Sett;
using SS;
using System.Diagnostics.Metrics;
using Windows.Globalization.DateTimeFormatting;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SpreadsheetGUI;


public partial class AddAttendanceTrackerPage : ContentPage
{
    Settings Settings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");

    public AddAttendanceTrackerPage()
	{
        InitializeComponent();
        List<string> fields = new List<string>();
        try
        {
            fields = Settings.agreementPageFields.Keys.ToList();
            fields.Add("None");
            InfoFieldsPicker.ItemsSource = fields;
            InfoFieldsPicker.SelectedIndex = 0;
        }
        catch
        {
            fields.Add("None");
            InfoFieldsPicker.ItemsSource = fields;
            InfoFieldsPicker.SelectedIndex = 0;
        }
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

        if (SpecificFieldEntry.Text == "")
        {
            await DisplayAlert("Error", "Please Fill out all fields.", "Ok");
            return;
        }

        if (InfoFieldsPicker.SelectedItem.ToString().Equals("None"))
        {
            await DisplayAlert("Error", "Select an Info Field, new ones\ncan be made on the manager page's\n 'Customize Settings'.", "Ok");
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
        
        Spreadsheet TrackerSheet = new Spreadsheet();
        //On the TrackerSheet, the First Row will contain the name of this class/module, Number of current absences per student, all the dates within the chosen days of the week between the specified To and From date.
        TrackerSheet.SetContentsOfCell("A1", SpecificFieldEntry.Text);
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
        string infoTag = SpecificFieldEntry.Text.Replace(" ", "").ToLower();
        //Go through the user list and add all students there that are specified with the specific info field tag
        try
        {
            Spreadsheet userList = new Spreadsheet(Settings.saveFileLocation + "userList.csv", s => true, s => s.ToUpper(), "lab");
            //First find which column header matches the Selected Info Field.
            string infoCol = userList.cellValues.First(entryLog => entryLog.Value.Equals(InfoFieldsPicker.SelectedItem.ToString())).Key;
            //Check if double letter col
            if (infoCol[1] >= 65)
            {
                infoCol = infoCol[..2];
            }
            else infoCol = infoCol.First().ToString();
            int cellNum = 2;
            int trackerRow = 2;
            while (true)
            {
                if (!userList.cellValues.ContainsKey(infoCol + cellNum.ToString()))
                    break;
                string userFieldInfo = userList.cellValues[infoCol + cellNum.ToString()].ToString().Replace(" ", "").ToLower();
                if (userFieldInfo.Equals(infoTag))
                {
                    TrackerSheet.SetContentsOfCell('A' + trackerRow.ToString(), userList.cellValues['A' + cellNum.ToString()].ToString());
                    TrackerSheet.SetContentsOfCell('B' + trackerRow.ToString(), "0");
                    trackerRow++;
                }
                cellNum++;
            }
        }
        //Just don't add any students here if there is an error accessing the userList.
        catch { }

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