// Written by Joe Zachary and Travis Martin for CS 3500, September 2011, 2021
// Edited by Trevor Williams and Jace Herrmann for CS 3500, October 2022.
//Edited by Trevor Williams for use in the Senior Design Lab in MEK Bldg at the U, September 2023.
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using PointF = Microsoft.Maui.Graphics.PointF;
using SpreadsheetUtilities;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Devices;
using System.Diagnostics;

namespace SS;

/// <summary>
/// The type of delegate used to register for SelectionChanged events
/// </summary>
/// <param name="sender"></param>
public delegate void SelectionChangedHandler(SpreadsheetGrid sender);

/// <summary>
/// A grid that displays a spreadsheet with 26 columns (labeled A-Z) and 99 rows
/// (labeled 1-99).  Each cell on the grid can display a non-editable string.  One 
/// of the cells is always selected (and highlighted).  When the selection changes, a 
/// SelectionChanged event is fired.  Clients can register to be notified of
/// such events.
/// 
/// None of the cells are editable.  They are for display purposes only.
/// </summary>
public class SpreadsheetGrid : ScrollView, IDrawable
{
    /// <summary>
    /// The event used to send notifications of a selection change
    /// </summary>
    public event SelectionChangedHandler SelectionChanged;

    // These constants control the layout of the spreadsheet grid.
    // The height and width measurements are in pixels.
    private int dataColWidth = 80;
    private const int DATA_ROW_HEIGHT = 20;
    private const int LABEL_COL_WIDTH = 30;
    private const int LABEL_ROW_HEIGHT = 30;
    private const int PADDING = 4;
    private int colCount = 26;
    private int rowCount = 100;
    private const int FONT_SIZE = 12;

    // Columns and rows are numbered beginning with 0.  This is the coordinate
    // of the selected cell.
    private int _selectedCol;
    private int _selectedRow;

    // Coordinate of cell in upper-left corner of display
    private int _firstColumn = 0;
    private int _firstRow = 0;

    // Scrollbar positions
    private double _scrollX = 0;
    private double _scrollY = 0;

    // The strings contained by this grid
    private Dictionary<Address, String> _values = new();

    // GraphicsView maintains the actual drawing of the grid and listens
    // for click events
    private GraphicsView graphicsView = new();

    //backing spreadsheet for the grid
    private AbstractSpreadsheet sheet = new Spreadsheet(s => true, s => s.ToUpper(), "lab");

    //File path for saving spreadsheet.
    public string FilePath = null;
    //color for our background cells
    public Color CellColor;
    //color for our text
    public Color ourFontColor;
    /// <summary>
    /// create basic spreadsheet grid with 99 rows and 26 columns and having a vertical and horizontal scroll bar
    /// </summary>
    public SpreadsheetGrid()
    {
        CellColor = Colors.White;
        ourFontColor = Colors.Black;
        BackgroundColor = Colors.LightGray;
        graphicsView.Drawable = this;
        graphicsView.HeightRequest = LABEL_ROW_HEIGHT + (rowCount + 1) * DATA_ROW_HEIGHT;
        graphicsView.WidthRequest = LABEL_COL_WIDTH + (colCount + 1) * dataColWidth;
        graphicsView.BackgroundColor = Colors.LightGrey;
        graphicsView.EndInteraction += OnEndInteraction;
        this.Content = graphicsView;
        this.Scrolled += OnScrolled;
        this.Orientation = ScrollOrientation.Both;
    }

    /// <summary>
    /// Changes the text color variable for drawing canvas
    /// </summary>
    /// <param name="s">Name of color</param>
    public void textColor(string s) {
        //switch case to change the string of color to desired color
        switch (s)
        {
            case "White":
                ourFontColor = Colors.GhostWhite;
                break;
            case "Pink":
                ourFontColor = Colors.DeepPink;
                break;
            case "Blue":
                ourFontColor = Colors.DeepSkyBlue;
                break;
            case "Red":
                ourFontColor = Colors.Red;
                break;
            case "Green":
                ourFontColor = Colors.MediumSpringGreen;
                break;
            case "Black":
                ourFontColor = Colors.Black;
                break;
        }
        //redraw
        Invalidate();
    }
    /// <summary>
    /// Changes the cell color variable for drawing canvas
    /// </summary>
    /// <param name="s">Name of color</param>
    public void backgroundCellColor(string s) {
        //switch case to change the string of color to desired color
        switch (s)
        {
            case "White":
                CellColor = Colors.GhostWhite;
                break;
            case "Pink":
                CellColor = Colors.DeepPink;
                break;
            case "Blue":
                CellColor = Colors.DeepSkyBlue;
                break;
            case "Red":
                CellColor = Colors.Red;
                break;
            case "Green":
                CellColor = Colors.MediumSpringGreen;
                break;
            case "Black":
               CellColor = Colors.Black;
                break;
        }
        //redraw
        Invalidate();
    }
    /// <summary>
    /// Clears the display.
    /// </summary>
    public void Clear()
    {
        _values.Clear();
        sheet = new Spreadsheet(s => true, s => s.ToUpper(), "lab");
        Invalidate();
    }

