using SS;
using System.ComponentModel;
using Sett;

namespace SpreadsheetGUI;

public partial class HomePage : ContentPage
{
	public SpreadsheetPage SprdSht;
	public ManagerPage ManagerPg;
	private Settings Settings;
	static private readonly object lockObject = new object();
	public DateTime ScheduledTime = DateTime.Today.AddHours(1.0);
	public Spreadsheet bugReport = new();

    public HomePage(Settings sett)
	{
		lock (lockObject)
		{
			InitializeComponent();
			SprdSht = new SpreadsheetPage();

			Settings = sett;
			SprdSht.LoadSettings();
			Dictionary<String, Object> agreementPageInfo = Settings.GetAgreementPageInfo();
			List<String> SpecialVisibleFields = (List<String>)agreementPageInfo["SpecialVisibleFields"];
			SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList());
			SprdSht.GetCurrentlyLoggedIn(SpecialVisibleFields);
			SprdSht.AttendanceChecker();
			ScheduleAttendanceCheck(ScheduledTime);
			currentlyLoggedIn.Load(Settings.saveFileLocation + "currentlyLoggedIn.csv");
            //Setting up a bug report, in case anything ever crashes, the bug report should successfully update.
            try
            {
                bugReport = new Spreadsheet(Settings.saveFileLocation + "BugReport.csv", s => true, s => s.ToUpper(), "lab");
            }
            catch
            {
                bugReport.Save(Settings.saveFileLocation + "BugReport.csv");
            }
            try
			{
				if (!SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList()))
					DisplayAlert("User ID Spreadsheet Error", "There was an error checking the student List file, \n please make sure the file is closed and try again.", "Ok");
			}
			catch (Exception ex) 
			{
				bugReport.UpdateBugReport(ex);
				DisplayAlert("Error", "Problem loading the file with all Users ID's. Error: \n" + ex.Message, "OK."); 
			}
		}
	}

	public async void ScheduleAttendanceCheck(DateTime ExecutionTime)
	{
		try
		{
			if ((int)ExecutionTime.Subtract(DateTime.Now).TotalMilliseconds > 0)
				await Task.Delay((int)ExecutionTime.Subtract(DateTime.Now).TotalMilliseconds);
			else
			{
				//Its past the scheduled time so wait till tomorrow.
				ExecutionTime = ExecutionTime.AddDays(1.0);
				await Task.Delay((int)ExecutionTime.Subtract(DateTime.Now).TotalMilliseconds);
			}
			SprdSht.AttendanceChecker();
			ScheduleAttendanceCheck(ExecutionTime.AddDays(1.0));
		} 
		catch (Exception ex)
		{
            bugReport.UpdateBugReport(ex);
        }
    }

	async void GoToManagerMode(object sender, EventArgs e)
	{

		if (Settings.TestPassword(ManagerPasswordEntry.Text))
			await Navigation.PushAsync(new ManagerPage());
		else
		{
			string response = await DisplayActionSheet("Error: Incorrect Password", "Ok", null, "Forgot Password?");
			if (response.Equals("Forgot Password?"))
				await Navigation.PushAsync(new ManagerPasswordPage());
		}
        ManagerPasswordEntry.Text = "";
    }


	void LoginUser(object sender, EventArgs e)
	{
		try
		{
			lock (lockObject)
			{
				SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList());
				currentlyLoggedIn.Load(Settings.saveFileLocation + "currentlyLoggedIn.csv");

				string userName = "";
				if (UIDEntry.Text.Length == 8)
				{
					try
					{
						string ID = UIDEntry.Text;
						Dictionary<String, Object> agreementPageInfo = Settings.GetAgreementPageInfo();
						List<String> SpecialHiddenFields = (List<String>)agreementPageInfo["SpecialHiddenFields"];
						userName = SprdSht.LoginUser(ID, SpecialHiddenFields);

						System.Timers.Timer timer = new System.Timers.Timer();
						timer.Elapsed += new System.Timers.ElapsedEventHandler((sender, e) => AutomaticallyLogOutUser(sender, e, ID));
						timer.Interval = 3600000;
						timer.Enabled = true;
					}
					catch
					{
						DisplayAlert("Failure", "There was a problem attempting to edit the log file. \n Make sure all spreadsheet files in the log folder are closed and try again.", "Ok");
						UIDEntry.Text = "";
						return;
					}


					if (!userName.Equals("NOT FOUND"))
					{
						StudentFindability.Text = userName + DateTime.Now.ToShortTimeString();
					}
					else
						UserAgreementSigning(sender, e);
					UIDEntry.Text = "";
				}
			}
		}
		catch (Exception ex)
		{
			bugReport.UpdateBugReport(ex);
		}
    }

	//After 2 hours, the user will automatically "logout" in that the currentlyLoggedIn sheet won't show them anymore.
	private void AutomaticallyLogOutUser(object sender, EventArgs e, string ID)
	{
		try
		{
			lock (lockObject)
			{
				System.Timers.Timer timer = (System.Timers.Timer)sender;
				timer.Stop();
				SprdSht.GetIDList(Settings.agreementPageFields.Keys.ToList());
				SprdSht.LogoutUserFromCurrentlyLoggedInSheet(ID);
				currentlyLoggedIn.Load(Settings.saveFileLocation + "currentlyLoggedIn.csv");
			}
		}
		catch (Exception ex)
		{
			bugReport.UpdateBugReport(ex);
		}
    }


	async void UserAgreementSigning(object sender, EventArgs e)
	{
		UserAgreementPage SigningPage = new UserAgreementPage(Settings);
		await Navigation.PushAsync(SigningPage);
	}
}