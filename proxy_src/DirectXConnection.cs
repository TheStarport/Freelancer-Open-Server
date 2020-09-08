//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Net.Sockets;
//using System.Net;

//namespace FLServer
//{
//    class DirectXMessage
//    {
//        public byte bCommand;
//        public byte control;
//        public byte bSeq;
//        public byte bNRcv;
//        public byte[] message;
//    }

//    class DirectXConnection
//    {
//        UdpClient socket;

//        public void StartListening()
//        {
//            StopListening();

//            socket = new UdpClient(2306, AddressFamily.InterNetwork);
//            socket.BeginReceive(new AsyncCallback(RecieveComplete), socket);
//        }

//        public void StopListening()
//        {
//            if (socket != null)
//            {
//                socket.Close();
//                socket = null;
//            }
//        }


//        const byte CMD_DP_ENUM_QUERY = 0x02;
//        const byte CMD_DP_ENUM_RESPONSE = 0x03;
//        const byte CMD_DP_SESS_PATH_TEST = 0x05;
//        const byte CMD_DP_EXT_TRANS_COMMAND_CONNECT = 0x01;
//        const byte CMD_DP_EXT_TRANS_COMMAND_CONNECT_ACCEPT = 0x02;
//        const byte CMD_DP_EXT_TRANS_COMMAND_SACK = 0x06;

//        const uint PACKET_COMMAND_DATA = 0x01;
//        const uint PACKET_COMMAND_RELIABLE = 0x02;
//        const uint PACKET_COMMAND_SEQUENTIAL = 0x04;
//        const uint PACKET_COMMAND_POLL = 0x08;
//        const uint PACKET_COMMAND_NEW_MSG = 0x10;
//        const uint PACKET_COMMAND_END_MSG = 0x20;
//        const uint PACKET_COMMAND_USER_1 = 0x40;
//        const uint PACKET_COMMAND_USER_2 = 0x80;

//        const uint PACKET_CONTROL_RETRY = 0x01;
//        const uint PACKET_CONTROL_KEEPALIVE_OR_CORRELATE = 0x02;
//        const uint PACKET_CONTROL_COALESCE = 0x04;
//        const uint PACKET_CONTROL_END_STREAM = 0x08;
//        const uint PACKET_CONTROL_SACK1 = 0x10;
//        const uint PACKET_CONTROL_SACK2 = 0x20;
//        const uint PACKET_CONTROL_SEND1 = 0x40;
//        const uint PACKET_CONTROL_SEND2 = 0x80;

//        const uint TRANS_USERDATA_PLAYER_CONNECT_INFO = 0x000000C1;
//        const uint TRANS_USERDATA_SEND_SESSION_INFO = 0x000000C2;
//        const uint TRANS_USERDATA_ACK_SESSION_INFO = 0x000000C3;
//        const uint TRANS_USERDATA_CONNECT_FAILED = 0x000000C5;
//        const uint TRANS_USERDATA_CONNECT_ATTEMPT_FAILED = 0x000000C8;
//        const uint TRANS_USERDATA_ADD_PLAYER = 0x000000D0;
//        const uint TRANS_USERDATA_DESTROY_PLAYER = 0x000000D1;

//        byte next_tx_message_number;
//        byte last_rx_message_number;

//         The sequence number of the next message to send.
//        byte tx_seqid;

//         The sequence number of the next message we expect to receive
//        byte expected_bSeq;

//         Messages waiting to be acknowledged
//        List<DirectXMessage> msgs_waiting_for_ack = new List<DirectXMessage>();

//         Messages waiting to be sent
//        List<DirectXMessage> msgs_waiting_to_be_sent = new List<DirectXMessage>();

//         Payload received but out of order
//        List<DirectXMessage> msgs_out_of_sequence = new List<DirectXMessage>();

//         Checks that the sequence ID is expected or within 63 packets beyond the ID expected
//        bool InWindow(byte seqid)
//        {
//            if (expected_bSeq <= 192)
//            {
//                if (seqid >= expected_bSeq && seqid <= expected_bSeq + 63)
//                {
//                    return true;
//                }
//            }
//            else
//            {
//                if (seqid >= expected_bSeq || seqid < 256 - expected_bSeq)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        void ProcessRxMessage(IPEndPoint remoteEP, byte[] msg)
//        {
//            if ((msg.Length >= 4) && msg[0] == 0x00)
//            {
//                if (msg[1] == CMD_DP_ENUM_QUERY)
//                {
//                    int pos = 2;
//                    uint enumPayload = FLMsgType.GetUInt16(msg, ref pos);
//                    Console.WriteLine("rx> CMD_DP_ENUM_QUERY enumPayload={0}", enumPayload);
//                    SendCmdEnumResponse(remoteEP, enumPayload);
//                }
//            }
//             When a packet arrives, the recipient SHOULD first check whether 
//             it is large enough to be a minimal data frame (DFRAME) (4 bytes)
//             and whether the first byte has the low bit (PACKET_COMMAND_DATA) set.
//            if ((msg.Length >= 4) && ((msg[0] & 0x01) == 0x01))
//            {
//                ProcessRxDFrame(remoteEP, msg);
//            }
//             Otherwise, if the data is at least 12 bytes and the first byte is 
//             either 0x80 or 0x88 (PACKET_COMMAND_CFRAME or PACKET_COMMAND_CFRAME |
//             PACKET_COMMAND_POLL), it MUST process the message as a CFRAME 
//             (section 3.1.5.1) command frame.
//            else if ((msg.Length >= 12) && ((msg[0] == 0x80) || (msg[0] == 0x88)))
//            {
//                ProcessRxCFrame(remoteEP, msg);
//            }
//        }

