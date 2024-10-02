namespace SpreadsheetGUI;

public partial class CustomizationPage : ContentPage
{
	public CustomizationPage()
	{
		InitializeComponent();
	}

    async public void ReturnToMenu(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}