# SidexisConnector

Sidexis Connector is a C# utility that integrates [TidyClinic](https://tidyclinic.com) with the Sidexis dental imaging application.

It enables users to launch Sidexis and open a selected patient's files directly from TidyClinic.

## Features

- Listens for patient data sent from TidyClinic via a local WebSocket connection.
- Processes and transfers patient information to Sidexis using the Sidexis mail slot interface.
- Automatically launches Sidexis and brings the application to the foreground with the selected patient's data loaded.
- Logs all actions and errors to a local file for troubleshooting.

## Setup

Setup instructions and executable files can be downloaded [here](https://siliconavenue-my.sharepoint.com/:f:/g/personal/stephen_parinas_tidyint_com/IgDITqb1ZURNQKXvXIboD-3fAfwucXW6koxSA84sbJg6Zgs?e=kbr0ko).

## How It Works

1. Open a patient in TidyClinic and click the Sidexis button in the header.
2. TidyClinic sends patient data to the Sidexis Connector over a local WebSocket.
3. The connector writes the patient data to the Sidexis mail slot file in the required format.
4. Sidexis is launched (if not already running) and brought to the foreground, displaying the selected patient's records.

## Requirements

- Windows OS
- Sidexis installed and configured
- TidyClinic integration enabled

## Logging

All actions and errors are logged to `TidySidexisConnector.txt` in the application directory.

## Troubleshooting

- Make sure Sidexis is installed and the mail slot file path is correct.
- Run the connector as administrator for initial setup.
- Check the log file for error messages.

## Developer Notes

### Build Instructions

- Target framework: .NET Framework 4.7.2.
- Restore NuGet packages before building.
- Build the solution in Release mode for deployment.

### Additional Documentation

For detailed technical documentation regarding the Sidexis system integration, please refer to the official vendor documentation [here](https://www.dentsplysirona.com/en/lp/slida-partners/member-area.html).

