using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sett
{
    public class Settings
    {
        [JsonProperty]
        public String password;
        [JsonProperty]
        public (String, String) securityQuestion;

        [JsonProperty]
        public String agreementPageText;
        [JsonProperty]
        public Dictionary<String, bool> agreementPageFields;
        Settings settings;
        


        /// <summary>
        /// This constructor will be used for a first time setup or change of settings.
        /// </summary>
        public Settings(string password, String agreementPageText, Dictionary<String, bool> agreementPageFields, (String, String) securityQuestion)
        {
            this.password = password;
            this.securityQuestion = securityQuestion;
            this.agreementPageText = agreementPageText;
            this.agreementPageFields = agreementPageFields;

        }

        /// <summary>
        /// Will create a Settings object from a given filepath
        /// </summary>
        /// <param name="filePath"></param>
        public Settings(string filePath)
        {
            settings = new Settings();
            
            System.IO.File.Decrypt(filePath);
            string fileContents = System.IO.File.ReadAllText(filePath);
            fileContents = DecryptText(fileContents);
            settings = JsonConvert.DeserializeObject<Settings>(fileContents);
            password = settings.password;
            agreementPageText = settings.agreementPageText;
            agreementPageFields = settings.agreementPageFields;
            securityQuestion = settings.securityQuestion;
            SaveSettingsFile(filePath);
            System.IO.File.Encrypt(filePath);
        }

        //Default Constructor
        public Settings()
        {
            agreementPageFields = new Dictionary<String, bool>();
        }

        /// <summary>
        /// Saves the settings file.
        /// </summary>
        public void SaveSettingsFile(string filePath)
        {
            if (!System.IO.Directory.Exists(filePath))
                System.IO.Directory.CreateDirectory(filePath.Substring(0, filePath.LastIndexOf('\\')));
            string fileContents = EncryptText(JsonConvert.SerializeObject(this));
            //fileContents += "&$&";
            //fileContents += EncryptText(JsonConvert.SerializeObject())
            System.IO.File.WriteAllText(filePath, fileContents);
            System.IO.File.Encrypt(filePath);
        }

        /// <summary>
        /// This method aims to let the user add new things to the software,
        /// such as new fields to save when logging user, new things to show on homepage, etc.
        /// </summary>
        public void UpdateSettings(Dictionary<String, Object> settingsInfo, string filePath)
        {
            password = (String)settingsInfo[password];
            SaveSettingsFile(filePath);
        }

        public void AddUserAgreementText(String text, string filePath)
        {
            agreementPageText = text;
            SaveSettingsFile(filePath);
        }

        public void AddNewUserAgreementField(List<String> settingsInfo, string filePath)
        {
            foreach(String infoField in settingsInfo)
            {
                String fieldText = infoField.Substring(0, infoField.LastIndexOf("&&"));
                bool showFieldOnHomePage = bool.Parse(infoField.Substring(infoField.IndexOf("&&") + 2));
                agreementPageFields.Add(fieldText, showFieldOnHomePage);
            }

            SaveSettingsFile(filePath);
        }

        /// <summary>
        /// Gets all the information saved within the settings file needed for the user agreement page.
        /// </summary>
        /// <returns></returns>
        public Dictionary<String, Object> GetAgreementPageInfo()
        {
            Dictionary<String, Object> UAPInfo = new Dictionary<String, Object>();
            
            List<String> specialVisibleFields = new List<String>();
            List<String> specialHiddenFields = new List<String>();
            foreach (KeyValuePair<String, bool> field in agreementPageFields) 
            {
                if (field.Value)
                {
                    if (field.Key.Contains(":"))
                        specialVisibleFields.Add(field.Key);
                    else specialVisibleFields.Add(field.Key + ": ");
                }
                else
                {
                    if (field.Key.Contains(":"))
                        specialHiddenFields.Add(field.Key);
                    else specialHiddenFields.Add(field.Key + ": ");
                }
            }
            UAPInfo.Add("UAText", agreementPageText);
            UAPInfo.Add("SpecialVisibleFields", specialVisibleFields);
            UAPInfo.Add("SpecialHiddenFields", specialHiddenFields);
            return UAPInfo;
        }

        public bool TestPassword(String enteredPassword)
        {
            return password.Equals(enteredPassword);
        }

        public bool TestSecurityQuestionAnswer(String enteredAnswer)
        {
            return securityQuestion.Item2.Equals(enteredAnswer.ToLower().Trim());
        }

        /// <summary>
        /// Encrypts given text to be unreadable to anyone who doesn't know how to decrypt.
        /// Adds just a little bit of security to the system, so that the settings files aren't messed with.
        /// </summary>
        private string EncryptText(String text)
        {
            Random r = new Random();
            char[] chars = new char[10] { 'a', 't', 'k', 'l', 'w', 'q', 'p', 'P', 'A', 'T' };
            string encryptedText = "";
            for (int i = text.Length - 1; i >= 1; i -= 2)
            {
                encryptedText += text[i - 1];
                encryptedText += text[i];
                encryptedText += chars[r.Next(9)];
            }

            return encryptedText;
        }

        /// <summary>
        /// Decrypts the text given that was specifically encrypted with the Encryption method above.
        /// </summary>
        private string DecryptText(String text)
        {
            string decryptedText = "";
            while(text.Length > 0)
            {
                decryptedText += text[text.Length - 3];
                decryptedText += text[text.Length - 2];
                text = text.Substring(0, text.Length - 3);
            }
            if (decryptedText[0] != '{')
                decryptedText = "{" + decryptedText;
            return decryptedText;
        }
    }
}