using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using Org.LLRP.LTK.LLRPV1;
using Org.LLRP.LTK.LLRPV1.Impinj;
using Org.LLRP.LTK.LLRPV1.DataType;

namespace DocSample3
{
    class Program
    {
        static UInt32 msgID = 0;

        static int reportCount = 0;
        static int eventCount = 0;
        static int accessCount = 0;

        // Simple Handler for receiving the tag reports from the reader
        static void reader_OnRoAccessReportReceived(MSG_RO_ACCESS_REPORT msg)
        {
            // Report could be empty
            if (msg.TagReportData != null)
            {
                // Loop through and print out each tag
                for (int i = 0; i < msg.TagReportData.Length; i++)
                {
                    reportCount++;

                    // just write out the EPC as a hex string for now. It is guaranteed to be
                    // in all LLRP reports regardless of default configuration
                    string data = "EPC: ";
                    if (msg.TagReportData[i].EPCParameter[0].GetType() == typeof(PARAM_EPC_96))
                    {
                        data += ((PARAM_EPC_96)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                    }
                    else
                    {
                        data += ((PARAM_EPCData)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                    }

                    #region CheckForAccessResults
                    // check for read data results
                    if ((msg.TagReportData[i].AccessCommandOpSpecResult != null))
                    {
                        // there had better be one (since that what I asked for
                        if (msg.TagReportData[i].AccessCommandOpSpecResult.Count == 1)
                        {
                            // it had better be the read result
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[0].GetType()
                                == typeof(PARAM_C1G2ReadOpSpecResult))
                            {
                                PARAM_C1G2ReadOpSpecResult read = (PARAM_C1G2ReadOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[0];
                                accessCount++;
                                data += " AccessData: ";
                                if (read.Result == ENUM_C1G2ReadResultType.Success)
                                {
                                    data += read.ReadData.ToHexWordString();
                                }
                                else
                                {
                                    data += read.Result.ToString();
                                }
                            }
                        }
                    }
                    #endregion

                    Console.WriteLine(data);
                }
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
            Console.WriteLine("usage: docsample3.exe <readerName|IP");
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
                "Impinj C# LTK.NET RFID Application DocSample3 reader - " +
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
                reader.OnReaderEventNotification += new delegateReaderEventNotification(reader_OnReaderEventNotification);
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

                imp_msg.MSG_ID = msgID++;
                // note :this doesn't need to bet set as the library will default

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
                msg_cfg.MSG_ID = msgID++;
                //this doesn't need to bet set as the library will default

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

            #region getReaderCapabilities
            {
                Console.WriteLine("Getting Reader Capabilities\n");

                MSG_GET_READER_CAPABILITIES cap = new MSG_GET_READER_CAPABILITIES();
                cap.MSG_ID = msgID++;
                cap.RequestedData = ENUM_GetReaderCapabilitiesRequestedData.All;

                //Send the custom message and wait for 8 seconds
                MSG_ERROR_MESSAGE msg_err;
                MSG_GET_READER_CAPABILITIES_RESPONSE msg_rsp =
                          reader.GET_READER_CAPABILITIES(cap, out msg_err, 8000);

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
                    Console.WriteLine("GET reader Capabilities Command Timed out\n");
                    reader.Close();
                    return;
                }

                // Get the reader model number since some features are not
                // available on Speedway revolution.
                PARAM_GeneralDeviceCapabilities dev_cap = msg_rsp.GeneralDeviceCapabilities;

                // Check to make sure the model number mathces and that this device
                // is an impinj reader (deviceManufacturerName == 25882)
                if ((dev_cap == null) ||
                    (dev_cap.DeviceManufacturerName != 25882))
                {
                    Console.WriteLine("Could not determine reader model number\n");
                    reader.Close();
                    return;
                }

                // Find out how many inventory filters we support
                // don't run the application unless we support at least
                // two inventory filters and 1 access Spec
                PARAM_C1G2LLRPCapabilities g2_cap =
                    (PARAM_C1G2LLRPCapabilities)msg_rsp.AirProtocolLLRPCapabilities[0];
                if ((g2_cap == null) ||
                    (g2_cap.MaxNumSelectFiltersPerQuery < 2))
                {
                    Console.WriteLine("  reader supports " +
                        g2_cap.MaxNumSelectFiltersPerQuery +
                        " inventory filters \n");
                    Console.WriteLine("Reader does not support enough" +
                        " inventory (select) filters--- closing\n");
                    reader.Close();
                    return;
                }

                // find out how many access spec we support.  Don't run
                // the application unless we have 1 accessSpec
                PARAM_LLRPCapabilities llrp_cap = msg_rsp.LLRPCapabilities;
                if ((llrp_cap == null) ||
                   (llrp_cap.MaxNumAccessSpecs < 1))
                {
                    Console.WriteLine("  reader supports " +
                        llrp_cap.MaxNumAccessSpecs +
                        " accessSpecs \n");
                    Console.WriteLine("Reader does not support enough" +
                        " accessSpecs --- closing\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region SetReaderConfigWithXML
            {
                Console.WriteLine("Adding SET_READER_CONFIG from XML file \n");

                Org.LLRP.LTK.LLRPV1.DataType.Message obj;
                ENUM_LLRP_MSG_TYPE msg_type;

                // read the XML from a file and validate its an ADD_ROSPEC
                try
                {
                    FileStream fs = new FileStream(@"..\..\setReaderConfig.xml", FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
                    string s = sr.ReadToEnd();
                    fs.Close();

                    LLRPXmlParser.ParseXMLToLLRPMessage(s, out obj, out msg_type);

                    if (obj == null || msg_type != ENUM_LLRP_MSG_TYPE.SET_READER_CONFIG)
                    {
                        Console.WriteLine("Could not extract message from XML");
                        reader.Close();
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("Unable to convert to valid XML");
                    reader.Close();
                    return;
                }

                // Communicate that message to the reader
                MSG_SET_READER_CONFIG msg = (MSG_SET_READER_CONFIG)obj;
                msg.MSG_ID = msgID++;

                MSG_ERROR_MESSAGE msg_err;
                MSG_SET_READER_CONFIG_RESPONSE rsp = reader.SET_READER_CONFIG(msg, out msg_err, 12000);
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
                    Console.WriteLine("SET_READER_CONFIG Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region ADDRoSpecWithXML
            {
                Console.WriteLine("Adding RoSpec from XML file \n");

                Org.LLRP.LTK.LLRPV1.DataType.Message obj;
                ENUM_LLRP_MSG_TYPE msg_type;

                // read the XML from a file and validate its an ADD_ROSPEC
                try
                {
                    string filename;
                    filename = @"..\..\addRoSpec.xml";
                    FileStream fs = new FileStream(filename, FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
                    string s = sr.ReadToEnd();
                    fs.Close();

                    LLRPXmlParser.ParseXMLToLLRPMessage(s, out obj, out msg_type);

                    if (obj == null || msg_type != ENUM_LLRP_MSG_TYPE.ADD_ROSPEC)
                    {
                        Console.WriteLine("Could not extract message from XML");
                        reader.Close();
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("Unable to convert to valid XML");
                    reader.Close();
                    return;
                }

                MSG_ADD_ROSPEC msg = (MSG_ADD_ROSPEC)obj;
                msg.MSG_ID = msgID++;

                // Communicate that message to the reader
                MSG_ERROR_MESSAGE msg_err;
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

            #region ADDAccessSpecWithXML
            {
                Console.WriteLine("Adding AccessSpec from XML file \n");

                Org.LLRP.LTK.LLRPV1.DataType.Message obj;
                ENUM_LLRP_MSG_TYPE msg_type;

                // read the XML from a file and validate its an ADD_ACCESS_SPEC
                try
                {
                    FileStream fs = new FileStream(@"..\..\addAccessSpec.xml", FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
                    string s = sr.ReadToEnd();
                    fs.Close();

                    LLRPXmlParser.ParseXMLToLLRPMessage(s, out obj, out msg_type);

                    if (obj == null || msg_type != ENUM_LLRP_MSG_TYPE.ADD_ACCESSSPEC)
                    {
                        Console.WriteLine("Could not extract message from XML");
                        reader.Close();
                        return;
                    }
                }
                catch
                {
                    Console.WriteLine("Unable to convert to valid XML");
                    reader.Close();
                    return;
                }

                // Communicate that message to the reader
                MSG_ADD_ACCESSSPEC msg = (MSG_ADD_ACCESSSPEC)obj;
                msg.MSG_ID = msgID++;

                MSG_ERROR_MESSAGE msg_err;
                MSG_ADD_ACCESSSPEC_RESPONSE rsp = reader.ADD_ACCESSSPEC(msg, out msg_err, 12000);
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
                    Console.WriteLine("ADD_ACCESSSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region ADDAccessSpec
            {
                /* This section adds a second accessSpec identical to the
                 * first (except for its ID).  This is duplicate code with
                 * the goal of showing an example of how to build LLRP specs
                 * from C# objects rather than XML */
                Console.WriteLine("Adding AccessSpec from C# objects \n");

                // create the target tag filter spec to perform access only on these tags
                // This only requires a single filter (LTK/LLRP supports up to 2 )
                PARAM_C1G2TargetTag[] targetTag = new PARAM_C1G2TargetTag[1];
                targetTag[0] = new PARAM_C1G2TargetTag();
                targetTag[0].Match = true;
                targetTag[0].MB = new TwoBits(1);
                targetTag[0].Pointer = 16;
                targetTag[0].TagData = LLRPBitArray.FromHexString("300035");
                targetTag[0].TagMask = LLRPBitArray.FromHexString("f800ff");

                PARAM_C1G2TagSpec tagSpec = new PARAM_C1G2TagSpec();
                tagSpec.C1G2TargetTag = targetTag;

                // create the read operation to perform when this accessSpec is run
                PARAM_C1G2Read read = new PARAM_C1G2Read();
                read.AccessPassword = 0;
                read.MB = new TwoBits(3);
                read.WordCount = 2;
                read.WordPointer = 0;
                read.OpSpecID = 2;

                // add the opSpec and the TagSpec to the AccessCmd
                PARAM_AccessCommand accessCmd = new PARAM_AccessCommand();
                accessCmd.AirProtocolTagSpec = new UNION_AirProtocolTagSpec();
                accessCmd.AirProtocolTagSpec.Add(tagSpec);
                accessCmd.AccessCommandOpSpec.Add(read);

                // create the stop trigger for the Access Spec
                PARAM_AccessSpecStopTrigger stop = new PARAM_AccessSpecStopTrigger();
                stop.AccessSpecStopTrigger = ENUM_AccessSpecStopTriggerType.Null;
                stop.OperationCountValue = 0;

                // Create and set up the basic accessSpec
                PARAM_AccessSpec accessSpec = new PARAM_AccessSpec();
                accessSpec.AccessSpecID = 24;
                accessSpec.AntennaID = 0;
                accessSpec.ROSpecID = 0;
                accessSpec.CurrentState = ENUM_AccessSpecState.Disabled;
                accessSpec.ProtocolID = ENUM_AirProtocols.EPCGlobalClass1Gen2;

                // add the access command and stop trigger to the accessSpec
                accessSpec.AccessCommand = accessCmd;
                accessSpec.AccessSpecStopTrigger = stop;

                // Add the Access Spec to the ADD_ACCESSSPEC message
                MSG_ADD_ACCESSSPEC addAccess = new MSG_ADD_ACCESSSPEC();
                addAccess.MSG_ID = msgID++;
                addAccess.AccessSpec = accessSpec;

                // communicate the message to the reader
                MSG_ERROR_MESSAGE msg_err;
                MSG_ADD_ACCESSSPEC_RESPONSE rsp = reader.ADD_ACCESSSPEC(addAccess, out msg_err, 12000);
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
                    Console.WriteLine("ADD_ACCESSSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region EnableAccessSpec
            {
                Console.WriteLine("Enabling AccessSpec\n");
                MSG_ENABLE_ACCESSSPEC msg = new MSG_ENABLE_ACCESSSPEC();
                msg.MSG_ID = msgID++;

                MSG_ERROR_MESSAGE msg_err;
                msg.AccessSpecID = 23; // this better match the ACCESSSPEC we created above
                MSG_ENABLE_ACCESSSPEC_RESPONSE rsp = reader.ENABLE_ACCESSSPEC(msg, out msg_err, 12000);
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
                    Console.WriteLine("ENABLE_ACCESSSPEC Command Timed out\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region EnableRoSpec
            {
                Console.WriteLine("Enabling RoSpec\n");
                MSG_ENABLE_ROSPEC msg = new MSG_ENABLE_ROSPEC();
                msg.MSG_ID = msgID++;
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
                msg.MSG_ID = msgID++;

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

            // wait around to collect some data.
            for (int delay = 0; delay < 5; delay++)
            {
                Thread.Sleep(30000);
                #region PollReaderReports
                {
                    Console.WriteLine("Polling Report Data\n");
                    MSG_GET_REPORT msg = new MSG_GET_REPORT();
                    MSG_ERROR_MESSAGE msg_err;
                    msg.MSG_ID = msgID++;
                    reader.GET_REPORT(msg, out msg_err, 10000);
                }
                #endregion
            }

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
                msg_cfg.MSG_ID = msgID++;; // note this doesn't need to bet set as the library will default

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

            Console.WriteLine("  Received " + accessCount + " Access Reports.");
            Console.WriteLine("  Received " + reportCount + " Tag Reports.");
            Console.WriteLine("  Received " + eventCount + " Events.");
            Console.WriteLine("Closing\n");
            // clean up the reader
            reader.Close();
            reader.OnReaderEventNotification -= new delegateReaderEventNotification(reader_OnReaderEventNotification);
            reader.OnRoAccessReportReceived -= new delegateRoAccessReport(reader_OnRoAccessReportReceived);


        }

    }
}
