////////////////////////////////////////////////////////////////////////////////
//
//    Asynchronous Connection
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
                // Pass in a reader hostname or IP address as a 
                // command line argument when running the example
                if (args.Length != 1)
                {
                    Console.WriteLine("Error: No hostname specified.  Pass in the reader hostname as a command line argument when running the Sdk Example.");
                    return;
                }
                string hostname = args[0];
                
                // Assign the ConnectAsyncComplete event handler.
                // This specifies which method to call
                // when the connection attempt has completed.
                reader.ConnectAsyncComplete += OnConnectAsyncComplete;

                // Attempt to connect to this reader asynchronously. 
                // This method return immediately. 
                // An event will be raised when the connect attempt has succeeded or failed.
                Console.WriteLine("Attempting connection to {0}", hostname);
                reader.ConnectAsync(hostname);

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
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

        static void OnConnectAsyncComplete(ImpinjReader reader, ConnectAsyncResult result, string errorMessage)
        {
            // This event handler is called asynchronously 
            // when the connection attempt has completed.
            
            // Check the result of the connection attempt
            if (result == ConnectAsyncResult.Success)
            {
                // Successfully connection to the reader. Now configure  and start it.
                Console.WriteLine("Successfully connected to {0}", reader.Address);
                reader.ApplyDefaultSettings();
                Console.WriteLine("Starting reader...");
                reader.Start();
                Console.WriteLine("Stopping reader in 5 seconds...");
                Thread.Sleep(5000);
                reader.Stop();
                reader.Disconnect();
                Console.WriteLine("Reader stopped. Press enter to exit.");
            }
            else
            {
                // Failed to connect to the reader
                Console.WriteLine("Failure while connecting to {0} : {1}", reader.Address, errorMessage);
            }
        }
    }
}
