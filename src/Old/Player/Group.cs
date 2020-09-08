using System.Collections.Generic;

namespace FLServer.Player
{
    public class Group
    {
        protected bool Equals(Group other)
        {
            return ID == other.ID && Equals(Members, other.Members);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) ID*397) ^ (Members != null ? Members.GetHashCode() : 0);
            }
        }

        public static uint GroupIdsUsed;

        /// <summary>
        ///     Group ID
        /// </summary>
        public readonly uint ID;

        /// <summary>
        ///     A List of Player Objects of all group members
        /// </summary>
        public readonly List<Player> Members;

        public Group()
        {
            ID = ++GroupIdsUsed;
            Members = new List<Player>();
        }


        public void AddPlayer(Player player)
        {
            if (!Members.Contains(player))
            {
                Members.Add(player);
            }
        }

        public void InviteAccepted(Player playerJoined, Player playerInviter)
        {
            Packets.SendChatCommand(playerJoined, Player.ChatCommand.GROUPJOINED, playerInviter.FLPlayerID);

            foreach (Player member in Members)
            {
                Packets.SendChatCommand(member, Player.ChatCommand.NEWGROUPMEMBER, playerJoined.FLPlayerID);

                if (member != playerInviter)
                {
                    Packets.SendChatCommand(playerJoined, Player.ChatCommand.NEWGROUPMEMBER, member.FLPlayerID);
                }
            }

            AddPlayer(playerJoined);
        }

        public void Leave(Player player)
        {
            RemovePlayer(player);

            Packets.SendChatCommand(player, Player.ChatCommand.GROUPLEFT, player.FLPlayerID);

            foreach (var member in Members)
            {
                Packets.SendChatCommand(member, Player.ChatCommand.GROUPMEMBERLEFT, player.FLPlayerID);
            }
        }

        public void RemovePlayer(Player player)
        {
            if (Members.Contains(player))
            {
                Members.Remove(player);
            }
        }

        public override bool Equals(System.Object a)
        {
            if (ReferenceEquals(null, a)) return false;
            if (ReferenceEquals(this, a)) return true;
            if (a.GetType() != GetType()) return false;
            return Equals((Group) a);
        }
    }
}