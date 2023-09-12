using System;
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
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:37319/");
                listener.Start();
                Console.WriteLine("WebSocket server is listening...");

                try
                {
                    var contextTask = listener.GetContextAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                    await Task.WhenAny(contextTask, timeoutTask);

                    if (contextTask.IsCompleted)
                    {
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
                        Console.WriteLine("WebSocket server has stopped listening...");
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
                var ws = (await context.AcceptWebSocketAsync(null)).WebSocket;
                Console.WriteLine("Connected to the server.");

                WsServer = new WebSocketServer(ws);
                await WsServer.ReceivePatientDataAsync(AppData.Connector, AppData.SlidaPath);
                await WsServer.CloseAsync();
                Console.WriteLine("Disconnected from the server.");

                AppData.TaskSwitch();
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }
}