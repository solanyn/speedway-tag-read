////////////////////////////////////////////////////////////////////////////////
//
//    Query Reader Settings
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Reflection;
using Impinj.OctaneSdk;

namespace OctaneSdkExamples
{
    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();

        static void DisplayCurrentSettings()
        {
            // Query the current reader settings and print the results.
            Console.WriteLine("Reader Settings");
            Console.WriteLine("---------------");

            Settings settings = reader.QuerySettings();
            Console.WriteLine("Reader mode : {0}", settings.ReaderMode);
            Console.WriteLine("Search mode : {0}", settings.SearchMode);
            Console.WriteLine("Session : {0}", settings.Session);

            if (settings.Antennas.GetAntenna(1).MaxRxSensitivity)
            {
                Console.WriteLine("Rx sensitivity (Antenna 1) : Max");
            }
            else
            {
                Console.WriteLine("Rx sensitivity (Antenna 1) : {0} dBm",
                    settings.Antennas.GetAntenna(1).RxSensitivityInDbm);
            }

            if (settings.Antennas.GetAntenna(1).MaxTxPower)
            {
                Console.WriteLine("Tx power (Antenna 1) : Max");
            }
            else
            {
                Console.WriteLine("Tx power (Antenna 1) : {0} dBm",
                    settings.Antennas.GetAntenna(1).TxPowerInDbm);
            }

            Console.WriteLine("");
        }

        static void Main(string[] args)
        {
            try
            {
                // Connect to the reader.
                // Pass in a reader hostname or IP address as a 
                // command line argument when running the example
                if (args.Length != 1)
                {
                    Console.WriteLine("Error: No hostname specified.  Pass in the reader hostname as a command line argument when running the Sdk Example.");
                    return;
                }
                string hostname = args[0];
                reader.Connect(hostname);

                string executingPath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, ".."));

                // Query the reader features and print the results.
                Console.WriteLine("Reader Features");
                Console.WriteLine("---------------");
                FeatureSet features = reader.QueryFeatureSet();
                Console.WriteLine("Model name : {0}", features.ModelName);
                Console.WriteLine("Model number : {0}", features.ModelNumber);
                Console.WriteLine("Reader model : {0}", features.ReaderModel.ToString());
                Console.WriteLine("Firmware version : {0}", features.FirmwareVersion);
                Console.WriteLine("Antenna count : {0}\n", features.AntennaCount);

                // Write the reader features to file.
                string featuresFile = Path.Combine(executingPath, "features.xml");
                features.Save(featuresFile);

                // Query the current reader status.
                Console.WriteLine("Reader Status");
                Console.WriteLine("---------------");
                Status status = reader.QueryStatus();
                Console.WriteLine("Is connected : {0}", status.IsConnected);
                Console.WriteLine("Is singulating : {0}", status.IsSingulating);
                Console.WriteLine("Temperature : {0}° C\n", status.TemperatureInCelsius);

                // Configure the reader with the default settings.
                reader.ApplyDefaultSettings();

                // Display the current reader settings.
                DisplayCurrentSettings();
                
                // Save the settings to file in XML format.
                Console.WriteLine("Saving settings to file.");
                Settings settings = reader.QuerySettings();
                string settingsFile = Path.Combine(executingPath, "settings.xml");
                settings.Save(settingsFile);

                // Wait here, so we can edit the 
                // settings.xml file in a text editor.
                Console.WriteLine("Edit settings.xml and press enter.");
                Console.ReadLine();

                // Load the modified settings from file.
                Console.WriteLine("Loading settings from file.");
                settings = Settings.Load(settingsFile);

                // Apply the settings we just loaded from file.
                Console.WriteLine("Applying settings from file.\n");
                reader.ApplySettings(settings);

                // Display the settings again to show the changes.
                DisplayCurrentSettings();

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

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
    }
}
