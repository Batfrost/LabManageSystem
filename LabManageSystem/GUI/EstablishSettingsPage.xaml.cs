using Sett;

namespace SpreadsheetGUI;

public partial class EstablishSettingsPage : ContentPage
{
	public Settings settings;
	public EstablishSettingsPage(ref Settings settings)
	{
		this.settings = settings;
		InitializeComponent();
	}

	async void ConfirmSettingsAndGoHome(object sender, EventArgs e)
	{
		if (PasswordEntry.Text != ConfirmPasswordEntry.Text) 
		{ 
			await DisplayAlert("Error", "Passwords do not match. Please re-enter.", "Alright");
			return;
		}

		settings = new Settings(PasswordEntry.Text, new List<string>(), new List<string>());
		settings.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
		await Navigation.PopAsync();
	}
}