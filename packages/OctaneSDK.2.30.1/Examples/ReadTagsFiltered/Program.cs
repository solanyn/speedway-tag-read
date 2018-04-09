////////////////////////////////////////////////////////////////////////////////
//
//    Read Tags Filtered
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

                // Setup a tag filter.
                // Only the tags that match this filter will respond.
                // First, setup tag filter #1.
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

                // Next, setup tag filter #2
                // This filter will apply to the User memory bank.
                settings.Filters.TagFilter2.MemoryBank = MemoryBank.User;
                // Start matching on the third bit (bit pointer is zero-based)
                settings.Filters.TagFilter2.BitPointer = 2;
                // Only match tags that have the third bit of User memory set.
                // Mask = 0x80 (hex) = 10000000 (bin)
                settings.Filters.TagFilter2.TagMask = "80";
                // Filter is 1 bit long
                settings.Filters.TagFilter2.BitCount = 1;
                // To test this filter, write 0x2000 to 
                // the first word of User memory.
                // 0x2000 (hex) = 0010000000000000 (bin)

                // Set the filter mode.
                // Both filters must match.
                settings.Filters.Mode = TagFilterMode.Filter1AndFilter2;

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
                Console.WriteLine("EPC : {0} Antenna : {1}",
                                    tag.Epc, tag.AntennaPortNumber);
            }
        }
    }
}
