using SS;
using Microcharts;

namespace SpreadsheetGUI;

public partial class StatisticsPage : ContentPage
{
	Spreadsheet SprdSht = new Spreadsheet();
	public StatisticsPage()
	{
		InitializeComponent();
		ChartList.SelectedIndex = 0;
		ModeList.SelectedIndex = 5;
	}

    public void GatherStatistics(object sender, EventArgs e)
    {
		List<ChartEntry> chartInfo = SprdSht.GatherStatistics(FromDate.Date.ToString(), ToDate.Date.ToString(), ModeList.SelectedIndex);
        try
        {
            switch (ChartList.SelectedItem)
            {
                case "Bar Chart":
                    Chart.Chart = new BarChart { Entries = chartInfo };
                    break;
                case "Pie Chart":
                    Chart.Chart = new PieChart { Entries = chartInfo };
                    break;
                case "Line Chart":
                    Chart.Chart = new LineChart { Entries = chartInfo };
                    break;
                case "Point Chart":
                    Chart.Chart = new PointChart { Entries = chartInfo };
                    break;
                default:
                    Chart.Chart = new BarChart { Entries = chartInfo };
                    break;
            }
        }
        catch
        {
            DisplayAlert("Failure", "Something went wrong with gathering stats. \n Make sure all spreadsheet files in the log folder are closed and try again. \n If Problem keeps persisting, one of the files may have been corrupted or messed with.", "Ok");
            return;
        }
        
	}

    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}