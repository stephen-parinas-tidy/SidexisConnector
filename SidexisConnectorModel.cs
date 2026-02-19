using System;
using System.Collections.Generic;
using System.IO;

namespace SidexisConnector
{
    /// <summary>
    /// Represents a SLIDA token builder used to construct and send patient-related messages to Sidexis.
    /// </summary>
    public class SidexisConnectorModel
    {
        /// <summary>
        /// File path used for diagnostic/error logging.
        /// </summary>
        public string ErrorFilePath { get; set; }
        
        /// <summary>
        /// Supported SLIDA token types for this integration.
        /// </summary>
        public enum SlidaTokens
        {
            A, // Auto-select patient/image
            N, // New patient
            S, // Select patient
            U, // Update patient
        }

        #region patientIdentificationData

        // Existing patient identity fields (used for selecting/updating/opening).
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string DateOfBirth { get; set; }
        public string ExtCardIndex { get; set; }

        #endregion

        #region newPatientData

        // "New" patient fields (used when creating a patient or providing new values during updates).
        public string LastNameNew { get; set; }
        public string FirstNameNew { get; set; }
        public string DateOfBirthNew { get; set; }
        public string ExtCardIndexNew { get; set; }
        public string SexNew { get; set; }
        public string PermanentDentistNew { get; set; }

        #endregion

        #region practiceData

        // Practice and workstation context that Sidexis expects alongside patient tokens.
        public string StationName { get; set; }
        public string DateOfCall { get; set; }
        public string TimeOfCall { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }

        #endregion

        #region imageData

        // Optional image context for tokens that support it.
        public string ImageNumber { get; set; }

        #endregion

        #region dataSizeLimits

        // Field size constraints enforced before sending tokens to Sidexis.
        // These are used by the Process* methods to truncate input.
        private const int MaxSizeName = 32;
        private const int MaxSizeDate = 10;
        private const int MaxSizeTime = 8;
        private const int MaxSizeIndex = 19;
        private const int MaxSizeSex = 1;
        private const int MaxSizeDentist = 12;
        private const int MaxSizeStation = 20;
        private const int MaxSizeImageNumber = 10;

        #endregion

        public SidexisConnectorModel(string errorFilePath)
        {
            ErrorFilePath = errorFilePath;
            ClearData();    // makes all attributes empty strings
        }

        /// <summary>
        /// Builds and sends a SLIDA token message to the specified mailslot file.
        /// The data fields used depend on the provided token type.
        /// </summary>
        public void SendData(string mailslotFilename, SlidaTokens tokenType)
        {
            // Add the required data to send to Sidexis
            var tokenData = new List<string>();
            switch (tokenType)
            {
                // Select and open patient in Sidexis
                case SlidaTokens.A:
                    tokenData.Add(ProcessName(LastName));
                    tokenData.Add(ProcessName(FirstName));
                    tokenData.Add(ProcessDate(DateOfBirth));
                    tokenData.Add(ProcessIndex(ExtCardIndex));
                    tokenData.Add(ProcessStation(StationName));
                    tokenData.Add(ProcessDate(DateOfCall));
                    tokenData.Add(ProcessTime(TimeOfCall));
                    tokenData.Add(Sender);
                    tokenData.Add(Receiver);
                    tokenData.Add(ProcessImageNumber(ImageNumber));
                    break;
                
                // Create new patient
                case SlidaTokens.N:
                    tokenData.Add(ProcessName(LastNameNew));
                    tokenData.Add(ProcessName(FirstNameNew));
                    tokenData.Add(ProcessDate(DateOfBirthNew));
                    tokenData.Add(ProcessIndex(ExtCardIndexNew));
                    tokenData.Add(ProcessSex(SexNew));
                    tokenData.Add(ProcessDentist(PermanentDentistNew));
                    tokenData.Add(Sender);
                    tokenData.Add(Receiver);
                    break;

                // Select patient to open in Sidexis
                case SlidaTokens.S:
                    tokenData.Add(ProcessName(LastName));
                    tokenData.Add(ProcessName(FirstName));
                    tokenData.Add(ProcessDate(DateOfBirth));
                    tokenData.Add(ProcessIndex(ExtCardIndex));
                    tokenData.Add(ProcessStation(StationName));
                    tokenData.Add(ProcessDate(DateOfCall));
                    tokenData.Add(ProcessTime(TimeOfCall));
                    tokenData.Add(Sender);
                    tokenData.Add(Receiver);
                    break;

                // Update patient data
                case SlidaTokens.U:
                    if (DateOfBirthNew != DateOfBirth && (LastNameNew != LastName || FirstNameNew != FirstName))
                    {
                        LogExceptionToFile("Could not update patient data: DateOfBirth and Name updated simultaneously");
                    }
                    else
                    {
                        tokenData.Add(ProcessName(LastName));
                        tokenData.Add(ProcessName(FirstName));
                        tokenData.Add(ProcessDate(DateOfBirth));
                        tokenData.Add(ProcessIndex(ExtCardIndex));
                        tokenData.Add(ProcessName(LastNameNew));
                        tokenData.Add(ProcessName(FirstNameNew));
                        tokenData.Add(ProcessDate(DateOfBirthNew));
                        tokenData.Add(ProcessIndex(ExtCardIndexNew));
                        tokenData.Add(ProcessSex(SexNew));
                        tokenData.Add(ProcessDentist(PermanentDentistNew));
                        tokenData.Add(Sender);
                        tokenData.Add(Receiver);
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null);
            }

            // Generate message and send it to the mailslot
            if (tokenData.Count <= 0) return;
            var mailslotMessage = SlidaInterface.GenerateMessage(tokenType.ToString()[0], tokenData);
            SlidaInterface.SendMessage(mailslotFilename, mailslotMessage, ErrorFilePath);
            
            using (StreamWriter writer = File.AppendText(ErrorFilePath))
            {
                // Write timestamp and error message to the file
                writer.WriteLine($"{DateTime.Now}: Token {tokenType.ToString()[0]}: {tokenData[3]} {tokenData[1]} {tokenData[0]} {tokenData[2]}");
            }
            
            ClearData();
        }

