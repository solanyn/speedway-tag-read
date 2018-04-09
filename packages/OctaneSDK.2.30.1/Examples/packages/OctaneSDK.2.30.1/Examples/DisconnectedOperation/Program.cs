////////////////////////////////////////////////////////////////////////////////
//
//    Disconnected Operation
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using Impinj.OctaneSdk;

namespace OctaneSdkExamples
{
    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();

        static bool ReaderIsAvailable(string address)
        {
            // Ping the reader.
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;
            byte[] buffer = Encoding.Default.GetBytes("12345");
            PingReply reply = pingSender.Send(address, 500, buffer, options);
            if (reply.Status == IPStatus.Success)
                return true;
            else
                return false;
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

                Console.WriteLine("Connecting to the reader.");
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

                // Set the start trigger to Immediate.
                // This will allow the reader to start as soon as it boots up.
                settings.AutoStart.Mode = AutoStartMode.Immediate;

                // Tell the reader to hold all tag reports and events
                // when we disconnect from the reader.
                settings.HoldReportsOnDisconnect = true;

                // Tell the reader to include the antenna number
                // and timestamp in all tag reports. Other fields 
                // can be added to the reports in the same way by 
                // setting the appropriate Report.IncludeXXXXXXX property.
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludeFirstSeenTime = true;

                // Apply the newly modified settings.
                Console.WriteLine("Configuring the reader.");
                reader.ApplySettings(settings);

                // The current configuration will be saved to to persistent storage. 
                // The saved parameters then become the reader's power-on defaults.
                // If AutoStart mode is set to Immediate, the reader will
                // automatically start after booting up.
                Console.WriteLine("Press enter to save settings and reboot");
                Console.ReadLine();
                reader.SaveSettings();

                // Disconnect from the reader
                Console.WriteLine("Disconnecting from reader.");
                reader.Disconnect();

                // Issue an RShell command to reboot.
                // Open up an RShell connection on the reader.
                // Specify the reader address, user name, password and connection timeout.
                string reply;
                reader.RShell.OpenSecureSession(hostname, "root", "impinj", 5000);
                RShellCmdStatus status = reader.RShell.Send("reboot", out reply);
                // Close the RShell connection.
                reader.RShell.Close();

                // Check the status of the RShell command.
                if (status == RShellCmdStatus.Success)
                {
                    Console.WriteLine("Reader rebooting...\n");
                    Thread.Sleep(15000);

                    Console.Write("Waiting for reader to come back online.");
                    // Ping the reader until it's back online.
                    while (!ReaderIsAvailable(hostname))
                    {
                        Console.Write(".");
                        Thread.Sleep(1000);
                    }

                    Console.WriteLine("\nThe reader is back online. Press enter to reconnect and get tag data.\n");
                    Console.ReadLine();
                    Console.WriteLine("Reconnecting to reader.");

                    // Reconnect to the reader.
                    reader.Connect(hostname);

                    // Enable tag reports and events.
                    reader.ResumeEventsAndReports();
                }
                else
                {
                    // Error executing RShell command. Print out reply.
                    Console.WriteLine("RShell command failed to execute.\n");
                    Console.WriteLine("RShell command reply : \n\n" + reply + "\n");
                }

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Apply the default settings.
                // This removes the saved settings.
                reader.ApplyDefaultSettings();

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
                Console.WriteLine("Antenna : {0} EPC : {1} Timestamp : {2}",
                                     tag.AntennaPortNumber, tag.Epc, tag.FirstSeenTime);
            }
        }
    }
}