    /// <summary>
    /// If the zero-based column and row are in range, sets the value of that
    /// cell and returns true.  Otherwise, returns false.
    /// </summary>
    /// <param name="col"> column of cell</param>
    /// <param name="row">row of cell</param>
    /// <param name="c"> content to be put into</param>
    /// <returns> true or false depending on sucess of setting</returns>
    public bool SetValue(int col, int row, string c)
    {
        //nonexistant cell
        if (InvalidAddress(col, row))
        {
            return false;
        }
        //create new address entry
        Address a = new Address(col, row);
        //try setting the value in the spreadsheet
        try
        {
            sheet.SetContentsOfCell(a.ToGridCell(), c);
        }
        //catch any formula errors and throw false
        catch (FormulaFormatException f) {
            System.Diagnostics.Debug.WriteLine(f);
            return false;
        }
        //otherwise
        // if the content is null or a blank string remove from values
        if (c == null || c == "")
        {
            _values.Remove(a);
        }
        else
        {
            //if it is a formula error
            if (sheet.GetCellValue(a.ToGridCell()) is FormulaError f)
                _values[a] = f.Reason;
            else 
            //else set the value to what the content means i.e. string = string double = double formula = double 
                _values[a] = sheet.GetCellValue(a.ToGridCell()).ToString();
             
        }
        //When the last row has a data value inputted in, double the amount of rows shown.
        if(a.Row == rowCount - 1)
        {
            rowCount += 100;
            graphicsView.HeightRequest = LABEL_ROW_HEIGHT + (rowCount + 1) * DATA_ROW_HEIGHT;
        }
        //Same for when the last col has a value put in:
        if(a.Col == colCount)
        {
            colCount += 26;

        }
            
        
        Invalidate();
        return true;
    }

    /// <summary>
    /// If the zero-based column and row are in range, assigns the value
    /// of that cell to the out parameter and returns true.  Otherwise,
    /// returns false.
    /// </summary>
    /// <param name="col"> column of cell</param>
    /// <param name="row">row of cell</param>
    /// <param name="c"> Value of cell</param>
    /// <returns> true or false depending on sucess of setting</returns>
    public bool GetValue(int col, int row, out string c)
    {
        // invalid adress return false and null c
        if (InvalidAddress(col, row))
        {
            c = null;
            return false;
        }
        //if not in values return blank string and true
        if (!_values.TryGetValue(new Address(col, row), out c))
        {
            c = "";
        }
        //otherwise return value and true
        return true;
    }

    /// <summary>
    /// If the zero-based column and row are in range, uses them to set
    /// the current selection and returns true.  Otherwise, returns false.
    /// </summary>
    /// <param name="col"> column of cell</param>
    /// <param name="row">row of cell</param>
    /// <returns>true or false depending on sucess</returns>
    public bool SetSelection(int col, int row)
    {
        //invalid cell return false
        if (InvalidAddress(col, row))
        {
            return false;
        }
        //valid cell change location of selected cell
        _selectedCol = col;
        _selectedRow = row;
        Invalidate();
        return true;
    }

    /// <summary>
    /// Assigns the column and row of the current selection to the
    /// out parameters.
    /// </summary>
    /// <param name="col"> column of cell</param>
    /// <param name="row">row of cell</param>
    public void GetSelection(out int col, out int row)
    {
        col = _selectedCol;
        row = _selectedRow;
    }

    /// <summary>
    /// checks if the address is invalid for the size of the grid
    /// </summary>
    /// <param name="col"> column of cell</param>
    /// <param name="row">row of cell</param>
    /// <returns>true or false depending on valid address</returns>
    private bool InvalidAddress(int col, int row)
    {
        return col < 0 || row < 0 || col >= colCount || row >= rowCount;
    }

