////////////////////////////////////////////////////////////////////////////////
//
//    QT
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

        static void SetQtMode(QtDataProfile mode, QtAccessRange range)
        {
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

            // We will create two tag operations here.
            // One to set the QT properties (data profile, access range, persistence)
            // and another to read back the QT properties of the tag.

            // Create a new operation to set the QT properties 
            TagQtSetOp qtSetOp = new TagQtSetOp();

            // Set the distance range that the tag will be accessible over.
            qtSetOp.AccessRange = range;
            // Put the tag into Public or Private mode.
            // Public mode limits data access to the tag.
            qtSetOp.DataProfile = mode;
            // The QT mode will persist, even after the tag is no longer powered.
            qtSetOp.Persistence = QtPersistence.Permanent;

            // Add the QT operation to the tag operation sequence.
            seq.Ops.Add(qtSetOp);

            // Now, create a tag operation to read the current QT configuration.
            TagQtGetOp qtGetOp = new TagQtGetOp();

            // Add the operation to the sequence.
            seq.Ops.Add(qtGetOp);

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

                // Assign the TagOpComplete event handler.
                // This specifies which method to call
                // when tag operations are complete.
                reader.TagOpComplete += OnTagOpComplete;

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Configure the reader with the default settings.
                reader.ApplyDefaultSettings();

                // Prompt user for QT mode.
                Console.WriteLine("Select a QT mode\n");
                Console.WriteLine("1 : Public");
                Console.WriteLine("2 : Private");
                ushort choice = ushort.Parse(Console.ReadLine());

                if (choice == 1)
                {
                    // Put the tag into QT public mode.
                    Console.WriteLine("Putting the tag into QT public mode.");
                    SetQtMode(QtDataProfile.Public, QtAccessRange.ShortRange);
                }
                else if (choice == 2)
                {
                    // Put the tag into QT private mode.
                    Console.WriteLine("Putting the tag into QT private mode.");
                    SetQtMode(QtDataProfile.Private, QtAccessRange.NormalRange);
                }

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

            // Wait for the user to press enter.
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        }

        // This event handler is called asynchronously 
        // when tag reports are available.
        static void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // Loop through each tag in the report 
            // and print the data.
            foreach (Tag tag in report)
            {
                Console.WriteLine("EPC : {0}", tag.Epc);
            }
        }

        // This event handler will be called when tag 
        // operations have been executed by the reader.
        static void OnTagOpComplete(ImpinjReader reader, TagOpReport report)
        {
            // Loop through all the completed tag operations
            foreach (TagOpResult result in report)
            {
                // Was this completed operation QT set operation?
                if (result is TagQtSetOpResult)
                {
                    // Cast it to a QT set result.
                    TagQtSetOpResult qtSetResult = result as TagQtSetOpResult;

                    // Print the status.
                    Console.WriteLine("QT set operation complete. Status : {0}", qtSetResult.Result);
                }
                // Was this completed operation QT get operation?
                else if (result is TagQtGetOpResult)
                {
                    // Cast it to a QT get result.
                    TagQtGetOpResult qtGetResult = result as TagQtGetOpResult;

                    // Print the status.
                    Console.WriteLine("QT get operation complete. Status : {0}", qtGetResult.Result);
                    Console.WriteLine("    Data Profile : {0}, Access Range : {1}", qtGetResult.DataProfile, qtGetResult.AccessRange);
                }
            }
        }
    }
}