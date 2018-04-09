////////////////////////////////////////////////////////////////////////////////
//
//    Wait for Query
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

                // Tell the reader not to send tag reports.
                // We will ask for them.
                settings.Report.Mode = ReportMode.WaitForQuery;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Assign an event handler that will
                // be called when the tag report buffer is almost full.
                reader.ReportBufferWarning += OnReportBufferWarning;

                // Assign an event handler that will
                // be called when the tag report buffer has overflowed.
                reader.ReportBufferOverflow += OnReportBufferOverflow;

                // Start reading.
                reader.Start();

                // Wait a while.
                Console.WriteLine("Waiting while the reader reads tags.");
                Thread.Sleep(5000);

                // Ask for the tag reports.
                reader.QueryTags();

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

        static void OnReportBufferOverflow(ImpinjReader reader, ReportBufferOverflowEvent e)
        {
            Console.WriteLine("The tag report buffer has overflowed!");
        }

        static void OnReportBufferWarning(ImpinjReader reader, ReportBufferWarningEvent e)
        {
            Console.WriteLine("The tag report buffer is {0}% full!", e.PercentFull);
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
