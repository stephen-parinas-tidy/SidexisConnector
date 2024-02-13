using System;
using System.Linq;
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

        public string PatientDataStatus { get; set; }

        public WebSocketServer(WebSocket webSocket)
        {
            _webSocket = webSocket;
            PatientDataStatus = string.Empty;
        }

        public async Task ReceivePatientDataAsync(SidexisConnectorModel connector, String filename)
        {
            var buffer = new byte[1024];
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseSent)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType != WebSocketMessageType.Close)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var patient = JsonConvert.DeserializeObject<SidexisPatient>(message);
                
                    ProcessTokenN(connector, patient, filename);
                    ProcessTokenU(connector, patient, filename);
                    ProcessTokenA(connector, patient, filename);
                }
            }
        }

        public async Task SendStatusAsync()
        {
            var bytes = Encoding.UTF8.GetBytes(PatientDataStatus);
            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
            await _webSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
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
            connector.SexNew = patient.Sex;
            connector.PermanentDentistNew = patient.PreferredDoctor;
            connector.Sender = connector.CreateSenderAddress(Environment.MachineName, "TidyClinic");
            connector.Receiver = connector.CreateReceiverAddress(Environment.MachineName, "PDATA");
            connector.SendData(filename, SidexisConnectorModel.SlidaTokens.N);
        }

        private static void ProcessTokenU(SidexisConnectorModel connector, SidexisPatient patient, string filename)
        {
            // Update patient data
            connector.LastName = patient.LastName;
            connector.FirstName = patient.FirstName;
            connector.DateOfBirth = patient.DateOfBirth;
            connector.ExtCardIndex = patient.Code;
            connector.LastNameNew = patient.LastName;
            connector.FirstNameNew = patient.FirstName;
            connector.DateOfBirthNew = patient.DateOfBirth;
            connector.ExtCardIndexNew = patient.Code;
            connector.SexNew = patient.Sex;
            connector.PermanentDentistNew = patient.PreferredDoctor;
            connector.Sender = connector.CreateSenderAddress(Environment.MachineName, "TidyClinic");
            connector.Receiver = connector.CreateReceiverAddress(Environment.MachineName, "PDATA");
            connector.SendData(filename, SidexisConnectorModel.SlidaTokens.U);
        }
        
        private static void ProcessTokenA(SidexisConnectorModel connector, SidexisPatient patient, string filename)
        {
            // Open patient in Sidexis
            connector.LastName = patient.LastName;
            connector.FirstName = patient.FirstName;
            connector.DateOfBirth = patient.DateOfBirth;
            connector.ExtCardIndex = patient.Code;
            connector.StationName = Environment.MachineName;
            connector.DateOfCall = (DateTime.Now).ToString("dd.MM.yyyy");
            connector.TimeOfCall = (DateTime.Now).ToString("HH:mm:ss");
            connector.Sender = connector.CreateSenderAddress(Environment.MachineName, "TidyClinic");
            connector.Receiver = connector.CreateReceiverAddress(Environment.MachineName, "PDATA");
            connector.ImageNumber = "";
            connector.SendData(filename, SidexisConnectorModel.SlidaTokens.A);
        }

        // TBD:
        // image-related tokens
    }
}