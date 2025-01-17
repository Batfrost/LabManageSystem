using Sett;
using SS;

namespace SpreadsheetGUI;

public partial class EditAttendanceTrackerPage : ContentPage
{
    Settings s = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
    Spreadsheet TrackerSheet = null;
    public EditAttendanceTrackerPage()
	{
		InitializeComponent();
        
        List<string> trackers = new List<string>();
        if (s.AttendanceTrackers != null)
            trackers = s.AttendanceTrackers;
        //Double check that all trackers exist and are able to be loaded, if not, remove the tracker that isn't working and continue
        for (int i = 0; i < trackers.Count; i++)
        {
            try
            {
                TrackerSheet = new Spreadsheet(s.saveFileLocation + "AttendanceTrackers\\" + trackers[i] + ".csv", s => true, s => s.ToUpper(), "lab");
            }
            catch
            {
                //This Tracker doesn't exist here anymore, (it got deleted or moved), so remove it from settings, save, and continue
                s.AttendanceTrackers.Remove(trackers[i]);
                s.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");

            }
        }
        TrackerList.ItemsSource = trackers;
        TrackerList.SelectedIndex = 0;
        if (s.AttendanceTrackers == null || s.AttendanceTrackers.Count == 0)
            TrackerList.ItemsSource = new List<string>() { "None" };
    }

    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void TrackerList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (TrackerList.SelectedItem.Equals("None"))
            return;
        try
        {
            TrackerSheet = new Spreadsheet(s.saveFileLocation + "AttendanceTrackers\\" + TrackerList.SelectedItem + ".csv", s => true, s => s.ToUpper(), "lab");
        }
        catch
        {
            //If an error occurs here, then the file was moved somewhere else or is deleted, so just remove from settings
            TrackerList.SelectedItem = "None";
        }
    }

    private void AddToTrackerButton_Clicked(object sender, EventArgs e)
    {
        if (TrackerList.SelectedItem.Equals("None"))
        {
            DisplayAlert("Error", "Please select a Tracker to Edit first.", "Ok");
            return;
        }
        if (AddToTrackerEntry.Text == "" || AddToTrackerEntry.Text.Length != 8)
        {
            DisplayAlert("Error", "Please enter an ID to add, or double check it is 8 characters.", "Ok");
            return;
        }
        try
        {
            //First figure out where the next empty row is.
            string row = "2";
            while (TrackerSheet.cellValues.ContainsKey("A" + row))
            {
                row = (int.Parse(row) + 1).ToString();
            }

            //Now we can put in this student, and figure out if they have been logging in for the tracker or not and update their absences
            string student = "u" + AddToTrackerEntry.Text[1..];
            string test = "Test";
            try
            {
                //Check if the ID exists in this tracker or not already
                test = TrackerSheet.cellValues.First(entryLog => entryLog.Value.Equals(student)).ToString();
            }
            catch
            {
                test = "Test";
            }
            if (!test.Equals("Test"))
            {
                DisplayAlert("Error", "That ID was already in the Tracker.", "Ok");
                return;
            }
            TrackerSheet.SetContentsOfCell("A" + row, student);
            TrackerSheet.SetContentsOfCell("B" + row, "0");


            DateTime fromDate = DateTime.Parse(TrackerSheet.cellValues["C1"].ToString());
            DateTime DayToCheck;
            string cellLetter = "C";
            while (TrackerSheet.cellValues.ContainsKey(cellLetter + "1"))
            {
                DayToCheck = DateTime.Parse(TrackerSheet.cellValues[cellLetter + "1"].ToString());
                //Attempt to open up the log for the day getting checked, and see whether the current student getting checked or not logged in then
                try
                {
                    Spreadsheet SheetToCheck = new Spreadsheet(s.saveFileLocation + "Logs\\" + DateTime.Now.ToString("yyyy-MMMM") + "\\log" + DayToCheck.ToString().Split(" ").First().Replace("/", "-") + ".csv", s => true, s => s.ToUpper(), "lab");
                    
                    if (SheetToCheck.cellValues.ContainsValue(student))
                        TrackerSheet.SetContentsOfCell(cellLetter + row, "yes");
                    else
                    {
                        if (DayToCheck != DateTime.Today)
                        {
                            TrackerSheet.SetContentsOfCell(cellLetter + row, "no");
                            string prevAbsenceCount = TrackerSheet.cellValues['B' + row].ToString()!;
                            int absenceCount = int.Parse(prevAbsenceCount);
                            TrackerSheet.SetContentsOfCell('B' + row, (absenceCount + 1).ToString());
                        }
                        else
                        {
                            break;
                        }

                    }
                    
                }
                catch
                {
                    //Only reason we might be here is if the day we are checking, nobody logged in and thus a log wasn't created.
                    //So if that date we were trying to check is before the current date, then just put a no and update absence count.
                    if (DayToCheck < DateTime.Today)
                    {
                        TrackerSheet.SetContentsOfCell(cellLetter + row, "no");
                        string prevAbsenceCount = TrackerSheet.cellValues['B' + row].ToString()!;
                        int absenceCount = int.Parse(prevAbsenceCount);
                        TrackerSheet.SetContentsOfCell('B' + row, (absenceCount + 1).ToString());
                    }
                }
                if (DayToCheck >= DateTime.Today)
                    break;
                
                cellLetter = ((char)(cellLetter[0] + 1)).ToString();
            }
        
        }
        catch (Exception ex) 
        {
            DisplayAlert("Error", "There was a problem adding to this tracker: \n" + ex.Message, "Ok");
            return;
        }
        TrackerSheet.Save(s.saveFileLocation + "AttendanceTrackers\\" + TrackerList.SelectedItem + ".csv");
        DisplayAlert("Success", "Student has successfully been added to Tracker.", "Ok");
    }

    private void RemoveFromTrackerButton_Clicked(object sender, EventArgs e)
    {
        if (RemoveFromTrackerEntry.Text == "" ||  RemoveFromTrackerEntry.Text.Length != 8)
        {
            DisplayAlert("Error", "Please enter an ID to remove, or double check it is 8 characters.", "Ok");
            return;
        }
        try
        {
            string row = "";
            try
            {
                //Check if the ID exists in this tracker or not.
                row = TrackerSheet.cellValues.First(entryLog => entryLog.Value.Equals("u" + RemoveFromTrackerEntry.Text[1..])).Key[1..];
            }
            catch
            {
                DisplayAlert("Error", "That ID was not found in the tracker.", "Ok");
                return;
            }
            //Now that we have the row we are planning to remove,
            //We will find the last row too, to replace this row and then become empty.
            string lastRow = "2";
            while (TrackerSheet.cellValues.ContainsKey("A" + lastRow))
            {
                lastRow = (int.Parse(lastRow) + 1).ToString();
            }
            lastRow = (int.Parse(lastRow) - 1).ToString();
            //Check whether they are just the same row, if so just replace with empty string.
            bool sameRow = false;
            if (lastRow.Equals(row))
                sameRow = true;
            List<string> lastRowInfo = new List<string>();
            string cellLetter = "A";
            while (TrackerSheet.cellValues.ContainsKey(cellLetter + lastRow))
            {
                lastRowInfo.Add(TrackerSheet.cellValues[cellLetter + lastRow].ToString());
                TrackerSheet.SetContentsOfCell(cellLetter + lastRow, "");
                cellLetter = ((char)(cellLetter[0] + 1)).ToString();
            }
            if (!sameRow)
            {
                cellLetter = "C";
                TrackerSheet.SetContentsOfCell("A"+ row, lastRowInfo[0]);
                TrackerSheet.SetContentsOfCell("B" + row, lastRowInfo[1]);
                for (int i = 2; i < lastRowInfo.Count; i++)
                {
                    TrackerSheet.SetContentsOfCell(cellLetter + row, lastRowInfo[i]);
                    cellLetter = ((char)(cellLetter[0] + 1)).ToString();
                }
                //If for whatever reason this row had more things in it, just remove them.
                while (TrackerSheet.cellValues.ContainsKey(cellLetter + row))
                {
                    TrackerSheet.SetContentsOfCell(cellLetter + row, "");
                    cellLetter = ((char)(cellLetter[0] + 1)).ToString();
                }
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", "There was a problem removing from this tracker: \n" + ex.Message, "Ok");
            return;
        }
        TrackerSheet.Save(s.saveFileLocation + "AttendanceTrackers\\" + TrackerList.SelectedItem + ".csv");
        DisplayAlert("Success", "Successfully removed that ID from the tracker.", "Ok");
    }

    private void RemoveFromTrackerEntry_Completed(object sender, EventArgs e)
    {
        string row;
        try
        {
            //Check if the ID exists in this tracker or not.
            row = TrackerSheet.cellValues.First(entryLog => entryLog.Value.Equals("u" + RemoveFromTrackerEntry.Text[1..])).Key[1..];
        }
        catch
        {
            IDSearchLabel.Text = "ID Not Found in this Tracker.";
            return;
        }
        IDSearchLabel.Text = "ID Was Found in this Tracker.";
    }
}