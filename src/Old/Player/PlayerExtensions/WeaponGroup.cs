using System;
using System.Collections.Generic;

namespace FLServer.Player.PlayerExtensions
{
    /// <summary>
    /// Class used to store char weapon groups.
    /// </summary>
    public class WeaponGroup
    {
        //TODO: Send them to player and store in charfile

        private List<string>[] _hpdata;

        public WeaponGroup() 
        {
            _hpdata = new List<string>[6];
        }

        public void SetWeaponGroup(string[] str)
        {
            _hpdata = new List<string>[6];
            foreach (var s in str)
            {
                if (s == "") continue;
                var num = Convert.ToByte(s[s.IndexOf(",", StringComparison.Ordinal) - 1]) - 48;
                if (_hpdata[num] == null) _hpdata[num] = new List<string>();
                _hpdata[num].Add(s.Substring(s.IndexOf(",", StringComparison.Ordinal) + 1));
            }
        }
    }
}
