using System;
using System.Net;
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
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:37319/");
            listener.Start();

            Console.WriteLine("WebSocket server is listening...");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    var ws = (await context.AcceptWebSocketAsync(null)).WebSocket;
                    Console.WriteLine("TidyClinic client has connected to the server.");
                    
                    WsServer = new WebSocketServer(ws);
                    await WsServer.ReceivePatientDataAsync(AppData.Connector, AppData.SlidaPath);
                    await WsServer.CloseAsync();
                    Console.WriteLine("TidyClinic client has disconnected from the server.");
                    
                    AppData.TaskSwitch();
                    break;
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }
    }
}