using System.Text;
using FLServer.Player;

namespace FLServer.Chat
{
    internal class FLMsgDecoder
    {
        /// <summary>
        /// </summary>
        /// <param name="playerid"></param>
        /// <param name="msg"></param>
        /// <param name="desc"></param>
        /// <returns></returns>
        public static string PrintMsgToClient(string desc, Session playerid, byte[] msg)
        {
            // Decompress the message if it is compressed.
            /*if (msg[0] == FLMsgType.MSG_TYPE_COMPRESSED)
            {
                using (MemoryStream ms = new MemoryStream(msg, 2, msg.Length - 2))
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        byte[] deflateBuffer = new byte[32767];
                        int msgLength = ds.Read(deflateBuffer, 0, deflateBuffer.Length);
                        Array.Resize(ref deflateBuffer, msgLength);
                        msg = deflateBuffer;
                    }
                }
            }*/

            var sb = new StringBuilder();
            sb.AppendFormat("{0} [{1:x8}] ", desc, playerid);
            foreach (byte b in msg)
                sb.AppendFormat("{0:x2}.", b);

            /*try
            {
                if (msg.Length > 0)
                {
                    byte type = msg[0];
                    switch (type)
                    {
                        case FLMsgType.MSG_TYPE_SERVER_IDLE:
                        case FLMsgType.MSG_TYPE_CLIENT_IDLE:
                            break;
                        case FLMsgType.MSG_TYPE_CHAR_NEW:
                            {
                                int pos = 2;
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_CHAR_NEW");
                                sb.AppendLine(String.Format(" - name = {0}", FLMsgType.GetUnicodeString(msg, ref pos)));
                                sb.AppendLine(String.Format(" - nickname = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - base = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - package = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - pilot = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_CHAR_DELETE:
                            sb.AppendLine();
                            sb.AppendLine("MSG_TYPE_CHAR_DELETE");
                            break;
                        case FLMsgType.MSG_TYPE_COMPRESSED:
                            sb.AppendLine();
                            sb.AppendLine("MSG_TYPE_COMPRESSED");
                            break;
                        case FLMsgType.MSG_TYPE_LOGIN_REPORT:
                            {
                                int status = msg[2];

                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_LOGIN_REPORT");
                                sb.AppendLine(String.Format(" - status = {0}", status));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_CHAR_INFO_REQUEST:
                            {
                                int status = msg[1];

                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_CHAR_INFO_REQUEST");
                                sb.AppendLine(String.Format(" - status = {0}", status));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_LOGIN_SERVER_STATUS:
                            {
                                sb.AppendLine();
                                int pos = 1;
                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                uint cmd = FLMsgType.GetUInt16(msg, ref pos);
                                if (subtype == 0x02 && cmd == 0x10)
                                {
                                    sb.AppendLine("MSG_TYPE_LOGIN_SERVER_NEWS");
                                    sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                    sb.AppendLine(String.Format(" - news = {0}", FLMsgType.GetUnicodeString(msg, ref pos)));
                                }
                                else
                                {
                                    sb.AppendLine("MSG_TYPE_LOGIN_SERVER_STATUS");
                                    sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                }
                                break;
                            }
                        case FLMsgType.MSG_TYPE_CHAR_INFO:
                            {
                                int pos = 1;
                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                if (subtype == 0x02)
                                {
                                    sb.AppendLine();
                                    sb.AppendLine("MSG_TYPE_CHAR_INFO");
                                    sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                    uint numchars = FLMsgType.GetUInt8(msg, ref pos);
                                    sb.AppendLine(String.Format(" - numchars = {0}", numchars));

                                    for (int i = 0; i < numchars; i++)
                                    {
                                        sb.AppendLine(String.Format(""));

                                        sb.AppendLine(String.Format(" - charfilename = {0}", FLMsgType.GetAsciiString(msg, ref pos)));
                                        pos += 2;
                                        sb.AppendLine(String.Format(" - charname = {0}", FLMsgType.GetUnicodeString(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - modtime = {0}", FLMsgType.GetUnicodeString(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno = {0}", FLMsgType.GetUInt32(msg, ref pos)));

                                        sb.AppendLine(String.Format(" - tstamp1 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - tstamp2 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - ship = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - cash = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - system = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - base = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - lastbase? = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - voice = {0}", FLMsgType.GetUInt32(msg, ref pos)));

                                        sb.AppendLine(String.Format(" - rank = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno2 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno3 = {0}", FLMsgType.GetFloat(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno4 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno5 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno6 = {0}", FLMsgType.GetUInt32(msg, ref pos)));

                                        sb.AppendLine(String.Format(" - idunno7 (1byte) = {0}", FLMsgType.GetUInt8(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - body = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - head = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - left_hand = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - right_hand = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno8 = {0}", FLMsgType.GetUInt32(msg, ref pos)));

                                        sb.AppendLine(String.Format(" - idunno9 (1byte) = {0}", FLMsgType.GetUInt8(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - body = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - head = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - left_hand = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - right_hand = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                        sb.AppendLine(String.Format(" - idunno10 = {0}", FLMsgType.GetUInt32(msg, ref pos)));

                                        uint numitems = FLMsgType.GetUInt8(msg, ref pos);
                                        sb.AppendLine(String.Format(" - numitems = {0}", numitems));

                                        for (int j = 0; j < numitems; j++)
                                        {
                                            sb.Append(String.Format(" - count = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                            sb.Append(String.Format(" damage = {0}", FLMsgType.GetFloat(msg, ref pos)));
                                            sb.Append(String.Format(" item = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                            sb.Append(String.Format(" slot = {0}", FLMsgType.GetUInt16(msg, ref pos)));
                                            sb.Append(String.Format(" type = {0}", FLMsgType.GetUInt16(msg, ref pos)));
                                            sb.AppendLine(String.Format(" mountpoint = {0}", FLMsgType.GetAsciiString(msg, ref pos)));
                                        }

                                        uint numdunno = FLMsgType.GetUInt32(msg, ref pos);
                                        for (int j = 0; j < numdunno; j++)
                                        {
                                            sb.Append(String.Format(" - type = {0}", FLMsgType.GetUInt16(msg, ref pos)));
                                            sb.AppendLine(String.Format(" dunno = {0}", FLMsgType.GetFloat(msg, ref pos)));
                                        }
                                    }
                                    sb.AppendLine();
                                    sb.AppendLine(String.Format(" - dunno = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                }
                                break;
                            }
                        case FLMsgType.MSG_TYPE_LOGIN_CHAR_SELECT:
                            {
                                int subtype = msg[1];
                                if (subtype == 3)
                                {
                                    int pos = 2;
                                    string charfilename = FLMsgType.GetAsciiString(msg, ref pos);
                                    sb.AppendLine();
                                    sb.AppendLine("MSG_TYPE_LOGIN_CHAR_SELECT");
                                    sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                    sb.AppendLine(String.Format(" - charfilename = {0}", charfilename));
                                }
                                break;
                            }
                        case FLMsgType.MSG_TYPE_SET_NEWS:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);

                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_SET_NEWS");
                                sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                sb.AppendLine(String.Format(" - dunno1 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - dunno2 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - base = {0}", FLMsgType.GetUInt32(msg, ref pos)));

                                for (int i = 0; i < 4; i++)
                                {
                                    sb.Append(String.Format(" - resid = {0}", FLMsgType.GetUInt16(msg, ref pos)));
                                    sb.AppendLine(String.Format(" dll = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                }

                                uint flaglen = FLMsgType.GetUInt32(msg, ref pos);
                                sb.AppendLine(String.Format(" - flaglen = {0}", flaglen));
                                sb.AppendLine(String.Format(" - flag = {0}", new System.Text.ASCIIEncoding().GetString(msg, pos, (int)flaglen)));

                                break;
                            }
                        case FLMsgType.MSG_TYPE_PLAYER_INFO:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_PLAYER_INFO");
                                sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                sb.AppendLine(String.Format(" - clientID? = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - clientIDTo? = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - dunno3 = {0}", FLMsgType.GetUInt8(msg, ref pos)));

                                uint namelen = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine(String.Format(" - namelen = {0}", namelen));
                                if (namelen > 0)
                                {
                                    sb.AppendLine(String.Format(" - name = {0}", new System.Text.UnicodeEncoding().GetString(msg, pos, (int)(namelen-1) * 2)));
                                }
                                break;
                            }
                        case FLMsgType.MSG_TYPE_LOCATION_INFO_REQUEST:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_LOCATION_INFO_REQUEST");
                                sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                sb.AppendLine(String.Format(" - location = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - bool = {0}", FLMsgType.GetUInt8(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_BASE_LOCATION_ENTER:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_BASE_LOCATION_ENTER");
                                sb.AppendLine(String.Format(" - subtype = {0} [{1}]", subtype, subtype == 2 ? "base" : "location"));
                                sb.AppendLine(String.Format(" - base/location = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_BASE_LOCATION_EXIT:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_BASE_EXIT");
                                sb.AppendLine(String.Format(" - subtype = {0} [{1}]", subtype, subtype == 2 ? "base" : "location"));
                                sb.AppendLine(String.Format(" - base = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_ENTER_TRADER_SCREEN:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_BASE_EXIT");
                                sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                sb.AppendLine(String.Format(" - dunno1 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - dunno1= {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                break;
                            }
                        default:
                            break;
                    }
                }

            }
            catch { sb.Append(" [decode error]"); }*/
            return sb.ToString();
        }

