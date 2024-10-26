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

		if (PasswordEntry.Text == "" || SecurityQuestionEntry.Text == "" || SecQuestAnswer.Text == "" || UserAgreementText.Text == "")
		{
            await DisplayAlert("Error", "Please fill out everything.", "Alright");
            return;
        }

		settings = new Settings(PasswordEntry.Text, "", new Dictionary<string, bool>(), (SecurityQuestionEntry.Text, SecQuestAnswer.Text.ToLower().Trim()));
		settings.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
		settings.AddNewUserAgreementField(infoFields, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        settings.AddUserAgreementText(UserAgreementText.Text, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        App.Current.MainPage = new NavigationPage(new HomePage(settings));
	}

	/// <summary>
	/// Asks User to fill out fields and adds a new Information field based on provided Information.
	/// </summary>
    async private void AddNewFieldButton_Clicked(object sender, EventArgs e)
    {
		try
		{
			if (infoFields.Count > 4)
			{
				await DisplayAlert("Whoops", "Only 5 special fields (in addition\n to ID and Name fields) Can be added.", "Alrighty");
				return;
			}

            String newInfoField = await DisplayPromptAsync("Adding A New Information Field", "Please type the name of the field you want the User to answer. \n", "Accept", "Cancel", "E.g. 'Advisor:'");
            String showInfoResponse = await DisplayActionSheet("Do you want to Show this Information on\n the Home Page for whoever is currently logged in?", "Cancel", null, "Yes", "No");
            bool showInfo = false;
            if (showInfoResponse.Equals("Yes"))
                showInfo = true;
            newInfoField += "&&" + showInfo.ToString();
            infoFields.Add(newInfoField);
			AddedFields.Text = "";
			for (int i = 0; i < infoFields.Count; i++) //Displaying the info fields the user has currently added.
			{
				AddedFields.Text += infoFields[i].ToString().Substring(0, infoFields[i].LastIndexOf("&&"));
				AddedFields.Text += " --> Field will show on Home Page: " + infoFields[i].ToString().Substring(infoFields[i].LastIndexOf("&&") + 2) + "\n";
			}
        }
		catch
		{
			await DisplayAlert("Cancel Alert", "Cancelling adding a new Info Field.", "Ok");
		}
		
    }

	/// <summary>
	/// Handler for resetting all custom info fields.
	/// </summary>
    private async void ResetFieldsButton_Clicked(object sender, EventArgs e)
    {
		string response = await DisplayActionSheet("Are you sure you want to reset\n all created fields?", "No", null, "Yes");
		if (response.Equals("No"))
			return;
		infoFields.Clear();
        AddedFields.Text = "";
    }
}