using System;
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
        public string MessageFilePath { get; set; }

        public SidexisConnectorModel Connector { get; set; }

        public ProgramData()
        {
            // Initialise connector and file paths
            SidexisPath = string.Empty;
            SlidaPath = string.Empty;
            ProgramPath = typeof(Program).Assembly.Location;
            MessageFilePath = Path.Combine(Path.GetDirectoryName(ProgramPath) ?? string.Empty, "TidySidexisConnector.txt");
            
            if (!File.Exists(MessageFilePath))
            {
                // If the file doesn't exist, create it
                File.Create(MessageFilePath).Close();
                using (StreamWriter writer = File.AppendText(MessageFilePath))
                {
                    // Write timestamp and message to the file
                    writer.WriteLine($"{DateTime.Now}: TidySidexisConnector.txt created");
                }
            }
            
            Connector = new SidexisConnectorModel(MessageFilePath);

            // Register custom URI scheme of application if it does not exist
            # if DEBUG
                string customProtocol = "SidexisConnectorDebug";
            # else
                string customProtocol = "SidexisConnector";
            # endif

            try
            {
                var regKey = Registry.ClassesRoot.OpenSubKey(customProtocol, false);
                if (regKey == null)
                {
                    // First time registering app
                    RegisterUriScheme(customProtocol);
                    LogMessageToFile($"Registering '{customProtocol}://' URI in {ProgramPath}");
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
                        LogMessageToFile($"Registering new location for '{customProtocol}://' URI in {ProgramPath}");
                        Environment.Exit(0);
                    }

                    subKey.Close();
                }

                regKey.Close();
            }
            catch (Exception e)
            {
                LogExceptionToFile(e);
            }
            

            // Retrieve Sidexis installation and Slida mail slot file path
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
                LogExceptionToFile(e);
            }
        }

        private void RegisterUriScheme(string uriScheme)
        {
            using (var key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Classes\\" + uriScheme))
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
        
        private void LogExceptionToFile(Exception ex)
        {
            // Create or append to the file
            using (StreamWriter writer = File.AppendText(MessageFilePath))
            {
                // Write timestamp and error message to the file
                writer.WriteLine($"{DateTime.Now}: {ex.GetType().Name} - {ex.Message}");
            }
        }
        
        private void LogMessageToFile(string message)
        {
            // Create or append to the file
            using (StreamWriter writer = File.AppendText(MessageFilePath))
            {
                // Write timestamp and message to the file
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }
    }
}