//        void ProcessRxCFrame(IPEndPoint remoteEP, byte[] msg)
//        {
//             3.1.5.1.1 CONNECT
//            if (msg[1] == 0x01)
//            {
//                 TODO: If the source IP and port corresponds to an existing fully 
//                 established connection, this message SHOULD be ignored
//                int pos = 2;
//                uint bMsgID = FLMsgType.GetUInt8(msg, ref pos);
//                uint bRspId = FLMsgType.GetUInt8(msg, ref pos);
//                uint dwCurrentProtocolVersion = FLMsgType.GetUInt32(msg, ref pos);
//                uint dwSessID = FLMsgType.GetUInt32(msg, ref pos);
//                uint tTimestamp = FLMsgType.GetUInt32(msg, ref pos);
//                Console.WriteLine("rx> TRANS_COMMAND_CONNECT bMsgID={0} mRspId={1} dwCurrentProtocolVersion={2} dwSessID={3} tTimestamp={4}",
//                    bMsgID, bRspId, dwCurrentProtocolVersion, dwSessID, tTimestamp);


//                 TODO: If the address is for a previously received inbound connection 
//                 that has not completed the handshake process and if the dwSessID 
//                 field matches the previously received CONNECT, another CONNECTED 
//                 message SHOULD immediately be sent;

//                 TODO: Check the dwCurrentProtocolVersion field for compatibility
//                 and reject incompatible version numbers

//                 TODO: Allocate resources for the new connection and send a CONNECTED
//                 response. This includes setting the connect retry timer to continue 
//                 retrying the CONNECTED reply until either a valid CONNECTED response
//                 arrives from the connector, or the maximum number of retries elapses
//                 and the connection is terminated.

//                SendCmdConnectAccept(remoteEP, 0, bMsgID, 0x00010004, dwSessID, 0);
//            }
//             3.1.5.1.2 CONNECTED
//            else if (msg[1] == 0x02)
//            {
//                 TODO: If the address does not correspond to one with an existing 
//                 partially or fully established connection, it SHOULD be ignored.
//                int pos = 2;
//                uint bMsgID = FLMsgType.GetUInt8(msg, ref pos);
//                uint bRspId = FLMsgType.GetUInt8(msg, ref pos);
//                uint dwCurrentProtocolVersion = FLMsgType.GetUInt32(msg, ref pos);
//                uint dwSessID = FLMsgType.GetUInt32(msg, ref pos);
//                uint tTimestamp = FLMsgType.GetUInt32(msg, ref pos);
//                Console.WriteLine("rx> TRANS_COMMAND_CONNECT bMsgID={0} mRspId={1} dwCurrentProtocolVersion={2} dwSessID={3} tTimestamp={4}",
//                    bMsgID, bRspId, dwCurrentProtocolVersion, dwSessID, tTimestamp);

//                SendCmdKeepAlive(remoteEP, dwSessID);
//            }
//            else if (msg[1] == 0x03)
//            {
//                 TODO: Validate sending IP/port
//            }
//             3.1.5.1.4 HARD_DISCONNECT
//            else if (msg[1] == 0x04)
//            {
//                 TODO: Validate sending IP/port
//            }
//             3.1.5.1.5 SACK
//            else if (msg[1] == 0x06)
//            {
//                 TODO: Validate sending IP/port
//            }
//        }

//        void ProcessRxDFrame(IPEndPoint remoteEP, byte[] msg)
//        {
//             TODO: Validate sending IP/port and ignore message if it does not correspond to
//             a valid session.




//             The TRANS_USERDATA_HEADER bSeq field MUST be either the next sequence
//             ID expected or within 63 packets beyond the ID expected by the receiver.
//             If the sequence ID is not within this range, the payload MUST be ignored.
//             In addition, a SACK packet SHOULD be sent indicating the expected sequence ID.
//            if (!InWindow(msg.bSeq))
//            {
//                SendSACK(expected_bSeq);
//                return;
//            }

