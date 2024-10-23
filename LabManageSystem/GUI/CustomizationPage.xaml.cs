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

    async private void PasswordChangeButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CustomizationPage1()); //CustPage1 lets user change the manager password
    }

    async private void EditUAPButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CustomizationPage2()); //CustPage2 lets user edit the user agreement page 
    }

}