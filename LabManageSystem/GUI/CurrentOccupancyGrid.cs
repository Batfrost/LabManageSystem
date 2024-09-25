using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using PointF = Microsoft.Maui.Graphics.PointF;
using SpreadsheetUtilities;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Devices;
using System.Diagnostics;
using SS;

namespace SS;

public class CurrentOccupancyGrid : ScrollView, IDrawable
{

// These constants control the layout of the spreadsheet grid.
// The height and width measurements are in pixels.
private int dataColWidth = 90;
private const int DATA_ROW_HEIGHT = 25;
private const int LABEL_COL_WIDTH = 0;
private const int LABEL_ROW_HEIGHT = 0;
private const int PADDING = 4;
private const int COL_COUNT = 12;
private int rowCount = 15;
private const int FONT_SIZE = 12;

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
public CurrentOccupancyGrid()
{
    CellColor = Colors.White;
    ourFontColor = Colors.Black;
    BackgroundColor = Colors.White;
    graphicsView.Drawable = this;
    graphicsView.HeightRequest = LABEL_ROW_HEIGHT + (rowCount + 1) * DATA_ROW_HEIGHT;
    graphicsView.WidthRequest = LABEL_COL_WIDTH + (COL_COUNT + 1) * dataColWidth;
    graphicsView.BackgroundColor = Colors.AliceBlue;
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
    else
    {
        canvas.StrokeColor = Colors.Black;
    }

    // Color the background of the data area white
    canvas.FillColor = CellColor;
    canvas.FillRectangle(
        LABEL_COL_WIDTH,
        LABEL_ROW_HEIGHT,
        (COL_COUNT - _firstColumn) * dataColWidth,
        (rowCount - _firstRow) * DATA_ROW_HEIGHT);

    // Draw the column lines
    int bottom = LABEL_ROW_HEIGHT + (rowCount - _firstRow) * DATA_ROW_HEIGHT;
    canvas.DrawLine(0, 0, 0, bottom);
    for (int x = 0; x <= (COL_COUNT - _firstColumn); x++)
    {
        canvas.DrawLine(
            LABEL_COL_WIDTH + x * dataColWidth, 0,
            LABEL_COL_WIDTH + x * dataColWidth, bottom);
    }


    // Draw the row lines
    int right = LABEL_COL_WIDTH + (COL_COUNT - _firstColumn) * dataColWidth;
    canvas.DrawLine(0, 0, right, 0);
    for (int y = 0; y <= rowCount - _firstRow; y++)
    {
        canvas.DrawLine(
            0, LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT,
            right, LABEL_ROW_HEIGHT + y * DATA_ROW_HEIGHT);
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
    }
    else { dataColWidth = 80; }


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
}