        public static string PrintMsgFromClient(string desc, Session dplayid, byte[] msg)
        {
            // Decompress the message if it is compressed.
            /*if (msg[0] == FLMsgType.MSG_TYPE_COMPRESSED)
            {
                using (MemoryStream ms = new MemoryStream(msg, 2, msg.Length - 2))
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        byte[] deflateBuffer = new byte[32767];
                        int msgLength = ds.Read(deflateBuffer, 0, deflateBuffer.Length);
                        Array.Resize(ref deflateBuffer, msgLength);
                        msg = deflateBuffer;
                    }
                }
            }*/

            var sb = new StringBuilder();
            sb.AppendFormat("{0} [{1:x8}] ", desc, dplayid.DPlayID);
            foreach (byte b in msg)
                sb.AppendFormat("{0:x2}.", b);

            /*try
            {

                if (msg.Length > 0)
                {
                    byte type = msg[0];
                    switch (type)
                    {
                        case FLMsgType.MSG_TYPE_CHAR_NEW:
                            {
                                int pos = 2;
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_CHAR_NEW");
                                sb.AppendLine(String.Format(" - name = {0}", FLMsgType.GetUnicodeString(msg, ref pos)));
                                sb.AppendLine(String.Format(" - nickname = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - base = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - package = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - pilot = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_CHAR_DELETE:
                            sb.AppendLine();
                            sb.AppendLine("MSG_TYPE_CHAR_DELETE");
                            break;
                        case FLMsgType.MSG_TYPE_COMPRESSED:
                            sb.AppendLine();
                            sb.AppendLine("MSG_TYPE_COMPRESSED");
                            break;
                        case FLMsgType.MSG_TYPE_CHAR_INFO_REQUEST:
                            {
                                int status = msg[1];

                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_CHAR_INFO_REQUEST");
                                sb.AppendLine(String.Format(" - status = {0}", status));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_LOGIN_CHAR_SELECT:
                            {
                                int subtype = msg[1];
                                if (subtype == 3)
                                {
                                    int pos = 2;
                                    string charfilename = FLMsgType.GetAsciiString(msg, ref pos);
                                    sb.AppendLine();
                                    sb.AppendLine("MSG_TYPE_LOGIN_CHAR_SELECT");
                                    sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                    sb.AppendLine(String.Format(" - charfilename = {0}", charfilename));
                                }
                                break;
                            }
                        
                        case FLMsgType.MSG_TYPE_LOCATION_INFO_REQUEST:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_LOCATION_INFO_REQUEST");
                                sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                sb.AppendLine(String.Format(" - location = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - bool = {0}", FLMsgType.GetUInt8(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_BASE_LOCATION_ENTER:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_BASE_LOCATION_ENTER");
                                sb.AppendLine(String.Format(" - subtype = {0} [{1}]", subtype, subtype == 2 ? "base" : "location"));
                                sb.AppendLine(String.Format(" - base/location = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_BASE_LOCATION_EXIT:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_BASE_LOCATION_EXIT");
                                sb.AppendLine(String.Format(" - subtype = {0} [{1}]", subtype, subtype == 2 ? "base" : "location"));
                                sb.AppendLine(String.Format(" - base = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                break;
                            }
                        case FLMsgType.MSG_TYPE_ENTER_TRADER_SCREEN:
                            {
                                int pos = 1;

                                uint subtype = FLMsgType.GetUInt8(msg, ref pos);
                                sb.AppendLine();
                                sb.AppendLine("MSG_TYPE_ENTER_TRADER_SCREEN");
                                sb.AppendLine(String.Format(" - subtype = {0}", subtype));
                                sb.AppendLine(String.Format(" - dunno1 = {0}", FLMsgType.GetUInt32(msg, ref pos)));
                                sb.AppendLine(String.Format(" - dunno1= {0}", FLMsgType.GetUInt32(msg, ref pos)));

                                break;
                            }
                        default:
                            break;
                    }
                }

            }
            catch { sb.Append(" [decode error]"); } */
            return sb.ToString();
        }
    }
}