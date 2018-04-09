////////////////////////////////////////////////////////////////////////////////
//
//    Query Status
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

                // Get the reader status.
                Status status = reader.QueryStatus();

                // Antenna status
                foreach (AntennaStatus antStatus in status.Antennas)
                {
                    Console.WriteLine("Antenna : {0} IsConnected : {1}", 
                        antStatus.PortNumber, antStatus.IsConnected);
                }

                // GPI status
                foreach (GpiStatus gpiStatus in status.Gpis)
                {
                    Console.WriteLine("GPI : {0} State : {1}", gpiStatus.PortNumber, gpiStatus.State);
                }

                // Antenna Hub status
                foreach (AntennaHubStatus hubStatus in status.AntennaHubs)
                {
                    Console.WriteLine("Antenna Hub : {0} Connected : {1} Fault : {2}", 
                        hubStatus.HubId, hubStatus.Connected, hubStatus.Fault);
                }

                // Get the reader temperature
                Console.WriteLine("Reader temperature : {0}° C", status.TemperatureInCelsius);

                // Is the reader running?
                Console.WriteLine("Is singulating : {0}", status.IsSingulating);

                // Is a client application connected to the reader?
                Console.WriteLine("Is connected : {0}", status.IsConnected);

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
