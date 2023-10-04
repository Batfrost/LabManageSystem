using SS;

namespace SpreadsheetGUI;

public partial class UserAgreementPage : ContentPage
{
	public UserAgreementPage()
	{
		InitializeComponent();
	}

	//When the cancel button is pressed, asks if the user is sure to cancel, and go back to homepage if they are sure.
	async public void CancelSigning(object sender, EventArgs e)
	{
        string response = await DisplayActionSheet("Are you sure you wish to Cancel?" + "\n" +" Any student can sign.", "No", null, "Yes, Cancel");
        if (response == "Yes, Cancel")
		{
			await Navigation.PushAsync(new HomePage());
			return;
		}

    }

	//Calls methods to save user's info into user list file
	async public void SubmitSigning(object sender, EventArgs e)
	{
		SpreadsheetGrid grid = new SpreadsheetGrid();
		if (ClassList.SelectedItem == null || UIDBox.Text == "" || NameBox.Text == "")
		{
			await DisplayAlert("Error", "Make sure all boxes are filled out.", "Ok");
			return;
		}
		try
		{
            grid.AddUsersInformation(UIDBox.Text, NameBox.Text, ClassList.SelectedItem.ToString());
            await DisplayAlert("Success", "The user should be entered into the system now and logged, thanks.", "Ok");
            await Navigation.PushAsync(new HomePage());
        } catch
		{
			//Most likely wasn't able to save to the file of users due to the file being currently open
			await DisplayAlert("Failure", "There was a problem saving the user's information. \n Make sure the users file is closed and try again.", "Ok");
		}
		
	}
}