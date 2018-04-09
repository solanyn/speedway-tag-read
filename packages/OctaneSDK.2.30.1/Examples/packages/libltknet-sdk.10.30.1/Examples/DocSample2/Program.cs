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

        static int txPwrIndx = 0;
        static int ChannelIndex = 0;
        static int hopTableID = 0;

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

                    // check for read data results

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
                "Impinj C# LTK.NET RFID Application DocSample2 reader - " +
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

            #region getReaderCapabilities
            {
                Console.WriteLine("Getting Reader Capabilities\n");

                MSG_GET_READER_CAPABILITIES cap = new MSG_GET_READER_CAPABILITIES();
                cap.MSG_ID = 2; // not this doesn't need to bet set as the library will default
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

                // Get the reader model number 
                PARAM_GeneralDeviceCapabilities dev_cap = msg_rsp.GeneralDeviceCapabilities;

                // Check to make sure the model number mathces and that this device
                // is an impinj reader (deviceManufacturerName == 25882)
                if ((dev_cap != null) &&
                    (dev_cap.DeviceManufacturerName == 25882))
                {
                }
                else
                {
                    Console.WriteLine("Could not determine reader model number\n");
                    reader.Close();
                    return;
                }


                // get the uhf band capabilities.  Inside this is the power table.
                // take the last element of the power table to get the highest power.
                PARAM_UHFBandCapabilities uhf = msg_rsp.RegulatoryCapabilities.UHFBandCapabilities;
                PARAM_TransmitPowerLevelTableEntry entry =
                        uhf.TransmitPowerLevelTableEntry[uhf.TransmitPowerLevelTableEntry.Length - 1];
                txPwrIndx = entry.Index;
                double power = entry.TransmitPowerValue / 100;

                Console.WriteLine("     Max Power " + power.ToString() + " dbm.");
            }
            #endregion

            #region getReaderConfig
            {
                Console.WriteLine("Getting Reader Configuration\n");

                MSG_GET_READER_CONFIG cap = new MSG_GET_READER_CONFIG();
                cap.MSG_ID = 2; // not this doesn't need to bet set as the library will default
                cap.RequestedData = ENUM_GetReaderConfigRequestedData.All;

                //Send the custom message and wait for 8 seconds
                MSG_ERROR_MESSAGE msg_err;
                MSG_GET_READER_CONFIG_RESPONSE msg_rsp =
                          reader.GET_READER_CONFIG(cap, out msg_err, 8000);

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
                    Console.WriteLine("GET reader Config Command Timed out\n");
                    reader.Close();
                    return;
                }

                // Get the hopTableId and Channel Index
                if ((null != msg_rsp.AntennaConfiguration)
                    && (0 < msg_rsp.AntennaConfiguration.Length)
                    && (null != msg_rsp.AntennaConfiguration[0].RFTransmitter))
                {
                    PARAM_RFTransmitter rftx =
                        msg_rsp.AntennaConfiguration[0].RFTransmitter;

                    // we have to get these two values as well otherwise
                    // we won't know what to fill in the RFTransmitter
                    // parameter when we set transmit power
                    ChannelIndex = rftx.ChannelIndex;
                    hopTableID = rftx.HopTableID;
                    Console.WriteLine(" Saving ChanIndex " +
                                        ChannelIndex.ToString() +
                                      " hopTableId " +
                                        hopTableID.ToString());
                }
                else
                {
                    Console.WriteLine("Could not get rf transmitter parameters\n");
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

                // set the max power available but don' forget to
                // apply the hoptable and channelIndex from the
                // current configuration
                PARAM_AntennaConfiguration ant = msg.AntennaConfiguration[0];
                ant.RFTransmitter.TransmitPower = (ushort)txPwrIndx;
                ant.RFTransmitter.ChannelIndex = (ushort)ChannelIndex;
                ant.RFTransmitter.HopTableID = (ushort)hopTableID;

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
                    FileStream fs = new FileStream(@"..\..\addRoSpec.xml", FileMode.Open);
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

                // covert to the proper message type
                MSG_ADD_ROSPEC msg = (MSG_ADD_ROSPEC)obj;

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
            Thread.Sleep(60000);

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
