////////////////////////////////////////////////////////////////////////////////
//
//    Tx Power Ramp
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;
using System.Threading;

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

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;
                
                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Tell the reader to include the Peak RSSI
                // in all tag reports. Other fields can be added
                // to the reports in the same way by setting the 
                // appropriate Report.IncludeXXXXXXX property.
                settings.Report.IncludePeakRssi = true;

                // Loop through all the valid Tx settings. 
                FeatureSet features = reader.QueryFeatureSet();
                foreach (TxPowerTableEntry tx in features.TxPowers)
                {
                    // Set the transmit power (in dBm).
                    Console.WriteLine("Setting Tx Power to {0} dBm", tx.Dbm);
                    settings.Antennas.GetAntenna(1).TxPowerInDbm = tx.Dbm;

                    // Apply the new transmit power settings.
                    reader.ApplySettings(settings);
                    
                    // Start the reader.
                    reader.Start();

                    // Wait
                    Thread.Sleep(1000);

                    // Stop the reader.
                    reader.Stop();
                }

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
 
                // Disconnect from the reader.
                reader.Disconnect();
            }
            catch (OctaneSdkException e)
            {
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
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
                Console.WriteLine("EPC : {0}, Peak RSSI : {1} dBm",
                           tag.Epc, String.Format("{0:0.00}", tag.PeakRssiInDbm));
            }
        }
    }
}
