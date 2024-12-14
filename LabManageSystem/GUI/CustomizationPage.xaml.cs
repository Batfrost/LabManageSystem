using Sett;
using SS;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WindowsFolderPicker = Windows.Storage.Pickers.FolderPicker;

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

    async private void ChangeSaveFolder(object sender, EventArgs e)
    {
        var folderPicker = new WindowsFolderPicker();
        folderPicker.FileTypeFilter.Add("*");
        var hwnd = ((MauiWinUIWindow)App.Current.Windows[0].Handler.PlatformView).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        var result = await folderPicker.PickSingleFolderAsync();
        Settings settings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        Spreadsheet IDList = new Spreadsheet(settings.saveFileLocation + "userList.csv", s => true, s => s.ToUpper(), "lab");
        settings.saveFileLocation = result.Path + "\\";
        settings.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        IDList.Save(settings.saveFileLocation + "userList.csv");

    }

}