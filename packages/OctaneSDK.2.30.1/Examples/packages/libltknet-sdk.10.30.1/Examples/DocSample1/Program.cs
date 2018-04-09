using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using Org.LLRP.LTK.LLRPV1;
using Org.LLRP.LTK.LLRPV1.Impinj;
using Org.LLRP.LTK.LLRPV1.DataType;

namespace SimpleLLRPSample
{
    class Program
    {
        static int reportCount = 0;
        static int eventCount = 0;

        // Simple Handler for receiving the tag reports from the reader
        static void reader_OnRoAccessReportReceived(MSG_RO_ACCESS_REPORT msg)
        {
            // Report could be empty
            if (msg.TagReportData == null) return;

            // Loop through and print out each tag
            for (int i = 0; i < msg.TagReportData.Length; i++)
            {
                reportCount++;

                // just write out the EPC as a hex string for now. It is guaranteed to be
                // in all LLRP reports regardless of default configuration
                string epc;
                if (msg.TagReportData[i].EPCParameter[0].GetType() == typeof(PARAM_EPC_96))
                {
                    epc = ((PARAM_EPC_96)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                }
                else
                {
                    epc = ((PARAM_EPCData)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                }
                Console.WriteLine(epc);
            }
        }

        // Simple Handler for receiving the reader events from the reader
        static void reader_OnReaderEventNotification(MSG_READER_EVENT_NOTIFICATION msg)
        {
            // Events could be empty
            if (msg.ReaderEventNotificationData == null) return;

            // Just write out the LTK-XML for now
            eventCount++;

            // speedway readers always report UTC timestamp
            UNION_Timestamp t = msg.ReaderEventNotificationData.Timestamp;
            PARAM_UTCTimestamp ut = (PARAM_UTCTimestamp)t[0];
            double millis = (ut.Microseconds + 500) / 1000;

            // LLRP reports time in microseconds relative to the Unix Epoch
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = epoch.AddMilliseconds(millis);

            Console.WriteLine("======Reader Event " + eventCount.ToString() + "======" +
                now.ToString("O"));

            // this is how you would look for individual events of interest
            // Here I just dump the event
            if(msg.ReaderEventNotificationData.AISpecEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.AISpecEvent.ToString());
            if (msg.ReaderEventNotificationData.AntennaEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.AntennaEvent.ToString());
            if (msg.ReaderEventNotificationData.ConnectionAttemptEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ConnectionAttemptEvent.ToString());
            if (msg.ReaderEventNotificationData.ConnectionCloseEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ConnectionCloseEvent.ToString());
            if (msg.ReaderEventNotificationData.GPIEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.GPIEvent.ToString());
            if (msg.ReaderEventNotificationData.HoppingEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.HoppingEvent.ToString());
            if (msg.ReaderEventNotificationData.ReaderExceptionEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReaderExceptionEvent.ToString());
            if (msg.ReaderEventNotificationData.ReportBufferLevelWarningEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReportBufferLevelWarningEvent.ToString());
            if (msg.ReaderEventNotificationData.ReportBufferOverflowErrorEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReportBufferOverflowErrorEvent.ToString());
            if (msg.ReaderEventNotificationData.ROSpecEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ROSpecEvent.ToString());



        }

        // Duplicate the above handler for encapsulated reader event notifications
        // You would use encapsulated notifications if you wanted to use the same
        // static handler for multiple readers
        static void reader_OnEncapedReaderEventNotification(ENCAPED_READER_EVENT_NOTIFICATION enc)
        {
            MSG_READER_EVENT_NOTIFICATION msg = enc.ntf;

            // Events could be empty
            if (msg.ReaderEventNotificationData == null) return;

            // Just write out the LTK-XML for now
            eventCount++;

            // speedway readers always report UTC timestamp
            UNION_Timestamp t = msg.ReaderEventNotificationData.Timestamp;
            PARAM_UTCTimestamp ut = (PARAM_UTCTimestamp)t[0];
            double millis = (ut.Microseconds + 500) / 1000;

            // LLRP reports time in microseconds relative to the Unix Epoch
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime now = epoch.AddMilliseconds(millis);

            Console.WriteLine("======Reader Event from " + enc.reader + " " + eventCount.ToString() + "======" +
                now.ToString("O"));

            // this is how you would look for individual events of interest
            // Here I just dump the event
            if (msg.ReaderEventNotificationData.AISpecEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.AISpecEvent.ToString());
            if (msg.ReaderEventNotificationData.AntennaEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.AntennaEvent.ToString());
            if (msg.ReaderEventNotificationData.ConnectionAttemptEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ConnectionAttemptEvent.ToString());
            if (msg.ReaderEventNotificationData.ConnectionCloseEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ConnectionCloseEvent.ToString());
            if (msg.ReaderEventNotificationData.GPIEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.GPIEvent.ToString());
            if (msg.ReaderEventNotificationData.HoppingEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.HoppingEvent.ToString());
            if (msg.ReaderEventNotificationData.ReaderExceptionEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReaderExceptionEvent.ToString());
            if (msg.ReaderEventNotificationData.ReportBufferLevelWarningEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReportBufferLevelWarningEvent.ToString());
            if (msg.ReaderEventNotificationData.ReportBufferOverflowErrorEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ReportBufferOverflowErrorEvent.ToString());
            if (msg.ReaderEventNotificationData.ROSpecEvent != null)
                Console.WriteLine(msg.ReaderEventNotificationData.ROSpecEvent.ToString());



        }

        static void usage()
        {
            Console.WriteLine("usage: docsample2.exe <readerName|IP");
            return;
        }

        static void Main(string[] args)
        {
            LLRPClient reader;

            if (args.Length != 1)
            {
                usage();
                return;
            }
            string readerName = args[0];


            Console.WriteLine(
                "Impinj C# LTK.NET RFID Application DocSample1 reader - " +
                readerName + "\n");

            #region Initializing
            {
                Console.WriteLine("Initializing\n");

                //Create an instance of LLRP reader client.
                reader = new LLRPClient();

                //Impinj Best Practice! Always Install the Impinj extensions
                Impinj_Installer.Install();
            }
            #endregion

            #region EventHandlers
            {
                Console.WriteLine("Adding Event Handlers\n");
                // don't need two kinds of reader events, just use encaped here to demonstrate 
                //reader.OnReaderEventNotification += new delegateReaderEventNotification(reader_OnReaderEventNotification);
                reader.OnEncapedReaderEventNotification += new delegateEncapReaderEventNotification(reader_OnEncapedReaderEventNotification);
                reader.OnRoAccessReportReceived += new delegateRoAccessReport(reader_OnRoAccessReportReceived);
            }
            #endregion

            #region Connecting
            {
                Console.WriteLine("Connecting To Reader\n");

                ENUM_ConnectionAttemptStatusType status;

                //Open the reader connection.  Timeout after 5 seconds
                bool ret = reader.Open(readerName, 5000, out status);

                //Ensure that the open succeeded and that the reader
                // returned the correct connection status result

                if (!ret || status != ENUM_ConnectionAttemptStatusType.Success)
                {
                    Console.WriteLine("Failed to Connect to Reader \n");
                    return;
                }
            }
            #endregion

            #region EnableExtensions
            {
                Console.WriteLine("Enabling Impinj Extensions\n");

                MSG_IMPINJ_ENABLE_EXTENSIONS imp_msg =
                                                new MSG_IMPINJ_ENABLE_EXTENSIONS();
                MSG_ERROR_MESSAGE msg_err;

                imp_msg.MSG_ID = 1; // not this doesn't need to bet set as the library will default

                //Send the custom message and wait for 8 seconds
                MSG_CUSTOM_MESSAGE cust_rsp = reader.CUSTOM_MESSAGE(imp_msg, out msg_err, 8000);
                MSG_IMPINJ_ENABLE_EXTENSIONS_RESPONSE msg_rsp =
                    cust_rsp as MSG_IMPINJ_ENABLE_EXTENSIONS_RESPONSE;

                if (msg_rsp != null)
                {
                    if (msg_rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(msg_rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("Enable Extensions Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region FactoryDefault
            {
                Console.WriteLine("Factory Default the Reader\n");

                // factory default the reader
                MSG_SET_READER_CONFIG msg_cfg = new MSG_SET_READER_CONFIG();
                MSG_ERROR_MESSAGE msg_err;

                msg_cfg.ResetToFactoryDefault = true;
                msg_cfg.MSG_ID = 2; //this doesn't need to bet set as the library will default

                //if SET_READER_CONFIG affects antennas it could take several seconds to return
                MSG_SET_READER_CONFIG_RESPONSE rsp_cfg = reader.SET_READER_CONFIG(msg_cfg, out msg_err, 12000);

                if (rsp_cfg != null)
                {
                    if (rsp_cfg.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp_cfg.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("SET_READER_CONFIG Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region ADDRoSpecWithObjects
            {
                Console.WriteLine("Adding RoSpec\n");

                // set up the basic parameters in the ROSpec. Use all the defaults from the reader
                MSG_ADD_ROSPEC msg = new MSG_ADD_ROSPEC();
                MSG_ERROR_MESSAGE msg_err;
                msg.ROSpec = new PARAM_ROSpec();
                msg.ROSpec.CurrentState = ENUM_ROSpecState.Disabled;
                msg.ROSpec.Priority = 0x00;
                msg.ROSpec.ROSpecID = 1111;

                // setup the start and stop triggers in the Boundary Spec
                msg.ROSpec.ROBoundarySpec = new PARAM_ROBoundarySpec();
                msg.ROSpec.ROBoundarySpec.ROSpecStartTrigger = new PARAM_ROSpecStartTrigger();
                msg.ROSpec.ROBoundarySpec.ROSpecStartTrigger.ROSpecStartTriggerType = ENUM_ROSpecStartTriggerType.Null;

                msg.ROSpec.ROBoundarySpec.ROSpecStopTrigger = new PARAM_ROSpecStopTrigger();
                msg.ROSpec.ROBoundarySpec.ROSpecStopTrigger.ROSpecStopTriggerType = ENUM_ROSpecStopTriggerType.Null;
                msg.ROSpec.ROBoundarySpec.ROSpecStopTrigger.DurationTriggerValue = 0; // ignored by reader

                // Add a single Antenna Inventory to the ROSpec
                msg.ROSpec.SpecParameter = new UNION_SpecParameter();
                PARAM_AISpec aiSpec = new PARAM_AISpec();

                aiSpec.AntennaIDs = new UInt16Array();
                aiSpec.AntennaIDs.Add(0);       // all antennas
                aiSpec.AISpecStopTrigger = new PARAM_AISpecStopTrigger();
                aiSpec.AISpecStopTrigger.AISpecStopTriggerType = ENUM_AISpecStopTriggerType.Null;

                // use all the defaults from the reader.  Just specify the minimum required
                aiSpec.InventoryParameterSpec = new PARAM_InventoryParameterSpec[1];
                aiSpec.InventoryParameterSpec[0] = new PARAM_InventoryParameterSpec();
                aiSpec.InventoryParameterSpec[0].InventoryParameterSpecID = 1234;
                aiSpec.InventoryParameterSpec[0].ProtocolID = ENUM_AirProtocols.EPCGlobalClass1Gen2;

                msg.ROSpec.SpecParameter.Add(aiSpec);

                MSG_ADD_ROSPEC_RESPONSE rsp = reader.ADD_ROSPEC(msg, out msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("ADD_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region EnableRoSpec
            {
                Console.WriteLine("Enabling RoSpec\n");
                MSG_ENABLE_ROSPEC msg = new MSG_ENABLE_ROSPEC();
                MSG_ERROR_MESSAGE msg_err;
                msg.ROSpecID = 1111; // this better match the ROSpec we created above
                MSG_ENABLE_ROSPEC_RESPONSE rsp = reader.ENABLE_ROSPEC(msg, out msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("ENABLE_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region StartRoSpec
            {
                Console.WriteLine("Starting RoSpec\n");
                MSG_START_ROSPEC msg = new MSG_START_ROSPEC();
                MSG_ERROR_MESSAGE msg_err;
                msg.ROSpecID = 1111; // this better match the RoSpec we created above
                MSG_START_ROSPEC_RESPONSE rsp = reader.START_ROSPEC(msg, out msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("START_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            // wait around to collect some data
            Thread.Sleep(10000);

            #region StopRoSpec
            {
                Console.WriteLine("Stopping RoSpec\n");
                MSG_STOP_ROSPEC msg = new MSG_STOP_ROSPEC();
                MSG_ERROR_MESSAGE msg_err;
                msg.ROSpecID = 1111; // this better match the RoSpec we created above
                MSG_STOP_ROSPEC_RESPONSE rsp = reader.STOP_ROSPEC(msg, out msg_err, 12000);
                if (rsp != null)
                {
                    if (rsp.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("STOP_ROSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region Clean Up Reader Configuration
            {
                Console.WriteLine("Factory Default the Reader\n");

                // factory default the reader
                MSG_SET_READER_CONFIG msg_cfg = new MSG_SET_READER_CONFIG();
                MSG_ERROR_MESSAGE msg_err;

                msg_cfg.ResetToFactoryDefault = true;
                msg_cfg.MSG_ID = 2; // not this doesn't need to bet set as the library will default

                // Note that if SET_READER_CONFIG affects antennas it could take several seconds to return
                MSG_SET_READER_CONFIG_RESPONSE rsp_cfg = reader.SET_READER_CONFIG(msg_cfg, out msg_err, 12000);

                if (rsp_cfg != null)
                {
                    if (rsp_cfg.LLRPStatus.StatusCode != ENUM_StatusCode.M_Success)
                    {
                        Console.WriteLine(rsp_cfg.LLRPStatus.StatusCode.ToString());
                        reader.Close();
                        return;
                    }
                }
                else if (msg_err != null)
                {
                    Console.WriteLine(msg_err.ToString());
                    reader.Close();
                    return;
                }
                else
                {
                    Console.WriteLine("SET_READER_CONFIG Command Timed out\n");
                    reader.Close();
                    return;
                }
            }

            #endregion

            Console.WriteLine("  Received " + reportCount + " Reports.");
            Console.WriteLine("  Received " + eventCount + " Events.");
            Console.WriteLine("Closing\n");
            // clean up the reader
            reader.Close();
            reader.OnReaderEventNotification -= new delegateReaderEventNotification(reader_OnReaderEventNotification);
            reader.OnRoAccessReportReceived -= new delegateRoAccessReport(reader_OnRoAccessReportReceived);
        }

    }
}
