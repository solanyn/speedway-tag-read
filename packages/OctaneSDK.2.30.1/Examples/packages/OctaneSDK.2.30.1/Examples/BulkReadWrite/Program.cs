////////////////////////////////////////////////////////////////////////////////
//
//    Bulk Read and Write
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;
using System.Collections.Generic;

namespace OctaneSdkExamples
{
    class Program
    {
        // Change this constant to reflect the tag you are using.
        // Monza X-2K has 136 words of User memory.
        const ushort NUM_WORDS_USER_MEMORY = 136;
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();
        // A string to hold the tag data
        static string tagData;
        // Keep track of how many operations were added and 
        // how many have been executed, so we know when we're done.
        static ushort numOpsAdded, numOpsExecuted, numWordsWritten;
        // Used to generate random data.
        static Random rand = new Random();

        static void BulkRead(TagData accessPassword, MemoryBank bank, ushort wordPointer, ushort wordCount)
        {
            TagOpSequence seq;
            TagReadOp op;
            
            // Initialize variables
            tagData = "";
            numOpsExecuted = 0;
            numOpsAdded = 0;

            // Each TagReadOp can only access up to 32 words.
            // So, we need to break this read up into multiple operations. 
            while (wordCount > 0)
            {
                // Define a new tag operation sequence.
                seq = new TagOpSequence();

                // Define a tag read operation
                op = new TagReadOp();
                op.AccessPassword = accessPassword;
                op.MemoryBank = bank;
                op.WordPointer = wordPointer;
                op.WordCount = (wordCount < 32) ? wordCount : (ushort) 32;

                // Add the read op to the operation sequence
                seq.Ops.Add(op);
                
                // Adjust the word count and pointer for the next reader operation
                wordCount -= op.WordCount;
                wordPointer += op.WordCount;
                
                // Add the operation sequence to the reader
                reader.AddOpSequence(seq);
                numOpsAdded++;
            }
        }

        static void BulkWrite(TagData accessPassword, MemoryBank bank, ushort wordPointer, TagData data)
        {
            TagOpSequence seq;
            TagWriteOp op;

            // How many words are we going to write?
            ushort wordCount = (ushort) (data.CountBytes / 2);

            // Initialize variables
            numOpsExecuted = 0;
            numOpsAdded = 0;
            numWordsWritten = 0;

            // Each TagWriteOp can only access up to 32 words.
            // So, we need to break this write up into multiple operations. 
            while (wordCount > 0)
            {
                // Define a new tag operation sequence.
                seq = new TagOpSequence();

                // If you are using Monza 4, Monza 5 or Monza X tag chips,
                // uncomment these two lines. This enables 32-bit block writes
                // which significantly improves write performance.
                //seq.BlockWriteEnabled = true;
                //seq.BlockWriteWordCount = 2;

                // Define a tag read operation
                op = new TagWriteOp();
                op.AccessPassword = accessPassword;
                op.MemoryBank = bank;
                op.WordPointer = wordPointer;
                ushort opSizeWords = (wordCount < 32) ? wordCount : (ushort)32;
                op.Data = TagData.FromWordList(data.ToList().GetRange(wordPointer, opSizeWords));

                // Add the write op to the operation sequence
                seq.Ops.Add(op);

                // Adjust the word count and pointer for the next reader operation
                wordCount -= opSizeWords;
                wordPointer += opSizeWords;

                // Add the operation sequence to the reader
                reader.AddOpSequence(seq);
                numOpsAdded++;
            }
        }

        static TagData GetRandomData(ushort numWords)
        {
            byte[] bytes = new byte[numWords * 2];
            rand.NextBytes(bytes);
            return TagData.FromByteArray(bytes);
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

                // Assign the TagOpComplete event handler.
                // This specifies which method to call
                // when tag operations are complete.
                reader.TagOpComplete += OnTagOpComplete;

                // Apply the default settings.
                reader.ApplyDefaultSettings();

                // Start reading.
                reader.Start();


                Console.WriteLine("Writing to the tag. Press enter when the operation is complete.");
                // Write random words to user memory
                TagData data = GetRandomData(NUM_WORDS_USER_MEMORY);
                BulkWrite(null, MemoryBank.User, 0, data);
                Console.ReadLine();

                // Remove all operation sequences from the reader that haven't executed.
                reader.DeleteAllOpSequences();

                Console.WriteLine("Reading from the tag. Press enter when the operation is complete.");
                // Read all of User memory
                BulkRead(null, MemoryBank.User, 0, NUM_WORDS_USER_MEMORY);
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

        static void HandleReadOpComplete(TagReadOpResult readResult)
        {
            // A read operation has occurred. Increment the count.
            numOpsExecuted++;

            // Check the result of the read (pass / fail)
            if (readResult.Result == ReadResultStatus.Success)
            {
                tagData += readResult.Data.ToHexWordString() + " ";

                // Have all the read operations executed?
                if (numOpsExecuted == numOpsAdded)
                {
                    Console.WriteLine("\nBulk read complete. Tag data :\n\n{0}", tagData);
                }
            }
            else
            {
                Console.WriteLine("Read operation failed : {0}", readResult.Result);
                // Data for this section of memory is unknown. 
                // Add a marker in the output to show this.
                tagData += "<missing data>";
            }
        }

        static void HandleWriteOpComplete(TagWriteOpResult writeResult)
        {
            // A write operation has occurred. Increment the count.
            numOpsExecuted++;

            // Check the result of the read (pass / fail)
            if (writeResult.Result == WriteResultStatus.Success)
            {
                numWordsWritten += writeResult.NumWordsWritten;

                // Have all the write operations executed?
                if (numOpsExecuted == numOpsAdded)
                {
                    Console.WriteLine("\nBulk write complete. {0} words written.", numWordsWritten);
                }
            }
            else
            {
                Console.WriteLine("Write operation failed : {0}", writeResult.Result);
            }
        }

        // This event handler will be called when tag 
        // operations have been executed by the reader.
        static void OnTagOpComplete(ImpinjReader reader, TagOpReport report)
        {
            // Loop through all the completed tag operations
            foreach (TagOpResult result in report)
            {
                // Was this completed operation a tag read operation?
                if (result is TagReadOpResult)
                {
                    // Cast it to the correct type.
                    TagReadOpResult readResult = result as TagReadOpResult;
                    
                    // Process the read results
                    HandleReadOpComplete(readResult);
                }
                // Was it a tag write operation?
                else if (result is TagWriteOpResult)
                {
                    // Cast it to the correct type.
                    TagWriteOpResult writeResult = result as TagWriteOpResult;

                    // Process the write results
                    HandleWriteOpComplete(writeResult);
                }
            }
        }
    }
}
