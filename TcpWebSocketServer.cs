using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SidexisConnector
{
    public class TcpWebSocketServer
    {
        private readonly TcpClient _client;
        public string PatientDataStatus { get; set; }

        public TcpWebSocketServer(TcpClient client)
        {
            _client = client;
            PatientDataStatus = string.Empty;
        }
        
        public void HandleWebSocketHandshake(NetworkStream stream, string httpRequest)
        {
            string swk = Regex.Match(httpRequest, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");
            stream.Write(response, 0, response.Length);
        }

        public string HandleWebSocketMessage(byte[] bytes, string filePath)
        {
            bool fin = (bytes[0] & 0b10000000) != 0;
            bool mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
            int opcode = bytes[0] & 0b00001111; // expecting 1 - text message
            int offset = 2;
            ulong msglen = bytes[1] & (ulong)0b01111111;

            var text = "";

            if (msglen == 126)
            {
                // Bytes are reversed because WebSocket will print them in Big-Endian, whereas
                // BitConverter will want them arranged in little-endian on windows
                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            }
            else if (msglen == 127)
            {
                // To test the below code, we need to manually buffer larger messages — since the NIC's autobuffering
                // may be too latency-friendly for this code to run (that is, we may have only some of the bytes in this
                // websocket frame available through client.Available).
                msglen = BitConverter.ToUInt64(
                    new byte[] { bytes[9], bytes[8], bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2] },
                    0);
                offset = 10;
            }

            if (msglen == 0)
            { 
                LogMessageToFile("msglen == 0", filePath);
            }
            else if (mask)
            {
                byte[] decoded = new byte[msglen];
                byte[] masks = new byte[4]
                    { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                offset += 4;

                for (ulong i = 0; i < msglen; ++i)
                    decoded[i] = (byte)(bytes[offset + Convert.ToInt32(i.ToString())] ^ masks[i % 4]);

                text = Encoding.UTF8.GetString(decoded);
            }
            else
            {
                LogMessageToFile("mask bit not set", filePath);
            }

            return text;
        }

        public void ProcessPatientData(SidexisConnectorModel connector, string patientData, string slidaFilePath)
        {
            // Create tokens to send to Sidexis mail slot
            var patient = JsonConvert.DeserializeObject<SidexisPatient>(patientData);
            ProcessTokenN(connector, patient, slidaFilePath);
            ProcessTokenU(connector, patient, slidaFilePath);
            ProcessTokenA(connector, patient, slidaFilePath);
        }
        
        private static void ProcessTokenN(SidexisConnectorModel connector, SidexisPatient patient, string slidaFilePath)
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
            connector.SendData(slidaFilePath, SidexisConnectorModel.SlidaTokens.N);
        }

        private static void ProcessTokenU(SidexisConnectorModel connector, SidexisPatient patient, string slidaFilePath)
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
            connector.SendData(slidaFilePath, SidexisConnectorModel.SlidaTokens.U);
        }
        
        private static void ProcessTokenA(SidexisConnectorModel connector, SidexisPatient patient, string slidaFilePath)
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
            connector.SendData(slidaFilePath, SidexisConnectorModel.SlidaTokens.A);
        }

        public void SendPatientStatus(NetworkStream stream)
        {
            byte[] status = Encoding.UTF8.GetBytes(PatientDataStatus);
            
            byte[] statusBytes = new byte[2 + status.Length];
            statusBytes[0] = 0b10000001; // Fin bit set, Opcode for text frame
            statusBytes[1] = (byte)status.Length;
            Array.Copy(status, 0, statusBytes, 2, status.Length);

            // Send the status to the client
            stream.Write(statusBytes, 0, statusBytes.Length);
            
            // Close the connection
            _client.Close();
        }
        
        private static void LogMessageToFile(string message, string messageFilePath)
        {
            // Create or append to the file
            using var writer = File.AppendText(messageFilePath);
            
            // Write timestamp and error message to the file
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }
    
    public class SidexisPatient
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string DateOfBirth { get; set; }
        public string Code { get; set; }
        public string Sex { get; set; }
        public string PreferredDoctor { get; set; }
    }
}