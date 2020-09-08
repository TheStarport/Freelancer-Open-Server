using System;

namespace FLDataFile
{
    public static class Extensions
    {


        #region "Setting extensions"
        /// <summary>
        /// Returns the fully qualified INI string of the value.
        /// </summary>
        /// <param name="ins"></param>
        /// <returns></returns>
        public static string String(this Setting ins)
        {

            return string.Format(@"{0} = {1}    ; {2}", ins.Name, string.Join(", ", ins), ins.Comments);
        }

        public static uint GetUInt32(this Setting ins, int position)
        {
            return Convert.ToUInt32(ins[position],10);
        }

        public static int GetInt(this Setting ins, int position)
        {
            return Convert.ToInt32(ins[position], 10);
        }

        /// <summary>
        /// Add another value to current setting.
        /// </summary>
        /// <param name="ins"></param>
        /// <param name="value"></param>
        public static void AddValue(this Setting ins, string value)
        {
            ins.Add(value);
        }
        #endregion


        #region "Section extensions"
        /// <summary>
        /// Add setting to the section. Accepts only non-commented values.
        /// </summary>
        /// <param name="sec">This section.</param>
        /// <param name="str">String to parse.</param>
        public static void AddSetting(this Section sec, string str)
        {
            var s = str.Split('=');
            if (s.Length > 1)
                sec.Settings.Add(new Setting(s[1],s[0]));
        }
        #endregion

        #region "DataFile extensions"


        

        #endregion
    }
}
