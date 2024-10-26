using Microsoft.Maui.Controls;
using Sett;

namespace SpreadsheetGUI;

public partial class App : Application
{
    public Settings Settings;

    public App()
	{
		InitializeComponent();
        try
        {
            Settings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
            MainPage = new NavigationPage(new HomePage(Settings));
        }
        catch //If an exception occurs, the Settings file is not detected or somethings wrong with the file, so User will be asked to create new settings.
        {
            MainPage = new EstablishSettingsPage(ref Settings);
        }
        
	}
}

