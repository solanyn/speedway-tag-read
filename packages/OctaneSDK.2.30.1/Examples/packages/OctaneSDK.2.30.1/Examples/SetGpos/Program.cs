////////////////////////////////////////////////////////////////////////////////
//
//    Set GPO
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using Impinj.OctaneSdk;

namespace OctaneSdkExamples
{
    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();

        static void Main(string[] args)
        {
            ushort i;

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

                // Configure the reader with the default settings.
                reader.ApplyDefaultSettings();

                // Turn the general purpose outputs 
                // (GPOs) on one at a a time
                Console.WriteLine("Setting general purpose outputs...");
                for (i = 1; i <= 4; i++)
                {
                    reader.SetGpo(i, true);
                    Thread.Sleep(1500);
                    reader.SetGpo(i, false);
                }

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
