using System.Collections.Generic;

namespace FLServer
{
    internal enum FLFormatStringElementType
    {
        Resource,
        String,
        Number
    };

    internal class FLFormatStringElement
    {
        public FLFormatStringElementType Type;
        public uint Value;
    }

    public class FLFormatString
    {
        private readonly List<FLFormatStringElement> _elements = new List<FLFormatStringElement>();
        private readonly uint _resourceid;

        public FLFormatString(uint resourceid)
        {
            _resourceid = resourceid;
        }

        public void AddString(uint value)
        {
            var el = new FLFormatStringElement {Type = FLFormatStringElementType.String, Value = value};
            _elements.Add(el);
        }

        public void AddNumber(uint value)
        {
            var el = new FLFormatStringElement {Type = FLFormatStringElementType.Number, Value = value};
            _elements.Add(el);
        }

        public byte[] GetBytes()
        {
            var tmp = new byte[0];
            uint stringCount = 0;
            uint numberCount = 0;
            FLMsgType.AddUInt32(ref tmp, (uint) (6 + (7*_elements.Count))); // size
            FLMsgType.AddUInt32(ref tmp, _resourceid);
            FLMsgType.AddUInt16(ref tmp, (uint) _elements.Count);
            foreach (FLFormatStringElement el in _elements)
            {
                switch (el.Type)
                {
                    case FLFormatStringElementType.String:
                        FLMsgType.AddUInt16(ref tmp, 's');
                        FLMsgType.AddUInt8(ref tmp, stringCount++);
                        FLMsgType.AddUInt32(ref tmp, el.Value);
                        break;
                    case FLFormatStringElementType.Number:
                        FLMsgType.AddUInt16(ref tmp, 'd');
                        FLMsgType.AddUInt8(ref tmp, numberCount++);
                        FLMsgType.AddUInt32(ref tmp, el.Value);
                        break;
                }
            }
            return tmp;
        }
    }
}