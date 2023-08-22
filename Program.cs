using System;
using System.Threading.Tasks;

namespace SidexisConnector
{
    internal class Program
    {
        private static ProgramData AppData { get; set; }
        
        private static WebSocketClient WsClient { get; set; }

        public static async Task Main(string[] args)
        {
            AppData = new ProgramData();
            
            // Create WebSocket server and receive patient data
            WsClient = new WebSocketClient();
            if (await WsClient.ConnectAsync("ws://localhost:37319/ws"))
            {
                // Process patient data then close server
                await WsClient.ReceivePatientDataAsync(AppData.Connector, AppData.SlidaPath);
                await WsClient.CloseAsync();
                
                // Launch Sidexis
                AppData.TaskSwitch();
            }
            //Console.WriteLine("Press any key to exit");
            //Console.ReadKey();
        }
    }
}