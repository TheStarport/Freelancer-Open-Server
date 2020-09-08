namespace FLServer.Chat
{
    public class Rdl
    {
        private byte[] _msg = {};

        public Rdl()
        {
        }

        public Rdl(uint tra, uint mask, string text)
        {
            AddTRA(tra, mask);
            AddText(text);
        }

        public void AddTRA(uint tra, uint mask)
        {
            FLMsgType.AddUInt32(ref _msg, 0x01);
            FLMsgType.AddUInt32(ref _msg, 0x08); // size of data
            FLMsgType.AddUInt32(ref _msg, tra);
            FLMsgType.AddUInt32(ref _msg, mask);
        }

        public void AddText(string text)
        {
            FLMsgType.AddUInt32(ref _msg, 0x02);
            FLMsgType.AddUInt32(ref _msg, 2 + (uint) text.Length*2);
            FLMsgType.AddUnicodeStringLen0(ref _msg, text + "\0");
        }

        public void AddStyle(uint style)
        {
            FLMsgType.AddUInt32(ref _msg, 0x06); // rdl type of style
            FLMsgType.AddUInt32(ref _msg, 0x02); // size of data
            FLMsgType.AddUInt16(ref _msg, style);
        }

        public byte[] GetBytes()
        {
            return _msg;
        }
    }
}