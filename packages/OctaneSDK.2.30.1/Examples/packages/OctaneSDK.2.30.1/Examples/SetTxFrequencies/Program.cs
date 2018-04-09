////////////////////////////////////////////////////////////////////////////////
//
//    Set Tx Frequencies
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
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

                // Get the reader features to determine if the 
                // reader supports a fixed-frequency table.
                FeatureSet features = reader.QueryFeatureSet();

                if (!features.IsHoppingRegion)
                {

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

                    // Send a tag report for every tag read.
                    settings.Report.Mode = ReportMode.Individual;

                    // Specify the transmit frequencies to use.
                    // Make sure your reader supports this and
                    // that the frequencies are valid for your region.
                    // The following example is for ETSI (Europe) 
                    // readers.
                    settings.TxFrequenciesInMhz.Add(865.7);
                    settings.TxFrequenciesInMhz.Add(866.3);
                    settings.TxFrequenciesInMhz.Add(866.9);
                    settings.TxFrequenciesInMhz.Add(867.5);

                    // Apply the newly modified settings.
                    reader.ApplySettings(settings);

                    // Assign the TagsReported event handler.
                    // This specifies which method to call
                    // when tags reports are available.
                    reader.TagsReported += OnTagsReported;

                    // Start reading.
                    reader.Start();
                }
                else
                {
                    Console.WriteLine("This reader does not allow the transmit frequencies to be specified.");
                }

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
                Console.WriteLine("EPC : {0} Antenna : {1}",
                                    tag.Epc, tag.AntennaPortNumber);
            }
        }
    }
}
