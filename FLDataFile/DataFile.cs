using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace FLDataFile
{
    /// <summary>
    /// Generic INI file.
    /// </summary>
    public class DataFile
    {

        public List<Section> Sections;
        private Dictionary<string, Section> _secDictionary; 
        public string Path;


        #region "INI loader"
        /// <summary>
        ///     The super duper microsoft encryption key
        /// </summary>
        private static readonly byte[] Gene = { (byte)'G', (byte)'e', (byte)'n', (byte)'e' };

        /// <summary>
        /// Loads INI and tries to deGene if it's coded.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static byte[] LoadBytes(string path)
        {
            byte[] buf;
            if (!File.Exists(path)) return null;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, (int)fs.Length);
                fs.Close();
            }

            if (buf.Length < 4 || buf[0] != 'F' || buf[1] != 'L' || buf[2] != 'S' || buf[3] != '1') return buf;

            // If this is an encrypted FL ini file then decypt it.
            var dbuf = new byte[buf.Length - 4];

            for (var i = 4; i < buf.Length; i++)
            {
                var k = (Gene[i % 4] + (i - 4)) % 256;
                dbuf[i - 4] = (byte)(buf[i] ^ (k | 0x80));
            }
            return dbuf;
        }

        public DataFile()
        {
            Sections = new List<Section>();
        }

        public DataFile(string path)
        {
            if (path == string.Empty)
                throw new ArgumentException("Bad filename.");

            Path = path;

            var buf = LoadBytes(path);

            //checks for empty file
            if (buf.Length == 0) throw new Exception(string.Format("Empty file: {0}",path));

            Sections = new List<Section>();

            // If this is a bini file then decode it
            if (buf.Length >= 12 && buf[0] == 'B' && buf[1] == 'I' && buf[2] == 'N' && buf[3] == 'I')
            {
                ParseBinary(buf);
            }
            else
            {
                Parse(buf);
            }

        }

        private void Parse(byte[] buf)
        {
            //Watch it go!

            using (var ms = new MemoryStream(buf))
            using (var sr = new StreamReader(ms))
            {
                string s;
                Section curSection = null;
                while ((s = sr.ReadLine()) != null)
                {
                    s = s.Trim();

                    if (s.Length == 0) continue;

                    if (s[0] == ';') continue;

                    if (s[0] == '[')
                    {
                        if (curSection != null)
                        {
                            Sections.Add(curSection);
                        }
                        curSection = new Section(s.Substring(1, s.Length - 2));
                    }
                    else
                    {
                        curSection.AddSetting(s);
                    }
                }

                Sections.Add(curSection);
            }
        }

        //TODO: clean dis
        private void ParseBinary(byte[] buf)
        {
            var p = 8;
            //var version = BitConverter.ToInt32(buf, p); p += 4;
            //not used, change the first offset to 4 if reenabled

            var strTableOffset = BitConverter.ToInt32(buf, p); p += 4;

            while (p < buf.Length && p < strTableOffset)
            {
                int sectionStrOffset = BitConverter.ToInt16(buf, p); p += 2;
                int sectionNumEntries = BitConverter.ToInt16(buf, p); p += 2;
                string sectionName = BufToString(buf, strTableOffset + sectionStrOffset);

                var section = new Section(sectionName);
                Sections.Add(section);

                while (sectionNumEntries-- > 0)
                {
                    int entryStrOffset = BitConverter.ToInt16(buf, p); p += 2;
                    int entryNumValues = buf[p++];

                    
                    var set = new Setting(BufToString(buf, strTableOffset + entryStrOffset));
                    //string desc = fileName + ":0x" + p.ToString("x") + " '" + settingName + "'";
                    //object[] values = new object[entryNumValues];

                    for (var currentValue = 0; currentValue < entryNumValues; currentValue++)
                    {
                        int valueType = buf[p++];
                        var value = BitConverter.ToInt32(buf, p); p += 4;
                        switch (valueType)
                        {
                            case 1: // Integer
                                set.AddValue(value.ToString(CultureInfo.InvariantCulture));
                                break;
                            case 2: // Float
                                set.AddValue(BitConverter.ToString(buf, p - 4));
                                break;
                            case 3: // String
                                set.AddValue(BufToString(buf, strTableOffset + value));
                                break;
                            default:
                                throw new Exception("Unexpected value type at offset=" + (p - 1));
                        }
                    }
                    section.Settings.Add(set);
                }
            }

        }

        /// <summary>
        /// Return the string ending with a null byte.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string BufToString(byte[] buf, int offset)
        {
            var strLen = 0;
            while (buf[strLen] != 0) strLen++;
            return System.Text.Encoding.ASCII.GetString(buf, offset, strLen);
        }

        #endregion


        #region "stuff retrieving"
        /// <summary>
        /// Returns first section with this name. Speeds up consequentive calls if used.
        /// </summary>
        /// <param name="name">Name of the section.</param>
        /// <returns>Section class.</returns>
        public Section GetFirstOf(string name)
        {
            if (_secDictionary == null) _secDictionary = new Dictionary<string, Section>();
            if (!_secDictionary.ContainsKey(name))
                _secDictionary[name] = Sections.FirstOrDefault(a => a.Name == name);

            return _secDictionary[name];
        }

        /// <summary>
        /// Returns all sections matching the name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<Section> GetSections(string name)
        {
            return Sections.Where(a => a.Name == name);
        }


        /// <summary>
        /// Return a list of all items with the specified section name and setting name
        /// </summary>
        /// <param name="sectionName">The section name.</param>
        /// <param name="settingName">The setting name.</param>
        /// <returns></returns>
        public List<Setting> GetSettings(string sectionName, string settingName)
        {
            var ret = new List<Setting>();
            foreach (var sect in GetSections(sectionName))
            {
                ret.AddRange(sect.GetSettings(settingName));
            }
            return ret;
        }


        /// <summary>
        /// Return a list of all items within the specified section(s).
        /// </summary>
        /// <param name="sectionName">The section name.</param>
        /// <returns></returns>
        public List<Setting> GetSettings(string sectionName)
        {
            var ret = new List<Setting>();
            foreach (var sect in GetSections(sectionName))
            {
                ret.AddRange(sect.Settings);
            }
            return ret;
        }

        public Setting GetSetting(string sectionName, string settingName)
        {
            return GetFirstOf(sectionName).GetFirstOf(settingName);
        }

        #endregion


        public void Save(string path)
        {
            if (File.Exists(path)) File.Delete(path);

            var buf = new List<string>();

            foreach (var sec in Sections)
            {
                buf.Add(String.Format("[{0}]",sec.Name));
                buf.AddRange(sec.Settings.Select(set => set.String()));
            }

            File.WriteAllLines(path,buf);
        }
    }
}
