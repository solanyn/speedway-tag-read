////////////////////////////////////////////////////////////////////////////////
//
//    Write EPC
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

        const ushort EPC_OP_ID = 123;
        const ushort PC_BITS_OP_ID = 321;

        static Random random = new Random((int) DateTime.Now.Ticks);

        // Create a random EPC from 1 to 6 words in length.
        static string GetRandomEpc()
        {
            string epc = "";
            int numWords = random.Next(1, 7);

            for (int i = 0; i < numWords; i++)
                epc += random.Next(0, ushort.MaxValue + 1).ToString("X4");
            
            return epc;
        }

        static void ProgramEpc(string currentEpc, ushort currentPcBits, string newEpc)
        {
            // Check that the specified EPCs are a valid length
            if ((currentEpc.Length % 4 != 0) || (newEpc.Length % 4 != 0))
                throw new Exception("EPCs must be a multiple of 16 bits (4 hex chars)");

            Console.WriteLine("Adding a write operation to change the EPC from :");
            Console.WriteLine("{0} to {1}\n", currentEpc, newEpc);

            // Create a tag operation sequence.
            // You can add multiple read, write, lock, kill and QT
            // operations to this sequence.
            TagOpSequence seq = new TagOpSequence();

            // Specify a target tag based on the EPC.
            seq.TargetTag.MemoryBank = MemoryBank.Epc;
            seq.TargetTag.BitPointer = BitPointers.Epc;
            seq.TargetTag.Data = currentEpc;

            // If you are using Monza 4, Monza 5 or Monza X tag chips,
            // uncomment these two lines. This enables 32-bit block writes
            // which significantly improves write performance.
            //seq.BlockWriteEnabled = true;
            //seq.BlockWriteWordCount = 2;

            // Create a tag write operation to change the EPC.
            TagWriteOp writeEpc = new TagWriteOp();
            // Set an ID so we can tell when this operation has executed.
            writeEpc.Id = EPC_OP_ID;
            // Write to EPC memory
            writeEpc.MemoryBank = MemoryBank.Epc;
            // Specify the new EPC data
            writeEpc.Data = TagData.FromHexString(newEpc);
            // Starting writing at word 2 (word 0 = CRC, word 1 = PC bits)
            writeEpc.WordPointer = WordPointers.Epc;

            // Add this tag write op to the tag operation sequence.
            seq.Ops.Add(writeEpc);

            // Is the new EPC a different length than the current EPC?
            if (currentEpc.Length != newEpc.Length)
            {
                // We need adjust the PC bits and write them back to the 
                // tag because the length of the EPC has changed.
                
                // Adjust the PC bits (4 hex characters per word).
                ushort newEpcLenWords = (ushort)(newEpc.Length / 4);
                ushort newPcBits = PcBits.AdjustPcBits(currentPcBits, newEpcLenWords);
                
                Console.WriteLine("Adding a write operation to change the PC bits from :");
                Console.WriteLine("{0} to {1}\n", currentPcBits.ToString("X4"), newPcBits.ToString("X4"));

                TagWriteOp writePc = new TagWriteOp();
                writePc.Id = PC_BITS_OP_ID;
                // The PC bits are in the EPC memory bank.
                writePc.MemoryBank = MemoryBank.Epc;
                // Specify the data to write (the modified PC bits).
                writePc.Data = TagData.FromWord(newPcBits);
                // Start writing at the start of the PC bits.
                writePc.WordPointer = WordPointers.PcBits;

                // Add this tag write op to the tag operation sequence.
                seq.Ops.Add(writePc);
            }
        
            // Add the tag operation sequence to the reader.
            // The reader supports multiple sequences.
            reader.AddOpSequence(seq);
        }

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

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Assign the TagOpComplete event handler.
                // This specifies which method to call
                // when tag operations are complete.
                reader.TagOpComplete += OnTagOpComplete;

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Tell the reader to include the Protocol Control 
                // bits in all tag reports. We will need to modify 
                // the PC bits if we change the length of the EPC. 
                settings.Report.IncludePcBits = true;

                // Enable antenna #1. Disable all others.
                settings.Antennas.DisableAll();
                settings.Antennas.GetAntenna(1).IsEnabled = true;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

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

        // This event handler is called asynchronously 
        // when tag reports are available.
        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // We've read the tag we want write to, so
            // we're not interested in tag reports any more.
            // Unsubscribe from the event.
            reader.TagsReported -= OnTagsReported;

            // Change the EPC of the first tag we read to a random value.
            Tag tag = report.Tags[0];
            ProgramEpc(tag.Epc.ToHexString(), tag.PcBits, GetRandomEpc());
        }
        
        // This event handler will be called when tag 
        // operations have been executed by the reader.
        static void OnTagOpComplete(ImpinjReader reader, TagOpReport report)
        {
            // Loop through all the completed tag operations.
            foreach (TagOpResult result in report)
            {
                // Was this completed operation a tag write operation?
                if (result is TagWriteOpResult)
                {
                    // Cast it to the correct type.
                    TagWriteOpResult writeResult = result as TagWriteOpResult;
                    if (writeResult.OpId == EPC_OP_ID)
                        Console.WriteLine("Write to EPC complete : {0}", writeResult.Result);
                    else if (writeResult.OpId == PC_BITS_OP_ID)
                        Console.WriteLine("Write to PC bits complete : {0}", writeResult.Result);
                    
                    // Print out the number of words written
                    Console.WriteLine("Number of words written : {0}", writeResult.NumWordsWritten);
                }
            }
        }
    }
}
