using Sett;

namespace SpreadsheetGUI;

public partial class EstablishSettingsPage : ContentPage
{
	public Settings settings;
	private List<String> infoFields;

	public EstablishSettingsPage(ref Settings settings)
	{
		this.settings = settings;
		InitializeComponent();
		infoFields = new List<String>();
	}

	async void ConfirmSettingsAndGoHome(object sender, EventArgs e)
	{
		if (PasswordEntry.Text != ConfirmPasswordEntry.Text) 
		{ 
			await DisplayAlert("Error", "Passwords do not match. Please re-enter.", "Alright");
			return;
		}

		settings = new Settings(PasswordEntry.Text, "", new Dictionary<string, bool>());
		settings.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
		settings.AddNewUserAgreementField(infoFields, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
        settings.AddUserAgreementText(UserAgreementText.Text, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
        App.Current.MainPage = new NavigationPage(new HomePage(settings));
	}

	/// <summary>
	/// Asks User to fill out fields and adds a new Information field based on provided Information.
	/// </summary>
    async private void AddNewFieldButton_Clicked(object sender, EventArgs e)
    {
		try
		{
            String newInfoField = await DisplayPromptAsync("Adding A New Information Field", "Please type the name of the field you want the User to answer. ", "Accept", "Cancel", "E.g. 'Advisor:'");
            String showInfoResponse = await DisplayActionSheet("Do you want to Show this Information on the Home Page for whoever is currently logged in?", "Cancel", null, "Yes", "No");
            bool showInfo = false;
            if (showInfoResponse.Equals("Yes"))
                showInfo = true;
            newInfoField += "&&" + showInfo.ToString();
            infoFields.Add(newInfoField);
        }
		catch
		{
			await DisplayAlert("Cancel Alert", "Cancelling adding a new Info Field.", "Ok");
		}
		
    }
}