    /// <summary>
    /// Listener for click events on the grid.
    /// </summary>
    private void OnEndInteraction(object sender, TouchEventArgs args)
    {
        PointF touch = args.Touches[0];
        OnMouseClick(touch.X, touch.Y);
    }

    /// <summary>
    /// Listener for scroll events. Redraws the panel, maintaining the
    /// row and column headers.
    /// </summary>
    private void OnScrolled(object sender, ScrolledEventArgs e)
    {
        _scrollX = e.ScrollX;
        _firstColumn = (int)e.ScrollX / dataColWidth;
        _scrollY = e.ScrollY;
        _firstRow = (int)e.ScrollY / DATA_ROW_HEIGHT;
        Invalidate();
    }

    /// <summary>
    /// Determines which cell, if any, was clicked.  Generates a SelectionChanged
    /// event.  All of the indexes are zero based.
    /// </summary>
    /// <param name="e"></param>
    private void OnMouseClick(float eventX, float eventY)
    {
        //get x and y values of click location
        int x = (int)(eventX - _scrollX - LABEL_COL_WIDTH) / dataColWidth + _firstColumn;
        int y = (int)(eventY - _scrollY - LABEL_ROW_HEIGHT) / DATA_ROW_HEIGHT + _firstRow;
        //if mouse is clicked on a cell...
        if (eventX > LABEL_COL_WIDTH && eventY > LABEL_ROW_HEIGHT && (x < colCount) && (y < rowCount))
        {
            //set SelectedCol and SelectedRow to x and y
            _selectedCol = x;
            _selectedRow = y;
            if (SelectionChanged != null)
            {
                SelectionChanged(this);
            }
        }
        Invalidate();
    }

    /// <summary>
    /// inform the graphics view it needs to be redrawn
    /// </summary>
    private void Invalidate()
    {
        graphicsView.Invalidate();
    }

    /// <summary>
    /// Used internally to keep track of cell addresses
    /// </summary>
    private class Address
    {
        //column with basic get and set
        public int Col { get; set; }
        //row with basic get and set
        public int Row { get; set; }

        /// <summary>
        ///address pair that consists of a column and row
        /// </summary>
        /// <param name="c"> Column Value</param>
        /// <param name="r"> Row Value</param>
        public Address(int c, int r)
        {
            Col = c;
            Row = r;
        }

        /// <summary>
        /// Returns the hash code of the address (the hashcode of the column to the hashcode of the row)
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Col.GetHashCode() ^ Row.GetHashCode();
        }

        /// <summary>
        /// asserts whether two addresses are equal to eachother
        /// </summary>
        /// <param name="obj">address to be compared to</param>
        /// <returns>true if equal otherwise false</returns>
        public override bool Equals(object obj)
        {
            if ((obj == null) || !(obj is Address))
            {
                return false;
            }
            Address a = (Address)obj;
            return Col == a.Col && Row == a.Row;
        }

