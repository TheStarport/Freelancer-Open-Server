using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FOS_ng.Logging;
using FOS_ng.Player;

namespace FOS_ng.DirectplayServer
{
    /// <summary>
    ///     This is a bastardised implementation of the directplay protocol version identified as 0x10004
    ///     and only for the parts used by freelancer.
    ///     Handles low-level comms, checks the messages' corruptance and passes events to higher-level server.
    ///     TODO: fixme: implement packet coalescence to reduce the number of small packets transferred. This'll
    ///     be important when we've got lots of ship updates running.
    /// </summary>
    public class DirectplayServer
    {

        #region "FL consts"
        /// <summary>
        ///     The freelancer instance GUID. Copied from the real freelancer server application.
        /// </summary>
        public static byte[] ApplicationInstanceGUID =
        {
            0xa8, 0xc6, 0x27, 0x1d, 0x41, 0x66, 0xd8, 0x49, 0x89, 0xeb, 0x1e,
            0xbc, 0x42, 0x21, 0xca, 0xe9
        };

        /// <summary>
        ///     The dplay GUID. Copied from the real freelancer server application.
        /// </summary>
        public static byte[] ApplicationGUID =
        {
            0x26, 0xf0, 0x90, 0xa6, 0xf0, 0x26, 0x57, 0x4e, 0xac, 0xa0, 0xec, 0xf8,
            0x68, 0xe4, 0x8d, 0x21
        };
        #endregion

        #region "Events"
        public event EventHandler<Session> PlayerConnected;

        public delegate void DelegateGotMsg(Session sess, byte[] message);
        public event DelegateGotMsg GotMessage;

        public delegate void DelegateDestr(Session sess, string reason);
        public event DelegateDestr PlayerDestroyed;

        protected virtual void OnPlayerDestroyed(Session sess, string reason)
        {
            IdtoSessions.Remove(sess.DPlayID);
            DelegateDestr handler = PlayerDestroyed;
            if (handler != null) handler(sess, reason);
        }

        protected virtual void OnGotMessage(Session sess, byte[] message)
        {
            DelegateGotMsg handler = GotMessage;
            if (handler != null) handler(sess, message);
        }

        protected virtual void OnPlayerConnected(Session e)
        {
            IdtoSessions.Add(e.DPlayID,e);
            EventHandler<Session> handler = PlayerConnected;
            if (handler != null) handler(this, e);
        }
        #endregion

        /// <summary>
        ///     This is a map of remote ip/ports of the clients that have connected to us.
        /// </summary>
        private readonly Dictionary<IPEndPoint, Session> dplay_sessions = new Dictionary<IPEndPoint, Session>();

        /// <summary>
        ///     This is a map of DPlay ID to player sessions.
        /// </summary>
        private readonly Dictionary<uint, Session> IdtoSessions = new Dictionary<uint, Session>();

        /// <summary>
        /// </summary>
        //private readonly Server _server;

        /// <summary>
        ///     Socket to recieve and send comms to clients.
        /// </summary>
        private readonly UdpListener _socket;

        /// <summary>
        ///     Set this to the maximum number of players.
        /// </summary>
        public uint MaxPlayers = 10000;

        private Random rand = new Random();

        #region "Server settings"
        /// <summary>
        ///     Set this to the server description as shown on the server selection screen.
        ///     The description must be null terminated.
        /// </summary>
        public string ServerDescription = "\0";

        /// <summary>
        ///     Set this to the ID of the server.
        /// </summary>
        public string ServerID = "00000000-00000000-00000000-00000000";

        /// <summary>
        ///     Set this to the name of the server.
        ///     The name must be null terminated.
        /// </summary>
        public string ServerName = "\0";

        /// <summary>
        ///     The server version integer encoded as a string.
        /// </summary>
        public string ServerVersion = "0";
        #endregion

