using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SidexisConnector
{
    internal class Program
    {
        private static ProgramData AppData { get; set; }
        private static TcpWebSocketServer TwsServer { get; set; }

        public static async Task Main(string[] args)
        {
            AppData = new ProgramData();
            LogMessageToFile("SidexisConnector has started.");
            
            // Setup WebSocket server on a localhost port and start listening to it
            try
            {
                var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 37319);
                server.Start();

                try
                {
                    LogMessageToFile($"Connecting to WebSocket server on {server.LocalEndpoint}...");
                    
                    // Listen for a connection from TidyClinic (up to 10 seconds before it times out)
                    var clientTask = server.AcceptTcpClientAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    await Task.WhenAny(clientTask, timeoutTask);

                    if (clientTask.IsCompleted)
                    {
                        // If connected, receive the patient data
                        LogMessageToFile("Connection has been established.");
                        var client = await clientTask;
                        await HandleTcpConnection(client);
                    }
                    if (timeoutTask.IsCompleted)
                    {
                        throw new TimeoutException("WebSocket server timed out after 10 seconds.");
                    }
                }
                catch (TimeoutException e)
                {
                    LogExceptionToFile(e);
                }
                finally
                {
                    // Stop listening to the localhost port with the WebSocket server
                    server.Stop();
                }
            }

            catch (HttpListenerException e)
            {
                LogExceptionToFile(e);
            }
            
            LogMessageToFile("SidexisConnector has stopped.");
        }

        private static Task HandleTcpConnection(TcpClient client)
        {
            TwsServer = new TcpWebSocketServer(client);
            var stream = client.GetStream();
            var connect = true;
            
            while (connect) {
                while (!stream.DataAvailable);
                while (client.Available < 3); // match against "get"

                var bytes = new byte[client.Available];
                var read = stream.Read(bytes, 0, bytes.Length);
                var s = Encoding.UTF8.GetString(bytes);

                try
                {
                    if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                    {
                        // Upgrade connection to WebSocket
                        TwsServer.HandleWebSocketHandshake(stream, s);
                    }
                    else
                    {
                        // Receive patient data from TidyClinic and send it to Sidexis
                        var patientData = TwsServer.HandleWebSocketMessage(bytes, AppData.MessageFilePath);
                        TwsServer.ProcessPatientData(AppData.Connector, patientData, AppData.SlidaPath);

                        // Launch Sidexis
                        TaskSwitch();
                        
                        // Send status back to TidyClinic, then close the connection
                        TwsServer.SendPatientStatus(stream);

                        // Bring Sidexis window to foreground so it processes the patient data
                        BringToForeground();

                        connect = false;
                        client.Close();
                    }
                }
                catch (Exception e)
                {
                    LogExceptionToFile(e);
                    
                    connect = false;
                    client.Close();
                }
            }

            return Task.CompletedTask;
        }
        
        private static void TaskSwitch()
        {
            try
            {
                Process.Start(AppData.SidexisPath);
                TwsServer.PatientDataStatus = "Success: Sidexis launched and patient data sent.";
            }
            catch (Exception e)
            {
                LogExceptionToFile(e);
                TwsServer.PatientDataStatus = "Could not open Sidexis.";
            }
        }

        private static void BringToForeground()
        {
            // Bring Sidexis window to foreground so it processes the patient data
            var processes = Process.GetProcessesByName("SIDEXIS");
            var targetProcess = processes.FirstOrDefault(process => process.MainWindowTitle.Contains("SIDEXIS"));

            if (targetProcess == null)
            {
                return;
            }
            
            CloseSecondaryWindow(targetProcess);
            var mainWindowHandle = targetProcess.MainWindowHandle;
            if (mainWindowHandle != IntPtr.Zero)
            {
                WindowsApi.SetForegroundWindow(mainWindowHandle);
            }
        }
        
        private static void CloseSecondaryWindow(Process targetProcess)
        {
            // Close any open secondary Sidexis windows so it processes the patient data
            WindowsApi.EnumWindows((hWnd, lParam) =>
            {
                int windowProcessId;
                WindowsApi.GetWindowThreadProcessId(hWnd, out windowProcessId);

                if (windowProcessId == targetProcess.Id && WindowsApi.IsWindowVisible(hWnd))
                {
                    var windowTitle = GetWindowTitle(hWnd);
                    if (!string.IsNullOrEmpty(windowTitle) && !windowTitle.ToLower().Contains("sidexis"))
                    {
                        WindowsApi.SendMessage(hWnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    }
                }

                return true;
            }, 0);
        }
        
        private static string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            var sb = new StringBuilder(nChars);
            return WindowsApi.GetWindowText(hWnd, sb, nChars) > 0 ? sb.ToString() : null;
        }
        
        private static void LogExceptionToFile(Exception ex)
        {
            // Create or append to the file
            using var writer = File.AppendText(AppData.MessageFilePath);
            
            // Write timestamp and error message to the file
            writer.WriteLine($"{DateTime.Now}: {ex.GetType().Name} - {ex.Message}");
        }
        
        private static void LogMessageToFile(string message)
        {
            // Create or append to the file
            using var writer = File.AppendText(AppData.MessageFilePath);
            
            // Write timestamp and error message to the file
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }

    public static class WindowsApi
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int EnumWindows(EnumWindowsProc lpEnumFunc, int lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        
        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
    }
}