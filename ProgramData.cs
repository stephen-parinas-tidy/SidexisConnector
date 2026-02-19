using System;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;
using NGOptMan;

namespace SidexisConnector
{
    /// <summary>
    /// Holds runtime configuration and environment-specific data required for the Sidexis connector to function.
    /// </summary>
    public class ProgramData
    {
        /// <summary>
        /// Path to the current executable.
        /// </summary>
        private string ProgramPath { get; set; }
        
        /// <summary>
        /// Full path to the Sidexis installation.
        /// </summary>
        public string SidexisPath { get; set; }
        
        /// <summary>
        /// Path to the SLIDA mailslot file.
        /// </summary>
        public string SlidaPath { get; set; }
        
        /// <summary>
        /// Path to the file used for diagnostic/error logging.
        /// </summary>
        public string MessageFilePath { get; set; }

        /// <summary>
        /// Connector model used to build and send SLIDA tokens.
        /// </summary>
        public SidexisConnectorModel Connector { get; set; }

        public ProgramData()
        {
            // Initialise connector and file paths
            SidexisPath = string.Empty;
            SlidaPath = string.Empty;
            ProgramPath = typeof(Program).Assembly.Location;
            MessageFilePath = Path.Combine(Path.GetDirectoryName(ProgramPath) ?? string.Empty, "TidySidexisConnector.txt");
            
            // Ensure the error log file exists. If it doesn't exist, create it
            if (!File.Exists(MessageFilePath))
            {
                File.Create(MessageFilePath).Close();
                using (StreamWriter writer = File.AppendText(MessageFilePath))
                {
                    // Write timestamp and message to the file
                    writer.WriteLine($"{DateTime.Now}: TidySidexisConnector.txt created");
                }
            }
            
            // Initialise SLIDA connector model with log file path
            Connector = new SidexisConnectorModel(MessageFilePath);

            // Register custom URI scheme of application if it does not exist
            # if DEBUG
                string customProtocol = "SidexisConnectorDebug";
            # else
                string customProtocol = "SidexisConnector";
            # endif

            // Ensure custom URI scheme is registered and points to this executable
            try
            {
                var userRegKey = Registry.CurrentUser.OpenSubKey("Software\\Classes", true);
                
                // Clean up any user-level registration
                if (userRegKey != null)
                {
                    userRegKey.DeleteSubKeyTree(customProtocol, false);
                }
                
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
                    // Validate existing registration points to correct executable
                    var subKey = regKey.OpenSubKey("DefaultIcon", false);
                    var subKeyValue = subKey.GetValue("", "");
                    
                    // Re-register if executable location changed
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
            

            // Retrieve Sidexis installation and SLIDA configuration via COM
            try
            {
                // SIDEXISNG.OptionsManager provides configuration values
                var optMan = (IOptionsManager) Activator.CreateInstance(Type.GetTypeFromProgID("SIDEXISNG.OptionsManager"));

                if (SidexisPath == string.Empty)
                {
                    SidexisPath = optMan.GetOption("SidexisPath", "");
                    SidexisPath = Path.Combine(SidexisPath, "Sidexis.exe");
                }

                if (SlidaPath == string.Empty)
                {
                    // SLIDA file path used to write token messages
                    SlidaPath = optMan.GetOption("Sifiledb.ini/FromStation0/File", "");
                }
            }
            catch (Exception e)
            {
                LogExceptionToFile(e);
            }
        }

        /// <summary>
        /// Registers the custom URI scheme in the Windows Registry.
        /// Enables browser to launch this application via: SidexisConnector://
        /// </summary>
        private void RegisterUriScheme(string uriScheme)
        {
            // Registry write requires administrator privileges
            if (!IsAdministrator())
            {
                LogMessageToFile($"Could not register the '{uriScheme}://' URI. Please run this program as an administrator.");
                Environment.Exit(0);
            }
            
            // Register protocol under HKLM\Software\Classes
            try
            {
                var key = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Classes\\" + uriScheme);

                using (key)
                {
                    key?.SetValue("", "URL:" + uriScheme + " Protocol");
                    key?.SetValue("URL Protocol", "");

                    // Set application icon reference
                    using (var defaultIcon = key?.CreateSubKey("DefaultIcon"))
                    {
                        defaultIcon?.SetValue("", ProgramPath + ",1");
                        defaultIcon?.Close();
                    }

                    // Set command executed when URI is triggered
                    using (var commandKey = key?.CreateSubKey(@"shell\open\command"))
                    {
                        commandKey?.SetValue("", "\"" + ProgramPath + "\" \"%1\"");
                        commandKey?.Close();
                    }

                    key?.Close();
                }
            }
            catch (Exception e)
            {
                LogExceptionToFile(e);
            }
        }
        
        /// <summary>
        /// Determines whether the current process is running with Administrator privileges.
        /// </summary>
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        /// <summary>
        /// Writes exception details to the connector log file.
        /// </summary>
        private void LogExceptionToFile(Exception ex)
        {
            // Create or append to the file
            using var writer = File.AppendText(MessageFilePath);
            
            // Write timestamp and error message to the file
            writer.WriteLine($"{DateTime.Now}: {ex.GetType().Name} - {ex.Message}");
        }
        
        /// <summary>
        /// Writes informational messages to the connector log file.
        /// </summary>
        private void LogMessageToFile(string message)
        {
            // Create or append to the file
            using var writer = File.AppendText(MessageFilePath);
            
            // Write timestamp and message to the file
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }
}