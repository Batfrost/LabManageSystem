using Sett;

namespace SpreadsheetGUI;

public partial class CustomizationPage1 : ContentPage
{
	public CustomizationPage1()
	{
		InitializeComponent();
	}

    async private void CancelButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    async private void SubmitButton_Clicked(object sender, EventArgs e)
    {
        string response = await DisplayActionSheet("Confirm Password Change?", "Cancel", null, "Yes");
        if (response == "Cancel")
            return;
        if (OldPasswordEntry.Text == "" || NewPasswordEntry.Text == "" || PasswordConfirmEntry.Text == "")
        {
            await DisplayAlert("Error", "Fill all fields.", "Ok");
            return;
        }
        
        Settings s = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
        if (s.TestPassword(NewPasswordEntry.Text))
        {
            await DisplayAlert("Error", "Old Password is Wrong.", "Ok");
            return;
        }

        s.password = NewPasswordEntry.Text;
        s.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
        

        await Navigation.PopAsync();
    }
}