        /// <summary>
        /// Resets all data fields to empty strings. Called after sending a token message.
        /// </summary>
        private void ClearData()
        {
            LastName = string.Empty;
            FirstName = string.Empty;
            DateOfBirth = string.Empty;
            ExtCardIndex = string.Empty;

            LastNameNew = string.Empty;
            FirstNameNew = string.Empty;
            DateOfBirthNew = string.Empty;
            ExtCardIndexNew = string.Empty;
            SexNew = string.Empty;
            PermanentDentistNew = string.Empty;

            StationName = string.Empty;
            DateOfCall = string.Empty;
            TimeOfCall = string.Empty;
            Sender = string.Empty;
            Receiver = string.Empty;

            ImageNumber = string.Empty;
        }
        
        /// <summary>
        /// Appends an exception message to the configured error log file.
        /// </summary>
        private void LogExceptionToFile(string message)
        {
            // Create or append to the file
            using (StreamWriter writer = File.AppendText(ErrorFilePath))
            {
                // Write timestamp and error message to the file
                writer.WriteLine($"{DateTime.Now}: {message}");
            }
        }
        
        # region processPatientData
        
        /// <summary>
        /// Applies name formatting rules and length constraints.
        /// </summary>
        private static string ProcessName(string text)
        {
            // Cannot have '@' symbol or leading/trailing spaces
            var textNew = text.Length > MaxSizeName ? text.Substring(0, MaxSizeName) : text;
            return textNew.TrimStart().TrimEnd().Replace("@", "");
        }

        /// <summary>
        /// Applies date field length constraints.
        /// </summary>
        private static string ProcessDate(string text)
        {
            return text.Length > MaxSizeDate ? text.Substring(0, MaxSizeDate) : text;
        }

        /// <summary>
        /// Applies time field length constraints.
        /// </summary>
        private static string ProcessTime(string text)
        {
            return text.Length > MaxSizeTime ? text.Substring(0, MaxSizeTime) : text;
        }

        /// <summary>
        /// Normalizes and validates the external patient index. Generates one if not provided.
        /// </summary>
        private string ProcessIndex(string text)
        {
            var textNew = text.Length > MaxSizeIndex
                ? text.Substring(0, MaxSizeIndex).ToUpper().TrimEnd()
                : text.ToUpper().TrimEnd();
            return textNew == "" ? CreateExtCardIndexNo() : textNew;
        }

        /// <summary>
        /// Applies sex field constraints.
        /// </summary>
        private static string ProcessSex(string text)
        {
            return text.Length > MaxSizeSex ? text.Substring(0, MaxSizeSex) : text;
        }

        /// <summary>
        /// Applies dentist field constraints.
        /// </summary>
        private static string ProcessDentist(string text)
        {
            return text.Length > MaxSizeDentist ? text.Substring(0, MaxSizeDentist) : text;
        }

        /// <summary>
        /// Applies station field constraints.
        /// </summary>
        private static string ProcessStation(string text)
        {
            return text.Length > MaxSizeStation ? text.Substring(0, MaxSizeStation) : text;
        }

        /// <summary>
        /// Applies image number field constraints.
        /// </summary>
        private static string ProcessImageNumber(string text)
        {
            return text.Length > MaxSizeStation ? text.Substring(0, MaxSizeImageNumber) : text;
        }

        /// <summary>
        /// Generates an external card index using patient identity fields if none is provided.
        /// </summary>
        private string CreateExtCardIndexNo()
        {
            // If external card index not provided,
            // create one from the provided last name, first name and date of birth
            return LastName == string.Empty
                ? $"{ProcessName(LastNameNew)}{ProcessName(FirstNameNew)}{ProcessName(DateOfBirthNew)}"
                : $"{ProcessName(LastName)}{ProcessName(FirstName)}{ProcessName(DateOfBirth)}";
        }

        /// <summary>
        /// Builds a SLIDA sender address in UNC-style format.
        /// </summary>
        public string CreateSenderAddress(string stationName, string appName)
        {
            // Neither field can be a wildcard '*'
            if (stationName == "*" || appName == "*")
            {
                throw new ArgumentException("Sender stationName or appName is not defined");
            }
            return $"\\\\{stationName}\\{appName}";
        }
        
        /// <summary>
        /// Builds a SLIDA receiver address in UNC-style format.
        /// </summary>
        public string CreateReceiverAddress(string stationName, string appName)
        {
            // Only up to one field can be a wildcard '*'
            if (stationName == "*" && appName == "*")
            {
                throw new ArgumentException("Receiver stationName and appName are not defined");
            }
            return $"\\\\{stationName}\\{appName}";
        }
        
        # endregion
    }
}