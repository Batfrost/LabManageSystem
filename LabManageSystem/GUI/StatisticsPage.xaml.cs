using Microcharts.Maui;
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
		switch(ChartList.SelectedItem)
		{
			case "Bar Chart":
				Chart.Chart = new BarChart{ Entries = chartInfo };
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

    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}