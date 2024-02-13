using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SidexisConnector
{
    public static class SlidaInterface
    {
        public static byte[] GenerateMessage(char tokenType, List<string> tokenInfo)
        {
            // Determine length of mailslot message
            int messageLen = 6; // Header and return statement
            foreach (string info in tokenInfo)
            {
                messageLen += (Encoding.UTF8.GetByteCount(info) + 1);
            }
            byte[] message = new byte[messageLen];
        
            // Write message length into the message: binary value, Intel notation
            message[0] = (byte) messageLen;
            message[1] = (byte) (messageLen >> 8);
        
            // Write token type into the message
            message[2] = (byte) tokenType;
            message[3] = 0;
        
            // Write token data fields into the message
            var index = 4;
            foreach (string info in tokenInfo)
            {
                var data = Encoding.UTF8.GetBytes(info);
                data.CopyTo(message, index);
                index += data.Length;
                message[index++] = 0;
            }
        
            // Write return statement into the message
            message[index++] = (byte) '\r';
            message[index] = (byte) '\n';

            return message;
        }

        public static void SendMessage(string filename, byte[] message, string errorFilePath)
        {
            // Write message to .sdx file
            try
            {
                using (var fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    fs.Seek(0, SeekOrigin.End);
                    fs.Write(message, 0, message.Length);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = File.AppendText(errorFilePath))
                {
                    // Write timestamp and error message to the file
                    writer.WriteLine($"{DateTime.Now}: {ex.GetType().Name} - {ex.Message}");
                }
            }
        }
    }
}