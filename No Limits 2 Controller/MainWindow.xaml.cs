﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;
using System.Timers;
using System.Threading;

namespace No_Limits_2_Controller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Static Variables
        /**
 * Message Enum
 * Type: Request
 * Can be send by the client to keep connetion alive. No other purpose. Returned message by server is MSG_OK
 * DataSize = 0
 */
        private const int N_MSG_IDLE = 0;

        /**
         * Message Enum
         * Type: Reply
         * Typical answer from server for messages that were successfully processed and do not require a specific returned answer
         * DataSize = 0
         */
        private const int N_MSG_OK = 1;

        /**
         * Message Enum
         * Type: Reply   
         * Will be send by the server in case of an error. The data component contains an UTF-8 encoded error message
         * DataSize = number of bytes of UTF8 encoded string
         *    UTF8 string
         */
        private const int N_MSG_ERROR = 2;

        /**
         * Message Enum
         * Type: Request   
         * Can be used by the client to request the application version. The server will reply with MSG_VERSION
         * DataSize = 0
         */
        private const int N_MSG_GET_VERSION = 3;

        /**
         * Message Enum
         * Type: Reply   
         * Will be send by the server as an answer to MSG_GET_VERSION
         * DataSize = 4 
         *    4 Bytes (major to minor version numbers e.g. 2, 2, 0, 0 for '2.2.0.0')
         */
        private const int N_MSG_VERSION = 4;

        /**
         * Message Enum
         * Type: Request   
         * Can be used by the client to request common telemetry data. The server will reply with MSG_TELEMETRY
         * DataSize = 0
         */
        private const int N_MSG_GET_TELEMETRY = 5;

        /**
         * Message Enum
         * Type: Reply   
         * Will be send by the server as an anwser to MSG_GET_TELEMETRY
         * DataSize = 76 
         *    int32 (state flags)
         *      bit0 -> in play mode
         *      bit1 -> braking
         *      bit2-31 -> reserved
         *    int32 (current rendered frame number) -> can be used to detect if telemetry data is new
         *    int32 (view mode)
         *    int32 (current coaster)
         *    int32 (coaster style id)
         *    int32 (current train)
         *    int32 (current car)
         *    int32 (current seat)
         *    float32 (speed)
         *    float32 (Position x)
         *    float32 (Position y)
         *    float32 (Position z)
         *    float32 (Rotation quaternion x)
         *    float32 (Rotation quaternion y)
         *    float32 (Rotation quaternion z)
         *    float32 (Rotation quaternion w)
         *    float32 (G-Force x)
         *    float32 (G-Force y)
         *    float32 (G-Force z)   
         */
        private const int N_MSG_TELEMETRY = 6;

        /**
         * Message Enum
         * Type: Request   
         * Can be used by the client to request the number of coasters. The server will reply with MSG_INT_VALUE
         * DataSize = 0
         */
        private const int N_MSG_GET_COASTER_COUNT = 7;

        /**
         * Message Enum
         * Type: Reply   
         * Will be send by the server as an answer to messages requesting various numbers
         * DataSize = 4
         *    int32 (meaning depends on requested information)
         */
        private const int N_MSG_INT_VALUE = 8;

        /**
         * Message Enum
         * Type: Request   
         * Can be used by the client to request the name of a specific coaster. The server will reply with MSG_STRING
         * DataSize = 4
         *    int32 (coaster index 0..N-1), use MSG_GET_COASTER_COUNT to query the number of available coasters
         */
        private const int N_MSG_GET_COASTER_NAME = 9;

        /**
         * Message Enum 
         * Type: Reply
         * Will be send by the server as an answer to messages requesting various strings
         * DataSize = length of UTF8 encoded string
         *    UTF8 string
         */
        private const int N_MSG_STRING = 10;

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to request the current coaster and nearest station indices. The server will reply with MSG_INT_VALUE_PAIR
         * DataSize = 0
         */
        private const int N_MSG_GET_CURRENT_COASTER_AND_NEAREST_STATION = 11;

        /**
         * Message Enum
         * Type: Reply
         * Will be send by the server as an answer to messages requesting various value pairs
         * DataSize = 8
         *    int32 (first value)
         *    int32 (second value)
         */
        private const int N_MSG_INT_VALUE_PAIR = 12;

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to set the emergency stop. The server will reply with MSG_OK
         * DataSize = 5
         *    int32 (coaster index)
         *    uchar8 (1 = on, 0 = off)
         */
        private const int N_MSG_SET_EMERGENCY_STOP = 13;

        /**
         * Message Enum 
         * Type: Request
         * Can be used by the client to request the state of a specific station. The server will reply with MSG_STATION_STATE
         * DataSize = 8
         *    int32 (coaster index)
         *    int32 (station index (from MSG_GET_CURRENT_COASTER_AND_NEAREST_STATION))
         */
        private const int N_MSG_GET_STATION_STATE = 14; //size=8 (int32=coaster index, int32=station index)

        /**
         * Message Enum
         * Type: Reply
         * Will be send by server as an answer to a MSG_GET_STATION_STATE messge
         * DataSize = 4
         *    int32 (flags)
         *      bit0 -> E-Stop On/Off
         *      bit1 -> Manual Dispatch On/Off
         *      bit2 -> Can Dispatch
         *      bit3 -> Can Close Gates
         *      bit4 -> Can Open Gates
         *      bit5 -> Can Close Harness 
         *      bit6 -> Can Open Harness
         *      bit7 -> Can Raise Platform
         *      bit8 -> Can Lower Platform
         *      bit9 -> Can Lock Flyer Car
         *      bit10 -> Can Unlock Flyer Car
         *      bit11-31 -> reserved
         */
        private const int N_MSG_STATION_STATE = 15;

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to switch between manual and automatic station mode
         * DataSize = 9
         *    int32 (coaster index)
         *    int32 (station index)
         *    uchar8 (1 = on, 0 = off)
         */
        private const int N_MSG_SET_MANUAL_MODE = 16;

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to dispatch a train in manual mode
         * DataSize = 8
         *    int32 (coaster index)
         *    int32 (station index)
         */
        private const int N_MSG_DISPATCH = 17; // size=8 (int32 = coaster index, int32 = station index)

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to change gates in manual mode
         * DataSize = 9
         *    int32 (coaster index)
         *    int32 (station index)
         *    uchar8 (1 = on, 0 = off)
         */
        private const int N_MSG_SET_GATES = 18; // size=9 (int32= coaster index, int32= station index, byte= boolean (false=closed/true=open))

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to change harness in manual mode
         * DataSize = 9
         *    int32 (coaster index)
         *    int32 (station index)
         *    uchar8 (1 = on, 0 = off)
         */
        private const int N_MSG_SET_HARNESS = 19; // size=9 (int32= coaster index, int32= station index, byte= boolean (false=closed/true=open))

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to lower/raise platform in manual mode
         * DataSize = 9
         *    int32 (coaster index)
         *    int32 (station index)
         *    uchar8 (1 = on, 0 = off)
         */
        private const int N_MSG_SET_PLATFORM = 20; // size=9 (int32= coaster index, int32= station index, byte= boolean (false=raised/true=lowered))

        /**
         * Message Enum
         * Type: Request
         * Can be used by the client to lock/unlock flyer car in manual mode
         * DataSize = 9
         *    int32 (coaster index)
         *    int32 (station index)
         *    uchar8 (1 = on, 0 = off)
         */
        private const int N_MSG_SET_FLYER_CAR = 21; // size=9 (int32= coaster index, int32= station index, byte= boolean (false=locked/true=unlocked))

        /**
         * Start of extra size data within message
         */
        private const int c_nExtraSizeOffset = 9;

        /**
         * The request ID can be freely assigned by the client, we simply use an increasing counter.
         * The server will reply to request messages using the same id.
         * The request ID can be used to identify matching replys from the server to requests from the client.
         */
        private static int s_nRequestId;
        #endregion

        #region StationStatus
        public static Boolean manualDispatch = false;
        public static Boolean estop = false;
        public static Boolean canDispatch = false;
        public static Boolean cancloseGates = false;
        public static Boolean canOpenGates = false;
        public static Boolean cancloseHarness = false;
        public static Boolean canOpenHarness = false;
        public static Boolean canRaisePlatform = false;
        public static Boolean canLowerPlatform = false;
        public static Boolean canLockFlyerCar = false;
        public static Boolean canUnlockFlyercar = false;

        #endregion

        public TcpClient NL2Client;
        public MainWindow(TcpClient Client)
        {
            InitializeComponent();
            NL2Client = Client;
            //Get the Status of the Station and set the Variables
            GetStationStatus();
            //Disabled the buttons if manual mode is not on
            if (manualDispatch == false)
            {
                btn_Dispatch.IsEnabled = false;
                btn_ToggleFloor.IsEnabled = false;
                btn_ToggleFlyer.IsEnabled = false;
                btn_ToggleGates.IsEnabled = false;
                btn_ToggleHarness.IsEnabled = false;
            }


        }

        //Create Simple Message
        private static byte[] createSimpleMessage(int requestId, int msg)
        {
            byte[] bytes = new byte[10];
            bytes[0] = (byte)'N';
            encodeUShort16(bytes, 1, msg);
            encodeInt32(bytes, 3, requestId);
            encodeUShort16(bytes, 7, 0);
            bytes[9] = (byte)'L';
            return bytes;
        }

        //create Complex Message
        private static byte[] createComplexMessage(int requestId, int msg, int extraSize)
        {
            if (extraSize < 0 || extraSize > 65535) return null;
            byte[] bytes = new byte[10 + extraSize];
            bytes[0] = (byte)'N';
            encodeUShort16(bytes, 1, msg);
            encodeInt32(bytes, 3, requestId);
            encodeUShort16(bytes, 7, extraSize);
            bytes[9 + extraSize] = (byte)'L';
            return bytes;
        }

        //Convert the Issued Command into the byte array to send to the server
        private static byte[] decodeCommand(String instring)
        {
            if (instring ==("i") || instring ==("idle"))
    {
                return createSimpleMessage(s_nRequestId++, N_MSG_IDLE);
            }
    else if (instring ==("gv") || instring ==("getversion"))
    {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_VERSION);
            }
    else if (instring ==("gt") || instring ==("gettelemetry"))
    {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_TELEMETRY);
            }
    else if (instring ==("gcc") || instring ==("getcoastercount"))
    {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_COASTER_COUNT);
            }
    else if (instring ==("gcn") || instring ==("getcoastername"))
    {
                int nCoasterIndex = 0; // first coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_GET_COASTER_NAME, 4);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                return bytes;
            }
    else if (instring ==("gccns") || instring ==("getcurrentcoasterandneareststation"))
    {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_CURRENT_COASTER_AND_NEAREST_STATION);
            }
    else if (instring ==("seon") || instring ==("setemergencystopon"))
    {
                int nCoasterIndex = 0; // first coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_EMERGENCY_STOP, 5);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 4, true);
                return bytes;
            }
    else if (instring ==("seoff") || instring ==("setemergencystopoff"))
    {
                int nCoasterIndex = 0; // first coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_EMERGENCY_STOP, 5);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 4, false);
                return bytes;
            }
    else if (instring ==("gss") || instring ==("getstationstate"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_GET_STATION_STATE, 8);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                return bytes;
            } 
    else if (instring ==("setsmmon") || instring ==("setstationmanualmodeon"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_MANUAL_MODE, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, true);
                return bytes;
            }
    else if (instring ==("setsmmoff") || instring ==("setstationmanualmodeoff"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_MANUAL_MODE, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, false);
                return bytes;
            }
    else if (instring ==("d") || instring ==("dispatch"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_DISPATCH, 8);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                return bytes;
            }
    else if (instring ==("sgc") || instring ==("stationgatesclose"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_GATES, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, false);
                return bytes;
            }
    else if (instring ==("sgo") || instring ==("stationgatesopen"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_GATES, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, true);
                return bytes;
            }
    else if (instring ==("shc") || instring ==("stationharnessclose"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_HARNESS, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, false);
                return bytes;
            }
    else if (instring ==("sho") || instring ==("stationharnessopen"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_HARNESS, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, true);
                return bytes;
            }  
    else if (instring ==("spr") || instring ==("stationplatformraise"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_PLATFORM, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, false);
                return bytes;
            }
    else if (instring ==("spl") || instring ==("stationplatformlower"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_PLATFORM, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, true);
                return bytes;
            }
    else if (instring ==("sfl") || instring ==("stationflyercarlock"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_FLYER_CAR, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, false);
                return bytes;
            }
    else if (instring ==("sfu") || instring ==("stationflyercarunlock"))
    {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] bytes = createComplexMessage(s_nRequestId++, N_MSG_SET_FLYER_CAR, 9);
                encodeInt32(bytes, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(bytes, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(bytes, c_nExtraSizeOffset + 8, true);
                return bytes;
            }
            return null;
        }
        //Decode Recieved Messages
        private static void decodeMessage(byte[] bytes)
        {
            int len = bytes.Length;
            if (len >= 10)
            {
                int msg = decodeUShort16(bytes[1], bytes[2]);
                int requestId = decodeInt32(bytes, 3);
                int size = decodeUShort16(bytes[7], bytes[8]);
                if (size + 10 == len)
                {
                    Console.WriteLine("Server replied to request " + requestId + ": ");
                    switch (msg)
                    {
                        case N_MSG_IDLE:
                            Console.WriteLine("Idle");
                            break;
                        case N_MSG_OK:
                            Console.WriteLine("Ok");
                            break;
                        case N_MSG_ERROR:
                            {
                                
                                Console.WriteLine("Error: " + BitConverter.ToString(bytes, c_nExtraSizeOffset, size));
                            }
                            break;
                        case N_MSG_STRING:
                            {
                                Console.WriteLine("String: " + BitConverter.ToString(bytes, c_nExtraSizeOffset, size));
                            }
                            break;
                        case N_MSG_VERSION:
                            if (size == 4)
                            {
                                Console.WriteLine("Version: " + bytes[c_nExtraSizeOffset] + "." + bytes[c_nExtraSizeOffset + 1] + "." + bytes[c_nExtraSizeOffset + 2] + "." + bytes[c_nExtraSizeOffset + 3]);
                            }
                            break;
                        case N_MSG_TELEMETRY:
                            if (size == 76)
                            {
                                int state = decodeInt32(bytes, c_nExtraSizeOffset);

                                int frameNo = decodeInt32(bytes, c_nExtraSizeOffset + 4);

                                Boolean inPlay = (state & 1) != 0;
                                Boolean onboard = (state & 2) != 0;
                                Boolean paused = (state & 4) != 0;

                                int viewMode = decodeInt32(bytes, c_nExtraSizeOffset + 8);
                                int coasterIndex = decodeInt32(bytes, c_nExtraSizeOffset + 12);
                                int coasterStyleId = decodeInt32(bytes, c_nExtraSizeOffset + 16);
                                int currentTrain = decodeInt32(bytes, c_nExtraSizeOffset + 20);
                                int currentCar = decodeInt32(bytes, c_nExtraSizeOffset + 24);
                                int currentSeat = decodeInt32(bytes, c_nExtraSizeOffset + 28);
                                //float speed = decodeFloat(bytes, c_nExtraSizeOffset + 32);

                                Quaternion quat = new Quaternion();

                                //float posx = decodeFloat(bytes, c_nExtraSizeOffset + 36);
                                //float posy = decodeFloat(bytes, c_nExtraSizeOffset + 40);
                                //float posz = decodeFloat(bytes, c_nExtraSizeOffset + 44);

                                //quat.x = decodeFloat(bytes, c_nExtraSizeOffset + 48);
                                //quat.y = decodeFloat(bytes, c_nExtraSizeOffset + 52);
                                //quat.z = decodeFloat(bytes, c_nExtraSizeOffset + 56);
                                //quat.w = decodeFloat(bytes, c_nExtraSizeOffset + 60);

                                //float gforcex = decodeFloat(bytes, c_nExtraSizeOffset + 64);
                                //float gforcey = decodeFloat(bytes, c_nExtraSizeOffset + 68);
                                //float gforcez = decodeFloat(bytes, c_nExtraSizeOffset + 72);

                                //double pitch = Math.toDegrees(quat.toPitchFromYUp());
                                //double yaw = Math.toDegrees(quat.toYawFromYUp());
                                //double roll = Math.toDegrees(quat.toRollFromYUp());

                                Console.WriteLine("Telemetry:");

                                Console.WriteLine("  State: " + state);
                                if (inPlay)
                                {
                                    Console.WriteLine(" (Play Mode)");
                                }
                                if (onboard)
                                {
                                    Console.WriteLine(" (Onboard)");
                                }
                                if (paused)
                                {
                                    Console.WriteLine(" (Paused)");
                                }
                                Console.WriteLine("");

                                Console.WriteLine("  Frame Number: " + frameNo);

                                Console.WriteLine("  View Mode: " + viewMode);
                                if (viewMode == 1)
                                {
                                    Console.WriteLine(" (Ride View)");
                                }
                                Console.WriteLine("");

                                Console.WriteLine("  Coaster Index: " + coasterIndex);
                                Console.WriteLine("  Coaster Style Id: " + coasterStyleId);
                                Console.WriteLine("  Current Train Index: " + currentTrain);
                                Console.WriteLine("  Current Car Index: " + currentCar);
                                Console.WriteLine("  Current Seat Index: " + currentSeat);
                                //Console.WriteLine("  Speed: " + speed + "m/s");
                                //Console.WriteLine("  Position: " + posx + " " + posy + " " + posz);
                                //Console.WriteLine("  Pitch: " + pitch + "deg");
                                //Console.WriteLine("  Yaw: " + yaw + "deg");
                                //Console.WriteLine("  Roll: " + roll + "deg");
                                //Console.WriteLine("  G-forces: " + gforcex + " " + gforcey + " " + gforcez);
                            }
                            break;
                        case N_MSG_INT_VALUE:
                            if (size == 4)
                            {
                                Console.WriteLine("Int value: " + decodeInt32(bytes, c_nExtraSizeOffset));
                            }
                            break;
                        case N_MSG_INT_VALUE_PAIR:
                            if (size == 8)
                            {
                                Console.WriteLine("Int value pair: " + decodeInt32(bytes, c_nExtraSizeOffset) + ", " + decodeInt32(bytes, c_nExtraSizeOffset + 4));
                            }
                            break;
                        case N_MSG_STATION_STATE:
                            if (size == 4)
                            {
                                int nState = decodeInt32(bytes, c_nExtraSizeOffset);

                                Boolean bEStop = (nState & (1 << 0)) != 0;
                                Boolean bManualDispatch = (nState & (1 << 1)) != 0;
                                Boolean bCanDispatch = (nState & (1 << 2)) != 0;
                                Boolean bCanCloseGates = (nState & (1 << 3)) != 0;
                                Boolean bCanOpenGates = (nState & (1 << 4)) != 0;
                                Boolean bCanCloseHarness = (nState & (1 << 5)) != 0;
                                Boolean bCanOpenHarness = (nState & (1 << 6)) != 0;
                                Boolean bCanRaisePlatform = (nState & (1 << 7)) != 0;
                                Boolean bCanLowerPlatform = (nState & (1 << 8)) != 0;
                                Boolean bCanLockFlyerCar = (nState & (1 << 9)) != 0;
                                Boolean bCanUnlockFlyerCar = (nState & (1 << 10)) != 0;

                                estop = bEStop;
                                manualDispatch = bManualDispatch;
                                canDispatch = bCanDispatch;
                                canOpenGates = bCanOpenGates;
                                cancloseGates = bCanCloseGates;
                                cancloseHarness = bCanCloseHarness;
                                canOpenHarness = bCanOpenHarness;
                                canLowerPlatform = bCanLowerPlatform;
                                canRaisePlatform = bCanRaisePlatform;
                                canUnlockFlyercar = bCanUnlockFlyerCar;
                                canLockFlyerCar = bCanLockFlyerCar;


                                Console.WriteLine("Station state: ");
                                Console.WriteLine("    E-Stop: " + bEStop);
                                Console.WriteLine("    Manual Dispatch: " + bManualDispatch);
                                Console.WriteLine("    Can Dispatch: " + bCanDispatch);
                                Console.WriteLine("    Can Close Gates: " + bCanCloseGates);
                                Console.WriteLine("    Can Open Gates: " + bCanOpenGates);
                                Console.WriteLine("    Can Close Harness: " + bCanCloseHarness);
                                Console.WriteLine("    Can Open Harness: " + bCanOpenHarness);
                                Console.WriteLine("    Can Raise Platform: " + bCanRaisePlatform);
                                Console.WriteLine("    Can Lower Platform: " + bCanLowerPlatform);
                                Console.WriteLine("    Can Lock Flyer Car: " + bCanLockFlyerCar);
                                Console.WriteLine("    Can Unlock Flyer Car: " + bCanUnlockFlyerCar);
                            }
                            break;
                        default:
                            Console.WriteLine("Unknown message");
                            break;
                    }
                }
            }
        }
        //Read the data from the stream
        private static byte[] readMessage(BinaryReader input)
  {
            int prefix = input.Read();
            if (prefix != (int)'N')
            {
                if (prefix != -1)
                {
                    throw new Exception("Invalid message received");
                }
                else
                {
                    throw new Exception("No data from server");
                }
            }

            int b1 = input.Read();
            if (b1 == -1)
            {
                throw new Exception("No data from server");
            }
            int b2 = input.Read();
            if (b2 == -1)
            {
                throw new Exception("No data from server");
            }
            int b3 = input.Read();
            if (b3 == -1)
            {
                throw new Exception("No data from server");
            }
            int b4 = input.Read();
            if (b4 == -1)
            {
                throw new Exception("No data from server");
            }

            int b5 = input.Read();
            if (b5 == -1)
            {
                throw new Exception("No data from server");
            }

            int b6 = input.Read();
            if (b6 == -1)
            {
                throw new Exception("No data from server");
            }

            int b7 = input.Read();
            if (b7 == -1)
            {
                throw new Exception("No data from server");
            }

            int b8 = input.Read();
            if (b8 == -1)
            {
                throw new Exception("No data from server");
            }

            int extraSize = decodeUShort16((byte)b7, (byte)b8);

            byte[] bytes = new byte[10 + extraSize];

            bytes[0] = (byte)prefix;
            bytes[1] = (byte)b1;
            bytes[2] = (byte)b2;
            bytes[3] = (byte)b3;
            bytes[4] = (byte)b4;
            bytes[5] = (byte)b5;
            bytes[6] = (byte)b6;
            bytes[7] = (byte)b7;
            bytes[8] = (byte)b8;

            for (int i = 0; i < extraSize; ++i)
            {
                int b = input.Read();
                if (b == -1)
                {
                    throw new Exception("No data from server");
                }
                bytes[9 + i] = (byte)b;
            }

            int postfix = input.Read();
            if (postfix != (int)'L')
            {
                if (postfix != -1)
                {
                    throw new Exception("Invalid message received");
                }
                else
                {
                    throw new Exception("No data from server");
                }
            }

            bytes[9 + extraSize] = (byte)postfix;

            return bytes;
        }


        //Everything below here is used for converting datatypes into byte arrayys
        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        private static void encodeUShort16(byte[] bytes, int offset, int n)
        {
            bytes[offset] = (byte)((n >> 8) & 0xFF);
            bytes[offset + 1] = (byte)(n & 0xFF);
        }

        private static void encodeInt32(byte[] bytes, int offset, int n)
        {
            bytes[offset] = (byte)((n >> 24) & 0xFF);
            bytes[offset + 1] = (byte)((n >> 16) & 0xFF);
            bytes[offset + 2] = (byte)((n >> 8) & 0xFF);
            bytes[offset + 3] = (byte)(n & 0xFF);
        }

        private static void encodeBoolean(byte[] bytes, int offset, Boolean b)
        {
            bytes[offset] = (byte)(b ? 1 : 0);
        }

        private static int decodeUShort16(byte b1, byte b2)
        {
            int n1 = (((int)b1) & 0xFF) << 8;
            int n2 = ((int)b2) & 0xFF;
            return n1 | n2;
        }

        private static int decodeInt32(byte[] bytes, int offset)
        {
            int n1 = (((int)bytes[offset]) & 0xFF) << 24;
            int n2 = (((int)bytes[offset + 1]) & 0xFF) << 16;
            int n3 = (((int)bytes[offset + 2]) & 0xFF) << 8;
            int n4 = ((int)bytes[offset + 3]) & 0xFF;
            return n1 | n2 | n3 | n4;
        }

        private static Boolean decodeBoolean(byte[] bytes, int offset)
        {
            return bytes[offset] != 0;
        }

        //Everything below here is the actually functions of the program

        //Send the Idle Message
        private void btn_SendIdle_Click(object sender, RoutedEventArgs e)
        {
            //Get the Stream to send and recieve data
            Byte[] data = decodeCommand("idle");

            NetworkStream stream = NL2Client.GetStream();

            stream.Write(data, 0, data.Length);

            //make a buffer to store the response 
            var reader = new BinaryReader(stream);
            data = readMessage(reader);

            //Int32 bytes = stream.Read(data, 0, data.Length);
            //Decode the recieved message from the server
            decodeMessage(data);

        }

        //Toggle Que Gates
        private void btn_ToggleGates_Click(object sender, RoutedEventArgs e)
        {
            NetworkStream stream = NL2Client.GetStream();
            //Open the gates on if the can be opened
            if (canOpenGates == true)
            {
                //send the command to close the gates
                Byte[] data = decodeCommand("sgo");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                //toggle the gate switch
                canOpenGates = false;
                cancloseGates = true;

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(5000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });



            }
            else if (canOpenGates ==false && cancloseGates == true)
            {
                //Send the command to close the gates
                Byte[] data = decodeCommand("sgc");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                canOpenGates = true;
                cancloseGates = false;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(5000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });
            }

            else
            {
                //if somehow the status is messed up get the status of the station again
                GetStationStatus();
            }

        }

        //Toggle the Harness
        private void btn_ToggleHarness_Click(object sender, RoutedEventArgs e)
        {
            NetworkStream stream = NL2Client.GetStream();
            //Open the gates on if the can be opened
            if (canOpenHarness == true)
            {
                //send the command to close the gates
                Byte[] data = decodeCommand("sho");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                //toggle the gate switch
                canOpenHarness = false;
                cancloseHarness = true;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(3000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });


            }
            else if (canOpenHarness == false && cancloseHarness == true)
            {
                //Send the command to close the gates
                Byte[] data = decodeCommand("shc");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                canOpenHarness = true;
                cancloseHarness = false;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(3000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });
            }

            else
            {
                //if somehow the status is messed up get the status of the station again
                GetStationStatus();
            }
        }

        //Toggle Platform
        private void btn_ToggleFloor_Click(object sender, RoutedEventArgs e)
        {
            NetworkStream stream = NL2Client.GetStream();
            //Open the gates on if the can be opened
            if (canRaisePlatform == true)
            {
                //send the command to close the gates
                Byte[] data = decodeCommand("spr");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                //toggle the gate switch
                canRaisePlatform = false;
                canLowerPlatform = true;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(10000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });



            }
            else if (canLowerPlatform == true && canRaisePlatform == false)
            {
                //Send the command to close the gates
                Byte[] data = decodeCommand("spl");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                canRaisePlatform = true;
                canLowerPlatform = false;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(10000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });
            }

            else
            {
                //if somehow the status is messed up get the status of the station again
                GetStationStatus();
            }
        }

        //toggle Flyer position
        private void btn_ToggleFlyer_Click(object sender, RoutedEventArgs e)
        {
            NetworkStream stream = NL2Client.GetStream();
            //Open the gates on if the can be opened
            if (canLockFlyerCar == true)
            {
                //send the command to close the gates
                Byte[] data = decodeCommand("sfl");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                //toggle the gate switch
                canLockFlyerCar = false;
                canUnlockFlyercar = true;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(10000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });


            }
            else if (canLockFlyerCar == false && canUnlockFlyercar == true)
            {
                //Send the command to close the gates
                Byte[] data = decodeCommand("sfu");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                canLockFlyerCar = true;
                canUnlockFlyercar = false;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(10000);
                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });
            }

            else
            {
                //if somehow the status is messed up get the status of the station again
                GetStationStatus();
            }
        }

        //toggle E-Stop
        private void btn_Estop_Click(object sender, RoutedEventArgs e)
        {
            NetworkStream stream = NL2Client.GetStream();
            //Open the gates on if the can be opened
            if (estop == true)
            {
                //send the command to close the gates
                Byte[] data = decodeCommand("seoff");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                //toggle the gate switch
                estop = false;
                GetStationStatus();


            }
            else if (estop == false)
            {
                //Send the command to close the gates
                Byte[] data = decodeCommand("seon");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                estop = true;
                GetStationStatus();
            }

            else
            {
                //if somehow the status is messed up get the status of the station again
                GetStationStatus();
            }
        }

        //Dispatch Train
        private void btn_Dispatch_ClickAsync(object sender, RoutedEventArgs e)
        {
            NetworkStream stream = NL2Client.GetStream();
            //Open the gates on if the can be opened
            if (canDispatch == true)
            {
                //send the command to close the gates
                Byte[] data = decodeCommand("d");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                //we dispatched wait for the next train to enter the station before enableing buttons again
                canDispatch = false;
                canOpenGates = false;
                canRaisePlatform = false;
                canUnlockFlyercar = false;
                canOpenHarness = false;
                btn_Dispatch.IsEnabled = false;
                btn_ToggleFloor.IsEnabled = false;
                btn_ToggleFlyer.IsEnabled = false;
                btn_ToggleGates.IsEnabled = false;
                btn_ToggleHarness.IsEnabled = false;

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(15000);

                    GetStationStatus();
                    // Do things here.
                    // NOTE: You may need to invoke this to your main thread depending on what you're doing
                });

            }
            else
            {
                GetStationStatus();
            }
        }

        //toggle manual mode
        private void btn_ToggleManual_Click(object sender, RoutedEventArgs e)
        {
            NetworkStream stream = NL2Client.GetStream();
            //Open the gates on if the can be opened
            if (manualDispatch == true)
            {
                //send the command to close the gates
                Byte[] data = decodeCommand("setsmmoff");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                //toggle the gate switch
                manualDispatch = false;
                btn_Dispatch.IsEnabled = false;
                btn_ToggleFloor.IsEnabled = false;
                btn_ToggleFlyer.IsEnabled = false;
                btn_ToggleGates.IsEnabled = false;
                btn_ToggleHarness.IsEnabled = false;


            }
            else if (manualDispatch == false)
            {
                //Send the command to close the gates
                Byte[] data = decodeCommand("setsmmon");
                stream.Write(data, 0, data.Length);
                //make a buffer to store the response 
                var reader = new BinaryReader(stream);
                data = readMessage(reader);
                decodeMessage(data);

                manualDispatch = true;
                btn_Dispatch.IsEnabled = true;
                btn_ToggleFloor.IsEnabled = true;
                btn_ToggleFlyer.IsEnabled = true;
                btn_ToggleGates.IsEnabled = true;
                btn_ToggleHarness.IsEnabled = true;
            }

            else
            {
                //if somehow the status is messed up get the status of the station again
                GetStationStatus();
            }
        }

        //Get the Station Status
        private void GetStationStatus()
        {
            //Get the Status
            Byte[] data = decodeCommand("gss");

            NetworkStream stream = NL2Client.GetStream();

            stream.Write(data, 0, data.Length);
            //make a buffer to store the response 
            var reader = new BinaryReader(stream);
            data = readMessage(reader);

            //Decode the recieved message from the server
            decodeMessage(data);

            //if we can dispatch enable the button otherwise disable it.
            if (canDispatch == true)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    btn_Dispatch.IsEnabled = true;
                }));

            }
            else if (canDispatch == false)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    btn_Dispatch.IsEnabled = false;
                }));
            }

            //Hide Buttons depending on the coaster type
            if (rb_sit.IsChecked == true)
            {
                btn_ToggleFloor.Visibility = Visibility.Hidden;
                btn_ToggleFlyer.Visibility = Visibility.Hidden;
            }
            
            else if (rb_Flr.IsChecked == true)
            {
                btn_ToggleFloor.Visibility = Visibility.Visible;
                btn_ToggleFlyer.Visibility = Visibility.Hidden;
            }

            else if (rb_flyer.IsChecked == true)
            {
                btn_ToggleFloor.Visibility = Visibility.Hidden;
                btn_ToggleFlyer.Visibility = Visibility.Visible;
            }

            if (cancloseGates || canOpenGates == true)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    btn_ToggleGates.IsEnabled = true;
                }));
            }

            if (canOpenHarness || cancloseHarness == true)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    btn_ToggleHarness.IsEnabled = true;
                }));
            }

            if (canRaisePlatform || canLowerPlatform == true)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    btn_ToggleFloor.IsEnabled = true;
                }));
            }

            if (canUnlockFlyercar || canLockFlyerCar == true)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {//this refer to form in WPF application 
                    btn_ToggleFlyer.IsEnabled = true;
                }));
            }

            this.Dispatcher.Invoke((Action)(() =>
            {//this refer to form in WPF application 
                if (btn_ToggleFloor.IsEnabled == false && btn_ToggleGates.IsEnabled == false && btn_ToggleHarness.IsEnabled == false && btn_Dispatch.IsEnabled == false && manualDispatch == true && rb_Flr.IsChecked == true)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(4000);

                        GetStationStatus();
                        // Do things here.
                        // NOTE: You may need to invoke this to your main thread depending on what you're doing
                    });
                }

                if (btn_ToggleFlyer.IsEnabled == false && btn_ToggleGates.IsEnabled == false && btn_ToggleHarness.IsEnabled == false && btn_Dispatch.IsEnabled == false && manualDispatch == true && rb_flyer.IsChecked == true)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(4000);

                        GetStationStatus();
                        // Do things here.
                        // NOTE: You may need to invoke this to your main thread depending on what you're doing
                    });
                }

                if (btn_ToggleGates.IsEnabled == false && btn_ToggleHarness.IsEnabled == false && btn_Dispatch.IsEnabled == false && manualDispatch == true && rb_sit.IsChecked == true)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(4000);

                        GetStationStatus();
                        // Do things here.
                        // NOTE: You may need to invoke this to your main thread depending on what you're doing
                    });
                }
            }));
            //if all the command buttons are disabled (Due to dispatch) wait 2 seconds and check again
           

        }

        private void CheckChanged(object sender, RoutedEventArgs e)
        {
            GetStationStatus();
        }
    }
}
