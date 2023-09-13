using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SidexisConnector
{
    internal class Program
    {
        private static ProgramData AppData { get; set; }
        
        private static WebSocketServer WsServer { get; set; }

        public static async Task Main(string[] args)
        {
            AppData = new ProgramData();
            
            // Create WebSocket server and receive patient data
            try
            {
                // Setup WebSocket server on a localhost port and start listening to it
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:37319/");
                listener.Start();

                try
                {
                    // Listen for a connection from TidyClinic (up to 10 seconds before it times out)
                    var contextTask = listener.GetContextAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    await Task.WhenAny(contextTask, timeoutTask);

                    if (contextTask.IsCompleted)
                    {
                        // If connected, receive the patient data
                        HttpListenerContext context = await contextTask;
                        await HandleWebSocketRequest(context);
                    }
                    else
                    {
                        throw new TimeoutException("WebSocket server timed out after 10 seconds.");
                    }
                }
                catch (TimeoutException e)
                {
                    Console.WriteLine($"A {e.GetType().Name} occurred: {e.Message}");
                }
                finally
                {
                    // Stop listening to the localhost port with the WebSocket server
                    if (listener.IsListening)
                    {
                        listener.Stop();
                    }
                }
            }

            catch (HttpListenerException e)
            {
                Console.WriteLine($"A {e.GetType().Name} occurred: {e.Message}");
            }
        }

        private static async Task HandleWebSocketRequest(HttpListenerContext context)
        {
            if (context.Request.IsWebSocketRequest)
            {
                // Establish connection to TidyClinic
                var ws = (await context.AcceptWebSocketAsync(null)).WebSocket;
                
                // Receive patient data from TidyClinic and send it to Sidexis
                WsServer = new WebSocketServer(ws);
                await WsServer.ReceivePatientDataAsync(AppData.Connector, AppData.SlidaPath);
                
                // Launch Sidexis
                TaskSwitch();
                
                // Send status back to TidyClinic, then close the connection
                await WsServer.SendStatusAsync();
                await WsServer.CloseAsync();

                // Bring Sidexis window to foreground so it processes the patient data
                BringToForeground();
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
        
        private static void TaskSwitch()
        {
            try
            {
                Process.Start(AppData.SidexisPath);
                WsServer.PatientDataStatus = "Success: Sidexis launched and patient data sent.";
            }
            catch (Exception e)
            {
                Console.WriteLine($"A {e.GetType().Name} occurred: {e.Message}");
                WsServer.PatientDataStatus = "Unable to open Sidexis.";
            }
        }

        private static void BringToForeground()
        {
            // Bring Sidexis window to foreground so it processes the patient data
            Process[] processes = Process.GetProcessesByName("SIDEXIS");
            Process targetProcess = processes.FirstOrDefault(process => process.MainWindowTitle.Contains("SIDEXIS XG"));

            if (targetProcess != null)
            {
                CloseSecondaryWindow(targetProcess);
                IntPtr mainWindowHandle = targetProcess.MainWindowHandle;
                if (mainWindowHandle != IntPtr.Zero)
                {
                    WindowsApi.SetForegroundWindow(mainWindowHandle);
                }
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
                    WindowsApi.SendMessage(hWnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
                }

                return true;
            }, 0);
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
        
        public delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);
    }
}