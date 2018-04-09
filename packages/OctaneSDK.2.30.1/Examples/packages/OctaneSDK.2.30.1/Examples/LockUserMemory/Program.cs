////////////////////////////////////////////////////////////////////////////////
//
//    Lock User Memory
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

                // Assign the TagOpComplete event handler.
                // This specifies which method to call
                // when tag operations are complete.
                reader.TagOpComplete += OnTagOpComplete;

                // Configure the reader with the default settings.
                reader.ApplyDefaultSettings();

                // Create a tag operation sequence.
                // You can add multiple read, write, lock, kill and QT
                // operations to this sequence.
                TagOpSequence seq = new TagOpSequence();

                // Define a tag write operation that sets the access password. 
                TagWriteOp writeOp = new TagWriteOp();
                // Assumes that current access password is not set
                // (zero is the default)
                writeOp.AccessPassword = null;
                // The access password is in the Reserved memory bank.
                writeOp.MemoryBank = MemoryBank.Reserved;
                // A pointer to the start of the access password.
                writeOp.WordPointer = WordPointers.AccessPassword;
                // The new access password to write.
                writeOp.Data = TagData.FromHexString("11112222");

                // Add this tag write op to the tag operation sequence.
                seq.Ops.Add(writeOp);

                // Create a tag lock operation to lock the 
                // access password and User memory.
                TagLockOp lockOp = new TagLockOp();
                lockOp.AccessPasswordLockType = TagLockState.Lock;
                lockOp.UserLockType = TagLockState.Lock;
                
                // Add this tag lock op to the tag operation sequence.
                seq.Ops.Add(lockOp);

                // Add the tag operation sequence to the reader.
                // The reader supports multiple sequences.
                reader.AddOpSequence(seq);

                // Start the reader
                reader.Start();
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

            // Wait for the user to press enter.
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();

            // Stop reading.
            reader.Stop();

            // Disconnect from the reader.
            reader.Disconnect();
        }

        // This event handler will be called when tag 
        // operations have been executed by the reader.
        static void OnTagOpComplete(ImpinjReader reader, TagOpReport report)
        {
            // Loop through all the completed tag operations
            foreach (TagOpResult result in report)
            {
                if (result is TagWriteOpResult)
                {
                    // These are the results of settings the access password.
                    // Cast it to the correct type.
                    TagWriteOpResult writeResult = result as TagWriteOpResult;
                    // Print out the results.
                    Console.WriteLine("Set access password complete.");
                    Console.WriteLine("EPC : {0}", writeResult.Tag.Epc);
                    Console.WriteLine("Status : {0}", writeResult.Result);
                    Console.WriteLine("Number of words written : {0}", writeResult.NumWordsWritten);
                }
                else if (result is TagLockOpResult)
                {
                    // Cast it to the correct type.
                    // These are the results of locking the access password or user memory.
                    TagLockOpResult lockResult = result as TagLockOpResult;
                    // Print out the results.
                    Console.WriteLine("Lock operation complete.");
                    Console.WriteLine("EPC : {0}", lockResult.Tag.Epc);
                    Console.WriteLine("Status : {0}", lockResult.Result);
                }
            }
        }
    }
}
