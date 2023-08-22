using System;
using Newtonsoft.Json;

namespace SidexisConnector
{
    public class Test
    {
        public string Filename { get; set; }

        public void TestTokenA(SidexisConnectorModel connector)
        {
            connector.LastName = "Kim";
            connector.FirstName = "Doyoung";
            connector.DateOfBirth = "01.02.1996";
            connector.ExtCardIndex = "P0201";
            connector.StationName = Environment.MachineName;
            connector.DateOfCall = (DateTime.Now).ToString("dd.MM.yyyy");
            connector.TimeOfCall = (DateTime.Now).ToString("HH:mm:ss");
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.ImageNumber = "1";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.A);
            connector.ClearData();
        }
        
        public void TestTokenA2(SidexisConnectorModel connector)
        {
            connector.LastName = "Swift";
            connector.FirstName = "Taylocal";
            connector.DateOfBirth = "13.12.1989";
            connector.ExtCardIndex = "  p1989ts   ";
            connector.StationName = Environment.MachineName;
            connector.DateOfCall = (DateTime.Now).ToString("dd.MM.yyyy");
            connector.TimeOfCall = (DateTime.Now).ToString("HH:mm:ss");
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = "\\\\{Environment.MachineName}\\SIDEXIS";
            connector.ImageNumber = "1";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.A);
            connector.ClearData();
        }

        public void TestTokenN(SidexisConnectorModel connector)
        {
            try
            {
                connector.LastNameNew = "   Swift   ";
                connector.FirstNameNew = "Tayloc@al";
                connector.DateOfBirthNew = "12.12.1989";
                connector.ExtCardIndexNew = "   ";
                connector.SexNew = "M";
                connector.PermanentDentistNew = "Junmyeon Kim";
                //connector.Sender = SidexisConnectorModel.CreateSenderAddress(Environment.MachineName, "TidyClinic");
                //connector.Receiver = SidexisConnectorModel.CreateReceiverAddress("*", "SIDEXIS");
                connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.N);
                connector.ClearData();
            }
            catch (Exception e)
            {
                Console.WriteLine($"An {e.GetType().Name} occurred: {e.Message}");
            }
        }

        public void TestTokenS(SidexisConnectorModel connector)
        {
            connector.LastName = "Oh";
            connector.FirstName = "Sehun";
            connector.DateOfBirth = "12.04.1994";
            connector.ExtCardIndex = "P0412";
            connector.StationName = Environment.MachineName;
            connector.DateOfCall = (DateTime.Now).ToString("dd.MM.yyyy");
            connector.TimeOfCall = (DateTime.Now).ToString("HH:mm:ss");
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.S);
            connector.ClearData();
        }

        public void TestTokenU(SidexisConnectorModel connector)
        {
            connector.LastName = "Kim";
            connector.FirstName = "Doyoung";
            connector.DateOfBirth = "01.02.1996";
            connector.ExtCardIndex = "P0201";
            connector.LastNameNew = "Kim";
            connector.FirstNameNew = "Dongyoung";
            connector.DateOfBirthNew = "01.02.1996";
            connector.ExtCardIndexNew = "P0201";
            connector.SexNew = "M";
            connector.PermanentDentistNew = "Lay Zhang";
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.U);
            connector.ClearData();
        }
        
        public void TestTokenU2(SidexisConnectorModel connector)
        {
            connector.LastName = "Kim";
            connector.FirstName = "Dongyoung";
            connector.DateOfBirth = "01.02.1996";
            connector.ExtCardIndex = "P0201";
            connector.LastNameNew = "Kim";
            connector.FirstNameNew = "Doyoung";
            connector.DateOfBirthNew = "02.02.1996";
            connector.ExtCardIndexNew = "P0201";
            connector.SexNew = "M";
            connector.PermanentDentistNew = "Lay Zhang";
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.U);
            connector.ClearData();
        }
        
        public void TestTokenU3(SidexisConnectorModel connector)
        {
            connector.LastName = "Kim";
            connector.FirstName = "Dongyoung";
            connector.DateOfBirth = "01.02.1996";
            connector.ExtCardIndex = "P0201";
            connector.LastNameNew = "Lee";
            connector.FirstNameNew = "Doyoung";
            connector.DateOfBirthNew = "02.02.1996";
            connector.ExtCardIndexNew = "P0201";
            connector.SexNew = "M";
            connector.PermanentDentistNew = "Lay Zhang";
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.U);
            connector.ClearData();
        }
        
        public void TestTokenU4(SidexisConnectorModel connector)
        {
            connector.LastName = "Kim";
            connector.FirstName = "Dongyoung";
            connector.DateOfBirth = "01.02.1996";
            connector.ExtCardIndex = "P0201";
            connector.LastNameNew = "Kim";
            connector.FirstNameNew = "Doyoung";
            connector.DateOfBirthNew = "01.02.1996";
            connector.ExtCardIndexNew = "P0201";
            connector.SexNew = "M";
            connector.PermanentDentistNew = "Lay Zhang";
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.U);
            connector.ClearData();
        }
        
        public void TestTokenU5(SidexisConnectorModel connector)
        {
            connector.LastName = "Kim";
            connector.FirstName = "Doyoung";
            connector.DateOfBirth = "02.02.1996";
            connector.ExtCardIndex = "P0201";
            connector.LastNameNew = "Kim";
            connector.FirstNameNew = "Doyoung";
            connector.DateOfBirthNew = "01.02.1996";
            connector.ExtCardIndexNew = "P0201";
            connector.SexNew = "M";
            connector.PermanentDentistNew = "Lay Zhang";
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.U);
            connector.ClearData();
        }
        
        public void TestTokenU6(SidexisConnectorModel connector)
        {
            connector.LastName = "Kim";
            connector.FirstName = "Jongin";
            connector.DateOfBirth = "14.01.1994";
            connector.ExtCardIndex = "P0012";
            connector.LastNameNew = "2";
            connector.FirstNameNew = "DemoX-ray";
            connector.DateOfBirthNew = "14.01.1994";
            connector.ExtCardIndexNew = "DEMO01";
            connector.SexNew = "M";
            connector.PermanentDentistNew = "Lay Zhang";
            //connector.Sender = SidexisConnectorModel.CreateSenderAddress(Environment.MachineName, "TidyClinic");
            //connector.Receiver = SidexisConnectorModel.CreateReceiverAddress(Environment.MachineName, "SIDEXIS");
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.U);
            connector.ClearData();
        }
        
        public void TestTokenU7(SidexisConnectorModel connector)
        {
            connector.LastName = "2";
            connector.FirstName = "DemoX-ray";
            connector.DateOfBirth = "14.01.1994";
            connector.ExtCardIndex = "DEMO01";
            connector.LastNameNew = "2";
            connector.FirstNameNew = "DemoX-ray";
            connector.DateOfBirthNew = "19.03.2001";
            connector.ExtCardIndexNew = "DEMO01";
            connector.SexNew = "M";
            connector.PermanentDentistNew = "Lay Zhang";
            connector.Sender = $"\\\\{Environment.MachineName}\\TidyClinic";
            connector.Receiver = $"\\\\{Environment.MachineName}\\SIDEXIS";
            connector.SendData(Filename, SidexisConnectorModel.SlidaTokens.U);
            connector.ClearData();
        }
    }
}