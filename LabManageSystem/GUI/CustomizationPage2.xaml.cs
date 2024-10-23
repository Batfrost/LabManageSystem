using Sett;
using SS;

namespace SpreadsheetGUI;

public partial class CustomizationPage2 : ContentPage
{
    Settings s = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
    Spreadsheet sprdsht = new Spreadsheet();
    bool HandlerRunning = false; //Don't want a handler to change one of the Xaml objects and call another handler right away. Only want handlers to run if User did something.
	public CustomizationPage2()
	{

        InitializeComponent();
        List<string> fields = new List<string>();
        try
        {
            fields = s.agreementPageFields.Keys.ToList();
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
            InfoFieldOnHomeCheckBox.IsChecked = s.agreementPageFields[InfoFieldsPicker.Items[0]];
            UserAgreementText.Text = s.agreementPageText;
        }
        catch
        {
            InfoFieldOnHomeCheckBox.IsChecked = false;
        }
        
    }

    async private void CancelButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    async private void SubmitButton_Clicked(object sender, EventArgs e)
    {
        string response = await DisplayActionSheet("Confirm changes?", "Cancel", null, "Yes");
        if (response == "Cancel")
            return;

        s.SaveSettingsFile(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
        await Navigation.PopToRootAsync();
        App.Current.MainPage = new NavigationPage(new HomePage(s));

    }

    private async void InfoFieldsPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;
        if (InfoFieldsPicker.SelectedItem.ToString().Equals("Add New Info Field (Max of 5)"))
        {
            if (s.agreementPageFields.Count > 4)
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
                InfoFieldOnHomeCheckBox.IsChecked = s.agreementPageFields[InfoFieldsPicker.Items[InfoFieldsPicker.SelectedIndex]];
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
            if (s.agreementPageFields.Count > 4)
            {
                await DisplayAlert("Error", "Only 5 User-Specified \nInfo Fields Allowed", "Ok");
                InfoFieldsPicker.SelectedIndex = 0;
                SelectedInfoFieldEntry.Text = "";
            }
            String newInfoField = SelectedInfoFieldEntry.Text;
            bool showInfo = false;
            newInfoField += "&&" + showInfo.ToString();
            List<string> fieldsToAdd = new List<string>{ newInfoField };
            s.AddNewUserAgreementField(fieldsToAdd, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
            List<string> fields = s.agreementPageFields.Keys.ToList();
            fields.Add("Add New Info Field (Max of 5)");
            InfoFieldsPicker.ItemsSource = fields;
            InfoFieldsPicker.SelectedIndex = InfoFieldsPicker.Items.IndexOf(SelectedInfoFieldEntry.Text);
        }
        else //Editing an existing info field name
        {
            sprdsht.EditIDListField(InfoFieldsPicker.SelectedItem.ToString(), SelectedInfoFieldEntry.Text);
            bool temp = s.agreementPageFields[InfoFieldsPicker.SelectedItem.ToString()];
            s.agreementPageFields.Remove(InfoFieldsPicker.SelectedItem.ToString());
            s.agreementPageFields.Add(SelectedInfoFieldEntry.Text, temp);
            List<string> fields = s.agreementPageFields.Keys.ToList();
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
            s.agreementPageFields[InfoFieldsPicker.Items[InfoFieldsPicker.SelectedIndex]] = InfoFieldOnHomeCheckBox.IsChecked;
        }
        catch { }
        

        HandlerRunning = false;
    }

    private void UserAgreementText_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;

        s.agreementPageText = UserAgreementText.Text;
        
        HandlerRunning = false;
    }

    private void RemoveSelectedInfoFieldButton_Clicked(object sender, EventArgs e)
    {
        if (HandlerRunning)
            return;
        HandlerRunning = true;
        try
        {
            s.agreementPageFields.Remove(SelectedInfoFieldEntry.Text);
        }
        catch
        {
            HandlerRunning = false;
            return;
        }
        
        List<string> fields = s.agreementPageFields.Keys.ToList();
        fields.Add("Add New Info Field (Max of 5)");
        InfoFieldsPicker.ItemsSource = fields;
        InfoFieldsPicker.SelectedIndex = 0;
        SelectedInfoFieldEntry.Text = InfoFieldsPicker.SelectedItem.ToString();
        try
        {
            InfoFieldOnHomeCheckBox.IsChecked = s.agreementPageFields[InfoFieldsPicker.Items[0]];
        } 
        catch
        {
            InfoFieldOnHomeCheckBox.IsChecked = false;
        }
        

        HandlerRunning = false;
    }

}