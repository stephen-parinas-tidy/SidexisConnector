using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NGOptMan;

namespace SidexisConnector
{
    public class ProgramData
    {
        public string SidexisPath { get; set; }
        public string SlidaPath { get; set; }
        public string ProgramPath { get; set; }
        
        public SidexisConnectorModel Connector { get; set; }

        public ProgramData()
        {
            // Initialise connector and file paths
            Connector = new SidexisConnectorModel();
            SidexisPath = string.Empty;
            SlidaPath = string.Empty;
            ProgramPath = Assembly.GetExecutingAssembly().Location;
        
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
                Console.WriteLine("Sidexis has been opened");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error starting Sidexis");
                Console.WriteLine($"An {e.GetType().Name} occurred: {e.Message}");
            }
        }
    }
}