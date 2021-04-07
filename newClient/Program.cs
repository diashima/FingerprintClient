using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using libzkfpcsharp;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Client
{
    public class Program
    {   
        static string fingerprintBrowser = "1";
        static string fingerprintClient = "";

        static byte[] BrowserTmp = new byte[2048];
        static byte[] ClientTmp = new byte[2048];

        static bool testBool = false;        
        static byte[] FPBuffer;
        static byte[][] RegTmps = new byte[3][];
        static byte[] RegTmp = new byte[2048];
        static byte[] CapTmp = new byte[2048];
        static int cbCapTmp = 2048;
        static int cbRegTmp = 0;
        static IntPtr mDBHandle = IntPtr.Zero;
        static IntPtr mDevHandle = IntPtr.Zero;

        private static int mfpWidth = 0;
        private static int mfpHeight = 0;
        private static int mfpDpi = 0;

        static bool bIsTimeToDie = false;    
        
        public class Fingerprint
        {
            public string Command { get; set; }
            public string Data { get; set; }
        }
        

        public class TestClass : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {                
                Sessions.Broadcast(e.Data);
            }
        }

        public static void Main(string[] args)
        {            
            var wssv = new WebSocketServer("ws://localhost:8005");
            wssv.AddWebSocketService<TestClass>("/device");
            wssv.Start();

            int ret = zkfperrdef.ZKFP_ERR_OK;
            zkfp2.Init();    
            

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };


            using (var ws = new WebSocket("ws://localhost:8005/device"))
            {
                ws.OnMessage += (sender, e) =>
                {
                    Fingerprint fingerprint = JsonConvert.DeserializeObject<Fingerprint>(e.Data);
                    switch (fingerprint.Command)
                    {
                        case "open_device":
                            mDevHandle = zkfp2.OpenDevice(0);
                            mDBHandle = zkfp2.DBInit();

                            for (int i = 0; i < 3; i++)
                            {
                                RegTmps[i] = new byte[2048];
                            }
                            byte[] paramValue = new byte[4];
                            int size = 4;
                            zkfp2.GetParameters(mDevHandle, 1, paramValue, ref size);
                            zkfp2.ByteArray2Int(paramValue, ref mfpWidth);

                            size = 4;
                            zkfp2.GetParameters(mDevHandle, 2, paramValue, ref size);
                            zkfp2.ByteArray2Int(paramValue, ref mfpHeight);

                            FPBuffer = new byte[mfpWidth * mfpHeight];

                            size = 4;
                            zkfp2.GetParameters(mDevHandle, 3, paramValue, ref size);
                            zkfp2.ByteArray2Int(paramValue, ref mfpDpi);
                            Thread captureThread = new Thread(new ThreadStart(DoCapture));
                            captureThread.IsBackground = true;
                            captureThread.Start();
                            break;

                        /*case "register":
                            *//*int ret = zkfp.ZKFP_ERR_OK;
                            int fid = 0, score = 0;
                            if (RegisterCount > 0 && zkfp2.DBMatch(mDBHandle, CapTmp, RegTmps[RegisterCount - 1]) <= 0)
                            {
                                ws.Send("Приложите палец ещё 3 раза для регистрации");
                                return;
                            }*//*
                            testBool = false;
                            switch (fingerprint.Data)
                            {
                                case "1":
                                    while (!testBool)
                                    {
                                    }
                                    Array.Copy(CapTmp, RegTmps[0], cbCapTmp);
                                    Dictionary<string, string> gotFirst = new Dictionary<string, string>
                                    {
                                        {"command", "got_first" },
                                        {"data", "" }
                                    };
                                    string jsonFirst = JsonConvert.SerializeObject(gotFirst, Formatting.Indented);
                                    ws.Send(jsonFirst);
                                    break;
                                case "2":
                                    while (!testBool)
                                    {
                                    }
                                    Array.Copy(CapTmp, RegTmps[1], cbCapTmp);
                                    Dictionary<string, string> gotSecond = new Dictionary<string, string>
                                    {
                                        {"command", "got_second" },
                                        {"data", "" }
                                    };
                                    string jsonSecond = JsonConvert.SerializeObject(gotSecond, Formatting.Indented);
                                    ws.Send(jsonSecond);
                                    break;
                                case "3":
                                    while (!testBool)
                                    {
                                    }
                                    Array.Copy(CapTmp, RegTmps[2], cbCapTmp);
                                    if (zkfp.ZKFP_ERR_OK == (ret = zkfp2.DBMerge(mDBHandle, RegTmps[0], RegTmps[1], RegTmps[2], RegTmp, ref cbRegTmp)))
                                    {
                                        Dictionary<string, string> registered = new Dictionary<string, string>
                                        {
                                            {"command", "registered" },
                                            {"data", zkfp2.BlobToBase64(RegTmp, cbRegTmp) }
                                        };
                                        string jsonRegistered = JsonConvert.SerializeObject(registered, Formatting.Indented);
                                        ws.Send(jsonRegistered);                                        
                                    }
                                    break;
                            }
                            break;             */                                    

                        case "match":                            
                            fingerprintBrowser = fingerprint.Data;
                            Console.WriteLine("Browser: " + fingerprintBrowser + ", client: " + fingerprintClient);
                            break;

                        case "do_matching":
                            testBool = false;
                            while (!testBool)
                            {
                            }
                            if (fingerprintBrowser != "1")
                            {
                                BrowserTmp = zkfp.Base64String2Blob(fingerprintBrowser);
                                ret = zkfp2.DBMatch(mDBHandle, BrowserTmp, CapTmp);
                                Dictionary<string, string> info = new Dictionary<string, string>
                                {
                                    {"command", "match_finished" },
                                    {"data", ret.ToString() }
                                };
                                string json = JsonConvert.SerializeObject(info, Formatting.Indented);
                                ws.Send(json);
                            }
                            break;

                        case "register":                           
                            testBool = false;
                            while (!testBool)
                            {
                            }
                            Array.Copy(CapTmp, RegTmps[0], cbCapTmp);
                            Dictionary<string, string> gotFirst = new Dictionary<string, string>
                            {
                                {"command", "got_first" },
                                {"data", "" }
                            };
                            testBool = false;
                            string jsonFirst = JsonConvert.SerializeObject(gotFirst, Formatting.Indented);
                            ws.Send(jsonFirst);
                            while (!testBool)
                            {
                            }
                            Array.Copy(CapTmp, RegTmps[1], cbCapTmp);
                            Dictionary<string, string> gotSecond = new Dictionary<string, string>
                            {
                                {"command", "got_second" },
                                {"data", "" }
                            };
                            testBool = false;
                            string jsonSecond = JsonConvert.SerializeObject(gotSecond, Formatting.Indented);
                            ws.Send(jsonSecond);
                            while (!testBool)
                            {
                            }
                            Array.Copy(CapTmp, RegTmps[2], cbCapTmp);
                            if (zkfp.ZKFP_ERR_OK == (ret = zkfp2.DBMerge(mDBHandle, RegTmps[0], RegTmps[1], RegTmps[2], RegTmp, ref cbRegTmp)))
                            {
                                Dictionary<string, string> registered = new Dictionary<string, string>
                                        {
                                            {"command", "registered" },
                                            {"data", zkfp2.BlobToBase64(RegTmp, cbRegTmp) }
                                        };
                                string jsonRegistered = JsonConvert.SerializeObject(registered, Formatting.Indented);
                                ws.Send(jsonRegistered);
                            }
                            break;

                        default:
                            break;
                    }
                };

                ws.Connect();
                Console.ReadKey(true);
                wssv.Stop();
            }            
        }

        private static void DoCapture()
        {
            while (!bIsTimeToDie)
            {                
                cbCapTmp = 2048;
                int ret = zkfp2.AcquireFingerprint(mDevHandle, FPBuffer, CapTmp, ref cbCapTmp);                
                if (ret == zkfp.ZKFP_ERR_OK)
                {
                    Console.WriteLine("Acquired");
                    testBool = true;
                }
                Thread.Sleep(200);
            }            
        }
    }
}