using FLServer.Objects;

namespace FLServer.Actors.Player.State.Base
{

    /// <summary>
    /// Notification about docking at base.
    /// </summary>
    class EnterBase
    {
        public uint BaseID;
        public CharDB.Account Account;

        public EnterBase(uint bid, CharDB.Account acc)
        {
            BaseID = bid;
            Account = acc;
        }
    }

    /// <summary>
    /// We're sending account and ship data to BaseState.
    /// </summary>
    class EnterBaseData
    {
        public FLServer.CharDB.Account Account;
        public ShipData ShipData;
        public uint BaseID;

        public EnterBaseData(FLServer.CharDB.Account account, ShipData shipData, uint baseID)
        {
            this.Account = account;
            this.ShipData = shipData;
            this.BaseID = baseID;
        }
        

    }

    /// <summary>
    /// Use NPC at base.
    /// </summary>
    class SelectObject
    {
        public uint CharID;

        public SelectObject(uint charid)
        {
            CharID = charid;
        }
    }

    /// <summary>
    /// Notification about entering location (room).
    /// </summary>
    class EnterLocation
    {
        public uint RoomID;

        public EnterLocation(uint bid)
        {
            RoomID = bid;
        }
    }

    class RankRequest
    {
        
    }

    /// <summary>
    /// Notification about exiting location (room).
    /// </summary>
    class ExitLocation
    {
        public uint RoomID;

        public ExitLocation(uint bid)
        {
            RoomID = bid;
        }
    }

    /// <summary>
    /// Player docked and requesting base's generic info and startroom.
    /// </summary>
    class BaseInfoRequest
    {
        public byte[] Message;

        public BaseInfoRequest(byte[] msg)
        {
            Message = msg;
        }

    }

    /// <summary>
    /// Player docked and requesting base's generic info and startroom.
    /// </summary>
    class LocInfoRequest
    {
        public byte[] Message;

        public LocInfoRequest(byte[] msg)
        {
            Message = msg;
        }
    }

    class RequestAddItem
    {
        /// <summary>
        /// Good's ID, see BaseDB.UniGoods
        /// </summary>
        public uint GoodID;
        public uint Count;
        public float Health;
        public bool IsMounted;
        public string HpName;

        public RequestAddItem(byte[] msg)
        {
            int pos = 2;
            GoodID = FLMsgType.GetUInt32(msg, ref pos);
            Count = FLMsgType.GetUInt32(msg, ref pos);
            Health = FLMsgType.GetFloat(msg, ref pos);
            IsMounted = FLMsgType.GetUInt8(msg, ref pos) == 1;
            HpName = FLMsgType.GetAsciiStringLen32(msg, ref pos);
        }



    }
}
