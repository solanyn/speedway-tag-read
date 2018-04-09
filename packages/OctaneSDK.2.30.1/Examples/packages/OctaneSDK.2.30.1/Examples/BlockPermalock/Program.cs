////////////////////////////////////////////////////////////////////////////////
//
//    Block Permalock
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
        // Change this to the EPC (or partial EPC) of the target tag.
        const string TARGET_EPC = "F00DC018DCDF";

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

                // Specify a target tag.
                // This is very important, since a block permalock
                // operation is irreversible.
                // The target tag is selected by EPC.
                seq.TargetTag.MemoryBank = MemoryBank.Epc;
                seq.TargetTag.BitPointer = BitPointers.Epc;
                // The EPC of the target tag.
                seq.TargetTag.Data = TARGET_EPC;

                
                // Define a Block Permalock operation.
                TagBlockPermalockOp blockLockOp = new TagBlockPermalockOp();
                // Define which block(s) to lock.
                // A BlockPermalockMask can be created from a single 
                // block number or an array of block numbers.
                // This mask permalocks block zero.
                blockLockOp.BlockMask = BlockPermalockMask.FromBlockNumber(0);
                // Add the block permalock operation to the tag operation sequence.
                seq.Ops.Add(blockLockOp);

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
                // Is this the result of a block permalock operation?
                if (result is TagBlockPermalockOpResult)
                {
                    // Print the results.
                    TagBlockPermalockOpResult bplResult = result as TagBlockPermalockOpResult;
                    Console.WriteLine("Block Permalock Complete ({0}) {1}", bplResult.Tag.Epc, bplResult.Result);
                }
            }
        }
    }
}
