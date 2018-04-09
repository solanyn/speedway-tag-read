////////////////////////////////////////////////////////////////////////////////
//
//    Optimized Read
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
        static int opIdUser, opIdTid;
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

                // Assign the TagOpComplete event handler.
                // This specifies which method to call
                // when tag operations are complete.
                reader.TagOpComplete += OnTagOpComplete;

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Create a tag read operation for User memory.
                TagReadOp readUser = new TagReadOp();
                // Read from user memory
                readUser.MemoryBank = MemoryBank.User;
                // Read two (16-bit) words
                readUser.WordCount = 2;
                // Starting at word 0
                readUser.WordPointer = 0;

                // Create a tag read operation for TID memory.
                TagReadOp readTid = new TagReadOp();
                // Read from TID memory
                readTid.MemoryBank = MemoryBank.Tid;
                // Read two (16-bit) words
                readTid.WordCount = 2;
                // Starting at word 0
                readTid.WordPointer = 0;

                // Add these operations to the reader as Optimized Read ops.
                // Optimized Read ops apply to all tags, unlike 
                // Tag Operation Sequences, which can be applied to specific tags.
                // Speedway Revolution supports up to two Optimized Read operations.
                settings.Report.OptimizedReadOps.Add(readUser);
                settings.Report.OptimizedReadOps.Add(readTid);

                // Store the operation IDs for later.
                opIdUser = readUser.Id;
                opIdTid = readTid.Id;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

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

        // This event handler will be called when tag 
        // operations have been executed by the reader.
        static void OnTagOpComplete(ImpinjReader reader, TagOpReport report)
        {
            string userData, tidData, epcData;

            userData = tidData = epcData = "";

            // Loop through all the completed tag operations
            foreach (TagOpResult result in report)
            {
                // Was this completed operation a tag read operation?
                if (result is TagReadOpResult)
                {
                    // Cast it to the correct type.
                    TagReadOpResult readResult = result as TagReadOpResult;

                    // Save the EPC
                    epcData = readResult.Tag.Epc.ToHexString();

                    // Are these the results for User memory or TID?
                    if (readResult.OpId == opIdUser)
                        userData = readResult.Data.ToHexString();
                    else if (readResult.OpId == opIdTid)
                        tidData = readResult.Data.ToHexString();
                }
            }

            // Print out the results after both Optimized Read operations have completed.
            Console.WriteLine("EPC : {0}, TID : {1}, User : {2}", epcData, tidData, userData);
        }
    }
}
