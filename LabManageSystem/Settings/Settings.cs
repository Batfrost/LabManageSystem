using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Settings
{
    public class Settings
    {
        String password;
        Settings settings;

        /// <summary>
        /// This constructor will be used for a first time setup or change of settings.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="agreementPageInfo"></param>
        public Settings(string password, List<string> agreementPageInfo)
        {
            this.password = password;

        }

        /// <summary>
        /// Will create a Settings object from a given filepath
        /// </summary>
        /// <param name="filePath"></param>
        public Settings(string filePath) 
        {
            
        }

        /// <summary>
        /// Will attempt to load a settings object, if one exists returns true, 
        /// else returns false to let caller know that we haven't established settings yet.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool LoadSettingsFile(string path)
        {
            try
            {
                settings = new Settings(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Log Files\settings.config");
            }
            catch
            {
                //Let the caller know that settings needs to be created, this is most likely first time boot of software.
                return false;
            }
            
            return true;
        }

        private void SaveSettingsFile(string path)
        {

        }

        /// <summary>
        /// This method aims to let the user add new things to the software,
        /// such as new fields to save when logging user, new things to show on homepage, etc.
        /// </summary>
        public void UpdateSettings()
        {

        }
    }
}
