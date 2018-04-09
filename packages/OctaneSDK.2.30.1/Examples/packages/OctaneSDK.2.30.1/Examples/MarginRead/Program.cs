////////////////////////////////////////////////////////////////////////////////
//
//    Margin Read
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
        const string TARGET_EPC = "E280";

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
                // The target tag is selected by EPC.
                seq.TargetTag.MemoryBank = MemoryBank.Epc;
                seq.TargetTag.BitPointer = BitPointers.Epc;
                // The EPC of the target tag.
                seq.TargetTag.Data = TARGET_EPC;

                // Define a Margin Read operation.
                TagMarginReadOp marginReadOp = new TagMarginReadOp();

                // Define the mask to margin read.
                // A MarginReadMask can be created from a hexadecimal string or a bit string.
                // This mask is margin reading 1160.
                marginReadOp.MarginMask = new MarginReadMask();
                marginReadOp.MarginMask.SetMaskFromHexString("1160");
                //marginReadOp.MarginMask.SetMaskFromBitString("0001000101100000");

                // Define the bit pointer (or "place to start looking") and the memory bank.
                // We're adding 16 to get to the second word of the EPC. Our TargetTag filter
                // already ensures the EPC starts with "E280"
                marginReadOp.BitPointer = BitPointers.Epc + 16;
                marginReadOp.MemoryBank = MemoryBank.Epc;

                // Add the margin operation to the tag operation sequence.
                seq.Ops.Add(marginReadOp);

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
                // Is this the result of a margin read operation?
                if (result is TagMarginReadOpResult)
                {
                    // Print the results.
                    TagMarginReadOpResult mrResult = result as TagMarginReadOpResult;
                    Console.WriteLine("Margin Read Complete ({0}) {1}", mrResult.Tag.Epc, mrResult.Result);
                }
            }
        }
    }
}