        /// <summary>
        ///     A new directplay server listening on the specified port.
        /// </summary>
        /// <param name="port">The port to listen to connections on</param>
        public DirectplayServer(int port)
        {
            //_server = server;

            // Apply iocntl to ignore ICMP responses from hosts when we send them
            // traffic otherwise the socket chucks and exception and dies. Ignore this
            // under mono.


            //create a new server
            _socket = new UdpListener(port);


            //start listening for messages and copy the messages back to the client
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var received = await _socket.Receive();
                    //listener.Reply("copy " + received.Message, received.Sender);
                    //if (received.Message == "quit")
                        //break;
                    //TODO: init another thread here?
                    ProcessPktFromClient(received.Message, received.Sender);
                }
            });

        }

        /// <summary>
        ///     Call this to shutdown the dplay server.
        /// </summary>
        public void Dispose()
        {
            //running = false;

            lock (dplay_sessions)
            {
                var dplaySessionsCopy = new Dictionary<IPEndPoint, Session>(dplay_sessions);
                foreach (var sess in dplaySessionsCopy.Values)
                    Destroy(sess, "dplay shutdown");
            }

            try
            {
                _socket.Close();
            }
            catch
            {
            }
        }


        public void ProcessPktFromClient(byte[] pkt, IPEndPoint client)
        {
            //utrack: i'm too young to debug this shit sry. maybe sometime with absinthe
            //TODO: make separate packet parser\checker
            Logger.AddLog(LogType.DplayMsg, "c>s client={0} pkt={1}", client, pkt);

            // If this message is too short, chuck it away
            if (pkt.Length < 2)
                return;

            // If this message is a enum server status then reply to the query. This tricks people
            // into thinking the server has a better ping than it does.
            var pos = 0;
            uint cmd = FLMsgType.GetUInt8(pkt, ref pos);
            if (cmd == 0x00 && pkt.Length >= 4)
            {
                uint opcode = FLMsgType.GetUInt8(pkt, ref pos);
                if (opcode != 0x02 || pkt.Length < 4) return;

                uint enumPayload = FLMsgType.GetUInt16(pkt, ref pos);
                SendCmdEnumResponse(client, (ushort)enumPayload);
            }

                // If the data is at least 12 bytes and the first byte is 
            // either 0x80 or 0x88 (PACKET_COMMAND_CFRAME or PACKET_COMMAND_CFRAME |
            // PACKET_COMMAND_POLL), it MUST process the message as a CFRAME 
            // (section 3.1.5.1) command frame.
            else if ((cmd == 0x80 || cmd == 0x88) && pkt.Length >= 12)
            {
                uint opcode = FLMsgType.GetUInt8(pkt, ref pos);

                // The CONNECT packet is used to request a connection. If accepted, the response 
                // is a CONNECTED (section 2.2.1.2) packet
                switch (opcode)
                {
                    case 0x01:
                    {
                        byte msg_id = FLMsgType.GetUInt8(pkt, ref pos);
                        //byte rsp_id = FLMsgType.GetUInt8(pkt, ref pos);

                        //uint version = FLMsgType.GetUInt32(pkt, ref pos);
                        uint dplayid = FLMsgType.GetUInt32(pkt, ref pos);
                        //uint timestamp = FLMsgType.GetUInt32(pkt, ref pos);

                        // Create a new session.
                        var sess = GetSession(client);
                        if (sess == null)
                        {
                            sess = new Session(client) {DPlayID = dplayid};
                            lock (dplay_sessions)
                            {
                                dplay_sessions[client] = sess;
                            }
                        }

                        lock (sess)
                        {
                            // If the session id has changed, assume that the server is wrong
                            // and kill the existing connection and start a new one.
                            // This behaviour differs from the dplay specification.
                            if (sess.DPlayID != 0 && sess.DPlayID != dplayid)
                            {
                                Destroy(sess, "changed dsessid");
                            }

                            // If the session is fully connected because the client has
                            // sent us a connect acknowledge then ignore this.
                            if (sess.SessionState == Session.State.Connected)
                                return;

                            // Otherwise this is a new connection. Reset the session information.
                            sess.SessionState = Session.State.Connecting;
                            sess.LastClientRxTime = DateTime.UtcNow;
                            sess.StartTime = DateTime.Now;

                            sess.Rtt = 200;
                            sess.LostRx = 0;
                            sess.BytesRx = 0;
                            sess.LostTx = 0;
                            sess.BytesTx = 0;

                            sess.NextRxSeq = 0;
                            sess.NextTxSeq = 0;

                            sess.MsgID = 0;
                            sess.OutOfOrder.Clear();
                            sess.UserData.Clear();
                            sess.UserDataPendingAck.Clear();

                            sess.MultipleDframePacket = false;
                            sess.SessionTimer = new Timer(SessionTimer, sess, 100, 20);

                            SendCmdConnectAccept(sess, msg_id);
                        }
                    }
                        break;
                    case 0x06:
                    {
                        byte flags = FLMsgType.GetUInt8(pkt, ref pos);
                        byte retry = FLMsgType.GetUInt8(pkt, ref pos);
                        // The seq field indicates the seq of the next message that the client will send.
                        byte seq = FLMsgType.GetUInt8(pkt, ref pos);
                        // The next_rx field indicates the message seq that the client is waiting to receive
                        byte nrcv = FLMsgType.GetUInt8(pkt, ref pos);
                        pos += 2; // skip padding
                        uint timestamp = FLMsgType.GetUInt32(pkt, ref pos);

                        // Ignore packets for sessions that don't exist
                        Session sess = GetSession(client);
                        if (sess == null)
                            return;

                        lock (sess)
                        {
                            sess.LastClientRxTime = DateTime.UtcNow;
                            sess.BytesRx += pkt.Length;

                            // If the hi sack mask is present, resend any requested packets.
                            if ((flags & 0x02) == 0x02)
                            {
                                uint mask = FLMsgType.GetUInt32(pkt, ref pos);
                                DoRetryOnSACKMask(sess, mask, nrcv);
                            }

                            // If the hi sack mask is present, resend any requested packets.
                            if ((flags & 0x04) == 0x04)
                            {
                                uint mask = FLMsgType.GetUInt32(pkt, ref pos);
                                DoRetryOnSACKMask(sess, mask, (byte)(nrcv + 32));
                            }

                            // At this point bSeq sequence ID is valid, the bNRcv field 
                            // is to be inspected. All previously sent TRANS_USERDATA_HEADER packets that 
                            // are covered by the bNRcv sequence ID, that is, those packets that had been sent
                            // with bSeq values less than bNRcv (accounting for 8-bit counter wrapping) are 
                            // acknowledged. These packets do not have to be remembered any longer, and their 
                            // retry timers can be canceled.
                            DoAcknowledgeUserData(sess, nrcv);

                            // Try to send data if there's data waiting to be sent and send a 
                            // selective acknowledgement if we didn't sent a dframe and the client
                            // requested an acknowledgement.
                            if (!SendDFrame(sess) && cmd == 0x88)
                            {
                                SendCmdSACK(sess);
                            }
                        }
                    }
                        break;
                }
            }

                // If a packet arrives, the recipient SHOULD first check whether 
            // it is large enough to be a minimal data frame (DFRAME) (4 bytes)
            // and whether the first byte has the low bit (PACKET_COMMAND_DATA) set.
            else if ((cmd & 0x01) == 0x01 && pkt.Length >= 4)
            {
                uint control = FLMsgType.GetUInt8(pkt, ref pos);
                byte seq = FLMsgType.GetUInt8(pkt, ref pos);
                byte nrcv = FLMsgType.GetUInt8(pkt, ref pos);

                // Ignore packets for sessions that don't exist
                Session sess = GetSession(client);
                if (sess == null)
                    return;

                lock (sess)
                {
                    sess.LastClientRxTime = DateTime.UtcNow;
                    sess.BytesRx += pkt.Length;

                    // This is a disconnect. We ignore the soft disconnect and immediately
                    // drop the session repeating the disconnect a few times to improve the
                    // probability of it getting through.
                    if ((control & 0x08) == 0x08)
                    {
                        Destroy(sess, "client request");
                        return;
                    }

                    // TRANS_USERDATA_HEADER bSeq field MUST be either the next sequence
                    // ID expected or within 63 packets beyond the ID expected by the receiver.
                    // If the sequence ID is not within this range, the payload MUST be ignored.
                    // In addition, a SACK packet SHOULD be sent indicating the expected sequence ID.
                    if (!InWindow(seq, sess.NextRxSeq))
                    {
                        SendCmdSACK(sess);
                        return;
                    }

                    // If the sequence ID is out of order, but still within 63 packets,
                    // the receiver SHOULD queue the payload until it receives either:
                    // - A delayed or retried transmission of the missing packet or packets,
                    // and can now process the sequence in order.
                    // - A subsequent packet with a send mask indicating that the missing
                    // packet or packets did not use PACKET_COMMAND_RELIABLE and will never
                    // be retried. Therefore, the receiver should advance its sequence as if 
                    // it had already received and processed the packets.
                    if (seq != sess.NextRxSeq)
                    {
                        Logger.AddLog(LogType.DplayMsg,
                            "c>s out of order pkt received client={0} queuing seq={1:X} next_rx_seq={2:X}",
                            sess.Client, seq, sess.NextRxSeq);
                        sess.OutOfOrder[seq] = pkt;
                        SendCmdSACK(sess);
                        return;
                    }

                    //Test code to simulate packet loss
                    //if (rand.Next(5) == 1)
                    //{
                    //    Logger.AddLog(String.Format("c>s: DROPPING THE PACKET NOW {0:X}", seq));
                    //    return;
                    //}

                    // Note if this was a retried dframe.
                    if ((control & 0x01) == 0x01)
                    {
                        sess.LostRx++;
                    }

                    // When one or both of the optional SACK mask 32-bit fields is present, and one
                    // or more bits are set in the fields, the sender is indicating that it received a
                    // packet or packets out of order, presumably due to packet loss. The two 32-bit,
                    // little-endian fields MUST be considered as one 64-bit field, where dwSACKMask1
                    // is the low 32 bits and dwSACKMask2 is the high 32 bits. If either 32-bit field
                    // is not available, the entire contents of the 64-bit field MUST be considered as all 0.

                    // The receiver of a SACK mask SHOULD loop through each bit of the combined 64-bit value
                    // in the ascending order of significance. Each bit corresponds to a sequence ID after
                    // bNRcv. If the bit is set, it indicates that the corresponding packet was received
                    // out of order.

                    // The receiver of a SACK mask SHOULD shorten the retry timer for the first frame of
                    // the window to speed recovery from the packet loss. The recommended duration is 
                    // 10 milliseconds. This value can be modified according to application and network
                    // requirements. The receiver MAY also choose to remove the selectively acknowledged 
                    // packets from its list to retry.
                    if ((control & 0x10) == 0x10)
                    {
                        uint mask = FLMsgType.GetUInt32(pkt, ref pos);
                        DoRetryOnSACKMask(sess, mask, nrcv);
                    }
                    if ((control & 0x20) == 0x20)
                    {
                        uint mask = FLMsgType.GetUInt32(pkt, ref pos);
                        DoRetryOnSACKMask(sess, mask, (byte)(nrcv + 32));
                    }


                    // When one or both of the optional send mask 32-bit fields is present, and one or
                    // more bits are set the fields, the sender is indicating that it sent a packet or
                    // packets that were not marked as reliable and did not receive an acknowledgement yet.
                    // The two 32-bit, little-endian fields MUST be considered as one 64-bit field, where
                    // dwSendMask1 is the low 32 bits and dwSendMask2 is the high 32 bits. If either 32-bit
                    // field is not available, the entire contents of the 64-bit field MUST be considered 
                    // as all 0.

                    // The receiver of a send mask SHOULD loop through each bit of the combined 64-bit
                    // value from the least significant bit to the most significant in little-endian byte
                    // order. Each bit corresponds to a sequence ID prior to bSeq, and if that is the bit
                    // that is set, it indicates that the corresponding packet was not sent reliably and 
                    // will not be retried. If the recipient of the send mask had not received the packet
                    // and had not already processed a send mask that identified the sequence ID, it SHOULD
                    // consider the packet as dropped and release its placeholder in the sequence. That is,
                    // any sequential messages that could not be indicated because of the gap in the sequence
                    // where the packet that was not marked as reliable had been SHOULD now be reported to
                    // the upper layer.
                    if ((control & 0x40) == 0x40)
                        FLMsgType.GetUInt32(pkt, ref pos);
                    if ((control & 0x80) == 0x80)
                        FLMsgType.GetUInt32(pkt, ref pos);
                    // However, freelancer always uses reliable packets and so ignore sendmasks.

                    // At this point, we've received the packet we wanted to. Advance the sequence number count
                    // and process this message.
                    sess.NextRxSeq++;
                    ProcessTransUserData(sess, pkt, pos);

                    // If there are queued out of order packets, try to process these.
                    while (sess.OutOfOrder.ContainsKey(sess.NextRxSeq))
                    {
                        Logger.AddLog(LogType.DplayMsg, "c>s unqueuing out of order pkt client={0} seq={1:X}", sess.Client,
                            sess.NextRxSeq);
                        pkt = sess.OutOfOrder[sess.NextRxSeq];
                        sess.OutOfOrder.Remove(sess.NextRxSeq);
                        sess.NextRxSeq++;
                        ProcessTransUserData(sess, pkt, pos); // fixme: pos could be wrong if we received a sack mask
                    }

                    // At this point bSeq sequence ID is valid, the bNRcv field 
                    // is to be inspected. All previously sent TRANS_USERDATA_HEADER packets that 
                    // are covered by the bNRcv sequence ID, that is, those packets that had been sent
                    // with bSeq values less than bNRcv (accounting for 8-bit counter wrapping) are 
                    // acknowledged. These packets do not have to be remembered any longer, and their 
                    // retry timers can be canceled.
                    DoAcknowledgeUserData(sess, nrcv);

                    // We always do an immediate acknowledge as bandwidth isn't a particular concern
                    // but fast recovery from lost packets is.
                    if (!SendDFrame(sess))
                    {
                        SendCmdSACK(sess);
                    }
                }
            }
        }

        //// are covered by the bNRcv sequence ID, that is, those packets that had been sent
        /// <summary>
        ///     All previously sent TRANS_USERDATA_HEADER packets that
        ///     with bSeq values less than bNRcv (accounting for 8-bit counter wrapping) are
        ///     acknowledged. These packets do not have to be remembered any longer, and their
        ///     retry timers can be canceled.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="nrcv"></param>
        private void DoAcknowledgeUserData(Session sess, byte nrcv)
        {
            lock (sess)
            {
                var first_possible_seq = (byte)(nrcv - 64);
                for (var i = (byte)(nrcv - 1); i != first_possible_seq; i--)
                {
                    if (sess.UserDataPendingAck.ContainsKey(i))
                    {
                        // string seqs = "[";
                        // foreach (byte key in sess.user_data_pending_ack.Keys)
                        //    seqs += String.Format("{0:X} ", key);
                        // seqs += "]";

                        //Logger.AddLog(LogType.DplayMsg, "c>s ack received client={0} seq={1:X} nrcv={2:X} user_data_pending_ack={3}",
                        //    sess.client, i, nrcv, seqs);

                        // Calculate the rtt deducting 20 ms for internal processing delays.
                        sess.Rtt =
                            (uint)((DateTime.UtcNow - sess.UserDataPendingAck[i].SendTime).TotalMilliseconds) - 20;
                        if (sess.Rtt < 0)
                            sess.Rtt = 0;

                        sess.UserDataPendingAck.Remove(i);
                    }
                    if (sess.UserDataPendingAck.Count == 0)
                    {
                        sess.MultipleDframePacket = false;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Resend packets starting from seq if the corresponding bit in the mask
        ///     field is set.
        /// </summary>
        private void DoRetryOnSACKMask(Session sess, uint mask, byte seq)
        {
            lock (sess)
            {
                Logger.AddLog(LogType.DplayMsg,
                    "s>c retry on sack mask client={0} seq={1:X} mask={2:X} user_data_pending_ack={3}",
                    sess.Client, seq, mask, sess.UserDataPendingAck.Count);

                string seqs = "user_data_pending_ack=[";
                foreach (byte key in sess.UserDataPendingAck.Keys)
                    seqs += String.Format("{0:X} ", key);
                seqs += "]";
                Logger.AddLog(LogType.DplayMsg, seqs);

                byte resend_seq = seq;
                for (int i = 1; i != 0; i <<= 1, resend_seq++)
                {
                    if ((mask & i) == i)
                    {
                        if (sess.UserDataPendingAck.ContainsKey(resend_seq))
                        {
                            Session.Pkt pkt = sess.UserDataPendingAck[resend_seq];

                            pkt.RetryCount++;
                            if (pkt.RetryCount < 3)
                                pkt.RetryTime = DateTime.UtcNow.AddMilliseconds(200 + 50 * pkt.RetryCount);
                            else
                                pkt.RetryTime =
                                    DateTime.UtcNow.AddMilliseconds(200 + 50 * pkt.RetryCount * pkt.RetryCount);

                            pkt.SendTime = DateTime.UtcNow;

                            Logger.AddLog(LogType.DplayMsg, "s>c send retry client={0} seq={1:X} retry_count={2}",
                                sess.Client, pkt.Data[2], pkt.RetryCount);
                            pkt.Data[1] |= 0x01;
                            pkt.Data[3] = sess.NextRxSeq;
                            sess.BytesTx += pkt.Data.Length;
                            TxStart(pkt.Data, sess.Client);

                            sess.LostTx++;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Checks that the sequence ID is expected or within 63 packets beyond the ID expected
        /// </summary>
        /// <param name="seqid"></param>
        /// <param name="expected_seq"></param>
        /// <returns></returns>
        private static bool InWindow(byte seqid, byte expected_seq)
        {
            if (expected_seq <= 192)
            {
                if (seqid >= expected_seq && seqid <= expected_seq + 63)
                {
                    return true;
                }
            }
            else
            {
                if (seqid >= expected_seq || seqid < 256 - expected_seq)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Process trans user data control messages.
        /// </summary>
        /// <param name="pkt"></param>
        /// <param name="pos"></param>
        private void ProcessTransUserData(Session sess, byte[] pkt, int pos)
        {
            // If the user 1 flag is set then this is a session management message.
            // Do not pass to the higher layers but rather process locally.
            if ((pkt[0] & 0x40) == 0x40)
            {
                uint type = FLMsgType.GetUInt32(pkt, ref pos);
                // On receipt of a trans user data session info we send back a dummy
                // entry that's just sufficient to let the client's dplay implementation
                // to recognise us.
                if (type == 0xC1)
                {
                    if (sess.SessionState != Session.State.Connecting)
                        return;

                    sess.SessionState = Session.State.ConnectingSessinfo;
                    SendTudSessionInfo(sess);
                }
                // On receipt of a trans connect acknowledge we declare this connection
                // to be valid to the higher layer.
                else if (type == 0xC3)
                {
                    if (sess.SessionState != Session.State.ConnectingSessinfo)
                        return;

                    sess.SessionState = Session.State.Connected;
                    OnPlayerConnected(sess);
                }
            }
            // If this is a complete message then pass up.
            else if ((pkt[0] & 0x30) == 0x30)
            {
                if ((pkt.Length - pos) <= 0) return;
                var msg = FLMsgType.GetArray(pkt, ref pos, pkt.Length - pos);
                OnGotMessage(sess, msg);
            }
            // Otherwise we don't support this. Flag the error.
            else
            {
                Logger.AddLog(LogType.Error, "c>s unsupported message client={0} pkt={1}", sess.Client, pkt);
            }
        }

        /// <summary>
        ///     Send a dummy trans
        /// </summary>
        /// <param name="sess"></param>
        private void SendTudSessionInfo(Session sess)
        {
            var pkt = new byte[0];
            FLMsgType.AddUInt32(ref pkt, 0xC2); // dwPacketType
            FLMsgType.AddUInt32(ref pkt, 0); // dwReplyOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwReplySize

            FLMsgType.AddUInt32(ref pkt, 0x50); // dwApplicationDescSize
            FLMsgType.AddUInt32(ref pkt, 0x01); // dwFlags
            FLMsgType.AddUInt32(ref pkt, MaxPlayers + 1); // dwMaxPlayers
            FLMsgType.AddUInt32(ref pkt, (uint)dplay_sessions.Count + 1); // dwCurrentPlayers
            FLMsgType.AddUInt32(ref pkt, 0x6C + 0x60); // dwSessionNameOffset
            FLMsgType.AddUInt32(ref pkt, (uint)ServerName.Length * 2); // dwSessionNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwPasswordOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwPasswordSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwReservedDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwApplicationReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwApplicationReservedDataSize
            FLMsgType.AddArray(ref pkt, ApplicationInstanceGUID);
            FLMsgType.AddArray(ref pkt, ApplicationGUID);
            FLMsgType.AddUInt32(ref pkt, sess.DPlayID); // dpnid
            FLMsgType.AddUInt32(ref pkt, sess.DPlayID); // dwVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwVersionNotUsed
            FLMsgType.AddUInt32(ref pkt, 2); // dwEntryCount
            FLMsgType.AddUInt32(ref pkt, 0); // dwMembershipCount

            // server name table entry
            FLMsgType.AddUInt32(ref pkt, 1); // dpnid
            FLMsgType.AddUInt32(ref pkt, 0); // dpnidOwner
            FLMsgType.AddUInt32(ref pkt, 0x000402); // dwFlags
            FLMsgType.AddUInt32(ref pkt, 2); // dwVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwVersionNotUsed
            FLMsgType.AddUInt32(ref pkt, 7); // dwDNETVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLSize

            // connecting client name table entry
            FLMsgType.AddUInt32(ref pkt, sess.DPlayID); // dpnid
            FLMsgType.AddUInt32(ref pkt, 0); // dpnidOwner
            FLMsgType.AddUInt32(ref pkt, 0x020000); // dwFlags
            FLMsgType.AddUInt32(ref pkt, sess.DPlayID); // dwVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwVersionNotUsed
            FLMsgType.AddUInt32(ref pkt, 7); // dwDNETVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLSize

            FLMsgType.AddUnicodeStringLen0(ref pkt, ServerName);

            sess.UserData.Enqueue(pkt);
            SendDFrame(sess);
        }

        /// <summary>
        ///     Return the current session for the client end point.
        /// </summary>
        /// <param name="client"></param>
        /// <returns>Return null if the session doesn't exist.</returns>
        private Session GetSession(IPEndPoint client)
        {
            lock (dplay_sessions)
            {
                if (dplay_sessions.ContainsKey(client))
                    return dplay_sessions[client];
            }
            return null;
        }

        /// <summary>
        ///     Queues user data for transmission to a client or group within the session. The message can be sent synchronously
        ///     or asynchronously.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="pkt"></param>
        /// <returns></returns>
        public bool SendTo(Session sess, byte[] pkt)
        {

                sess.UserData.Enqueue(pkt);
                lock (sess)
                {
                SendDFrame(sess);
                }
                return true;

        }

        /// <summary>
        ///     Reply to a enum query from freelancer clients with our server information.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="enum_payload"></param>
        public void SendCmdEnumResponse(IPEndPoint client, ushort enum_payload)
        {
            // TODO: we could need it
            const bool password = false;
            const bool nodpnsvr = true;

            var application_data = new byte[0];
            FLMsgType.AddAsciiStringLen0(ref application_data,
                "1:1:" + ServerVersion + ":-1910309061:" + ServerID + ":");
            FLMsgType.AddUnicodeStringLen0(ref application_data, ServerDescription);

            byte[] pkt = { 0x00, 0x03 };
            FLMsgType.AddUInt16(ref pkt, enum_payload);
            FLMsgType.AddUInt32(ref pkt, 0x58 + (uint)ServerName.Length * 2); // ReplyOffset
            FLMsgType.AddUInt32(ref pkt, (uint)application_data.Length); // ReplySize/ResponseSize
            FLMsgType.AddUInt32(ref pkt, 0x50); // ApplicationDescSize
            FLMsgType.AddUInt32(ref pkt, (password ? 0x80u : 0x00u) | (nodpnsvr ? 0x40u : 0x00u));
            // ApplicationDescFlags
            FLMsgType.AddUInt32(ref pkt, MaxPlayers + 1); // MaxPlayers
            FLMsgType.AddUInt32(ref pkt, (uint)dplay_sessions.Count + 1); // CurrentPlayers
            FLMsgType.AddUInt32(ref pkt, 0x58); // SessionNameOffset
            FLMsgType.AddUInt32(ref pkt, (uint)ServerName.Length * 2); // SessionNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // PasswordOffset
            FLMsgType.AddUInt32(ref pkt, 0); // PasswordSize
            FLMsgType.AddUInt32(ref pkt, 0); // ReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // ReservedDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // ApplicationReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // ApplicationReservedDataSize
            FLMsgType.AddArray(ref pkt, ApplicationInstanceGUID); // ApplicationInstanceGUID
            FLMsgType.AddArray(ref pkt, ApplicationGUID); // ApplicationGUID
            FLMsgType.AddUnicodeStringLen0(ref pkt, ServerName); // SessionName
            FLMsgType.AddArray(ref pkt, application_data); // ApplicationData

            TxStart(pkt, client);
        }

        /// <summary>
        ///     Send a connection accept to the client
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="rsp_id"></param>
        public void SendCmdConnectAccept(Session sess, byte rsp_id)
        {
            byte[] pkt = { 0x88, 0x02 };
            FLMsgType.AddUInt8(ref pkt, sess.MsgID++);
            FLMsgType.AddUInt8(ref pkt, rsp_id);
            FLMsgType.AddUInt32(ref pkt, 0x10004);
            FLMsgType.AddUInt32(ref pkt, sess.DPlayID);
            FLMsgType.AddUInt32(ref pkt, (uint)Stopwatch.GetTimestamp());

            sess.BytesTx += pkt.Length;
            TxStart(pkt, sess.Client);
        }

        /// <summary>
        ///     Send a SACK setting the sack mask flags to ask for retries if necessary
        /// </summary>
        /// <param name="sess"></param>
        public void SendCmdSACK(Session sess)
        {
            byte[] pkt = { 0x80, 0x06 };
            FLMsgType.AddUInt8(ref pkt, 0x00); // bFlags
            FLMsgType.AddUInt8(ref pkt, 0x00); // bRetry
            FLMsgType.AddUInt8(ref pkt, sess.NextTxSeq); // bNSeq
            FLMsgType.AddUInt8(ref pkt, sess.NextRxSeq); // bNRcv
            FLMsgType.AddUInt16(ref pkt, 0); // padding
            FLMsgType.AddUInt32(ref pkt, (uint)Stopwatch.GetTimestamp()); // timestamp

            // If there are queued out of order packets then set the SACK mask.
            // The receiver of a SACK mask will loop through each bit of the combined 64-bit value
            // in the ascending order of significance. Each bit corresponds to a sequence ID after
            // bNRcv. If the bit is set, it indicates that the corresponding packet was received
            // out of order.
            if (sess.OutOfOrder.Count > 0)
            {
                ulong sack_mask = 0;

                var last_possible_rx_seq = (byte)(sess.NextRxSeq + 64);
                for (byte seq = sess.NextRxSeq; seq <= last_possible_rx_seq; seq++)
                {
                    if (sess.OutOfOrder.ContainsKey(seq))
                        sack_mask |= (1u << seq);
                }

                // Set the sack flags to indicate that a sack mask is in this message
                // and add the mask to the message.
                pkt[2] |= 0x06;
                FLMsgType.AddUInt32(ref pkt, (uint)(sack_mask & 0xFFFFFFFF));
                FLMsgType.AddUInt32(ref pkt, (uint)((sack_mask >> 32) & 0xFFFFFFFF));
            }

            sess.BytesTx += pkt.Length;
            TxStart(pkt, sess.Client);
        }

        /// <summary>
        ///     Send a hard disconnect to the freelancer client.
        /// </summary>
        /// <param name="sess"></param>
        public void SendCmdHardDisconnect(Session sess)
        {
            byte[] pkt = { 0x80, 0x04 };
            FLMsgType.AddUInt8(ref pkt, sess.MsgID++);
            FLMsgType.AddUInt8(ref pkt, 0);
            FLMsgType.AddUInt32(ref pkt, 0x10004);
            FLMsgType.AddUInt32(ref pkt, sess.DPlayID);
            FLMsgType.AddUInt32(ref pkt, (uint)Stopwatch.GetTimestamp());

            sess.BytesTx += pkt.Length;
            TxStart(pkt, sess.Client);
        }

        /// <summary>
        ///     Try to send a dframe or a keep alive if no user data is waiting to be sent.
        /// </summary>
        /// <param name="sess"></param>
        /// <returns>Return true if a dframe was sent</returns>
        public bool SendDFrame(Session sess)
        {
            lock (sess)
            {
                if (sess.UserData.Count > 0)
                {
                    // If the window is full, don't send any thing
                    if (IsAckWindowFull(sess))
                        return false;

                    // If we have sent a user data message that had to be carried in multiple
                    // dframes then stop sending. We can't have more than one of these on the
                    // wire at one time (if I've intepreted the specs correctly).
                    if (sess.MultipleDframePacket)
                        return false;

                    // The retry time should start at 100 + rtt * 2.5 according to the specs but we use
                    // 2 as this is a round number.
                    uint retryTime = 100 + (sess.Rtt * 2);
                    while (sess.UserData.Count > 0)
                    {
                        byte[] ud = sess.UserData.First();
                        sess.UserData.Dequeue();

                        // Break the user data block into sizes smaller than the ethernet mtu. We
                        // assume an MTU of 1450 as some infrastructure steals some bytes.
                        int offset = 0;
                        bool firstPacket = true;
                        bool lastPacket = false;
                        while (offset < ud.Length)
                        {
                            int length;
                            if ((ud.Length - offset) > 1450)
                            {
                                length = 1450;
                                sess.MultipleDframePacket = true;
                            }
                            else
                            {
                                length = ud.Length - offset;
                                lastPacket = true;
                            }

                            byte[] pkt = { 0x07, 0x00, sess.NextTxSeq, sess.NextRxSeq };

                            // If this is the first packet, set the flag to indicate this.
                            if (firstPacket)
                                pkt[0] |= 0x10;

                            // If this is the last packet, set the flag to indicate this
                            if (lastPacket)
                                pkt[0] |= 0x20;

                            // If the session isn't fully connected then this must be a session establishment
                            // message
                            if (sess.SessionState == Session.State.ConnectingSessinfo)
                                pkt[0] |= 0x40;

                            FLMsgType.AddArray(ref pkt, ud, offset, length);

                            var spkt = new Session.Pkt
                            {
                                Data = pkt,
                                RetryTime = DateTime.UtcNow.AddMilliseconds(retryTime),
                                SendTime = DateTime.UtcNow
                            };

                            sess.UserDataPendingAck[sess.NextTxSeq] = spkt;
                            sess.BytesTx += pkt.Length;
                            TxStart(pkt, sess.Client);

                            // Increase the retry times if multiple packets are sent so that
                            // we're less likely to send a massive burst of packets to retry.
                            retryTime += 5;

                            sess.NextTxSeq++;

                            firstPacket = false;
                            offset += length;

                            // fixme: it's possible for a multi-dframe user data message to overrun
                            // the valid seq window size. this is bad and the connection will fail.
                        }

                        // If we have sent a user data message that had to be carried in multiple
                        // dframes then stop sending. We can't have more than one of these on the
                        // wire at one time (if I've intepreted the specs correctly).
                        if (sess.MultipleDframePacket)
                            break;

                        // If the window is full, don't send any more
                        if (IsAckWindowFull(sess))
                            break;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Actually transmit data to the freelancer client.
        /// </summary>
        /// <param name="pkt"></param>
        /// <param name="client"></param>
        private void TxStart(byte[] pkt, IPEndPoint client)
        {
            // Test code to simulate packet loss
            // if (rand.Next(5) == 1)
            // {
            //    Logger.AddLog(LogType.DplayMsg, "s>c DROPing last packet client={0}", client);
            //    return;
            //}

            _socket.Send(pkt, client);
            Logger.AddLog(LogType.DplayMsg, "s>c client={0} pkt={1} ", client, pkt);

            //try
            //{
                //_socket.Send(pkt, client);
                //Logger.AddLog(LogType.DplayMsg, "s>c client={0} pkt={1} ", client, pkt);
            //}
            //catch (Exception e)
            //{
                //Logger.AddLog(LogType.Error, "s>c tx error client={0} pkt={1} err={2}", client, pkt, e.Message);
            //}
        }

        public void SessionTimer(object obj)
        {
            var sess = obj as Session;
            lock (sess)
            {
                // Do nothing if the timer has been cancelled.
                if (sess.SessionTimer == null)
                    return;

                // If we haven't received any traffic from the client for 20 seconds
                // then this session is dead.
                if (sess.LastClientRxTime.AddSeconds(15) < DateTime.UtcNow)
                {
                    Destroy(sess, "client dead");
                    return;
                }

                // Do retries for unacknowledged packets. It is recommended that the retry period start at 2.5 
                // times round-trip time (RTT) plus the delayed acknowledgment (ACK) time-out (nominally 100 milliseconds),
                // and that there be linear backoff for the second and third retries, exponential backoff for the fourth
                // through eighth retries, and an overall cap at 5 seconds and 10 retries.
                foreach (Session.Pkt pkt in sess.UserDataPendingAck.Values)
                {
                    if (pkt.RetryCount >= 10)
                    {
                        Destroy(sess, "retry count exceeded");
                        return;
                    }

                    if (pkt.RetryTime < DateTime.UtcNow)
                    {
                        pkt.RetryCount++;
                        if (pkt.RetryCount < 3)
                            pkt.RetryTime = DateTime.UtcNow.AddMilliseconds(200 + 50 * pkt.RetryCount);
                        else
                            pkt.RetryTime = DateTime.UtcNow.AddMilliseconds(200 + 50 * pkt.RetryCount * pkt.RetryCount);

                        pkt.SendTime = DateTime.UtcNow;

                        Logger.AddLog(LogType.DplayMsg, "s>c send retry client={0} seq={1:X} retry_count={2}", sess.Client,
                            pkt.Data[2], pkt.RetryCount);
                        pkt.Data[1] |= 0x01;
                        pkt.Data[3] = sess.NextRxSeq;
                        sess.BytesTx += pkt.Data.Length;
                        TxStart(pkt.Data, sess.Client);
                    }
                }

                SendDFrame(sess);
            }
        }

        /// <summary>
        ///     Destroy a session and remove it from the session table.
        /// </summary>
        /// <param name="sess"></param>
        /// <param name="reason"></param>
        private void Destroy(Session sess, string reason)
        {
            lock (sess)
            {

                //TODO: save charstate on discon
                //TODO: Don't save here: http://www.youtube.com/watch?v=C0_p7Xaj7eQ

                Logger.AddLog(LogType.DplayMsg, "c>s session terminated client={0} reason={1}", sess.Client, reason);
                SendCmdHardDisconnect(sess);
                SendCmdHardDisconnect(sess);
                SendCmdHardDisconnect(sess);
                SendCmdHardDisconnect(sess);

                if (sess.SessionState == Session.State.Connected)
                {
                    OnPlayerDestroyed(sess, reason);
                }

                sess.SessionState = Session.State.Disconnected;

                if (sess.SessionTimer != null)
                {
                    sess.SessionTimer.Dispose();
                    sess.SessionTimer = null;
                }
            }

            lock (dplay_sessions)
            {
                dplay_sessions.Remove(sess.Client);
            }
        }

        private bool IsAckWindowFull(Session sess)
        {
            // fixme: really need to make sure that the lowest seq and highest seq
            // are no more than 64 away.
            if (sess.UserDataPendingAck.Count < 32)
                return false;
            return true;
        }

    }
}
