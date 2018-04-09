////////////////////////////////////////////////////////////////////////////////
//
//    Spatial Reader Direction Example
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

                reader.DirectionReported += OnDirectionReported;

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Put the spatial reader into direction mode
                settings.SpatialConfig.Mode = SpatialMode.Direction;

                // Retrieve the DirectionConfig object stored on the reader so that we can
                // modify the settings we are interested in.
                DirectionConfig directionConfig = settings.SpatialConfig.Direction;

                // Tells the spatial reader to perform tag reads more quickly at the expense of sensitivity.
                directionConfig.Mode = DirectionMode.HighSensitivity;

                // Enable the sectors you want to track tags in here. Note that you may only enable
                // non-adjacent sectors (e.g. 2 and 4, but not 2 and 3). Further note that sectors 2
                // and 9 are also considered adjacent.
                List<ushort> enabledSectorIDs = new List<ushort> {2, 4, 6};
                // xSpans can only enable sectors 2 and 3
                if (reader.IsXSpan) enabledSectorIDs = new List<ushort> {2, 3};
                directionConfig.EnabledSectorIDs = enabledSectorIDs;

                // Enable any reports you are interested in here. Entry reports are generated when
                // a tag is first read.  Updates are sent every "update interval" seconds indicating
                // that a tag is still visible to the reader. Exit reports are sent when a tag that
                // was seen previously, has not been read for "tag age interval" seconds. Both
                // "update interval" and "tag age interval" are set below to two and four seconds
                // respectively.
                directionConfig.EntryReportEnabled = true;
                directionConfig.UpdateReportEnabled = true;
                directionConfig.ExitReportEnabled = true;

                // Set this to true if the maximum transmit power is desired, false if a custom value is desired
                directionConfig.MaxTxPower = false;

                // If MaxTxPower is set to false, then a custom power can be used. Provide a power in .25 dBm increments
                directionConfig.TxPowerInDbm = 25.25;

                // Tells the spatial reader we want to track tags in as wide of an area as possible,
                // though a NARROW field of view is also available.
                directionConfig.FieldOfView = DirectionFieldOfView.Narrow;

                // Sets our application to only receive tag updates (or heartbeats) every two seconds.
                directionConfig.UpdateIntervalSeconds = 2;

                // Sets our application to only receive a tag's exit report after it has not been read
                // in any sector for four seconds.
                directionConfig.TagAgeIntervalSeconds = 4;

                // Sets a user limit on the tag population
                directionConfig.TagPopulationLimit = 20;

                // Define this is you want to filter tags
#if USE_FILTERING  
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
#endif

                // Pushes our specified configuration to the reader. If the set of enabled sectors violates the rules specified above,
                // an OctaneSDKException will be thrown here.
                reader.ApplySettings(settings);

                // Initiates our application and we should start to receive direction reports.
                reader.Start();

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // The application will terminate when the "Enter" key is pressed.
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

        // This event handler will be called when a direction report is ready.
        static void OnDirectionReported(ImpinjReader reader, DirectionReport report)
        {
            // Print out the report details
            Console.WriteLine("Direction report");
            Console.WriteLine("   Type = {0}", report.ReportType);
            Console.WriteLine("   EPC = {0}", report.Epc);
            Console.WriteLine("   Last Read Sector = {0}", report.LastReadSector);
            Console.WriteLine("   Last Read Timestamp = {0}", report.LastReadTimestamp);
            Console.WriteLine("   First Seen Sector = {0}", report.FirstSeenSector);
            Console.WriteLine("   First Seen Timestamp = {0}", report.FirstSeenTimestamp);
            Console.WriteLine("   Tag Population Status = {0}", report.TagPopulationStatus);
        }
    }
}