//             If the sequence ID is the next expected, the receiver 
//             SHOULD process the payload and advance the expected sequence ID. 
//            if (msg.bSeq == expected_bSeq)
//            {
//                expected_bSeq++;

//                 pass to next layer.
//            }
//             If the sequence ID is out of order, but still within 63 packets,
//             the receiver SHOULD queue the payload until it receives either:
//             - A delayed or retried transmission of the missing packet or packets,
//               and can now process the sequence in order.
//             -A subsequent packet with a send mask indicating that the missing
//               packet or packets did not use PACKET_COMMAND_RELIABLE and will never
//               be retried. Therefore, the receiver should advance its sequence as if 
//               it had already received and processed the packets.
//            else
//            {
//                msgs_out_of_sequence.Add(msg);
//            }
//             TODO: If an implementation has out-of-order packets beyond the current expected 
//             sequence ID queued, it SHOULD indicate this to the sender using appropriate
//             SACK masks on any outgoing TRANS_COMMAND_SACK or TRANS_USERDATA_HEADER based
//             messages.

//             If the TRANS_USERDATA_HEADER bSeq sequence ID is valid, the bNRcv field 
//             SHOULD be inspected. All previously sent TRANS_USERDATA_HEADER packets that 
//             are covered by the bNRcv sequence ID, that is, those packets that had been sent
//             with bSeq values less than bNRcv (accounting for 8-bit counter wrapping) are 
//             acknowledged. These packets do not have to be remembered any longer, and their 
//             retry timers can be canceled.
//            if (msgs_waiting_for_ack.Count > 0)
//            {
//                List<DirectXMessage> msgs_to_remove = new List<DirectXMessage>();
//                foreach (DirectXMessage amsg in msgs_waiting_for_ack)
//                {
//                    if ((amsg.bSeq + 256) < (msg.bNRcv + 256))
//                    {
//                        msgs_waiting_for_ack.Add(amsg);
//                    }
//                }
//                foreach (DirectXMessage amsg in msgs_to_remove)
//                {
//                    msgs_waiting_for_ack.Remove(amsg);
//                }
//            }

//             When one or both of the optional SACK mask 32-bit fields is present, and one
//             or more bits are set in the fields, the sender is indicating that it received a
//             packet or packets out of order, presumably due to packet loss. The two 32-bit,
//             little-endian fields MUST be considered as one 64-bit field, where dwSACKMask1
//             is the low 32 bits and dwSACKMask2 is the high 32 bits. If either 32-bit field
//             is not available, the entire contents of the 64-bit field MUST be considered as all 0.
            
//             The receiver of a SACK mask SHOULD loop through each bit of the combined 64-bit value
//             in the ascending order of significance. Each bit corresponds to a sequence ID after
//             bNRcv. If the bit is set, it indicates that the corresponding packet was received
//             out of order.
            
//             The receiver of a SACK mask SHOULD shorten the retry timer for the first frame of
//             the window to speed recovery from the packet loss. The recommended duration is 
//             10 milliseconds. This value can be modified according to application and network requirements. The receiver MAY also choose to remove the selectively acknowledged packets from its list to retry.

//             When one or both of the optional send mask 32-bit fields is present, and one or
//             more bits are set the fields, the sender is indicating that it sent a packet or
//             packets that were not marked as reliable and did not receive an acknowledgement yet.
//             The two 32-bit, little-endian fields MUST be considered as one 64-bit field, where
//             dwSendMask1 is the low 32 bits and dwSendMask2 is the high 32 bits. If either 32-bit
//             field is not available, the entire contents of the 64-bit field MUST be considered 
//             as all 0.

//             The receiver of a send mask SHOULD loop through each bit of the combined 64-bit
//             value from the least significant bit to the most significant in little-endian byte
//             order. Each bit corresponds to a sequence ID prior to bSeq, and if that is the bit
//             that is set, it indicates that the corresponding packet was not sent reliably and 
//             will not be retried. If the recipient of the send mask had not received the packet
//             and had not already processed a send mask that identified the sequence ID, it SHOULD
//             consider the packet as dropped and release its placeholder in the sequence. That is,
//             any sequential messages that could not be indicated because of the gap in the sequence
//             where the packet that was not marked as reliable had been SHOULD now be reported to
//             the upper layer.

//             Now check queued out of sequence messages. These might be able to be processed now.
//        }

//        void SendSACK(byte expected_seqid)
//        {

//        }


