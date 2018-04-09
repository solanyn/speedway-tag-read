////////////////////////////////////////////////////////////////////////////////
//
//    Spatial Reader Location Example
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;
using System.Collections.Generic;

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

                // Assign the LocationReported event handler.
                // This specifies which method to call
                // when a location report is available.
                reader.LocationReported += OnLocationReported;

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Put the spatial reader into location mode
                settings.SpatialConfig.Mode = SpatialMode.Location;

                // Enable all three report types
                settings.SpatialConfig.Location.EntryReportEnabled = true;
                settings.SpatialConfig.Location.UpdateReportEnabled = true;
                settings.SpatialConfig.Location.ExitReportEnabled = true;

                // Set spatial reader placement parameters

                // The mounting height of the spatial reader, in centimeters
                settings.SpatialConfig.Placement.HeightCm = 457;
                // These settings aren't required in a single spatial reader environment
                // They can be set to zero (which is the default)
                settings.SpatialConfig.Placement.FacilityXLocationCm = 0;
                settings.SpatialConfig.Placement.FacilityYLocationCm = 0;
                settings.SpatialConfig.Placement.OrientationDegrees = 0;

                // Set spatial reader location parameters
                settings.SpatialConfig.Location.ComputeWindowSeconds = 10;
                settings.ReaderMode = ReaderMode.AutoSetDenseReader;
                settings.Session = 2;
                settings.SpatialConfig.Location.TagAgeIntervalSeconds = 20;

                // Specify how often we want to receive location reports
                settings.SpatialConfig.Location.UpdateIntervalSeconds = 5;

                // Set this to true if the maximum transmit power is desired, false if a custom value is desired
                settings.SpatialConfig.Location.MaxTxPower = false;

                // If MaxTxPower is set to false, then a custom power can be used. Provide a power in .25 dBm increments
                settings.SpatialConfig.Location.TxPowerInDbm = 25.25;

                // Disable antennas targeting areas from which we may not want location reports,
                // in this case we're disabling antennas 10 and 15
                List<ushort> disabledAntennas = new List<ushort> { 10, 15 };
                settings.SpatialConfig.Location.DisabledAntennaList = disabledAntennas;

                // Uncomment this is you want to filter tags
                /*
                // Setup a tag filter.
                // Only the tags that match this filter will respond.
                // We want to apply the filter to the EPC memory bank.
                settings.Filters.TagFilter1.MemoryBank = MemoryBank.Epc;
                // Start matching at the third word (bit 32), since the 
                // first two words of the EPC memory bank are the
                // CRC and control bits. BitPointers.Epc is a helper
                // enumeration you can use, so you don't have to remember this.
                settings.Filters.TagFilter1.BitPointer = BitPointers.Epc;
                // Only match tags with EPCs that start with "3008"
                settings.Filters.TagFilter1.TagMask = "3008";
                // This filter is 16 bits long (one word).
                settings.Filters.TagFilter1.BitCount = 16;

                // Set the filter mode. Use only the first filter
                settings.Filters.Mode = TagFilterMode.OnlyFilter1;
                */

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Start the reader
                reader.Start();

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Apply the default settings before exiting.
                reader.ApplyDefaultSettings();

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

        // This event handler will be called when a location report is ready.
        static void OnLocationReported(ImpinjReader reader, LocationReport report)
        {
            // Print out the report details
            Console.WriteLine("Location report");
            Console.WriteLine("   Type = {0}", report.ReportType);
            Console.WriteLine("   EPC = {0}", report.Epc);
            Console.WriteLine("   X = {0} cm", report.LocationXCm);
            Console.WriteLine("   Y = {0} cm", report.LocationYCm);
            Console.WriteLine("   Timestamp = {0} ({1})", report.Timestamp, report.Timestamp.LocalDateTime);
            Console.WriteLine("   Read count = {0}", report.ConfidenceFactors.ReadCount);
        }
    }
}
