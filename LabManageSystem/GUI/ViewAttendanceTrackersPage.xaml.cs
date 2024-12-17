using SS;
namespace SpreadsheetGUI;

public partial class ViewAttendanceTrackersPage : ContentPage
{
	public ViewAttendanceTrackersPage()
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
        cellValue.Text = "Value : " + value.ToString();
        //set the entry to the currentely selected cells content
        spreadsheetGrid.getCellContent(col, row, out string c);
        cellContent.Text = c.ToString();
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


    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}