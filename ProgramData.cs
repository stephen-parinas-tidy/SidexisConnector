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
        private string SidexisPath { get; set; }
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
            string customProtocol = "SidexisConnector";
            var regKey = Registry.ClassesRoot.OpenSubKey(customProtocol, false);
            if (regKey == null)
            {
                RegisterUriScheme(customProtocol);
            }
            else
            {
                regKey.Close();
            }
            

            // Retrieve Sidexis installation and Slida mailslot file path
            try
            {
                var optMan = (IOptionsManager) Activator.CreateInstance(Type.GetTypeFromProgID("SIDEXISNG.OptionsManager"));

                if (SidexisPath == string.Empty)
                {
                    SidexisPath = optMan.GetOption("SidexisPath", ""); // Always includes trailing backslash
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
        
        public void TaskSwitch()
        {
            try
            {
                Process.Start(SidexisPath);
                Console.WriteLine("Sidexis has been opened!");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error starting Sidexis.");
                Console.WriteLine($"A {e.GetType().Name} occurred: {e.Message}");
            }
        }

        private void RegisterUriScheme(string uriScheme)
        {
            using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + uriScheme))
            {
                key.SetValue("", "URL:SidexisConnector Protocol");
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