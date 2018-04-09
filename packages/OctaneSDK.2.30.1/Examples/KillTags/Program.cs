////////////////////////////////////////////////////////////////////////////////
//
//    Kill Tags
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
        static TargetTag target;
        // Change this to the EPC of the target tag.
        const string TARGET_EPC = "FFFF33B2DDD906C000000000";
        // The kill password will be set to this value.
        const string KILL_PW = "11112222";
            
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
                // This is very important, since a kill operation is irreversible.
                target = new TargetTag();
                // Select a target tag based on the EPC.
                target.MemoryBank = MemoryBank.Epc;
                target.BitPointer = BitPointers.Epc;
                // The EPC of the target tag.
                target.Data = TARGET_EPC;

                // Apply the target tag to the tag operation sequence.
                seq.TargetTag = target;

                // Define a tag write operation that sets the kill password.
                // Tags cannot be killed with a zero password, so we must
                // set the password to a non-zero value first.
                TagWriteOp writeOp = new TagWriteOp();
                // Assumes that current access password is not set
                // (zero is the default)
                writeOp.AccessPassword = null;
                // The kill password is in the Reserved memory bank.
                writeOp.MemoryBank = MemoryBank.Reserved;
                // A pointer to the start of the kill password.
                writeOp.WordPointer = WordPointers.KillPassword;
                // The new kill password to write.
                writeOp.Data = TagData.FromHexString(KILL_PW);
                
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

        static void AddKillOp()
        {
            // Create a tag operation sequence.
            TagOpSequence seq = new TagOpSequence();

            // Apply the target tag to the tag operation sequence.
            seq.TargetTag = target;

            // Define a tag kill operation.
            TagKillOp killOp = new TagKillOp();

            // Specify the kill password for this tag. 
            // The kill password cannot be zero.
            killOp.KillPassword = TagData.FromHexString(KILL_PW);

            // Add this tag write op to the tag operation sequence.
            seq.Ops.Add(killOp);

            // Add the tag operation sequence to the reader.
            reader.AddOpSequence(seq);

            // Start the reader
            reader.Start();
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
                    // These are the results of settings the kill password.
                    // Cast it to the correct type.
                    TagWriteOpResult writeResult = result as TagWriteOpResult;
                    // Print out the results.
                    Console.WriteLine("Set kill password complete.");
                    Console.WriteLine("EPC : {0}", writeResult.Tag.Epc);
                    Console.WriteLine("Status : {0}", writeResult.Result);
                    Console.WriteLine("Number of words written : {0}\n", writeResult.NumWordsWritten);

                    // If we successfully set the kill password we can now 
                    // kill the chip. We must stop the reader first, to allow 
                    // the  chip to update the kill password.
                    if (writeResult.Result == WriteResultStatus.Success)
                    {
                        reader.Stop();
                        AddKillOp();
                    }
                }
                else if (result is TagKillOpResult)
                {
                    // Cast it to the correct type.
                    // These are the results of killing the tag.
                    TagKillOpResult killResult = result as TagKillOpResult;
                    // Print out the results.
                    Console.WriteLine("Kill operation complete.");
                    Console.WriteLine("EPC : {0}", killResult.Tag.Epc);
                    Console.WriteLine("Status : {0}", killResult.Result);
                }
            }
        }
    }
}
