////////////////////////////////////////////////////////////////////////////////
//
//    Spatial Reader Features
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
                // This example show some of features specific to spatial readers
                // For examples of Location mode, see the projects named SpatialReaderLocation

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

                // Turn the beacon LED on for 10 seconds
                Console.WriteLine("Turning beacon on for 10 seconds");
                reader.TurnBeaconOn(10000);

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to continue.");
                Console.ReadLine();

                // Turn off the beacon off
                reader.TurnBeaconOff();

                // Query the state of the tilt sensor
                Console.WriteLine("Querying tilt sensor. Press any key to exit.");
                while (true)
                {
                    // Exit if the user presses a key
                    if (Console.KeyAvailable) break;
                    Status status = reader.QueryStatus();
                    Console.WriteLine("X = {0}, Y = {1}",
                        status.TiltSensor.XAxis, status.TiltSensor.YAxis);
                }

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
