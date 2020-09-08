using FLDataFile;
using FLServer.DataWorkers;

namespace FLServer.GameDB.Arch
{
    public class Archetype
    {
        string _nickname;
        uint _archID;

        public string Nickname
        {
            get { return _nickname; }
            set
            {
                _nickname = value;
                _archID = FLUtility.CreateID(_nickname);
            }
        }

        public uint ArchetypeID
        {
            get { return _archID; }
        }

        public Archetype(Section sec)
        {
            
        }
    }
}
