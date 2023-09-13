using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using NGOptMan;

namespace SidexisConnector
{
    public class ProgramData
    {
        private string ProgramPath { get; set; }
        public string SidexisPath { get; set; }
        public string SlidaPath { get; set; }

        public SidexisConnectorModel Connector { get; set; }

        public ProgramData()
        {
            // Initialise connector and file paths
            Connector = new SidexisConnectorModel();
            SidexisPath = string.Empty;
            SlidaPath = string.Empty;
            ProgramPath = typeof(Program).Assembly.Location;

            // Register custom URI scheme of application if it does not exist
            # if DEBUG
                string customProtocol = "SidexisConnectorDebug";
            # else
                string customProtocol = "SidexisConnector";
            # endif
            
            var regKey = Registry.ClassesRoot.OpenSubKey(customProtocol, false);
            if (regKey == null)
            {
                // First time registering app
                RegisterUriScheme(customProtocol);
                Environment.Exit(0);
            }
            else
            {
                // If file location has changed, register new location
                var subKey = regKey.OpenSubKey("DefaultIcon");
                var subKeyValue = subKey.GetValue("", "");
                if (!((string)subKeyValue).Contains(ProgramPath))
                {
                    RegisterUriScheme(customProtocol);
                    Environment.Exit(0);
                }
                subKey.Close();
            }
            regKey.Close();

            // Retrieve Sidexis installation and Slida mailslot file path
            try
            {
                var optMan = (IOptionsManager) Activator.CreateInstance(Type.GetTypeFromProgID("SIDEXISNG.OptionsManager"));

                if (SidexisPath == string.Empty)
                {
                    SidexisPath = optMan.GetOption("SidexisPath", "");
                    SidexisPath = Path.Combine(SidexisPath, "Sidexis.exe");
                }

                if (SlidaPath == string.Empty)
                {
                    SlidaPath = optMan.GetOption("Sifiledb.ini/FromStation0/File", "");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An {e.GetType().Name} occurred: {e.Message}");
            }
        }

        private void RegisterUriScheme(string uriScheme)
        {
            Console.WriteLine("Registering '" + uriScheme + "://' URI");
            using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + uriScheme))
            {
                key.SetValue("", "URL:"+ uriScheme + " Protocol");
                key.SetValue("URL Protocol", "");

                using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
                {
                    defaultIcon.SetValue("", ProgramPath + ",1");
                    defaultIcon.Close();
                }

                using (var commandKey = key.CreateSubKey(@"shell\open\command"))
                {
                    commandKey.SetValue("", "\"" + ProgramPath + "\" \"%1\"");
                    commandKey.Close();
                }
                
                key.Close();
            }
        }
    }
}