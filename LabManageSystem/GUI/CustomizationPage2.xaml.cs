using Sett;
using SS;

namespace SpreadsheetGUI;

public partial class CustomizationPage2 : ContentPage
{
    Settings oldSettings;
    Spreadsheet oldIDList;
    Settings changedSettings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
    Spreadsheet changedIDList = new Spreadsheet(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\userList.csv", s => true, s => s.ToUpper(), "lab");
    Spreadsheet sprdsht = new Spreadsheet();
    bool HandlerRunning = false; //Don't want a handler to change one of the Xaml objects and call another handler right away. Only want handlers to run if User did something.
	public CustomizationPage2()
	{
        InitializeComponent();
        //Keep track of original settings if they cancel.
        oldSettings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        oldIDList = new Spreadsheet(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\userList.csv", s => true, s => s.ToUpper(), "lab");

        List<string> fields = new List<string>();
        try
        {
            fields = changedSettings.agreementPageFields.Keys.ToList();
            fields.Add("Add New Info Field (Max of 5)");
            InfoFieldsPicker.ItemsSource = fields;
            InfoFieldsPicker.SelectedIndex = 0;
        }
        catch
        {
            fields.Add("Add New Info Field (Max of 5)");
            InfoFieldsPicker.ItemsSource = fields;
            InfoFieldsPicker.SelectedIndex = 0;
            
        }
        SelectedInfoFieldEntry.Text = InfoFieldsPicker.SelectedItem.ToString();
        try
        {
            InfoFieldOnHomeCheckBox.IsChecked = changedSettings.agreementPageFields[InfoFieldsPicker.Items[0]];
            UserAgreementText.Text = changedSettings.agreementPageText;
        }
        catch
        {
            InfoFieldOnHomeCheckBox.IsChecked = false;
        }
        
    }

    async private void CancelButton_Clicked(object sender, EventArgs e)
    {
        //Revert changes with saved settings and IDList
        oldIDList.Save(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\userList.csv");
        oldSettings.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");

        await Navigation.PopAsync();
    }

    async private void SubmitButton_Clicked(object sender, EventArgs e)
    {
        string response = await DisplayActionSheet("Confirm changes?", "Cancel", null, "Yes");
        if (response == "Cancel")
            return;

        changedSettings.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
        await Navigation.PopToRootAsync();
        App.Current.MainPage = new NavigationPage(new HomePage(changedSettings));

    }

    private async void InfoFieldsPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;
        if (InfoFieldsPicker.SelectedItem.ToString().Equals("Add New Info Field (Max of 5)"))
        {
            if (changedSettings.agreementPageFields.Count > 4)
            {
                await DisplayAlert("Error", "Only 5 User-Specified \nInfo Fields Allowed", "Ok");
                InfoFieldsPicker.SelectedIndex = 0;
            }
            SelectedInfoFieldEntry.Text = "";
            SelectedInfoFieldEntry.Placeholder = "E.g. Class/Advisor/etc: ";
            InfoFieldOnHomeCheckBox.IsChecked = false;
        }
        else
        {
            SelectedInfoFieldEntry.Text = InfoFieldsPicker.SelectedItem.ToString();
            try
            {
                InfoFieldOnHomeCheckBox.IsChecked = changedSettings.agreementPageFields[InfoFieldsPicker.Items[InfoFieldsPicker.SelectedIndex]];
            }
            catch { InfoFieldOnHomeCheckBox.IsChecked = false; }
        }
        HandlerRunning = false;
            
    }

    async private void SelectedInfoFieldEntry_Completed(object sender, EventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;
        if (InfoFieldsPicker.SelectedItem.ToString().Equals("Add New Info Field (Max of 5)"))
        {
            if (changedSettings.agreementPageFields.Count > 4)
            {
                await DisplayAlert("Error", "Only 5 User-Specified \nInfo Fields Allowed", "Ok");
                InfoFieldsPicker.SelectedIndex = 0;
                SelectedInfoFieldEntry.Text = "";
            }
            String newInfoField = SelectedInfoFieldEntry.Text;
            if (newInfoField.Equals("Add New Info Field (Max of 5)"))
            {
                SelectedInfoFieldEntry.Text = "";
                return;
            }
            bool showInfo = false;
            newInfoField += "&&" + showInfo.ToString();
            List<string> fieldsToAdd = new List<string>{ newInfoField };
            changedSettings.AddNewUserAgreementField(fieldsToAdd, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\TWLogging\settings.config");
            List<string> fields = changedSettings.agreementPageFields.Keys.ToList();
            fields.Add("Add New Info Field (Max of 5)");
            InfoFieldsPicker.ItemsSource = fields;
            InfoFieldsPicker.SelectedIndex = InfoFieldsPicker.Items.IndexOf(SelectedInfoFieldEntry.Text);
            //Update the IDList too
            sprdsht.EditIDListField(null, null, SelectedInfoFieldEntry.Text, null);
        }
        else //Editing an existing info field name
        {
            sprdsht.EditIDListField(InfoFieldsPicker.SelectedItem.ToString(), SelectedInfoFieldEntry.Text, null, null);
            bool temp = changedSettings.agreementPageFields[InfoFieldsPicker.SelectedItem.ToString()];
            changedSettings.agreementPageFields.Remove(InfoFieldsPicker.SelectedItem.ToString());
            changedSettings.agreementPageFields.Add(SelectedInfoFieldEntry.Text, temp);
            List<string> fields = changedSettings.agreementPageFields.Keys.ToList();
            fields.Add("Add New Info Field (Max of 5)");
            InfoFieldsPicker.ItemsSource = fields;
            InfoFieldsPicker.SelectedIndex = InfoFieldsPicker.Items.IndexOf(SelectedInfoFieldEntry.Text);
        }
        HandlerRunning = false;
    }

    private void InfoFieldOnHomeCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;
        try
        {
            changedSettings.agreementPageFields[InfoFieldsPicker.Items[InfoFieldsPicker.SelectedIndex]] = InfoFieldOnHomeCheckBox.IsChecked;
        }
        catch { }
        

        HandlerRunning = false;
    }

    private void UserAgreementText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;

        changedSettings.agreementPageText = UserAgreementText.Text;
        
        HandlerRunning = false;
    }

    private void RemoveSelectedInfoFieldButton_Clicked(object sender, EventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;
        try
        {
            changedSettings.agreementPageFields.Remove(SelectedInfoFieldEntry.Text);
            sprdsht.EditIDListField(null, null, null, InfoFieldsPicker.SelectedItem.ToString());
        }
        catch
        {
            HandlerRunning = false;
            return;
        }
        
        List<string> fields = changedSettings.agreementPageFields.Keys.ToList();
        fields.Add("Add New Info Field (Max of 5)");
        InfoFieldsPicker.ItemsSource = fields;
        InfoFieldsPicker.SelectedIndex = 0;
        SelectedInfoFieldEntry.Text = InfoFieldsPicker.SelectedItem.ToString();
        try
        {
            InfoFieldOnHomeCheckBox.IsChecked = changedSettings.agreementPageFields[InfoFieldsPicker.Items[0]];
        } 
        catch
        {
            InfoFieldOnHomeCheckBox.IsChecked = false;
        }
        

        HandlerRunning = false;
    }

}