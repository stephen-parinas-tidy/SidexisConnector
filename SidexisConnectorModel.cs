using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SidexisConnector
{
    public class SidexisConnectorModel
    {
        public enum SlidaTokens
        {
            A, // Auto-select patient/image
            N, // New patient
            S, // Select patient
            U, // Update patient

            // Image-related tokens to be added later on
        }

        #region patientIdentificationData

        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string DateOfBirth { get; set; }
        public string ExtCardIndex { get; set; }

        #endregion

        #region newPatientData

        public string LastNameNew { get; set; }
        public string FirstNameNew { get; set; }
        public string DateOfBirthNew { get; set; }
        public string ExtCardIndexNew { get; set; }
        public string SexNew { get; set; }
        public string PermanentDentistNew { get; set; }

        #endregion

        #region practiceData

        public string StationName { get; set; }
        public string DateOfCall { get; set; }
        public string TimeOfCall { get; set; }
        public string Sender { get; set; }
        public string Receiver { get; set; }

        #endregion

        #region imageData

        public string ImageNumber { get; set; }

        #endregion

        #region dataSizeLimits

        private const int MaxSizeName = 32;
        private const int MaxSizeDate = 10;
        private const int MaxSizeTime = 8;
        private const int MaxSizeIndex = 19;
        private const int MaxSizeSex = 1;
        private const int MaxSizeDentist = 12;
        private const int MaxSizeStation = 20;
        private const int MaxSizeImageNumber = 10;

        #endregion

        public SidexisConnectorModel()
        {
            ClearData();    // makes all attributes empty strings
        }

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
                        Console.WriteLine("Cannot update patient data: DateOfBirth and Name updated simultaneously");
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
            SlidaInterface.SendMessage(mailslotFilename, mailslotMessage);
            ClearData();
        }

        public void ClearData()
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

        private string ProcessName(string text)
        {
            // Cannot have '@' symbol or leading/trailing spaces
            var textNew = text.Length > MaxSizeName ? text.Substring(0, MaxSizeName) : text;
            return textNew.TrimStart().TrimEnd().Replace("@", "");
        }

        private string ProcessDate(string text)
        {
            return text.Length > MaxSizeDate ? text.Substring(0, MaxSizeDate) : text;
        }

        private string ProcessTime(string text)
        {
            return text.Length > MaxSizeTime ? text.Substring(0, MaxSizeTime) : text;
        }

        private string ProcessIndex(string text)
        {
            var textNew = text.Length > MaxSizeIndex
                ? text.Substring(0, MaxSizeIndex).ToUpper().TrimEnd()
                : text.ToUpper().TrimEnd();
            return textNew == "" ? CreateExtCardIndexNo() : textNew;
        }

        private string ProcessSex(string text)
        {
            return text.Length > MaxSizeSex ? text.Substring(0, MaxSizeSex) : text;
        }

        private string ProcessDentist(string text)
        {
            return text.Length > MaxSizeDentist ? text.Substring(0, MaxSizeDentist) : text;
        }

        private string ProcessStation(string text)
        {
            return text.Length > MaxSizeStation ? text.Substring(0, MaxSizeStation) : text;
        }

        private string ProcessImageNumber(string text)
        {
            return text.Length > MaxSizeStation ? text.Substring(0, MaxSizeImageNumber) : text;
        }

        private string CreateExtCardIndexNo()
        {
            // If external card index not provided,
            // create one from the provided last name, first name and date of birth
            return LastName == string.Empty
                ? $"{ProcessName(LastNameNew)}{ProcessName(FirstNameNew)}{ProcessName(DateOfBirthNew)}"
                : $"{ProcessName(LastName)}{ProcessName(FirstName)}{ProcessName(DateOfBirth)}";
        }

        public string CreateSenderAddress(string stationName, string appName)
        {
            // Neither field can be a wildcard '*'
            if (stationName == "*" || appName == "*")
            {
                throw new ArgumentException("Sender stationName or appName is not defined");
            }
            return $"\\\\{stationName}\\{appName}";
        }
        
        public string CreateReceiverAddress(string stationName, string appName)
        {
            // Only up to one field can be a wildcard '*'
            if (stationName == "*" && appName == "*")
            {
                throw new ArgumentException("Receiver stationName and appName are not defined");
            }
            return $"\\\\{stationName}\\{appName}";
        }
    }
}