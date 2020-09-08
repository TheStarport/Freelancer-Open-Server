// ReSharper disable once CheckNamespace
using FLServer.Objects;


namespace FLServer.Actors
{
    partial class PlayerActor
    {
        /// <summary>
        /// Used to set the Player's Account ID.
        /// </summary>
        public class SetAccountID
        {
            public string AccountID;
            public uint FLPlayerID;

            public SetAccountID(string msg, uint flpid)
            {
                AccountID = msg;
                FLPlayerID = flpid;
            }
        }

        /// <summary>
        /// Used to search player with that AccID, it responds only if such accID is found.
        /// </summary>
        public class CheckAccountID
        {
            public string AccountID;

            public CheckAccountID(string msg)
            {
                AccountID = msg;
            }
        }

        /// <summary>
        /// This message is sent from State on character selection (game login).
        /// </summary>
        public class CharSelected
        {
            public CharDB.Account Account;
            public uint FLPlayerID;

            public CharSelected(CharDB.Account acc, uint flpid)
            {
                Account = acc;
                FLPlayerID = flpid;
            }
        }

        /// <summary>
        /// Notify Player about docking, get Account in return.
        /// </summary>
        public class EnterBase
        {
            public uint BaseID;

            public EnterBase(uint bid)
            {
                BaseID = bid;
            }
        }

        public class AccountShipData
        {
            public FLServer.CharDB.Account Account;
            public ShipData ShipData;

            public AccountShipData(FLServer.CharDB.Account account, ShipData shipData)
            {
                this.Account = account;
                this.ShipData = shipData;
            }
            
        }

    }
}