        /// <summary>
        /// creates the string representation of the cell A1 B2 ...
        /// </summary>
        /// <returns></returns>
        public string ToGridCell()
        {
            return ((Char)('A' + Col)).ToString() + (Row + 1).ToString();
        }
    }

    /// <summary>
    /// Draws the graphs 
    /// </summary>
    /// <param name="canvas">Background getting drawn on</param>
    /// <param name="dirtyRect"> Rectangle param </param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Move the canvas to the place that needs to be drawn.
        canvas.SaveState();
        canvas.Translate((float)_scrollX, (float)_scrollY);

        //if cell color is black set line color to white otherwise have line color be black
        if (CellColor.Equals(Colors.Black))
        {
            canvas.StrokeColor = Colors.White;
        }
        else {
            canvas.StrokeColor = Colors.Black;
        }
        
        // Color the background of the data area white
        canvas.FillColor = CellColor;
        canvas.FillRectangle(
            LABEL_COL_WIDTH,
            LABEL_ROW_HEIGHT,
            (colCount - _firstColumn) * dataColWidth,
            (rowCount - _firstRow) * DATA_ROW_HEIGHT);

        // Draw the column lines
        int bottom = LABEL_ROW_HEIGHT + (rowCount - _firstRow) * DATA_ROW_HEIGHT;
        canvas.DrawLine(0, 0, 0, bottom);
        for (int x = 0; x <= (colCount - _firstColumn); x++)
        {
            canvas.DrawLine(
                LABEL_COL_WIDTH + x * dataColWidth, 0,
                LABEL_COL_WIDTH + x * dataColWidth, bottom);
        }

        // Draw the column labels
        for (int x = 0; x < colCount - _firstColumn; x++)
        {
            DrawColumnLabel(canvas, x,
                (_selectedCol - _firstColumn == x) ? Font.Default : Font.DefaultBold);
        }

        // Draw the row lines
        int right = LABEL_COL_WIDTH + (colCount - _firstColumn) * dataColWidth;
        canvas.DrawLine(0, 0, right, 0);
        for (int y = 0; y <= rowCount - _firstRow; y++)
        {
            canvas.DrawLine(
                0, LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT,
                right, LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT);
        }

        // Draw the row labels
        for (int y = 0; y < (rowCount - _firstRow); y++)
        {
            DrawRowLabel(canvas, y,
                (_selectedRow - _firstRow == y) ? Font.Default : Font.DefaultBold);
        }

        // Highlight the selection, if it is visible
        if ((_selectedCol - _firstColumn >= 0) && (_selectedRow - _firstRow >= 0))
        {
            canvas.DrawRectangle(
                LABEL_COL_WIDTH + (_selectedCol - _firstColumn) * dataColWidth + 1,
                              LABEL_ROW_HEIGHT + (_selectedRow - _firstRow) * DATA_ROW_HEIGHT + 1,
                              dataColWidth - 2,
                              DATA_ROW_HEIGHT - 2);
        }
        //Get the longest value's length, so that we can scale up the spreadsheet boxes.
        int longestValLength = 0;
        if (_values.Count > 0)
        {
            longestValLength = _values.Max(address => address.Value.Length);
            //If the longest data is longer than 15 characters, then just put a limit at 110 for the boxes size.
            if (longestValLength > 15)
                dataColWidth = 110;
            else { dataColWidth = (int)(7.27 * longestValLength); }
        } else { dataColWidth = 80; }
            

        // Draw the text
        foreach (KeyValuePair<Address, String> address in _values)
        {
            String text = address.Value;
            
            //For data bigger than 15 characters, just shorten the data.
            if (address.Value.Length > 15)
            {
                dataColWidth = 110;
                text = address.Value.Substring(0, 10) + "..." + address.Value.Substring(address.Value.Length - 4);

            }
            Invalidate();
            
            int col = address.Key.Col - _firstColumn;
            int row = address.Key.Row - _firstRow;
            SizeF size = canvas.GetStringSize(text, Font.Default, FONT_SIZE + FONT_SIZE * 1.75f);
            canvas.Font = Font.Default;
            canvas.FontColor = ourFontColor;
            if (col >= 0 && row >= 0)
            {
                canvas.DrawString(text,
                    LABEL_COL_WIDTH + col * dataColWidth + PADDING,
                    LABEL_ROW_HEIGHT + row * DATA_ROW_HEIGHT + (DATA_ROW_HEIGHT - size.Height) / 2,
                    size.Width, size.Height, HorizontalAlignment.Left, VerticalAlignment.Center);
            }
        }
        canvas.RestoreState();
    }

    /// <summary>
    /// Draws a column label.  The columns are indexed beginning with zero.
    /// </summary>
    /// <param name="canvas">background it is drawn on</param>
    /// <param name="x">number of column</param>
    /// <param name="f"> font</param>
    private void DrawColumnLabel(ICanvas canvas, int x, Font f)
    {
        String label = ((char)('A' + x + _firstColumn)).ToString();
        SizeF size = canvas.GetStringSize(label, f, FONT_SIZE + FONT_SIZE * 1.75f);
        canvas.Font = f;
        canvas.FontSize = FONT_SIZE;
        canvas.DrawString(label,
              LABEL_COL_WIDTH + x * dataColWidth + (dataColWidth - size.Width) / 2,
              (LABEL_ROW_HEIGHT - size.Height) / 2, size.Width, size.Height,
              HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    /// <summary>
    /// Draws a row label.  The rows are indexed beginning with zero.
    /// </summary>
    /// <param name="canvas">background it is drawn on</param>
    /// <param name="y">number of rows</param>
    /// <param name="f"> font</param>
    private void DrawRowLabel(ICanvas canvas, int y, Font f)
    {
        String label = (y + 1 + _firstRow).ToString();
        SizeF size = canvas.GetStringSize(label, f, FONT_SIZE + FONT_SIZE * 1.75f);
        canvas.Font = f;
        canvas.FontSize = FONT_SIZE;
        canvas.DrawString(label,
            LABEL_COL_WIDTH - size.Width - PADDING,
            LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT + (DATA_ROW_HEIGHT - size.Height) / 2,
            size.Width, size.Height,
              HorizontalAlignment.Right, VerticalAlignment.Center);
    }

    /// <summary>
    /// returns content of given cell in an outparamater c
    /// </summary>
    /// <param name="col"> Column Value</param>
    /// <param name="row"> Row Value</param>
    /// <param name="c">String value of content</param>
    /// <returns></returns>
    public bool getCellContent(int col, int row, out string c) {
        // invalid adress return false and null c
        if (InvalidAddress(col, row))
        {
            c = null;
            return false;
        }
        //if not in values return blank string and true
        object x = sheet.GetCellContents(((Char)('A' + col)).ToString() + (row + 1).ToString());
        //otherwise 
        //if formula add = for content else set c = to string form of
        if (x is Formula)
           c = "=" + x.ToString();
        else
           c = x.ToString();
        //return true
        return true;

    }

    /// <summary>
    /// Saves the spreadsheet to the given filePath
    /// </summary>
    /// <param name="filePath">File name and file path.</param>
    public void saveSpreadsheet(string filePath)
    {
        sheet.Save(filePath);
        FilePath = filePath;
    }

    /// <summary>
    /// Checks if spreadsheet has been modified and is not saved.
    /// </summary>
    /// <returns></returns>
    public bool isChanged()
    {
        return sheet.Changed;
    }

    /// <summary>
    /// Loads the new spreadsheet from a given filepath
    /// </summary>
    /// <param name="filePath">The given filepath</param>
    public void Load(string filePath)
    {
        //Clear the current spreadsheet, and set the backing sheet to the loaded one.
        this.Clear();
        sheet = new Spreadsheet(filePath, s => true, s => s.ToUpper(), "lab");
        
        //Now for each cell in the loaded backing sheet software, add to GUI.
        foreach(string cell in sheet.GetNamesOfAllNonemptyCells())
        {
            //Convert cellName to rows and cols
            int col = cell.First() - 'A';
            int row = int.Parse(cell.Substring(1)) - 1;

            //Add cells, if formulas then add "=" before content.
            object x = sheet.GetCellContents(cell);
            if (x is Formula) 
                this.SetValue(col, row, "=" + sheet.GetCellContents(cell).ToString());
            else
                this.SetValue(col, row, sheet.GetCellContents(cell).ToString());
        }

        FilePath = filePath; 
    }

    /// <summary>
    /// Will log/save the time the user logs into the spreadsheet system using the spreadsheet's LoginUser method.
    /// </summary>
    /// <param name="ID"></param>
    /// <param name="logFilePath"></param>
    public string LoginUser(string ID, List<string> hiddenInfoFields)
    {
        return sheet.LoginUser(ID, hiddenInfoFields);
    }

    /// <summary>
    ///  Will add the user's given info to the signed users file by using spreadsheet's method.
    /// </summary>
    /// <returns></returns>
    public void AddUsersInformation(List<string> userInfo)
    {
        sheet.AddUsersInformation(userInfo);
    }

    //Will call sheet.GetIDList to save the list of ID's to try to prevent the issue of 
    //multiple programs editing/checking the same file - in case someone opens up the ID list file while this program is running.
    public bool GetIDList(List<string> infoFields)
    {
        return sheet.GetIDList(infoFields);
    }

    public Spreadsheet GetCurrentlyLoggedIn(List<String> VisibleFieldsList)
    {
        return sheet.GetCurrentlyLoggedInSpreadsheet(VisibleFieldsList);
    }

    public void LogoutUserFromCurrentlyLoggedInSheet(string ID)
    {
        sheet.LogoutUserFromCurrentlyLoggedInSheet(ID);
    }

    public void LoadSettings()
    {
        sheet.LoadSettings();
    }

    public void AttendanceChecker()
    {
        sheet.AttendanceChecker();
    }
}
