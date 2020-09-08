using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace FLServer.Player
{
      /// <summary>
        ///     The session class maintains the state of a connection to a single freelancer client.
        ///     It holds queues of messages to be sent and related timers to deal with retries.
        /// </summary>
        public class Session
        {
            /// <summary>
            ///     Possible session states
            /// </summary>
            public enum State
            {
// ReSharper disable InconsistentNaming
                DISCONNECTED,
                CONNECTING,
                CONNECTING_SESSINFO,
                CONNECTED
                // ReSharper restore InconsistentNaming
            }

            /// <summary>
            ///     Number of bytes received from this client
            /// </summary>
            public volatile int BytesRx;

            /// <summary>
            ///     Number of bytes transmitted to this client
            /// </summary>
            public int BytesTx;

            /// <summary>
            ///     The client port and ip to send traffic to.
            /// </summary>
            public IPEndPoint Client;

            /// <summary>
            ///     This is a combination dpnid and session id. We don't follow the rules
            ///     as specified by the protocol documentation here.
            /// </summary>
            public uint DPlayID;

            /// <summary>
            ///     The time we last received traffic from the client. This is used to
            ///     time out dead connections quickly.
            /// </summary>
            public DateTime LastClientRxTime;

            /// <summary>
            ///     Number of messages lost from client
            /// </summary>
            public volatile int LostRx;

            /// <summary>
            ///     Number of messages lost to client
            /// </summary>
            public volatile int LostTx;

            /// <summary>
            ///     The next CFRAME msg_id to use.
            /// </summary>
            public byte MsgID = 0;

            /// <summary>
            ///     True if we're sending user data that is spread over
            ///     several dframes.
            /// </summary>
            public bool MultipleDframePacket = false;

            /// <summary>
            ///     The expected sequence number of the next DFRAME packet received
            ///     from the client.
            /// </summary>
            public byte NextRxSeq = 0;

            /// <summary>
            ///     This field represents the sequence number of the next DFRAME to send.
            /// </summary>
            public byte NextTxSeq = 0;

            /// <summary>
            ///     Packets received out of order pending the correct sequence before
            ///     being passed to the application.
            /// </summary>
            public Dictionary<byte, byte[]> OutOfOrder = new Dictionary<byte, byte[]>();

            /// <summary>
            ///     Estimated round trip time to client and back to server.
            /// </summary>
            public uint Rtt;


            /// <summary>
            ///     This timer controls when packets are retried, when the session times out
            ///     and when queued messages should be sent.
            /// </summary>
            public Timer SessionTimer;

            /// <summary>
            ///     The time this session started.
            /// </summary>
            public DateTime StartTime;

            /// <summary>
            ///     The session state
            /// </summary>
            public State SessionState = State.DISCONNECTED;

            /// <summary>
            ///     Queue of user data to send.
            /// </summary>
            public LinkedList<byte[]> UserData = new LinkedList<byte[]>();

            /// <summary>
            ///     Messages sent to the client that have not been acknowledged.
            /// </summary>
            public Dictionary<byte, Pkt> UserDataPendingAck = new Dictionary<byte, Pkt>();

            public Session(IPEndPoint client)
            {
                Client = client;
                StartTime = DateTime.UtcNow;
                LastClientRxTime = DateTime.UtcNow;
            }

            public class Pkt
            {
                public byte[] Data;
                public int RetryCount;
                public DateTime RetryTime;
                public DateTime SendTime;
            }
        }
    }
