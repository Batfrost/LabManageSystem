using Sett;
using SS;

namespace SpreadsheetGUI;

public partial class LookupUserPage : ContentPage
{
    List<String> InfoFields = new List<String>();
    String IDCell = "";
    int FieldCount = 0;
	public LookupUserPage()
	{
        InitializeComponent();
        try
        {
            Settings S = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
            Dictionary<String, Object> agreementPageInfo = S.GetAgreementPageInfo();
            List<String> SpecialVisibleFields = (List<String>)agreementPageInfo["SpecialVisibleFields"];
            List<String> SpecialHiddenFields = (List<String>)agreementPageInfo["SpecialHiddenFields"];
            InfoFields = SpecialVisibleFields;
            InfoFields.AddRange(SpecialHiddenFields);
            FieldCount = InfoFields.Count;
            for (int i = 0; i < InfoFields.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        Field1L.IsEnabled = true;
                        Field1E.IsEnabled = true;
                        Field1L.IsVisible = true;
                        Field1E.IsVisible = true;
                        Field1L.Text = InfoFields[i];
                        break;
                    case 1:
                        Field2L.IsEnabled = true;
                        Field2E.IsEnabled = true;
                        Field2L.IsVisible = true;
                        Field2E.IsVisible = true;
                        Field2L.Text = InfoFields[i];
                        break;
                    case 2:
                        Field3L.IsEnabled = true;
                        Field3E.IsEnabled = true;
                        Field3L.IsVisible = true;
                        Field3E.IsVisible = true;
                        Field3L.Text = InfoFields[i];
                        break;
                    case 3:
                        Field4L.IsEnabled = true;
                        Field4L.IsVisible = true;
                        Field4E.IsVisible = true;
                        Field4E.IsEnabled = true;
                        Field4L.Text = InfoFields[i];
                        break;
                    case 4:
                        Field5L.IsEnabled = true;
                        Field5E.IsEnabled = true;
                        Field5L.IsVisible = true;
                        Field5E.IsVisible = true;
                        Field5L.Text = InfoFields[i];
                        break;
                }
            }
        }
        catch
        {
            DisplayAlert("Error", "Close settings.config file please.", "Ok");
            Navigation.PopAsync();
        }
    }

    private void LookupEntry_Completed(object sender, EventArgs e)
    {
        string ID = "u" + LookupEntry.Text[1..];
        Spreadsheet sprd = new Spreadsheet();
        sprd.GetIDList(InfoFields);
        List<string> userInfo = sprd.GetStudentInfo(ID);
        if (userInfo[0] == "NOT FOUND") //If they typed in the name instead, we'll try to search for the name inside the userList
        {
            Spreadsheet userList = new Spreadsheet(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\userList.csv", s => true, s => s.ToUpper(), "lab");
            foreach (string cell in userList.GetNamesOfAllNonemptyCells())
            {
                if (LookupEntry.Text.Trim().ToLower().Equals(userList.GetCellContents(cell).ToString().Trim().ToLower()))
                {
                    //Lookup is successful
                    IDCell = "A" + cell[1..];
                    ID = userList.GetCellContents(IDCell).ToString();
                    userInfo = sprd.GetStudentInfo(ID);
                    break;
                }
            }
        }
        IDEntry.Text = ID;
        NameEntry.Text = userInfo[0] + " " + userInfo[1];
        
        for (int i = 2; i < userInfo.Count; i++)
        {
            switch (i - 2)
            {
                case 0:
                    Field1L.Text = InfoFields[i - 2];
                    Field1E.Text = userInfo[i];
                    break;
                case 1:
                    Field2L.Text = InfoFields[i - 2];
                    Field2E.Text = userInfo[i];
                    break;
                case 2:
                    Field3L.Text = InfoFields[i - 2];
                    Field3E.Text = userInfo[i];
                    break;
                case 3:
                    Field4L.Text = InfoFields[i - 2];
                    Field4E.Text = userInfo[i];
                    break;
                case 4:
                    Field5L.Text = InfoFields[i - 2];
                    Field5E.Text = userInfo[i];
                    break;
            }
        }

    }

    private async void CancelButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void SubmitButton_Clicked(object sender, EventArgs e)
    {
        Spreadsheet userList = new Spreadsheet(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\userList.csv", s => true, s => s.ToUpper(), "lab");
        string potentialID = "u" + LookupEntry.Text[1..];
        if (IDCell == "") //If this var isn't empty, then the user looked up using the users Name, not their ID, so the software knows which var or entry contains the cell Name for the start of the user's information.
        {
            foreach (string cell in userList.GetNamesOfAllNonemptyCells())
            {
                if (potentialID.Equals(userList.GetCellContents(cell).ToString().Trim().ToLower()))
                {
                    IDCell = "A" + cell[1..];
                    break;
                }
            }
        }

        char cellLetter = 'A';
        string cellNum = IDCell[1..];
        //Delete this user's info, so we can add the newly edited info for them
        for (int i = 0; i < FieldCount + 1; i++)
        {
            userList.SetContentsOfCell(IDCell, "");
            cellLetter = (char)(cellLetter + 1);
            IDCell = cellLetter + cellNum;
        }
        userList.Save(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\userList.csv");

        SpreadsheetGrid grid = new SpreadsheetGrid();
        bool notEverythingFilled = false;
        if (IDEntry.Text == "" || NameEntry.Text == "")
            notEverythingFilled = true;
        List<string> userInfo = new List<string> { IDEntry.Text, NameEntry.Text };

        for (int i = 0; i < 5; i++)
        {
            switch (i)
            {
                case 0:
                    if (Field1E.IsEnabled)
                        if (Field1E.Text != "")
                            userInfo.Add(Field1L.Text + "&&" + Field1E.Text);
                        else notEverythingFilled = true;
                    break;
                case 1:
                    if (Field2E.IsEnabled)
                        if (Field2E.Text != "")
                            userInfo.Add(Field2L.Text + "&&" + Field2E.Text);
                        else notEverythingFilled = true;
                    break;
                case 2:
                    if (Field3E.IsEnabled)
                        if (Field3E.Text != "")
                            userInfo.Add(Field3L.Text + "&&" + Field3E.Text);
                        else notEverythingFilled = true;
                    break;
                case 3:
                    if (Field4E.IsEnabled)
                        if (Field4E.Text != "")
                            userInfo.Add(Field4L.Text + "&&" + Field4E.Text);
                        else notEverythingFilled = true;
                    break;
                case 4:
                    if (Field5E.IsEnabled)
                        if (Field5E.Text != "")
                            userInfo.Add(Field5L.Text + "&&" + Field5E.Text);
                        else notEverythingFilled = true;
                    break;
            }
            if (notEverythingFilled)
                break;
        }

        if (notEverythingFilled)
        {
            await DisplayAlert("Error", "Make sure all boxes are filled out.", "Ok");
            return;
        }

        try
        {
            grid.AddUsersInformation(userInfo);
            await DisplayAlert("Success", "The user's info should be updated.", "Ok");
            await Navigation.PopAsync();
        }
        catch
        {
            //Most likely wasn't able to save to the file of users due to the file being currently open
            await DisplayAlert("Failure", "There was a problem saving the user's information. \n Make sure the users file is closed and try again.", "Ok");
        }

    }
}