using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using Org.LLRP.LTK.LLRPV1;
using Org.LLRP.LTK.LLRPV1.Impinj;
using Org.LLRP.LTK.LLRPV1.DataType;

namespace DocSample5
{
    class Program
    {
        static UInt32 msgID = 0;

        static int reportCount = 0;
        static int eventCount = 0;
        static int opSpecCount = 0;
        static Random m_random = new Random();

        /* command line parsing fills out these configurations */
        static ENUM_ImpinjSerializedTIDMode m_tid = ENUM_ImpinjSerializedTIDMode.Disabled;
        static ENUM_ImpinjQTAccessRange m_shortRange = ENUM_ImpinjQTAccessRange.Normal_Range;
        static UInt32 m_password = 0;
        static UInt32 m_newPassword= 0;
        static UInt32 m_Verbose = 0;
        static UInt32 m_qtmode = 0;
        static string m_readerName = "unknown";

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
                    string data = "\nEPC: ";
                    if (msg.TagReportData[i].EPCParameter[0].GetType() == typeof(PARAM_EPC_96))
                    {
                        data += ((PARAM_EPC_96)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                    }
                    else
                    {
                        data += ((PARAM_EPCData)(msg.TagReportData[i].EPCParameter[0])).EPC.ToHexString();
                    }

                    #region CheckForAccessResults
                    // check for Standard and Impinj OpSpecResults and print them out
                    if ((msg.TagReportData[i].AccessCommandOpSpecResult != null))
                    {
                        for(int x = 0; x < msg.TagReportData[i].AccessCommandOpSpecResult.Count; x++)
                        {
                            // it had better be the read result
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType()
                                == typeof(PARAM_C1G2ReadOpSpecResult))
                            {
                                PARAM_C1G2ReadOpSpecResult read = 
                                    (PARAM_C1G2ReadOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];
                                data += "\n    ReadResult(" + read.OpSpecID.ToString() + "): " + read.Result.ToString();
                                opSpecCount++;
                                if (read.Result == ENUM_C1G2ReadResultType.Success)
                                {
                                    data += "\n      Data: " + read.ReadData.ToHexWordString();
                                }
                            }
                            // it had better be the read result
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType()
                                == typeof(PARAM_C1G2WriteOpSpecResult))
                            {
                                PARAM_C1G2WriteOpSpecResult write = 
                                    (PARAM_C1G2WriteOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];
                                data += "\n    WriteResult(" + write.OpSpecID.ToString() + "): " + write.Result.ToString();
                                opSpecCount++;
                            }
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType() ==
                                typeof(PARAM_ImpinjGetQTConfigOpSpecResult))
                            {
                                PARAM_ImpinjGetQTConfigOpSpecResult get =
                                    (PARAM_ImpinjGetQTConfigOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];

                                opSpecCount++;
                                data += "\n    getQTResult(" + get.OpSpecID.ToString() + "): " + get.Result.ToString();
                                if (get.Result == ENUM_ImpinjGetQTConfigResultType.Success)
                                {
                                    data += "\n      Range :" + get.AccessRange.ToString();
                                    data += "\n      Profile: " + get.DataProfile.ToString();
                                }
                            }
                            if (msg.TagReportData[i].AccessCommandOpSpecResult[x].GetType() ==
                                typeof(PARAM_ImpinjSetQTConfigOpSpecResult))
                            {
                                PARAM_ImpinjSetQTConfigOpSpecResult set =
                                    (PARAM_ImpinjSetQTConfigOpSpecResult)msg.TagReportData[i].AccessCommandOpSpecResult[x];
                                opSpecCount++;
                                data += "\n    setQTResult(" + set.OpSpecID.ToString() + "): " + set.Result.ToString();
                            }
                        }
                    }
                    #endregion

                    #region CheckForImpinjCustomTagData
                    // check for other Impinj Specific tag data and print it out 
                    if (msg.TagReportData[i].Custom != null)
                    {
                        opSpecCount++;
                        for (int x = 0; x < msg.TagReportData[i].Custom.Count; x++)
                        {

                            if (msg.TagReportData[i].Custom[x].GetType() ==
                                typeof(PARAM_ImpinjSerializedTID))
                            {
                                PARAM_ImpinjSerializedTID stid =
                                    (PARAM_ImpinjSerializedTID)msg.TagReportData[i].Custom[x];
                                opSpecCount++;
                                data += "\n    serialTID: " + stid.TID.ToHexWordString();
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

        // displays the usage information for this command 
        static void usage()
        {
            Console.WriteLine("Usage: docSample5.exe [options] READERHOSTNAME");
            Console.WriteLine("     -p <password> -- specify an optional password for operations");
            Console.WriteLine("     -n <password> -- specifies a new password for the set password command");
            Console.WriteLine("     -t  -- specify to automatically backscatter the TID");
            Console.WriteLine("     -s  -- if setting QT config, -s will short range the tag");
            Console.WriteLine("     -q <n>  -- run QT scenario n where n is defined as ");
            Console.WriteLine("         0 -- Read standard TID memory");
            Console.WriteLine("         1 -- set tag password (uses -p, -n )");
            Console.WriteLine("         2 -- Read private memory data without QT commands");
            Console.WriteLine("         3 -- read QT status of tag (uses -p)");
            Console.WriteLine("         4 -- set QT status of tag to private (uses -p, -s)");
            Console.WriteLine("         5 -- set QT status of tag to public (uses -p, -s)");
            Console.WriteLine("         6 -- Peek at private memory data with temporary QT command (uses -p)");
            Console.WriteLine("         7 -- Write 32 words of user data to random values");
            Console.WriteLine("         8 -- Write 6 words of public EPC data to random values");
            Console.WriteLine("         9 -- Read Reserved memory");
            Console.WriteLine("");
            return;
        }

        static void Main(string[] args)
        {
            LLRPClient reader;
            int i;

            #region ProcessCommandLine 

            if (args.Length < 1)
            {
                usage();
                return;
            }

            /* get the options. Skip the last one as its the hostname */
            for( i = 0; i < args.Length-1 ; i++)
            {
                if((args[i] == "-p") && (i < (args.Length-1)))
                {
                    i++;
                    m_password = System.Convert.ToUInt32(args[i]);
                }
                else if ((args[i] == "-n") && (i < (args.Length - 1)))
                {
                    i++;
                    m_newPassword = System.Convert.ToUInt32(args[i]);    
                }        
                else if (args[i] == "-t")
                {
                    m_tid = ENUM_ImpinjSerializedTIDMode.Enabled;
                }
                else if (args[i] == "-s") 
                {
                    m_shortRange = ENUM_ImpinjQTAccessRange.Short_Range;
                }
                else if ((args[i] == "-v") && (i < (args.Length - 1)))
                {
                    i++;
                    m_Verbose = System.Convert.ToUInt32(args[i]);
                }
                else if ((args[i] == "-q") && (i < (args.Length - 1)))
                {
                    i++;
                    m_qtmode = System.Convert.ToUInt32(args[i]);
                }   
                else
                {
                    usage();
                    return;
                }
            }

            m_readerName = args[i];

            Console.WriteLine(
                "Impinj C# LTK.NET RFID Application DocSample5 reader - " +
                m_readerName + "\n");

            Console.WriteLine(
                " qtMode:" + m_qtmode.ToString() +
                " Verbose:" + m_Verbose.ToString() +
                " Range:" + m_shortRange.ToString() +
                " SerializeTID:" + m_tid.ToString() +
                " OldPassword:" + m_password.ToString() +
                " NewPassword:" + m_newPassword.ToString());
            #endregion

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
                bool ret = reader.Open(m_readerName, 5000, out status);

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
                
                // Need to parse version number strings and compare to make sure
                // that the reader version is higher than 4.4.
                Version readerVersion = new Version(dev_cap.ReaderFirmwareVersion);
                Version minimumVersion = new Version("4.4.0.0");

                if (readerVersion < minimumVersion)
                {
                    Console.WriteLine("Must use Octane 4.4 or later\n");
                    reader.Close();
                    return;
                }
            }
            #endregion

            #region SetReaderConfig
            {
                Console.WriteLine("Adding SET_READER_CONFIG n");     

                // Communicate that message to the reader
                MSG_SET_READER_CONFIG msg = new MSG_SET_READER_CONFIG();
                msg.MSG_ID = msgID++;

                msg.ResetToFactoryDefault = false;

                // turn off all reports 
                msg.ROReportSpec = new PARAM_ROReportSpec();
                msg.ROReportSpec.TagReportContentSelector = new PARAM_TagReportContentSelector();
                msg.ROReportSpec.TagReportContentSelector.EnableAccessSpecID = false;
                msg.ROReportSpec.TagReportContentSelector.EnableAntennaID = false;
                msg.ROReportSpec.TagReportContentSelector.EnableChannelIndex = false;
                msg.ROReportSpec.TagReportContentSelector.EnableFirstSeenTimestamp = false;
                msg.ROReportSpec.TagReportContentSelector.EnableInventoryParameterSpecID = false;
                msg.ROReportSpec.TagReportContentSelector.EnableLastSeenTimestamp = false;
                msg.ROReportSpec.TagReportContentSelector.EnablePeakRSSI = false;
                msg.ROReportSpec.TagReportContentSelector.EnableROSpecID = false;
                msg.ROReportSpec.TagReportContentSelector.EnableSpecIndex = false;
                msg.ROReportSpec.TagReportContentSelector.EnableTagSeenCount = false;
                
                /* report all tags immediately */
                msg.ROReportSpec.ROReportTrigger = ENUM_ROReportTriggerType.Upon_N_Tags_Or_End_Of_ROSpec;
                msg.ROReportSpec.N = 1;

                /* turn on serialized TID if we are asked to */
                PARAM_ImpinjTagReportContentSelector impinjTagData = new PARAM_ImpinjTagReportContentSelector();
                impinjTagData.ImpinjEnableGPSCoordinates = new PARAM_ImpinjEnableGPSCoordinates();
                impinjTagData.ImpinjEnableGPSCoordinates.GPSCoordinatesMode = ENUM_ImpinjGPSCoordinatesMode.Disabled;
                impinjTagData.ImpinjEnablePeakRSSI = new PARAM_ImpinjEnablePeakRSSI();
                impinjTagData.ImpinjEnablePeakRSSI.PeakRSSIMode = ENUM_ImpinjPeakRSSIMode.Disabled;
                impinjTagData.ImpinjEnableRFPhaseAngle = new PARAM_ImpinjEnableRFPhaseAngle();
                impinjTagData.ImpinjEnableRFPhaseAngle.RFPhaseAngleMode = ENUM_ImpinjRFPhaseAngleMode.Disabled;
                impinjTagData.ImpinjEnableSerializedTID = new PARAM_ImpinjEnableSerializedTID();
                impinjTagData.ImpinjEnableSerializedTID.SerializedTIDMode = m_tid;
                msg.ROReportSpec.Custom.Add(impinjTagData);

                /* report access specs immediately as well */
                msg.AccessReportSpec = new PARAM_AccessReportSpec();
                msg.AccessReportSpec.AccessReportTrigger = ENUM_AccessReportTriggerType.End_Of_AccessSpec;
 
                // set the antenna configuration for all antennas 
                msg.AntennaConfiguration = new PARAM_AntennaConfiguration[1];
                msg.AntennaConfiguration[0] = new PARAM_AntennaConfiguration();
                msg.AntennaConfiguration[0].AntennaID = 0; /* all antennas  */

                // use DRM autset mode 
                PARAM_C1G2InventoryCommand c1g2Inv = new PARAM_C1G2InventoryCommand();
                c1g2Inv.C1G2RFControl = new PARAM_C1G2RFControl();
                c1g2Inv.C1G2RFControl.ModeIndex = 1000;       
                c1g2Inv.C1G2RFControl.Tari = 0;                

                // Use session 1 so we don't get too many reads 
                c1g2Inv.C1G2SingulationControl = new PARAM_C1G2SingulationControl();
                c1g2Inv.C1G2SingulationControl.Session = new TwoBits(1);
                c1g2Inv.C1G2SingulationControl.TagPopulation = 1;
                c1g2Inv.C1G2SingulationControl.TagTransitTime = 0;

                // add to the message 
                msg.AntennaConfiguration[0].AirProtocolInventoryCommandSettings.Add(c1g2Inv);

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
                targetTag[0].TagData = LLRPBitArray.FromHexString("");
                targetTag[0].TagMask = LLRPBitArray.FromHexString("");

                PARAM_C1G2TagSpec tagSpec = new PARAM_C1G2TagSpec();
                tagSpec.C1G2TargetTag = targetTag;

                PARAM_AccessCommand accessCmd = new PARAM_AccessCommand();
                accessCmd.AirProtocolTagSpec = new UNION_AirProtocolTagSpec();
                accessCmd.AirProtocolTagSpec.Add(tagSpec);

                switch(m_qtmode)
                {
                    case 0:
                        PARAM_C1G2Read readStdTID = new PARAM_C1G2Read();
                        readStdTID.AccessPassword = 0;
                        readStdTID.MB = new TwoBits(2);
                        readStdTID.OpSpecID = 1;
                        readStdTID.WordCount = 2;
                        readStdTID.WordPointer = 0;
                        accessCmd.AccessCommandOpSpec.Add(readStdTID);
                        break;

                    case 1:
                        PARAM_C1G2Write writePassword = new PARAM_C1G2Write();
                        writePassword.OpSpecID = 2;
                        writePassword.MB = new TwoBits(0);
                        writePassword.AccessPassword = m_password;
                        writePassword.WordPointer = 2;
                        writePassword.WriteData = new UInt16Array();
                        writePassword.WriteData.Add( (UInt16) ((m_newPassword >> 16) & 0x0000ffff));
                        writePassword.WriteData.Add( (UInt16) (m_newPassword & 0x0000ffff));
                        accessCmd.AccessCommandOpSpec.Add(writePassword);
                        break;

                    case 2:
                        PARAM_C1G2Read readSerializedTID = new PARAM_C1G2Read();
                        readSerializedTID.AccessPassword = 0;
                        readSerializedTID.MB = new TwoBits(2);
                        readSerializedTID.OpSpecID = 3;
                        readSerializedTID.WordCount = 6;
                        readSerializedTID.WordPointer = 0;
                        accessCmd.AccessCommandOpSpec.Add(readSerializedTID);

                        PARAM_C1G2Read readPublicEPC = new PARAM_C1G2Read();
                        readPublicEPC.AccessPassword = 0;
                        readPublicEPC.MB = new TwoBits(2);
                        readPublicEPC.OpSpecID = 4;
                        readPublicEPC.WordCount = 6;
                        readPublicEPC.WordPointer = 6;
                        accessCmd.AccessCommandOpSpec.Add(readPublicEPC);

                        PARAM_C1G2Read readUser = new PARAM_C1G2Read();
                        readUser.AccessPassword = 0;
                        readUser.MB = new TwoBits(3);
                        readUser.OpSpecID = 5;
                        readUser.WordCount = 32;
                        readUser.WordPointer = 0;
                        accessCmd.AccessCommandOpSpec.Add(readUser);
                        break;                        

                    case 3: 
                        PARAM_ImpinjGetQTConfig getQT = new PARAM_ImpinjGetQTConfig();
                        getQT.OpSpecID = 6;
                        getQT.AccessPassword = m_password;
                        accessCmd.AccessCommandOpSpec.Add(getQT);
                        break;
                    case 4: 
                        PARAM_ImpinjSetQTConfig setQTPrivate = new PARAM_ImpinjSetQTConfig();
                        setQTPrivate.OpSpecID = 7;
                        setQTPrivate.AccessPassword = m_password;
                        setQTPrivate.Persistence  = ENUM_ImpinjQTPersistence.Permanent;
                        setQTPrivate.DataProfile = ENUM_ImpinjQTDataProfile.Private;
                        setQTPrivate.AccessRange = m_shortRange;
                        accessCmd.AccessCommandOpSpec.Add(setQTPrivate);
                        break;
                    case 5: 
                        PARAM_ImpinjSetQTConfig setQTPublic = new PARAM_ImpinjSetQTConfig();
                        setQTPublic.OpSpecID = 8;
                        setQTPublic.AccessPassword = m_password;
                        setQTPublic.Persistence  = ENUM_ImpinjQTPersistence.Permanent;
                        setQTPublic.DataProfile = ENUM_ImpinjQTDataProfile.Public;
                        setQTPublic.AccessRange = m_shortRange;
                        accessCmd.AccessCommandOpSpec.Add(setQTPublic);
                        break;
                    case 6: 
                        PARAM_ImpinjSetQTConfig setQTPeek = new PARAM_ImpinjSetQTConfig();
                        setQTPeek.OpSpecID = 9;
                        setQTPeek.AccessPassword = m_password;
                        setQTPeek.Persistence  = ENUM_ImpinjQTPersistence.Temporary;
                        setQTPeek.DataProfile = ENUM_ImpinjQTDataProfile.Private;
                        setQTPeek.AccessRange = ENUM_ImpinjQTAccessRange.Normal_Range;
                        accessCmd.AccessCommandOpSpec.Add(setQTPeek);

                        PARAM_C1G2Read readSerializedTIDPeek = new PARAM_C1G2Read();
                        readSerializedTIDPeek.AccessPassword = 0;
                        readSerializedTIDPeek.MB = new TwoBits(2);
                        readSerializedTIDPeek.OpSpecID = 10;
                        readSerializedTIDPeek.WordCount = 6;
                        readSerializedTIDPeek.WordPointer = 0;
                        accessCmd.AccessCommandOpSpec.Add(readSerializedTIDPeek);

                        PARAM_C1G2Read readPrivateEPC = new PARAM_C1G2Read();
                        readPrivateEPC.AccessPassword = 0;
                        readPrivateEPC.MB = new TwoBits(1);
                        readPrivateEPC.OpSpecID = 11;
                        readPrivateEPC.WordCount = 8;
                        readPrivateEPC.WordPointer = 2;
                        accessCmd.AccessCommandOpSpec.Add(readPrivateEPC);

                        PARAM_C1G2Read readUserPeek = new PARAM_C1G2Read();
                        readUserPeek.AccessPassword = 0;
                        readUserPeek.MB = new TwoBits(3);
                        readUserPeek.OpSpecID = 12;
                        readUserPeek.WordCount = 32;
                        readUserPeek.WordPointer = 0;
                        accessCmd.AccessCommandOpSpec.Add(readUserPeek);
                        break;
                    case 7:
                        PARAM_C1G2Write writeUser = new PARAM_C1G2Write();
                        writeUser.AccessPassword = m_password;
                        writeUser.OpSpecID = 13;
                        writeUser.WordPointer = 0;
                        writeUser.MB = new TwoBits(3);

                        writeUser.WriteData = new UInt16Array();
                        for(int x = 0; x < 32; x++)
                        {
                            writeUser.WriteData.Add((UInt16)m_random.Next(65536));
                        }

                        accessCmd.AccessCommandOpSpec.Add(writeUser);
                        break;
                    case 8:
                        PARAM_C1G2Write writePubEPC = new PARAM_C1G2Write();
                        writePubEPC.AccessPassword = m_password;
                        writePubEPC.MB = new TwoBits(2);
                        writePubEPC.OpSpecID = 14;
                        writePubEPC.WordPointer = 6;

                        writePubEPC.WriteData = new UInt16Array();
                        for(int x = 0; x < 6; x++)
                        {
                            writePubEPC.WriteData.Add((UInt16)m_random.Next(65536));
                        }

                        accessCmd.AccessCommandOpSpec.Add(writePubEPC);
                        break;
                    case 9:
                        PARAM_C1G2Read readRsvd = new PARAM_C1G2Read();
                        readRsvd.AccessPassword = m_password;
                        readRsvd.MB = new TwoBits(0);
                        readRsvd.OpSpecID = 15;
                        readRsvd.WordCount = 4;
                        readRsvd.WordPointer = 0;
                        accessCmd.AccessCommandOpSpec.Add(readRsvd);
                        break;
                }

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
                msg.AccessSpecID = 24; // this better match the ACCESSSPEC we created above
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

            // this should be plenty long enough to do these commands
            Thread.Sleep(3000);

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

            Console.WriteLine("  Received " + opSpecCount + " OpSpec Results.");
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
