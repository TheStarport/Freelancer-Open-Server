using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace FLOpenServerProxy
{
    // todo: log rotations
    // todo: add ip/port to protocol traces

    class FLServerProxyListener
    {
        /// <summary>
        /// The freelancer instance GUID. Copied from the real freelancer server application.
        /// </summary>
        public static byte[] ApplicationInstanceGUID = { 0xa8, 0xc6, 0x27, 0x1d, 0x41, 0x66, 0xd8, 0x49, 0x89, 0xeb, 0x1e, 0xbc, 0x42, 0x21, 0xca, 0xe9 };

        /// <summary>
        /// The dplay GUID. Copied from the real freelancer server application.
        /// </summary>
        public static byte[] ApplicationGUID = { 0x26, 0xf0, 0x90, 0xa6, 0xf0, 0x26, 0x57, 0x4e, 0xac, 0xa0, 0xec, 0xf8, 0x68, 0xe4, 0x8d, 0x21 };
        
        /// <summary>
        /// Socket to recieve and send comms to clients.
        /// </summary>
        UdpClient socket;

        /// <summary>
        /// Logger thread
        /// </summary>
        Logger log;

        public class GameSession
        {
            public string name = "";
            public IPEndPoint client_endpoint;
            public UdpClient server_socket;
            public DateTime last_client_rx_time = DateTime.UtcNow;
            public DateTime start_time = DateTime.UtcNow;
            public int bytes_rx;
        }

        /// <summary>
        /// This is a map of remote ip/ports of the clients that have connected to us.
        /// </summary>
        public Dictionary<IPEndPoint, GameSession> fsessions = new Dictionary<IPEndPoint, GameSession>();

        /// <summary>
        /// Configuration defaults
        /// </summary>
        string log_path = "";
        string status_path = "";
        bool log_packets = false;
        bool emulate_enum = false;
        string server_ip = "188.40.133.201";
        int server_port = 2302;
        int listen_port = 2302;
        IPEndPoint server_endpoint;
        bool accept_new_connections = true;

        public FLServerProxyListener(bool silent)
        {
            // Read config and dump to console
            ReadConfig(silent);

            if (!silent)
            {
                Console.WriteLine("log_path=" + log_path);
                Console.WriteLine("status_path=" + status_path);
                Console.WriteLine("log_packets=" + log_packets);
                Console.WriteLine("emulate_enum=" + emulate_enum);
                Console.WriteLine("server_ip=" + server_ip);
                Console.WriteLine("server_port=" + server_port);
                Console.WriteLine("listen_port=" + listen_port);
                Console.WriteLine("accept_new_connections=" + accept_new_connections);
            }

            server_endpoint = new IPEndPoint(IPAddress.Parse(server_ip), server_port);

            // Apply iocntl to ignore ICMP responses from hosts when we send them
            // traffic otherwise the socket chucks and exception and dies. Ignore this
            // under mono.
            const int SIO_UDP_CONNRESET = -1744830452;

            // Start it up.
            log = new Logger(log_path);
            socket = new UdpClient(listen_port);
            try
            {
                socket.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }
            catch { }
            socket.BeginReceive(new AsyncCallback(RxFromClient), null);

            if (!silent)
            {
                log.AddLog("Startup complete: Listening for connections");
                Console.WriteLine("Startup complete: Listening for connections");
            }

            // Time out dead connections so that their ports can be reused
            while (true)
            {

                DateTime last_valid_rx_time = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10));

                Dictionary<IPEndPoint, GameSession> fsessions_copy;
                lock (fsessions)
                {
                    List<IPEndPoint> dead_list = new List<IPEndPoint>();
                    foreach (KeyValuePair<IPEndPoint,GameSession> kv in fsessions)
                    {
                        GameSession fs = kv.Value;
                        if (kv.Value.server_socket == null)
                        {
                            dead_list.Add(kv.Key);
                        }
                        else if (kv.Value.last_client_rx_time < last_valid_rx_time)
                        {
                            try
                            {
                                kv.Value.server_socket.Close();
                                kv.Value.server_socket = null;
                            }
                            catch { }
                            dead_list.Add(kv.Key);
                        }
                    }

                    foreach (IPEndPoint dead in dead_list)
                        fsessions.Remove(dead);

                    fsessions_copy = new Dictionary<IPEndPoint, GameSession>(fsessions);
                }

                StreamWriter writer = null;
                try
                {
                    if (status_path.Length > 0)
                    {
                        writer = File.CreateText(status_path);
                        writer.WriteLine("* SUMMARY time={0} active connections={1}", DateTime.UtcNow, fsessions.Count);
                        foreach (KeyValuePair<IPEndPoint, GameSession> kv in fsessions_copy)
                        {
                            GameSession fs = kv.Value;
                            TimeSpan connected_time = DateTime.UtcNow - fs.start_time;
                            double kbyte = fs.bytes_rx / 1000.0;
                            double kbyte_per_sec = kbyte / connected_time.TotalSeconds;
                            writer.WriteLine(String.Format("{0,30} {1,12} {2,6:0.00} KB {3,6:0.00} KB/s", fs.name, connected_time.TotalSeconds, kbyte, kbyte_per_sec));
                        }
                        writer.Close();
                    }

                }
                catch { }

                ReadConfig(true);

                System.Threading.Thread.Sleep(1000);
            }
        }

        private void ReadConfig(bool silent)
        {   
            try
            {
                FLDataFile cfg = new FLDataFile("flproxy.cfg", false);
                if (cfg.SettingExists("general", "log_path"))
                    log_path = cfg.GetSetting("general", "log_path").Str(0);
                if (cfg.SettingExists("general", "status_path"))
                    status_path = cfg.GetSetting("general", "status_path").Str(0);
                if (cfg.SettingExists("general", "log_packets"))
                    log_packets = cfg.GetSetting("general", "log_packets").Str(0) == "yes";
                if (cfg.SettingExists("general", "emulate_enum"))
                    emulate_enum = cfg.GetSetting("general", "emulate_enum").Str(0) == "yes";
                if (cfg.SettingExists("general", "server_ip"))
                    server_ip = cfg.GetSetting("general", "server_ip").Str(0);
                if (cfg.SettingExists("general", "server_port"))
                    server_port = (int)cfg.GetSetting("general", "server_port").UInt(0);
                if (cfg.SettingExists("general", "listen_port"))
                    listen_port = (int)cfg.GetSetting("general", "listen_port").UInt(0);
                if (cfg.SettingExists("general", "accept_new_connections"))
                    accept_new_connections = cfg.GetSetting("general", "accept_new_connections").Str(0) == "yes";
            }
            catch (Exception e)
            {
                if (!silent)
                    Console.WriteLine(e.Message);
            }
        }

        private string HexToAscii(byte[] pkt)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < pkt.Length; i++)
                sb.AppendFormat("{0:X2}:", pkt[i]);
            return sb.ToString();
        }

        /// <summary>
        /// Receive traffic from the server destined to a client.
        /// </summary>
        /// <param name="result"></param>
        public void RxFromServer(IAsyncResult result)
        {
            GameSession fs = result.AsyncState as GameSession;
            try
            {
                // Receive the data and start listening again.                
                IPEndPoint remoteEP = null;
                byte[] pkt = fs.server_socket.EndReceive(result, ref remoteEP);
                fs.server_socket.BeginReceive(new AsyncCallback(RxFromServer), fs);

                // Log it
                if (log_packets)
                    log.AddLog("s>c: " + HexToAscii(pkt));

                // Forward the traffic to the client
                socket.BeginSend(pkt, pkt.Length, fs.client_endpoint, new AsyncCallback(TxComplete), socket);
            }
            catch
            {
                try { fs.server_socket.Close(); }
                catch { }
                fs.server_socket = null;
            }
        }

        /// <summary>
        /// Receive traffic from the any client. Either reply immediately or
        /// forward it to the game server for processing.
        /// </summary>
        /// <param name="result"></param>
        public void RxFromClient(IAsyncResult result)
        {
            GameSession fs;
            
            // receive the data
            IPEndPoint remoteEP = null;
            byte[] pkt = null;
            
            pkt = socket.EndReceive(result, ref remoteEP);
            socket.BeginReceive(new AsyncCallback(RxFromClient), null);
            
            if (pkt == null)
                log.AddLog("connected abort " + remoteEP);

            // log it
            if (log_packets)
                log.AddLog("c>s: " + HexToAscii(pkt));

            // If this message is too short, chuck it away
            if (pkt.Length < 2)
                return;

            // If this message is a enum server status then reply to the query. This tricks people
            // into thinking the server has a better ping than it does.
            int pos = 0;
            uint type = FLMsgType.GetUInt8(pkt, ref pos);
            uint cmd = FLMsgType.GetUInt8(pkt, ref pos);
            if (type == 0x00 && cmd == 0x02 && pkt.Length < 4)
            {
                uint enum_payload = FLMsgType.GetUInt16(pkt, ref pos);
                // log.AddEvent("c>p: CMD_DP_ENUM_QUERY enum_payload={0}", enum_payload);
                if (emulate_enum)
                {
                    SendCmdEnumResponse(remoteEP, (ushort)enum_payload);
                    return;
                }
            }

            // This must be a standard FL messaage. Create the remote end point 
            // in our session table.
            lock (fsessions)
            {
                if (!fsessions.ContainsKey(remoteEP))
                {
                    if (!accept_new_connections)
                        return;

                    fs = new GameSession();
                    fsessions.Add(remoteEP, fs);
                }
                else
                {
                    fs = fsessions[remoteEP];
                }
            }

            // If this has a dead server connection, create a unique udp port to
            // listen on for server replies.
            if (fs.server_socket == null)
            {
                // this socket is for sending back to the client
                fs.client_endpoint = remoteEP;

                // this socket is for sending and receiving from the server
                fs.server_socket = new UdpClient(0);
                fs.server_socket.Connect(server_endpoint);

                int local_sport = (fs.server_socket.Client.LocalEndPoint as IPEndPoint).Port;
                fs.name = String.Format("{0}:{1} {2}", remoteEP.Address, remoteEP.Port, local_sport);

                log.AddLog(String.Format("new connection ip={0}", fs.name));
                fs.server_socket.BeginReceive(new AsyncCallback(RxFromServer), fs);

            }

            fs.last_client_rx_time = DateTime.UtcNow;
            fs.bytes_rx += pkt.Length;

            try
            {
                fs.server_socket.BeginSend(pkt, pkt.Length, new AsyncCallback(TxComplete), fs.server_socket);
            }
            catch { }
        }

        public void SendCmdEnumResponse(IPEndPoint remoteEP, ushort enum_payload)
        {
            bool password = false;
            bool nodpnsvr = true;
            uint max_players = 10000;
            uint curr_players = 0;

            string server_id = "1e22ff3b-c9f126c3-4cd2d60a-3c70be70";
            string session_name = "Discovery Freelancer RP 24/7\0";
            string server_description = "This is a the official 4.87 development test server\0";

            uint major = 48;
            uint minor = 60;
            uint patch = 0;
            uint build = 6;
            uint version = (major << 24) | (minor << 16) | (patch << 8) | build;

            byte[] application_data = new byte[0];
            FLMsgType.AddAsciiStringLen0(ref application_data, "1:1:" + version.ToString() + ":-1910309061:" + server_id + ":");
            FLMsgType.AddUnicodeStringLen0(ref application_data, server_description);

            byte[] pkt = { 0x00, 0x03 };
            FLMsgType.AddUInt16(ref pkt, enum_payload);
            FLMsgType.AddUInt32(ref pkt, 0x58 + (uint)session_name.Length * 2); // ReplyOffset
            FLMsgType.AddUInt32(ref pkt, (uint)application_data.Length); // ReplySize/ResponseSize
            FLMsgType.AddUInt32(ref pkt, 0x50); // ApplicationDescSize
            FLMsgType.AddUInt32(ref pkt, (password ? 0x80u : 0x00u) | (nodpnsvr ? 0x40u : 0x00u)); // ApplicationDescFlags
            FLMsgType.AddUInt32(ref pkt, max_players); // MaxPlayers
            FLMsgType.AddUInt32(ref pkt, curr_players); // CurrentPlayers
            FLMsgType.AddUInt32(ref pkt, 0x58); // SessionNameOffset
            FLMsgType.AddUInt32(ref pkt, (uint)session_name.Length * 2); // SessionNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // PasswordOffset
            FLMsgType.AddUInt32(ref pkt, 0); // PasswordSize
            FLMsgType.AddUInt32(ref pkt, 0); // ReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // ReservedDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // ApplicationReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // ApplicationReservedDataSize
            FLMsgType.AddArray(ref pkt, ApplicationInstanceGUID); // ApplicationInstanceGUID
            FLMsgType.AddArray(ref pkt, ApplicationGUID); // ApplicationGUID
            FLMsgType.AddUnicodeStringLen0(ref pkt, session_name); // SessionName
            FLMsgType.AddArray(ref pkt, application_data); // ApplicationData

            try
            {
                socket.BeginSend(pkt, pkt.Length, remoteEP, new AsyncCallback(TxComplete), socket);
                if (log_packets)
                    log.AddLog("s>c: " + HexToAscii(pkt));
            }
            catch { }
        }

        public void TxComplete(IAsyncResult result)
        {
            try
            {
                UdpClient socket = result.AsyncState as UdpClient;
                socket.EndSend(result);
            }
            catch { }
        }
    }
}
