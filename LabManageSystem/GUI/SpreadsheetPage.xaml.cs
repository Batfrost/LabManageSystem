using Microsoft.Maui;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Text;
using Microsoft.UI.Xaml.Media;
using SpreadsheetUtilities;
using SS;

namespace SpreadsheetGUI;

/// <summary>
/// Example of using a SpreadsheetGUI object
/// </summary>
public partial class SpreadsheetPage : ContentPage
{

    /// <summary>
    /// Constructor for basic spreadsheet
    /// </summary>
	public SpreadsheetPage()
    {
        InitializeComponent();
        //register the display selection event to the spreadsheet
        spreadsheetGrid.SelectionChanged += displaySelection;
        //set default seletion to A1 (0,0)
        spreadsheetGrid.SetSelection(0, 0);
    }

    /// <summary>
    /// Displays the name and value of the currently selected cell into the label and the content into the entry
    /// </summary>
    /// <param name="grid"> Spreadsheet grid with content </param>
    private void displaySelection(SpreadsheetGrid grid)
    {
        //get the column and row of the selected cell
        spreadsheetGrid.GetSelection(out int col, out int row);
        //get the value of the cell
        spreadsheetGrid.GetValue(col, row, out string value);
        //set the name label text to currently selected cell
        cellName.Text = ((Char)('A' + col)).ToString() + (row + 1).ToString();
        //set value to value of currently selected cell
        cellValue.Text = "Value : " +value.ToString();
        //set the entry to the currentely selected cells content
        spreadsheetGrid.getCellContent(col, row, out string c);
            cellContent.Text = c.ToString();



        //if (value == "")
        //{
        //    spreadsheetGrid.SetValue(col, row, DateTime.Now.ToLocalTime().ToString("T"));
        //    spreadsheetGrid.GetValue(col, row, out value);
        //    DisplayAlert("Selection:", "column " + col + " row " + row + " value " + value, "OK");
        //}
    }

    /// <summary>
    /// Saves the file, with a given file name, and will check to see if any overwriting will occur.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void SaveAsClicked(Object sender, EventArgs e)
    {
        //Get the file path name
        string pathName = await DisplayPromptAsync("Save", "Enter the file path/name you want to save as: ", "OK", "Cancel", "e.g: Save.sprd", -1, null, "");
        bool doesFileExist = true;

        //If user hits cancel on inputting file name, then just exit method.
        if (pathName is null)
            return;

        //Check to see if the file already exists
        try {
            File.ReadAllText(pathName);
        } catch (FileNotFoundException)
        {
            doesFileExist = false;
        }

        //If File already exists then check to see if the given path is the same as most previous file path.
        if (doesFileExist && pathName == spreadsheetGrid.FilePath)
        {
            try
            {
                spreadsheetGrid.saveSpreadsheet(pathName);
                await DisplayAlert("Save Successful", "Save Complete", "Nice");
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                await DisplayAlert("Save FAILED", "Save did not work, check file path", "Uh oh");
            }
            
            return;
        }
            
        //If File doesn't exist at all, then just save
        else if (!doesFileExist)
        {
            try
            {
                spreadsheetGrid.saveSpreadsheet(pathName);
                await DisplayAlert("Save Successful", "Save Complete", "Cool Beans");
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                await DisplayAlert("Save FAILED", "Save did not work, check file path", "Uh oh");
            }
            
            return;
        }
        //Else the file does exist, but the user is overwriting it and it isn't the most recently saved file, so ask user if they are sure.
        else
        {
            string response = await DisplayActionSheet("File Already Exists. Do you still wish to Save?", "No", null, "Yes");
            if (response == "Yes")
            {
                try
                {
                    spreadsheetGrid.saveSpreadsheet(pathName);
                    await DisplayAlert("Save Successful", "Save Complete", "Sounds Good");
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Debug.WriteLine(exc);
                    await DisplayAlert("Save FAILED", "Save did not work, check file path", "Uh oh");
                }
                
                return;
            }
                

        }

    }

    /// <summary>
    /// Saves the spreadsheet without asking for filepath IF the file just needs to be saved to previous file
    /// Else it will call SaveAsClicked()
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SaveClicked(Object sender, EventArgs e)
    {
        if (spreadsheetGrid.FilePath is not null)
        {
            try
            {
                spreadsheetGrid.saveSpreadsheet(spreadsheetGrid.FilePath);
                DisplayAlert("Save Successful", "Save Complete", "Epic");
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                DisplayAlert("Save FAILED", "Save did not work, check file path", "Uh oh");
            }

            
        }
        else 
            SaveAsClicked(sender, e);
        
    }

