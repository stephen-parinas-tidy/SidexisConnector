using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace SidexisConnector
{
    public class WebSocketServer
    {
        private readonly WebSocket _webSocket;

        public WebSocketServer(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public async Task ReceivePatientDataAsync(SidexisConnectorModel connector, String filename)
        {
            var buffer = new byte[1024];
            while (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseSent)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                
                Console.WriteLine("Patient data has been received.");
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine(message);
                var patient = JsonConvert.DeserializeObject<SidexisPatient>(message);
                ProcessTokenN(connector, patient, filename);
                ProcessTokenA(connector, patient, filename);
                Console.WriteLine("Patient data has been sent to Sidexis.");
            }
        }

        public async Task CloseAsync()
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
        }

        private static void ProcessTokenN(SidexisConnectorModel connector, SidexisPatient patient, string filename)
        {
            // Create new patient
            connector.LastNameNew = patient.LastName;
            connector.FirstNameNew = patient.FirstName;
            connector.DateOfBirthNew = patient.DateOfBirth;
            connector.ExtCardIndexNew = patient.Code;
            connector.SexNew = patient.Gender;
            connector.PermanentDentistNew = "TOMS";     // may need to change this
            connector.Sender = connector.CreateSenderAddress(Environment.MachineName, "TidyClinic");
            connector.Receiver = connector.CreateReceiverAddress("*", "SIDEXIS");
            connector.SendData(filename, SidexisConnectorModel.SlidaTokens.N);
        }
        
        private static void ProcessTokenA(SidexisConnectorModel connector, SidexisPatient patient, string filename)
        {
            // Open patient in Sidexis, and most recent image if it exists
            connector.LastName = patient.LastName;
            connector.FirstName = patient.FirstName;
            connector.DateOfBirth = patient.DateOfBirth;
            connector.ExtCardIndex = patient.Code;
            connector.StationName = Environment.MachineName;
            connector.DateOfCall = (DateTime.Now).ToString("dd.MM.yyyy");
            connector.TimeOfCall = (DateTime.Now).ToString("HH:mm:ss");
            connector.Sender = connector.CreateSenderAddress(Environment.MachineName, "TidyClinic");
            connector.Receiver = connector.CreateReceiverAddress("*", "SIDEXIS");
            connector.ImageNumber = "1";
            connector.SendData(filename, SidexisConnectorModel.SlidaTokens.A);
        }

        // TBD:
        // token U: update patient data (only used when data changes are detected)
        // image-related tokens
    }

    public class SidexisPatient
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string DateOfBirth { get; set; }
        public string Code { get; set; }
        public string Gender { get; set; }
    }
}