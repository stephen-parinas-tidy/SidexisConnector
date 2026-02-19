using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SidexisConnector
{
    /// <summary>
    /// Helper methods for building and writing SLIDA-compatible messages to the Sidexis integration file.
    ///
    /// SLIDA messages here are written in a simple binary format:
    /// - 2 bytes: total message length (little-endian)
    /// - 2 bytes: token header (token type + null byte)
    /// - N bytes: token fields, UTF-8, null-terminated
    /// - 2 bytes: CRLF terminator
    /// </summary>
    public static class SlidaInterface
    {
        /// <summary>
        /// Generates a single SLIDA message payload for the given token type and token fields.
        /// </summary>
        public static byte[] GenerateMessage(char tokenType, List<string> tokenInfo)
        {
            // Determine length of mailslot message.
            
            // Base size:
            // - 2 bytes length
            // - 2 bytes token header (type + 0)
            // - 2 bytes CRLF terminator
            int messageLen = 6; 
            
            // Each field is UTF-8 bytes plus a null terminator byte.
            foreach (string info in tokenInfo)
            {
                messageLen += (Encoding.UTF8.GetByteCount(info) + 1);
            }
            byte[] message = new byte[messageLen];
        
            // Write message length into the message: binary value, Intel notation
            message[0] = (byte) messageLen;
            message[1] = (byte) (messageLen >> 8);
        
            // Write token type into the message
            // Second byte is a null separator as expected by the SLIDA token format used here.
            message[2] = (byte) tokenType;
            message[3] = 0;
        
            // Write token data fields into the message
            var index = 4;
            foreach (string info in tokenInfo)
            {
                var data = Encoding.UTF8.GetBytes(info);
                data.CopyTo(message, index);
                index += data.Length;
                
                // Null terminator between fields.
                message[index++] = 0;
            }
        
            // Write return statement into the message
            message[index++] = (byte) '\r';
            message[index] = (byte) '\n';

            return message;
        }

        /// <summary>
        /// Appends a SLIDA message payload to the configured Sidexis mailslot file.
        /// This is the mechanism used to deliver token messages to Sidexis in this integration.
        /// </summary>
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