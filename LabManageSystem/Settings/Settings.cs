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

        public Settings() 
        {
            if (System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)))
            {
                
            }
        }
    }
}
