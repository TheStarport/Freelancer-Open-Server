namespace FLServer.Actors.Player.PlayerList
{

    /// <summary>
    /// Used to set the player list responder's data.
    /// </summary>
    class SetListData
    {
        public uint FLPlayerID;
        public uint GroupID;
        public uint Rank;
        public uint SystemID;
        public string CharName;

        public SetListData(uint flpid, uint gid, uint rank, uint sid,string cname)
        {
            FLPlayerID = flpid;
            GroupID = gid;
            Rank = rank;
            SystemID = sid;
            CharName = cname;
        }
    }

    /// <summary>
    /// This message is sent to every player if some char has joined the server.
    /// </summary>
    class PlayerJoined
    {
        public string Name;
        public uint FLPlayerID;
        public bool Hide;

        public PlayerJoined(string name, uint flpid, bool hide = false)
        {
            Name = name;
            FLPlayerID = flpid;
            Hide = hide;
        }
    }

	/// <summary>
	/// This message is sent to every player if someone parts the server.
	/// </summary>
	class PlayerParted
	{
		public string Name;
		public uint FLPlayerID;
		public bool Hide;

		public PlayerParted(string name, uint flpid, bool hide = false)
		{
			Name = name;
			FLPlayerID = flpid;
			Hide = hide;
		}
	}

    /// <summary>
    /// Every present player should send this response to newly joined player.
    /// </summary>
    class PlayerJoinEnumResponse
    {
        public string Name;
        public uint FLPlayerID;
        public bool Hide;
        public PlayerJoinEnumResponse(string name, uint flpid, bool hide = false)
        {
            Name = name;
            FLPlayerID = flpid;
            Hide = hide;
        }

    }

    class PlayerUpdate
    {
        public uint GroupID;
        public uint FLPlayerID;
        public uint Rank;
        public uint SystemID;

        public PlayerUpdate(uint gid, uint flpid, uint rank, uint sid)
        {
            GroupID = gid;
            FLPlayerID = flpid;
            Rank = rank;
            SystemID = sid;
        }

    }
}
