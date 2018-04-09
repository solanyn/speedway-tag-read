////////////////////////////////////////////////////////////////////////////////
//
//    Read Tags Over TLS
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;

namespace OctaneSdkExamples
{
    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();

        static void Main(string[] args)
        {
            try
            {
                // Pass in a reader hostname or IP address as a 
                // command line argument when running the example
                if (args.Length != 1)
                {
                    Console.WriteLine("Error: No hostname specified.  Pass in the reader hostname as a command line argument when running the Sdk Example.");
                    return;
                }
                string hostname = args[0];

                // To connect to a reader over TLS, the reader must first be configured to use encryption.
                // To do this, connect to the reader and enter the following rshell command :
                // 
                //     config rfid llrp inbound tcp security encrypt
                // 
                // If using the default settings, this command will both enable encryption, and change the 
                // port over which the LLRP connection will be made from 5084 to 5085. It should be noted
                // that the reader considers ports 5084 and 5085 to be special : 
                // 5084 will ONLY allow unsecure connections, and
                // 5085 will ONLY allow encrypted connections
                const bool useTLS = true;
                reader.Connect(hostname, useTLS);

                // If these default ports are not desired, the reader can be configured to use a custom port 
                // to facilitate an LLRP connection by using the following rshell command.
                // 
                //     config rfid llrp inbound tcp port <custom port>
                // 
                // Example :
                // 
                //     config rfid llrp inbound tcp port 9999
                // 
                // Then the following method can be used from the SDK to connect to the reader through the custom
                // port :
                // 
                // const int customPort = 9999;
                // reader.Connect(SolutionConstants.ReaderHostname, customPort, useTLS);

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Tell the reader to include the antenna number
                // in all tag reports. Other fields can be added
                // to the reports in the same way by setting the 
                // appropriate Report.IncludeXXXXXXX property.
                settings.Report.IncludeAntennaPortNumber = true;

                // The reader can be set into various modes in which reader
                // dynamics are optimized for specific regions and environments.
                // The following mode, AutoSetDenseReader, monitors RF noise and interference and then automatically
                // and continuously optimizes the reader’s configuration
                settings.ReaderMode = ReaderMode.AutoSetDenseReader;
                settings.SearchMode = SearchMode.DualTarget;
                settings.Session = 2;

                // Enable antenna #1. Disable all others.
                settings.Antennas.DisableAll();
                settings.Antennas.GetAntenna(1).IsEnabled = true;

                // Set the Transmit Power and 
                // Receive Sensitivity to the maximum.
                settings.Antennas.GetAntenna(1).MaxTxPower = true;
                settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;
                // You can also set them to specific values like this...
                //settings.Antennas.GetAntenna(1).TxPowerInDbm = 20;
                //settings.Antennas.GetAntenna(1).RxSensitivityInDbm = -70;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Start reading.
                reader.Start();

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Stop reading.
                reader.Stop();

                // Disconnect from the reader.
                reader.Disconnect();
            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }

        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // This event handler is called asynchronously 
            // when tag reports are available.
            // Loop through each tag in the report 
            // and print the data.
            foreach (Tag tag in report)
            {
                Console.WriteLine("Antenna : {0}, EPC : {1} ",
                                    tag.AntennaPortNumber, tag.Epc);
            }
        }
    }
}
