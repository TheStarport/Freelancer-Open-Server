using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Akka.Actor;


namespace FLServer.Actors.Session.Congestion
{
	partial class Congestion :TypedActor, 
		IHandle<string>, 
		IHandle<Congestion.AckUserData>, 
		IHandle<Congestion.AckIfInWindow>,
		IHandle<Congestion.DoRetryOnSACKMessage>,
		IHandle<Congestion.CheckIfOutOfOrder>,
		IHandle<byte[]>

	{

		class PacketMeta
		{
			public byte[] Data;
			public int RetryCount;
			public DateTime RetryTime;
			public DateTime SendTime;
		}

		/// <summary>
		///     Queue of user data to send.
		/// </summary>
		public LinkedList<byte[]> UserData = new LinkedList<byte[]>();

		/// <summary>
		///     True if we're sending user data that is spread over
		///     several dframes.
		/// </summary>
		public bool MultipleDframePacket = false;

		/// <summary>
		/// Estimated round-trip time to client.
		/// </summary>
		private uint _rtTime;

		private byte NextTxSeq;
		private byte NextRxSeq;

		private int LostRx;
		private int LostTx;

		/// <summary>
		/// Next CFRAME msg_id to use.
		/// </summary>
		private byte NextMsgID;

		private readonly Dictionary<byte, byte[]> _outOfOrder = new Dictionary<byte, byte[]>();

		/// <summary>
		///     Messages sent to the client that have not been acknowledged.
		/// </summary>
		readonly Dictionary<byte, PacketMeta> _userDataPendingAck = new Dictionary<byte, PacketMeta>();

		public void Handle(string message)
		{
			switch (message)
			{
				case "SendSACK":
					SendCmdSACK();
					break;
				case "LostRx":
					LostRx++;
					break;
				case "msgID":
					Context.Sender.Tell(NextMsgID,Self);
					break;
				case "Reset":
					Reset();
					break;
				case "NextRxSeq":
					NextRxSeq++;

					// If there are queued out of order packets, try to process these.
					while (_outOfOrder.ContainsKey(NextRxSeq))
					{
						//log.Debug("{0} dequeuing out of order packet, seq={1:X}",Context.Self.Path,NextRxSeq);

						var pkt = _outOfOrder[NextRxSeq];
						_outOfOrder.Remove(NextRxSeq);
						NextRxSeq++;
						//TODO: check if pos=4 for everyone
						Context.ActorSelection("../dplay-session").Tell(new DPlaySession.DPlaySession.ByteMessage(pkt,4));
						//ProcessTransUserData(sess, pkt, pos); // fixme: pos could be wrong if we received a sack mask
					}

					break;
			}
		}

		public void Handle(AckUserData message)
		{
			DoAcknowledgeUserData(message.NRcv);
		}


		public void Reset()
		{
			_rtTime = 200;
			LostRx = 0;
			//sess.BytesRx = 0;
			LostTx = 0;
			//sess.BytesTx = 0;

			NextRxSeq = 0;
			NextTxSeq = 0;

			NextMsgID = 0;
			_outOfOrder.Clear();
			UserData.Clear();
			_userDataPendingAck.Clear();

			MultipleDframePacket = false;
		}

		//// are covered by the bNRcv sequence ID, that is, those packets that had been sent
		/// <summary>
		///     All previously sent TRANS_USERDATA_HEADER packets that
		///     with bSeq values less than bNRcv (accounting for 8-bit counter wrapping) are
		///     acknowledged. These packets do not have to be remembered any longer, and their
		///     retry timers can be canceled.
		/// </summary>
		/// <param name="nrcv"></param>
		private void DoAcknowledgeUserData(byte nrcv)
		{

				var firstPossibleSeq = (byte)(nrcv - 64);
				for (var i = (byte)(nrcv - 1); i != firstPossibleSeq; i--)
				{
					if (_userDataPendingAck.ContainsKey(i))
					{
						// string seqs = "[";
						// foreach (byte key in sess.user_data_pending_ack.Keys)
						//    seqs += String.Format("{0:X} ", key);
						// seqs += "]";

						//log.AddLog(LogType.DPLAY_MSG, "c>s ack received client={0} seq={1:X} nrcv={2:X} user_data_pending_ack={3}",
						//    sess.client, i, nrcv, seqs);

						// Calculate the rtt deducting 20 ms for internal processing delays.
						// TODO: do we really need to deduct?
						_rtTime =
							(uint)((DateTime.UtcNow - _userDataPendingAck[i].SendTime).TotalMilliseconds);

						_userDataPendingAck.Remove(i);
					}
					if (_userDataPendingAck.Count == 0)
					{
						MultipleDframePacket = false;
						break;
					}
				}
		}

		/// <summary>
		///     Checks that the sequence ID is expected or within 63 packets beyond the ID expected
		/// </summary>
		/// <param name="seqid"></param>
		/// <param name="expectedSeq"></param>
		/// <returns></returns>
		private static bool InWindow(byte seqid, byte expectedSeq)
		{
			if (expectedSeq <= 192)
			{
				if (seqid >= expectedSeq && seqid <= expectedSeq + 63)
				{
					return true;
				}
			}
			else
			{
				if (seqid >= expectedSeq || seqid < 256 - expectedSeq)
				{
					return true;
				}
			}
			return false;
		}



		/// <summary>
		///     Send a SACK setting the sack mask flags to ask for retries if necessary
		/// </summary>
		public void SendCmdSACK()
		{
			if (SendDFrame()) return;

			byte[] pkt = { 0x80, 0x06 };
			FLMsgType.AddUInt8(ref pkt, 0x00); // bFlags
			FLMsgType.AddUInt8(ref pkt, 0x00); // bRetry
			FLMsgType.AddUInt8(ref pkt, NextTxSeq); // bNSeq
			FLMsgType.AddUInt8(ref pkt, NextRxSeq); // bNRcv
			FLMsgType.AddUInt16(ref pkt, 0); // padding
			FLMsgType.AddUInt32(ref pkt, (uint)Stopwatch.GetTimestamp()); // timestamp

			// If there are queued out of order packets then set the SACK mask.
			// The receiver of a SACK mask will loop through each bit of the combined 64-bit value
			// in the ascending order of significance. Each bit corresponds to a sequence ID after
			// bNRcv. If the bit is set, it indicates that the corresponding packet was received
			// out of order.
			if (_outOfOrder.Count > 0)
			{
				ulong sackMask = 0;

				var lastPossibleRxSeq = (byte)(NextRxSeq + 64);
				for (var seq = NextRxSeq; seq <= lastPossibleRxSeq; seq++)
				{
					if (_outOfOrder.ContainsKey(seq))
						sackMask |= (1u << seq);
				}

				// Set the sack flags to indicate that a sack mask is in this message
				// and add the mask to the message.
				pkt[2] |= 0x06;
				FLMsgType.AddUInt32(ref pkt, (uint)(sackMask & 0xFFFFFFFF));
				FLMsgType.AddUInt32(ref pkt, (uint)((sackMask >> 32) & 0xFFFFFFFF));
			}

			//sess.BytesTx += pkt.Length;
			Context.ActorSelection("../socket").Tell(pkt);
		}

		public void Handle(DoRetryOnSACKMessage message)
		{
			DoRetryOnSACKMask(message.Mask,message.NRcv);
		}

		/// <summary>
		///     Resend packets starting from seq if the corresponding bit in the mask
		///     field is set.
		/// </summary>
		private void DoRetryOnSACKMask(uint mask, byte seq)
		{
				//_log.AddLog(LogType.DPLAY_MSG,
				//    "s>c retry on sack mask client={0} seq={1:X} mask={2:X} user_data_pending_ack={3}",
				//    sess.Client, seq, mask, sess.UserDataPendingAck.Count);

				//var seqs = UserDataPendingAck.Keys.Aggregate("user_data_pending_ack=[",
					//(current, key) => current + String.Format("{0:X} ", key));
				//seqs += "]";
				//_log.AddLog(LogType.DPLAY_MSG, seqs);

				byte resendSeq = seq;
				for (var i = 1; i != 0; i <<= 1, resendSeq++)
				{
					if ((mask & i) != i) continue;

					if (!_userDataPendingAck.ContainsKey(resendSeq)) continue;

					var pkt = _userDataPendingAck[resendSeq];

					pkt.RetryCount++;
					pkt.RetryTime = pkt.RetryCount < 3 ? 
						DateTime.UtcNow.AddMilliseconds(200 + 50 * pkt.RetryCount) : 
						DateTime.UtcNow.AddMilliseconds(200 + 50 * pkt.RetryCount * pkt.RetryCount);

					pkt.SendTime = DateTime.UtcNow;

					//_log.AddLog(LogType.DPLAY_MSG, "s>c send retry client={0} seq={1:X} retry_count={2}",
					//    sess.Client, pkt.Data[2], pkt.RetryCount);
					pkt.Data[1] |= 0x01;
					pkt.Data[3] = NextRxSeq;
					//sess.BytesTx += pkt.Data.Length;
					Context.ActorSelection("../socket").Tell(pkt.Data);
					//TxStart(pkt.Data, sess.Client);

					LostTx++;
				}
		}

		/// <summary>
		///     Try to send a dframe or a keep alive if no user data is waiting to be sent.
		/// </summary>
		/// <returns>Return true if a dframe was sent</returns>
		public bool SendDFrame()
		{
			if (UserData.Count <= 0) return false;

			// If the window is full, don't send any thing
			if (IsAckWindowFull())
				return false;

			// If we have sent a user data message that had to be carried in multiple
			// dframes then stop sending. We can't have more than one of these on the
			// wire at one time (if I've intepreted the specs correctly).
			if (MultipleDframePacket)
				return false;

			// The retry time should start at 100 + rtt * 2.5 according to the specs but we use
			// 2 as this is a round number.
			uint retryTime = 100 + (_rtTime * 2);
			while (UserData.Count > 0)
			{
				byte[] ud = UserData.First();
				UserData.RemoveFirst();

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
						MultipleDframePacket = true;
					}
					else
					{
						length = ud.Length - offset;
						lastPacket = true;
					}

					byte[] pkt = { 0x07, 0x00, NextTxSeq, NextRxSeq };

					// If this is the first packet, set the flag to indicate this.
					if (firstPacket)
						pkt[0] |= 0x10;

					// If this is the last packet, set the flag to indicate this
					if (lastPacket)
						pkt[0] |= 0x20;

					// If the session isn't fully connected then this must be a session establishment
					// message

					var sessionState = Context.ActorSelection("../dplay-session").
						Ask<FLServer.Player.Session.State>("GetSessState");
					if (sessionState.Result == FLServer.Player.Session.State.CONNECTING_SESSINFO)
						pkt[0] |= 0x40;

					FLMsgType.AddArray(ref pkt, ud, offset, length);

					var spkt = new PacketMeta
					{
						Data = pkt,
						RetryTime = DateTime.UtcNow.AddMilliseconds(retryTime),
						SendTime = DateTime.UtcNow
					};

					_userDataPendingAck[NextTxSeq] = spkt;
					//sess.BytesTx += pkt.Length;
					Context.ActorSelection("../socket").Tell(pkt);
					//TxStart(pkt, sess.Client);

					// Increase the retry times if multiple packets are sent so that
					// we're less likely to send a massive burst of packets to retry.
					retryTime += 5;

					NextTxSeq++;

					firstPacket = false;
					offset += length;

					// fixme: it's possible for a multi-dframe user data message to overrun
					// the valid seq window size. this is bad and the connection will fail.
				}

				// If we have sent a user data message that had to be carried in multiple
				// dframes then stop sending. We can't have more than one of these on the
				// wire at one time (if I've intepreted the specs correctly).
				if (MultipleDframePacket)
					break;

				// If the window is full, don't send any more
				if (IsAckWindowFull())
					break;
			}

			return true;
		}


		private bool IsAckWindowFull()
		{
			// fixme: really need to make sure that the lowest seq and highest seq
			// are no more than 64 away.
			return _userDataPendingAck.Count >= 32;
		}

		public void Handle(AckIfInWindow message)
		{
			if (InWindow(message.Sequence, NextRxSeq))
			{
				Context.Sender.Tell(false, Context.Self);

			}
			else
			{
				Context.Sender.Tell(true, Context.Self);
				SendCmdSACK();
			}
			
		}


		public void Handle(CheckIfOutOfOrder message)
		{
			if (message.Sequence == NextRxSeq)
			{
				Context.Sender.Tell(false, Context.Self);

			}
			else
			{
				Context.Sender.Tell(true, Context.Self);
				_outOfOrder[message.Sequence] = message.Data;
				SendCmdSACK();
			}
			
		}

		/// <summary>
		/// Add long packets to queue.
		/// </summary>
		/// <param name="message"></param>
		public void Handle(byte[] message)
		{
			UserData.AddLast(message);
			SendDFrame();
		}
	}
}
