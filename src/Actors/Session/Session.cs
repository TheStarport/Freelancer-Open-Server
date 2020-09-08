using System;
using System.Diagnostics;
using System.Net;
using Akka.Actor;
using Akka.Event;
using FLServer.Actors.System;

namespace FLServer.Actors.Session
{
			
	/// <summary>
	/// This class controls the packet congestion and ordering,
	/// sending the ready packets to underlying DPlaySession.
	/// </summary>
	public partial class Session : TypedActor, 
		IHandle<Session.RxMessage>,
		IHandle<Session.ConnectingMessage>,
		IHandle<ReceiveTimeout>

	{
		/// <summary>
		/// Time span after which session is considered as timing out
		/// </summary>
		private static readonly TimeSpan TsTimeout = TimeSpan.FromSeconds(8);
		
		
		readonly LoggingAdapter _log = Akka.Event.Logging.GetLogger(Context);

		private readonly ActorRef _dplaySession;
		private readonly ActorRef _congestion;

		/// <summary>
		/// Shows if client is properly initiated the connection.
		/// </summary>
		private bool _dpInitialized = false;

		public Session(IPEndPoint ep, int port)
		{
			Context.ActorOf(Props.Create(() => new UdpSockReader(ep, port)), "socket");
			//trigger a receive timeout after some time
			SetReceiveTimeout(TsTimeout);

			Context.ActorOf<DPlaySession.DPlaySession>("dplay-session");
			_dplaySession = Context.Child("dplay-session");
			Context.ActorOf<Congestion.Congestion>("congestion");
			_congestion = Context.Child("congestion");
		}


		/// <summary>
		/// Handle the session timeout message.
		/// </summary>
		/// <param name="message"></param>
		public void Handle(ReceiveTimeout message)
		{
			Context.ActorSelection("./*").Tell(PoisonPill.Instance, Context.Self);
			_log.Debug("{0} shutting down: Timeout", Self.Path);
			SendCmdHardDisconnect();
			Context.Stop(Context.Self);
		}


		protected override void PostStop()
		{
			_log.Debug("Session shutdown: {0}", Self.Path);
			base.PostStop();
		}

		

		/// <summary>
		/// Rewrite of <see cref="FLServer.Server.DirectPlayServer.ProcessPktFromClient"/>
		/// </summary>
		/// <param name="message"></param>
		public void Handle(RxMessage message)
		{
			var pos = 0;
			uint cmd = FLMsgType.GetUInt8(message.Bytes, ref pos);
			if (cmd == 0x00 && message.Bytes.Length >= 4)
			{
				uint opcode = FLMsgType.GetUInt8(message.Bytes, ref pos);

				if (opcode != 0x02 || message.Bytes.Length < 4) return;

				//That's enum request, spit out server info.
				uint enumPayload = FLMsgType.GetUInt16(message.Bytes, ref pos);
				var glob = Context.ActorSelection("/user/server/globals");
				glob.Tell(new EnumRequest((ushort)enumPayload));
			}

				// If the data is at least 12 bytes and the first byte is 
				// either 0x80 or 0x88 (PACKET_COMMAND_CFRAME or PACKET_COMMAND_CFRAME |
				// PACKET_COMMAND_POLL), it MUST process the message as a CFRAME 
				// (section 3.1.5.1) command frame.
			else if ((cmd == 0x80 || cmd == 0x88) && message.Bytes.Length >= 12)
			{
				uint opcode = FLMsgType.GetUInt8(message.Bytes, ref pos);

				// The CONNECT packet is used to request a connection. If accepted, the response 
				// is a CONNECTED (section 2.2.1.2) packet
				switch (opcode)
				{
					case 0x01:
					{
						//now created on session start
						//var session = Context.Child("dplay-session");
						//if (session.IsNobody())
						//{
							//session = Context.ActorOf<DPlaySession.DPlaySession>("dplay-session");
							//Context.ActorOf<Congestion.Congestion>("congestion");
						//}

						if (!_dpInitialized)
							_dpInitialized = true;
						var msgID = FLMsgType.GetUInt8(message.Bytes, ref pos);
						var rspID = FLMsgType.GetUInt8(message.Bytes, ref pos);

						var version = FLMsgType.GetUInt32(message.Bytes, ref pos);
						var dplayid = FLMsgType.GetUInt32(message.Bytes, ref pos);
						var timestamp = FLMsgType.GetUInt32(message.Bytes, ref pos);

						// If the session id has changed, assume that the server is wrong
						// and kill the existing connection and start a new one.
						// This behaviour differs from the dplay specification.
						// If the session is fully connected because the client has
						// sent us a connect acknowledge then ignore this.
						//if (sess.SessionState == Session.State.CONNECTED)
						//    return;
						//Whole thing done in DPlaySession

						//this part is done in ConnectingMessage handler
						//we're nullifying all counters

						_dplaySession.Tell(new DPlaySession.DPlaySession.ConnectRequest(msgID,dplayid,rspID),Context.Child("socket"));
					}
						break;
					case 0x06:
					{
						var flags = FLMsgType.GetUInt8(message.Bytes, ref pos);
						var retry = FLMsgType.GetUInt8(message.Bytes, ref pos);
						// The seq field indicates the seq of the next message that the client will send.
						var seq = FLMsgType.GetUInt8(message.Bytes, ref pos);
						// The next_rx field indicates the message seq that the client is waiting to receive
						var nrcv = FLMsgType.GetUInt8(message.Bytes, ref pos);
						pos += 2; // skip padding
						var timestamp = FLMsgType.GetUInt32(message.Bytes, ref pos);

						// Ignore packets for sessions that don't exist
						
						if (!_dpInitialized) return;

						//sess.BytesRx += pkt.Length;
						
						// If the hi sack mask is present, resend any requested packets.
						if ((flags & 0x02) == 0x02)
						{
							var mask = FLMsgType.GetUInt32(message.Bytes, ref pos);
							_congestion.Tell(new Congestion.Congestion.DoRetryOnSACKMessage(mask, nrcv));
						}

						// If the hi sack mask is present, resend any requested packets.
						if ((flags & 0x04) == 0x04)
						{
							var mask = FLMsgType.GetUInt32(message.Bytes, ref pos);
							_congestion.Tell(new Congestion.Congestion.DoRetryOnSACKMessage(mask, (byte)(nrcv + 32)));
						}

						// At this point bSeq sequence ID is valid, the bNRcv field 
						// is to be inspected. All previously sent TRANS_USERDATA_HEADER packets that 
						// are covered by the bNRcv sequence ID, that is, those packets that had been sent
						// with bSeq values less than bNRcv (accounting for 8-bit counter wrapping) are 
						// acknowledged. These packets do not have to be remembered any longer, and their 
						// retry timers can be canceled.

						_congestion.Tell(new Congestion.Congestion.AckUserData(nrcv));

						// Try to send data if there's data waiting to be sent and send a 
						// selective acknowledgement if we didn't sent a dframe and the client
						// requested an acknowledgement.
						if (cmd == 0x88)
						{
							_congestion.Tell("SendSACK");
						}
					}
						break;
				}
			}

				// If a packet arrives, the recipient SHOULD first check whether 
			// it is large enough to be a minimal data frame (DFRAME) (4 bytes)
			// and whether the first byte has the low bit (PACKET_COMMAND_DATA) set.
			else if ((cmd & 0x01) == 0x01 && message.Bytes.Length >= 4)
			{
				uint control = FLMsgType.GetUInt8(message.Bytes, ref pos);
				var seq = FLMsgType.GetUInt8(message.Bytes, ref pos);
				var nrcv = FLMsgType.GetUInt8(message.Bytes, ref pos);

				// Ignore packets for sessions that don't exist
				//Session sess = GetSession(client);
				//if (sess == null)
					//return;

					// This is a disconnect. We ignore the soft disconnect and immediately
					// drop the session repeating the disconnect a few times to improve the
					// probability of it getting through.
					if ((control & 0x08) == 0x08)
					{
						Context.ActorSelection("./*").Tell(PoisonPill.Instance, Context.Self);
						_log.Debug("{0} shutting down: Client request", Self.Path);
						SendCmdHardDisconnect();
						Context.Stop(Context.Self);
						return;
					}

				//var cong = Context.Child("congestion");
				//if (cong.IsNobody()) return;
				if (!_dpInitialized) return;

				var ticks = new TimeSpan(45);
					// TRANS_USERDATA_HEADER bSeq field MUST be either the next sequence
					// ID expected or within 63 packets beyond the ID expected by the receiver.
					// If the sequence ID is not within this range, the payload MUST be ignored.
					// In addition, a SACK packet SHOULD be sent indicating the expected sequence ID.
					{
						var resp = _congestion.Ask<bool>(new Congestion.Congestion.AckIfInWindow(seq));
					resp.Wait(ticks);
					if (resp.Result) return;
					}
					// If the sequence ID is out of order, but still within 63 packets,
					// the receiver SHOULD queue the payload until it receives either:
					// - A delayed or retried transmission of the missing packet or packets,
					// and can now process the sequence in order.
					// - A subsequent packet with a send mask indicating that the missing
					// packet or packets did not use PACKET_COMMAND_RELIABLE and will never
					// be retried. Therefore, the receiver should advance its sequence as if 
					// it had already received and processed the packets.

				{
					//var ask = new Congestion.CheckIfOutOfOrder(seq, message.Bytes);
					var resp = _congestion.Ask<bool>(new Congestion.Congestion.CheckIfOutOfOrder(seq, message.Bytes));
					resp.Wait(ticks);
					if (resp.Result)
					{
						_log.Debug("{0} enqueued out of order packet {1}",Context.Self.Path,seq);
						return;
					}
				}

					//Test code to simulate packet loss
					//if (rand.Next(5) == 1)
					//{
					//    log.AddLog(String.Format("c>s: DROPPING THE PACKET NOW {0:X}", seq));
					//    return;
					//}

					// Note if this was a retried dframe.
					if ((control & 0x01) == 0x01)
					{
						_congestion.Tell("LostRx");
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
						var mask = FLMsgType.GetUInt32(message.Bytes, ref pos);
						_congestion.Tell(new Congestion.Congestion.DoRetryOnSACKMessage(mask, nrcv));
					}
					if ((control & 0x20) == 0x20)
					{
						var mask = FLMsgType.GetUInt32(message.Bytes, ref pos);
						_congestion.Tell(new Congestion.Congestion.DoRetryOnSACKMessage(mask, (byte)(nrcv + 32)));
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
						FLMsgType.GetUInt32(message.Bytes, ref pos);
					if ((control & 0x80) == 0x80)
						FLMsgType.GetUInt32(message.Bytes, ref pos);
					// However, freelancer always uses reliable packets and so ignore sendmasks.

					// At this point, we've received the packet we wanted to. Advance the sequence number count later
					// and process this message.
					_dplaySession.Tell(new DPlaySession.DPlaySession.ByteMessage(message.Bytes, pos), Context.Child("socket"));
					
					
					
					
					//ProcessTransUserData(sess, message.Bytes, pos);

					// If there are queued out of order packets, try to process these.
					// Done in Congestion on NextRxSeq
					// Advance the sequence number as well.
					_congestion.Tell("NextRxSeq");
					// At this point bSeq sequence ID is valid, the bNRcv field 
					// is to be inspected. All previously sent TRANS_USERDATA_HEADER packets that 
					// are covered by the bNRcv sequence ID, that is, those packets that had been sent
					// with bSeq values less than bNRcv (accounting for 8-bit counter wrapping) are 
					// acknowledged. These packets do not have to be remembered any longer, and their 
					// retry timers can be canceled.
					_congestion.Tell(new Congestion.Congestion.AckUserData(nrcv));

					// We always do an immediate acknowledge as bandwidth isn't a particular concern
					// but fast recovery from lost packets is.
					_congestion.Tell("SendSACK");
				
			}


		}


		public void Handle(ConnectingMessage message)
		{
			// This is a new connection. Reset the session information.
			_congestion.Tell("Reset");
			//_sessionTimer.Change(SessionTimeout, Timeout.Infinite);
			//TODO: lag timer
			//sess.SessionTimer = new Timer(SessionTimer, sess, 100, 20);
		}



		/// <summary>
		///     Send a hard disconnect to the freelancer client.
		/// </summary>
		public void SendCmdHardDisconnect()
		{
			var msgID = _congestion.Ask<byte>("msgID");
			msgID.Wait();
			var dPlayID = _dplaySession.Ask<uint>("GetDPlayID");
			dPlayID.Wait();

			byte[] pkt = { 0x80, 0x04 };
			FLMsgType.AddUInt8(ref pkt, (uint)(msgID.Result + 1));
			FLMsgType.AddUInt8(ref pkt, 0);
			FLMsgType.AddUInt32(ref pkt, 0x10004);
			FLMsgType.AddUInt32(ref pkt, dPlayID.Result);
			FLMsgType.AddUInt32(ref pkt, (uint)Stopwatch.GetTimestamp());
			//twice is OK, just to be sure
			Context.Child("socket").Tell(pkt);
			Context.Child("socket").Tell(pkt);
		}

	}
}
