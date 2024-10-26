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
using Sett;

namespace SS;

public class CurrentOccupancyGrid : ScrollView, IDrawable
{

    // These constants control the layout of the spreadsheet grid.
    // The height and width measurements are in pixels.
    private int dataColWidth = 150;
    private const int DATA_ROW_HEIGHT = 35;
    private const int PADDING = 4;
    private Settings Settings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");

    private int COL_COUNT = 1;
    private int rowCount = 100;
    private const int FONT_SIZE = 15;


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

    /// <summary>
    /// Creates a grid that will display info from who is currently logged into the software.
    /// Will show specific special info fields as well.
    /// </summary>
    public CurrentOccupancyGrid()
    {
        Dictionary<String, Object> agreementPageInfo = Settings.GetAgreementPageInfo();
        List<String> SpecialVisibleFields = (List<String>)agreementPageInfo["SpecialVisibleFields"];
        COL_COUNT = 4 + SpecialVisibleFields.Count; //ID, First Name, Last Name, time logged in, plus special visible fields User wants.
        graphicsView.Drawable = this;
        graphicsView.HeightRequest = (rowCount) * DATA_ROW_HEIGHT;
        graphicsView.WidthRequest = (COL_COUNT) * dataColWidth;
        graphicsView.BackgroundColor = Colors.White;
        this.Content = graphicsView;
        this.Scrolled += OnScrolled;
        this.Orientation = ScrollOrientation.Both;
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
        catch (FormulaFormatException f)
        {
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
        if (a.Row == rowCount - 1)
        {
            rowCount += 100;
            graphicsView.HeightRequest = (rowCount + 1) * DATA_ROW_HEIGHT;
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
    /// checks if the address is invalid for the size of the grid
    /// </summary>
    /// <param name="col"> column of cell</param>
    /// <param name="row">row of cell</param>
    /// <returns>true or false depending on valid address</returns>
    private bool InvalidAddress(int col, int row)
    {
        return col < 0 || row < 0 || col >= COL_COUNT || row >= rowCount;
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
    /// inform the graphics view it needs to be redrawn
    /// </summary>
    public void Invalidate()
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

        // Color the background of the data area white
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(0, 0, (COL_COUNT - _firstColumn) * dataColWidth, (rowCount - _firstRow) * DATA_ROW_HEIGHT);

        // Draw the column lines
        int bottom = (rowCount - _firstRow) * DATA_ROW_HEIGHT;
        canvas.DrawLine(0, 0, 0, bottom);
        for (int x = 0; x <= (COL_COUNT - _firstColumn); x++)
        {
            canvas.DrawLine(
                x * dataColWidth, 0,
                x * dataColWidth, bottom);
        }
        // Draw the row lines
        int right = (COL_COUNT - _firstColumn) * dataColWidth;
        canvas.DrawLine(0, 0, right, 0);
        for (int y = 0; y <= rowCount - _firstRow; y++)
        {
            canvas.DrawLine(
                0, y * DATA_ROW_HEIGHT,
                right, y * DATA_ROW_HEIGHT);
        }

        // Draw the text
        foreach (KeyValuePair<Address, String> address in _values)
        {
            String text = address.Value;
            //Get the longest value's length, so that we can scale up the spreadsheet boxes.
            int longestValLength = 0;
            if (_values.Count > 0)
            {
                longestValLength = _values.Max(address => address.Value.Length);
                //If the longest data is longer than 20 characters, then just put a limit at 200 for the boxes size.
                if (longestValLength > 20)
                    dataColWidth = 200;
                else { dataColWidth = (int)(8 * longestValLength); }
            }
            else { dataColWidth = 150; }
            //For data bigger than 20 characters, just shorten the data.
            if (address.Value.Length > 30)
            {
                text = address.Value.Substring(0, 25) + "..." + address.Value.Substring(address.Value.Length - 4);
            }
            Invalidate();
            graphicsView.HeightRequest = (rowCount) * DATA_ROW_HEIGHT;
            graphicsView.WidthRequest = (COL_COUNT) * dataColWidth;

            int col = address.Key.Col - _firstColumn;
            int row = address.Key.Row - _firstRow;
            SizeF size = canvas.GetStringSize(text, Font.Default, FONT_SIZE + FONT_SIZE * 1.75f);
            canvas.Font = Font.Default;
            canvas.FontSize = FONT_SIZE;
            canvas.FontColor = Colors.Black;
            if (col >= 0 && row >= 0)
            {
                canvas.DrawString(text, col * dataColWidth + PADDING, row * DATA_ROW_HEIGHT + (DATA_ROW_HEIGHT - size.Height) / 2, size.Width, size.Height, HorizontalAlignment.Left, VerticalAlignment.Center);
            }
        }
        canvas.RestoreState();
    }

    /// <summary>
    /// returns content of given cell in an outparamater c
    /// </summary>
    /// <param name="col"> Column Value</param>
    /// <param name="row"> Row Value</param>
    /// <param name="c">String value of content</param>
    /// <returns></returns>
    public bool getCellContent(int col, int row, out string c)
    {
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
    /// Loads the new spreadsheet from a given filepath
    /// </summary>
    /// <param name="filePath">The given filepath</param>
    public void Load(string filePath)
    {
        //Clear the current spreadsheet, and set the backing sheet to the loaded one.
        this.Clear();
        sheet = new Spreadsheet(filePath, s => true, s => s.ToUpper(), "lab");

        //Now for each cell in the loaded backing sheet software, add to GUI.
        foreach (string cell in sheet.GetNamesOfAllNonemptyCells())
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
    }
}
