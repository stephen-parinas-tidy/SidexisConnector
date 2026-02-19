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
        /// <summary>
        /// Holds resolved paths (Sidexis.exe, SLIDA file) and the configured connector model.
        /// </summary>
        private static ProgramData AppData { get; set; }
        
        /// <summary>
        /// WebSocket server wrapper used to handshake, decode messages, and send status.
        /// </summary>
        private static TcpWebSocketServer TwsServer { get; set; }

        /// <summary>
        /// Application entry point. Starts a local WebSocket listener, accepts one connection,
        /// forwards patient data to Sidexis via SLIDA, then responds with a status message.
        /// </summary>
        public static async Task Main(string[] args)
        {
            AppData = new ProgramData();
            LogMessageToFile("SidexisConnector has started.");
            
            // Start a local-only WebSocket listener that TidyClinic connects to.
            try
            {
                var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 37319);
                server.Start();

                try
                {
                    LogMessageToFile($"Connecting to WebSocket server on {server.LocalEndpoint}...");
                    
                    // Accept a single client connection, but only wait up to 10 seconds.
                    var clientTask = server.AcceptTcpClientAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    await Task.WhenAny(clientTask, timeoutTask);

                    // If connected, receive the patient data
                    if (clientTask.IsCompleted)
                    {
                        LogMessageToFile("Connection has been established.");
                        var client = await clientTask;
                        await HandleTcpConnection(client);
                    }
                    
                    // If we never got a connection, this run ends.
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
                // Thrown if the listener cannot bind/start.
                LogExceptionToFile(e);
            }
            
            LogMessageToFile("SidexisConnector has stopped.");
        }

        /// <summary>
        /// Handles the lifetime of a single TCP client connection.
        /// Performs WebSocket handshake if needed, otherwise reads patient data, sends it to Sidexis, replies with status, then exits.
        /// </summary>
        private static Task HandleTcpConnection(TcpClient client)
        {
            TwsServer = new TcpWebSocketServer(client);
            var stream = client.GetStream();
            var connect = true;
            
            // This loop is designed for a short-lived single connection:
            // - First request is typically a GET (WebSocket upgrade)
            // - Next frame is patient payload
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
                        // First message: HTTP GET request used to upgrade to WebSocket.
                        TwsServer.HandleWebSocketHandshake(stream, s);
                    }
                    else
                    {
                        // Subsequent message: WebSocket frame containing JSON patient data.
                        var patientData = TwsServer.HandleWebSocketMessage(bytes, AppData.MessageFilePath);
                        TwsServer.ProcessPatientData(AppData.Connector, patientData, AppData.SlidaPath);

                        // Launch Sidexis so it can receive/process the SLIDA messages.
                        TaskSwitch();
                        
                        // Return a status message to TidyClinic, then close the connection.
                        TwsServer.SendPatientStatus(stream);

                        // Bring Sidexis to foreground to encourage immediate processing of the patient selection.
                        BringToForeground();

                        connect = false;
                        client.Close();
                    }
                }
                catch (Exception e)
                {
                    // Any error ends this run; the connector is expected to be short-lived.
                    LogExceptionToFile(e);
                    
                    connect = false;
                    client.Close();
                }
            }

            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Launches the Sidexis application and sets a status message for the calling client.
        /// </summary>
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

        /// <summary>
        /// Brings the main Sidexis window to the foreground to help ensure it processes incoming data.
        /// </summary>
        private static void BringToForeground()
        {
            // SIDEXIS is the process name for the main application.
            var processes = Process.GetProcessesByName("SIDEXIS");
            
            // Choose the first Sidexis process that appears to have a main window title.
            var targetProcess = processes.FirstOrDefault(process => process.MainWindowTitle.Contains("SIDEXIS"));

            if (targetProcess == null)
            {
                return;
            }
            
            // Close secondary windows that may prevent focus/processing.
            CloseSecondaryWindow(targetProcess);
            
            var mainWindowHandle = targetProcess.MainWindowHandle;
            if (mainWindowHandle != IntPtr.Zero)
            {
                WindowsApi.SetForegroundWindow(mainWindowHandle);
            }
        }
        
        /// <summary>
        /// Closes non-main Sidexis windows for the target process to reduce interference
        /// and help the primary window handle the patient context update.
        /// </summary>
        private static void CloseSecondaryWindow(Process targetProcess)
        {
            WindowsApi.EnumWindows((hWnd, lParam) =>
            {
                int windowProcessId;
                WindowsApi.GetWindowThreadProcessId(hWnd, out windowProcessId);

                // Only act on windows belonging to the Sidexis process and currently visible.
                if (windowProcessId == targetProcess.Id && WindowsApi.IsWindowVisible(hWnd))
                {
                    var windowTitle = GetWindowTitle(hWnd);
                    
                    // Heuristic: close windows that don't include "sidexis" in the title.
                    if (!string.IsNullOrEmpty(windowTitle) && !windowTitle.ToLower().Contains("sidexis"))
                    {
                        WindowsApi.SendMessage(hWnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
                    }
                }

                return true;
            }, 0);
        }
        
        /// <summary>
        /// Retrieves the window title string for a given window handle.
        /// </summary>
        private static string GetWindowTitle(IntPtr hWnd)
        {
            const int nChars = 256;
            var sb = new StringBuilder(nChars);
            return WindowsApi.GetWindowText(hWnd, sb, nChars) > 0 ? sb.ToString() : null;
        }
        
        /// <summary>
        /// Appends exception details to the connector log file.
        /// </summary>
        private static void LogExceptionToFile(Exception ex)
        {
            // Create or append to the file
            using var writer = File.AppendText(AppData.MessageFilePath);
            
            // Write timestamp and error message to the file
            writer.WriteLine($"{DateTime.Now}: {ex.GetType().Name} - {ex.Message}");
        }
        
        /// <summary>
        /// Appends informational messages to the connector log file.
        /// </summary>
        private static void LogMessageToFile(string message)
        {
            // Create or append to the file
            using var writer = File.AppendText(AppData.MessageFilePath);
            
            // Write timestamp and error message to the file
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }

    /// <summary>
    /// Minimal P/Invoke wrapper for Windows window-management APIs used to focus Sidexis and close auxiliary windows.
    /// </summary>
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