//        public void SendCmdKeepAlive(IPEndPoint remoteEP, UInt32 dwSessID)
//        {
//            List<byte> pkt = new List<byte>();
//            pkt.Add(0x3f);
//            pkt.Add(0x02);
//            pkt.Add(0x00);
//            pkt.Add(0x00);
//            pkt.AddRange(BitConverter.GetBytes(dwSessID));
//            SendCommand(pkt.ToArray(), remoteEP);
//        }

//        public void SendCmdConnectAccept(IPEndPoint remoteEP, uint bMsgID, uint bRspId, uint dwCurrentProtocolVersion, uint dwSessID, uint tTimestamp)
//        {
//            byte[] msg = new byte[0];
//            FLMsgType.AddUInt8(ref msg, 0x80);
//            FLMsgType.AddUInt8(ref msg, CMD_DP_EXT_TRANS_COMMAND_CONNECT_ACCEPT);
//            FLMsgType.AddUInt8(ref msg, bMsgID);
//            FLMsgType.AddUInt8(ref msg, bRspId);
//            FLMsgType.AddUInt32(ref msg, dwCurrentProtocolVersion);
//            FLMsgType.AddUInt32(ref msg, dwSessID);
//            FLMsgType.AddUInt32(ref msg, tTimestamp);
//            SendCommand(msg, remoteEP);
//        }

//        public void SendCmdEnumResponse(IPEndPoint remoteEP, uint enumPayload)
//        {
//            string ServerName = "CannonTest\0";
//            string ServerDescHeader = "1:1:72679426:-1910309061:1e22ff3b-c9f126c3-4cd2d60a-3c70be70:"; // last field is server ID; probly a password field in this too.
//            string ServerDesc = "See forum for password. 486 update 7\0";

//            byte[] ApplicationDesc = ASCIIEncoding.ASCII.GetBytes(ServerDescHeader).Concat(UnicodeEncoding.Unicode.GetBytes(ServerDesc)).ToArray();

//            List<byte> pkt = new List<byte>();
//            pkt.Add(0);
//            pkt.Add(CMD_DP_ENUM_RESPONSE);
//            pkt.AddRange(BitConverter.GetBytes(enumPayload));

//            pkt.AddRange(BitConverter.GetBytes(0x58 + (ServerName.Length * 2))); // ReplyOffset
//            pkt.AddRange(BitConverter.GetBytes(ApplicationDesc.Length)); // ResponseSize
//            pkt.AddRange(BitConverter.GetBytes(0x50)); // ApplicationDescSize
//            pkt.AddRange(BitConverter.GetBytes(0x81)); // ApplicationDescFlags
//            pkt.AddRange(BitConverter.GetBytes(200)); // MaxPlayers 
//            pkt.AddRange(BitConverter.GetBytes(200)); // CurrentPlayers

//            pkt.AddRange(BitConverter.GetBytes(0x58)); // SessionNameOffset 
//            pkt.AddRange(BitConverter.GetBytes(ServerName.Length * 2)); // SessionNameSize

//            pkt.AddRange(BitConverter.GetBytes(0)); // PasswordOffset
//            pkt.AddRange(BitConverter.GetBytes(0)); // PasswordSize
//            pkt.AddRange(BitConverter.GetBytes(0)); // ReservedDataOffset
//            pkt.AddRange(BitConverter.GetBytes(0)); // ReservedDataSize
//            pkt.AddRange(BitConverter.GetBytes(0)); // ApplicationReservedDataOffset
//            pkt.AddRange(BitConverter.GetBytes(0)); // ApplicationReservedDataSize

//            pkt.AddRange(ApplicationInstanceGUID);
//            pkt.AddRange(ApplicationGUID);
//            pkt.AddRange(UnicodeEncoding.Unicode.GetBytes(ServerName)); // SessionName
//            pkt.AddRange(ApplicationDesc); // SessionName

//            SendCommand(pkt.ToArray(), remoteEP);
//        }

//        public void SendCommand(byte[] pkt, IPEndPoint remoteEP)
//        {
//            Console.Write("tx>");
//            for (int i = 0; i < pkt.Length; i++)
//                Console.Write("{0:X2}:", pkt[i]);
//            Console.WriteLine();
//             socket.Send(pkt, pkt.Length, remoteEP);
//        }



//        public void RecieveComplete(IAsyncResult result)
//        {
//            UdpClient socket = result.AsyncState as UdpClient;
//            IPEndPoint remoteEP = null;
//            byte[] bytesReceived = socket.EndReceive(result, ref remoteEP);

//            Console.Write("rx>");
//            for (int i = 0; i < bytesReceived.Length; i++)
//                Console.Write("{0:X2}:", bytesReceived[i]);
//            Console.WriteLine();
//            socket.BeginReceive(new AsyncCallback(RecieveComplete), socket);

//            ProcessRxMessage(remoteEP, bytesReceived);
//        }
//    }
//}
