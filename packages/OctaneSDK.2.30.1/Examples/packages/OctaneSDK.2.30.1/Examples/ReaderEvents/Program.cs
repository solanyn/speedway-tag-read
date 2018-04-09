////////////////////////////////////////////////////////////////////////////////
//
//    Reader Events
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

                // Enable all of the antenna ports.
                settings.Antennas.EnableAll();

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Assign handlers for various reader events.
                reader.GpiChanged += OnGpiEvent;
                reader.AntennaChanged += OnAntennaEvent;
                reader.ReaderStarted += OnReaderStarted;
                reader.ReaderStopped += OnReaderStopped;

                // Start the reader (required for antenna events).
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

        // This event handler gets called when the reader is started.
        static void OnReaderStarted(ImpinjReader reader, ReaderStartedEvent e)
        {
            Console.WriteLine("Reader started : {0}", reader.Address);
        }

        // This event handler gets called when the reader is stopped.
        static void OnReaderStopped(ImpinjReader reader, ReaderStoppedEvent e)
        {
            Console.WriteLine("Reader stopped : {0}", reader.Address);
        }

        // This event handler gets called when a GPI event occurs.
        static void OnGpiEvent(ImpinjReader sender, GpiEvent e)
        {
            Console.WriteLine("A GPI event occurred.");
            Console.WriteLine("Port : {0} State : {1}\n",
                e.PortNumber, e.State);
        }

        // This event handler gets called when an antenna event occurs.
        static void OnAntennaEvent(ImpinjReader sender, AntennaEvent e)
        {
            Console.WriteLine("An antenna event occurred.");
            Console.WriteLine("Port : {0} State : {1}\n",
                e.PortNumber, e.State);
        }
    }
}