    /// <summary>
    /// When new has been clicked in the file menu item clear the spreadsheet to a blank spreadsheet
    /// if spreadsheet has been changed ask user if they want to save first
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void NewClicked(Object sender, EventArgs e)
    {
        //Check if spreadsheet has been changed and not saved.
        if (spreadsheetGrid.isChanged())
        {
            string response = await DisplayActionSheet("Spreadsheet has been modified, are you sure about creating a new one?", "No", null, "Yes");
            if (response == "Yes")
            {
                spreadsheetGrid.Clear();
                spreadsheetGrid.SetSelection(0, 0);
                cellName.Text = "A1";
                cellContent.Text = "";
                cellValue.Text = "Value : ";
                spreadsheetGrid.FilePath = null;
            }
                
            return;
        }

        //clear spreadsheet
        spreadsheetGrid.Clear();
        spreadsheetGrid.SetSelection(0, 0);
        cellName.Text = "A1";
        cellContent.Text = "";
        cellValue.Text = "Value : ";
        spreadsheetGrid.FilePath = null;
    }

    /// <summary>
    /// Opens any file as text and prints its contents.
    /// Note the use of async and await, concepts we will learn more about
    /// later this semester.
    /// </summary>
    private async void OpenClicked(Object sender, EventArgs e)
    {
        if (spreadsheetGrid.isChanged())
        {
            string response = await DisplayActionSheet("Spreadsheet has been modified, are you sure about loading a new one?", "No", null, "Yes");
            if (response == "Yes")
            {
                //try creating spreadsheet
                try
                {
                    FileResult fileResult = await FilePicker.Default.PickAsync();
                    //if a file is found
                    if (fileResult != null)
                    {
                        //found selected file
                        System.Diagnostics.Debug.WriteLine("Successfully chose file: " + fileResult.FileName);

                        try
                        {
                            spreadsheetGrid.Load(fileResult.FullPath);
                        }
                        catch (Exception exce)
                        {
                            System.Diagnostics.Debug.WriteLine(exce);
                            await DisplayAlert("Load Failed", "Load did not work, check file", "Oof");
                        }
                    }
                    //no file has been selected
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No file selected.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error opening file:");
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            return;
        } 
        else
        {
            //try creating spreadsheet
            try
            {
                FileResult fileResult = await FilePicker.Default.PickAsync();
                //if a file is found
                if (fileResult != null)
                {
                    //found selected file
                    System.Diagnostics.Debug.WriteLine("Successfully chose file: " + fileResult.FileName);

                    try
                    {
                        spreadsheetGrid.Load(fileResult.FullPath);
                    }
                    catch (Exception exce)
                    {
                        System.Diagnostics.Debug.WriteLine(exce);
                        await DisplayAlert("Load Failed", "Load did not work, check file", "Oof");
                    }
                }
                //no file has been selected
                else
                {
                    System.Diagnostics.Debug.WriteLine("No file selected.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error opening file:");
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }

    /// <summary>
    /// when enter is pressed on the entry save the new content and update view, if invalid formula create popup and don't change
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EnterPressed(Object sender, EventArgs e)
    {
        //text before the user pressed enter*
        string text = ((Entry)sender).Text;
        //get the column and row of the selected cell
        spreadsheetGrid.GetSelection(out int col, out int row);
        //if setting value returns false formula error ocurrs restore entry and display alert
        if (!spreadsheetGrid.SetValue(col, row, text))
        {
            spreadsheetGrid.getCellContent(col, row, out string x);
            cellContent.Text = x.ToString();
            DisplayAlert("Selection error", "Formula Error invalid formula", "ok");

        }
        //get and set value (whether changed or not)
        spreadsheetGrid.GetValue(col, row, out string c);
        cellValue.Text = "Value : " + c;
    }
    /// <summary>
    /// changes the colors of the spreadsheet to pink (aside from taskbar items)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeTextColorPink(Object sender, EventArgs e){
        //change label and entry colors
        cellName.TextColor = Colors.DeepPink;
        cellValue.TextColor = Colors.DeepPink;
        cellContent.TextColor = Colors.DeepPink;
        //change the color of the text in the grid
        spreadsheetGrid.textColor("Pink");
    }
    /// <summary>
    /// changes the colors of the spreadsheet to White (aside from taskbar items)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeTextColorWhite(Object sender, EventArgs e)
    {
        //change label and entry colors
        cellName.TextColor = Colors.GhostWhite;
        cellValue.TextColor = Colors.GhostWhite;
        cellContent.TextColor= Colors.GhostWhite;
        //change the color of the text in the grid
        spreadsheetGrid.textColor("White");
    }
    /// <summary>
    /// changes the colors of the spreadsheet to red (aside from taskbar items)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeTextColorRed(Object sender, EventArgs e)
    {
        //change label and entry colors
        cellName.TextColor = Colors.Red;
        cellValue.TextColor = Colors.Red;
        cellContent.TextColor = Colors.Red;
        //change the color of the text in the grid
        spreadsheetGrid.textColor("Red");
    }
    /// <summary>
    /// changes the colors of the spreadsheet to green (aside from taskbar items)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeTextColorGreen(Object sender, EventArgs e)
    {
        //change label and entry colors
        cellName.TextColor = Colors.MediumSpringGreen;
        cellValue.TextColor = Colors.MediumSpringGreen;
        cellContent.TextColor = Colors.MediumSpringGreen;
        //change the color of the text in the grid
        spreadsheetGrid.textColor("Green");
    }
    /// <summary>
    /// changes the colors of the spreadsheet to blue (aside from taskbar items)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeTextColorBlue(Object sender, EventArgs e)
    {
        //change label and entry colors
        cellName.TextColor = Colors.DeepSkyBlue;
        cellValue.TextColor = Colors.DeepSkyBlue;
        cellContent.TextColor = Colors.DeepSkyBlue;
        //change the color of the text in the grid
        spreadsheetGrid.textColor("Blue");
    }
    /// <summary>
    /// changes the colors of the spreadsheet to black (aside from taskbar items)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeTextColorBlack(Object sender, EventArgs e)
    {
        //change label and entry colors
        cellName.TextColor = Colors.Black;
        cellValue.TextColor= Colors.Black;
        cellContent.TextColor = Colors.Black;
        //change the color of the text in the grid
        spreadsheetGrid.textColor("Black");
    }
    /// <summary>
    /// Changes background color to pink
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeCellColorPink(Object sender, EventArgs e){
        spreadsheetGrid.backgroundCellColor("Pink");
    }
    /// <summary>
    /// Changes background color to white
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeCellColorWhite(Object sender, EventArgs e)
    {
        spreadsheetGrid.backgroundCellColor("White");
    }
    /// <summary>
    /// Changes background color to red
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeCellColorRed(Object sender, EventArgs e)
    {
        spreadsheetGrid.backgroundCellColor("Red");
    }
    /// <summary>
    /// Changes background color to green
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeCellColorGreen(Object sender, EventArgs e)
    {
        spreadsheetGrid.backgroundCellColor("Green");
    }
    /// <summary>
    /// Changes background color to blue
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeCellColorBlue(Object sender, EventArgs e)
    {
        spreadsheetGrid.backgroundCellColor("Blue");
    }
    /// <summary>
    /// Changes background color to black and border lines to white
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ChangeCellColorBlack(Object sender, EventArgs e)
    {
        spreadsheetGrid.backgroundCellColor("Black");
    }
    /// <summary>
    /// creates a popup explaining the spreadsheet in detail
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void HelpPopup(Object sender, EventArgs e) {
        DisplayAlert("Help","A basic spreadsheet that accepts strings, doubles, and formulas. To have something evaluate as a formula " +
            "add an equals sign before the formula (=A1+a2). Formulas are not case sensitive. To select a cell please click on the desired cell" +
            ". The cell will highlight and the cell name, value, and content will fill out above the grid for the selected cell. To edit a cell you can " +
            "type the desired information into the entry label and press enter to save it otherwise it will not be updated. To save a spreadsheet you can " +
            "hit the save or the save as button. The save button will use the last known file path. If a path is not known it will act as save as and ask " +
            "for a full filepath that ends in .sprd. To load select open and select your desired file and hit okay. You are able to change the color of the" +
            " text by selecting text color and selecting the desired color. The taskbar colors will remain white for clarity. You can change the background " +
            "color of the grid by selecting Grid color and your desired color.", "okay");
    }

    /// <summary>
    /// Will be called from the home page and uses the Spreadsheets LoginUser method to log the time and user who logged in.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void LoginUser(int ID, string logFilePath)
    {
        spreadsheetGrid.LoginUser(ID, logFilePath);
    }
}
