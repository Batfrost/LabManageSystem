using Sett;
using SS;

namespace SpreadsheetGUI;

public partial class UserAgreementPage : ContentPage
{
	public UserAgreementPage(Settings S)
	{

        InitializeComponent();
        Dictionary<String, Object> agreementPageInfo = S.GetAgreementPageInfo();
        List<String> SpecialFieldsList = (List<String>)agreementPageInfo["SpecialFieldsList"];
		UAText.Text = (string)agreementPageInfo["UAText"]; ;
		for (int i = 0; i < SpecialFieldsList.Count; i++)
		{
			switch(i)
			{
				case 0:
					Field1L.IsEnabled = true;
					Field1E.IsEnabled = true;
                    Field1L.IsVisible = true;
                    Field1E.IsVisible = true;
                    Field1L.Text = SpecialFieldsList[i];
                    break;
				case 1:
                    Field2L.IsEnabled = true;
                    Field2E.IsEnabled = true;
                    Field2L.IsVisible = true;
                    Field2E.IsVisible = true;
                    Field2L.Text = SpecialFieldsList[i];
                    break;
				case 2:
                    Field3L.IsEnabled = true;
                    Field3E.IsEnabled = true;
                    Field3L.IsVisible = true;
                    Field3E.IsVisible = true;
                    Field3L.Text = SpecialFieldsList[i];
                    break;
				case 3:
                    Field4L.IsEnabled = true;
                    Field4L.IsVisible = true;
                    Field4E.IsVisible = true;
                    Field4E.IsEnabled = true;
                    Field4L.Text = SpecialFieldsList[i];
                    break;
				case 4:
                    Field5L.IsEnabled = true;
                    Field5E.IsEnabled = true;
                    Field5L.IsVisible = true;
                    Field5E.IsVisible = true;
                    Field5L.Text = SpecialFieldsList[i];
                    break;
			}
			
		}
	}

	//When the cancel button is pressed, asks if the user is sure to cancel, and go back to homepage if they are sure.
	async public void CancelSigning(object sender, EventArgs e)
	{
        string response = await DisplayActionSheet("Are you sure you wish to Cancel?" + "\n" +" Anyone can sign.", "No", null, "Yes, Cancel");
        if (response == "Yes, Cancel")
		{
			await Navigation.PopAsync();
		}

    }

	//Calls methods to save user's info into user list file
	async public void SubmitSigning(object sender, EventArgs e)
	{
		SpreadsheetGrid grid = new SpreadsheetGrid();
		bool notEverythingFilled = false;
        if (IDEntry.Text == "" || NameEntry.Text == "")
            notEverythingFilled = true;
        List<string> userInfo = new List<string>();
        userInfo.Add(IDEntry.Text);
        userInfo.Add(NameEntry.Text);

		for (int i = 0; i < 5; i++)
		{
            switch (i)
            {
                case 0:
					if (Field1E.IsEnabled)
                        if (Field1E.Text != "")
                            userInfo.Add(Field1E.Text);
                        else notEverythingFilled = true;
                    break;
                case 1:
                    if (Field2E.IsEnabled)
                        if (Field2E.Text != "")
                            userInfo.Add(Field1E.Text);
                        else notEverythingFilled = true;
                    break;
                case 2:
                    if (Field3E.IsEnabled)
                        if (Field3E.Text != "")
                            userInfo.Add(Field1E.Text);
                        else notEverythingFilled = true;
                    break;
                case 3:
                    if (Field4E.IsEnabled)
                        if (Field4E.Text != "")
                            userInfo.Add(Field1E.Text);
                        else notEverythingFilled = true;
                    break;
                case 4:
                    if (Field5E.IsEnabled)
                        if (Field5E.Text != "")
                            userInfo.Add(Field1E.Text);
                        else notEverythingFilled = true;
                    break;
            }
            if (notEverythingFilled)
                break;
        }

		if (notEverythingFilled)
		{
			await DisplayAlert("Error", "Make sure all boxes are filled out.", "Ok");
			return;
		}

		try
		{
            grid.AddUsersInformation(userInfo);
            await DisplayAlert("Success", "The user should be entered into the system now, thanks.", "Ok");
            await Navigation.PopAsync();
        } catch
		{
			//Most likely wasn't able to save to the file of users due to the file being currently open
			await DisplayAlert("Failure", "There was a problem saving the user's information. \n Make sure the users file is closed and try again.", "Ok");
		}
		
	}
}