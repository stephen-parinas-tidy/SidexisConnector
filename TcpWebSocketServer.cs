using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SidexisConnector
{
    /// <summary>
    /// Minimal WebSocket server implementation over a raw TCP client.
    /// </summary>
    public class TcpWebSocketServer
    {
        /// <summary>
        /// Underlying TCP client for this connection.
        /// </summary>
        private readonly TcpClient _client;
        
        /// <summary>
        /// Status message that will be sent back to the WebSocket client.
        /// This is typically set by the calling code based on whether patient processing succeeded.
        /// </summary>
        public string PatientDataStatus { get; set; }

        public TcpWebSocketServer(TcpClient client)
        {
            _client = client;
            PatientDataStatus = string.Empty;
        }
        
        /// <summary>
        /// Completes the WebSocket handshake by responding with
        /// "101 Switching Protocols" and the computed Sec-WebSocket-Accept value.
        /// </summary>
        public void HandleWebSocketHandshake(NetworkStream stream, string httpRequest)
        {
            // Extract the Sec-WebSocket-Key header from the client request.
            string swk = Regex.Match(httpRequest, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
            
            // Append the WebSocket protocol GUID and compute SHA1 hash.
            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
            string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

            // Send the HTTP upgrade response to establish the WebSocket connection.
            byte[] response = Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Connection: Upgrade\r\n" +
                "Upgrade: websocket\r\n" +
                "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");
            
            stream.Write(response, 0, response.Length);
        }

        /// <summary>
        /// Decodes an incoming WebSocket frame and extracts the UTF-8 text payload.
        /// Expected to receive masked text frames from a browser client.
        /// </summary>
        public string HandleWebSocketMessage(byte[] bytes, string filePath)
        {
            // FIN bit (currently not used for fragmentation handling).
            bool fin = (bytes[0] & 0b10000000) != 0;
            
            // All client-to-server WebSocket frames must be masked.
            bool mask = (bytes[1] & 0b10000000) != 0;
            
            // Opcode 1 indicates a text frame.
            int opcode = bytes[0] & 0b00001111;
            
            int offset = 2;
            ulong msglen = bytes[1] & (ulong)0b01111111;

            var text = "";

            if (msglen == 126)
            {
                // Extended 16-bit payload length (big-endian).
                msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                offset = 4;
            }
            else if (msglen == 127)
            {
                // Extended 64-bit payload length (big-endian).
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
                // Read the 4-byte masking key.
                byte[] decoded = new byte[msglen];
                byte[] masks = new byte[4]
                    { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                offset += 4;

                // Unmask payload (XOR with rotating mask key).
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

        /// <summary>
        /// Deserializes patient JSON and sends the required SLIDA token messages to create, update, and open the patient in Sidexis.
        /// </summary>
        public void ProcessPatientData(SidexisConnectorModel connector, string patientData, string slidaFilePath)
        {
            // Create tokens to send to Sidexis mail slot
            var patient = JsonConvert.DeserializeObject<SidexisPatient>(patientData);
            
            // Token N – create patient
            ProcessTokenN(connector, patient, slidaFilePath);
            
            // Token U – update patient
            ProcessTokenU(connector, patient, slidaFilePath);
            
            // Token A – auto-select/open patient
            ProcessTokenA(connector, patient, slidaFilePath);
        }
        
        /// <summary>
        /// Token N - create a new patient in Sidexis.
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="patient"></param>
        /// <param name="slidaFilePath"></param>
        private static void ProcessTokenN(SidexisConnectorModel connector, SidexisPatient patient, string slidaFilePath)
        {
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

        /// <summary>
        /// Token U - update existing patient data in Sidexis.
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="patient"></param>
        /// <param name="slidaFilePath"></param>
        private static void ProcessTokenU(SidexisConnectorModel connector, SidexisPatient patient, string slidaFilePath)
        {
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
        
        /// <summary>
        /// Token A - auto-select/open patient in Sidexis.
        /// </summary>
        /// <param name="connector"></param>
        /// <param name="patient"></param>
        /// <param name="slidaFilePath"></param>
        private static void ProcessTokenA(SidexisConnectorModel connector, SidexisPatient patient, string slidaFilePath)
        {
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

        /// <summary>
        /// Sends the current status message to the WebSocket client as a text frame and then closes the connection.
        /// </summary>
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
        
        /// <summary>
        /// Appends diagnostic messages to a log file.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageFilePath"></param>
        private static void LogMessageToFile(string message, string messageFilePath)
        {
            // Create or append to the file
            using var writer = File.AppendText(messageFilePath);
            
            // Write timestamp and error message to the file
            writer.WriteLine($"{DateTime.Now}: {message}");
        }
    }
    
    /// <summary>
    /// Data model representing patient information received from the WebSocket client.
    /// </summary>
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