using Sett;

namespace SpreadsheetGUI;

public partial class ManagerPasswordPage : ContentPage
{
    Settings s;
	public ManagerPasswordPage()
	{
        InitializeComponent();
        s = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
        string secQuestText = s.securityQuestion.Item1;
        SecurityQuestionLabel.Text = secQuestText;
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
        if (SecQuestAnswer.Text == "" || NewPasswordEntry.Text == "" || PasswordConfirmEntry.Text == "")
        {
            await DisplayAlert("Error", "Fill all fields.", "Ok");
            return;
        }

        if (!s.TestSecurityQuestionAnswer(SecQuestAnswer.Text))
        {
            await DisplayAlert("Error", "Security Question Answer Wrong", "Ok");
            return;
        }

        s.password = NewPasswordEntry.Text;
        s.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
        await Navigation.PopToRootAsync();
        App.Current.MainPage = new NavigationPage(new HomePage(s));
    }
}