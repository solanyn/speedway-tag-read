////////////////////////////////////////////////////////////////////////////////
//
//    Write User Memory
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

                // Specify a target tag based on the EPC.
                seq.TargetTag.MemoryBank = MemoryBank.Epc;
                seq.TargetTag.BitPointer = BitPointers.Epc;
                // Setting this to null will specify any tag.
                // Replace this line with the one below it to target a particular tag.
                seq.TargetTag.Data = null;
                //seq.TargetTag.Data = "11112222333344445555666677778888";

                // If you are using Monza 4, Monza 5 or Monza X tag chips,
                // uncomment these two lines. This enables 32-bit block writes
                // which significantly improves write performance.
                //seq.BlockWriteEnabled = true;
                //seq.BlockWriteWordCount = 2;
              
                // Create a tag write operation.
                TagWriteOp writeOp = new TagWriteOp();
                // Write to user memory
                writeOp.MemoryBank = MemoryBank.User;
                // Write two (16-bit) words
                writeOp.Data = TagData.FromHexString("ABBAD00D");
                // Starting at word 0
                writeOp.WordPointer = 0;

                // Add this tag write op to the tag operation sequence.
                seq.Ops.Add(writeOp);

                // Add the tag operation sequence to the reader.
                // The reader supports multiple sequences.
                reader.AddOpSequence(seq);

                // Start the reader
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
            // Loop through all the completed tag operations
            foreach (TagOpResult result in report)
            {
                // Was this completed operation a tag write operation?
                if (result is TagWriteOpResult)
                {
                    // Cast it to the correct type.
                    TagWriteOpResult writeResult = result as TagWriteOpResult;
                    // Print out the results.
                    Console.WriteLine("Write complete.");
                    Console.WriteLine("EPC : {0}", writeResult.Tag.Epc);
                    Console.WriteLine("Status : {0}", writeResult.Result);
                    Console.WriteLine("Number of words written : {0}", writeResult.NumWordsWritten);
                }
            }
        }
    }
}
