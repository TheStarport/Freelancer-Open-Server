using System;
using System.Collections.Generic;
using System.Linq;

namespace FLDataFile
{
    public class Setting : List<String>
    {

        public string Name { get; set; }
        public string Comments { get; set; }
        private static readonly string[] Delimiters = { ", ","," };
        private static readonly char[] TrimChars = { ' ', '\t' };
        //public  Values;

        /// <summary>
        /// Creates an empty setting.
        /// </summary>
        /// <param name="name"></param>
        public Setting(string name)
        {
            Name = name;
            //Values = new List<string>();
        }

        /// <summary>
        /// Creates a setting parsed from the list of values.
        /// </summary>
        /// <param name="text">Values to parse.</param>
        /// <param name="name">Setting's name.</param>
        public Setting(string text,string name)
        {
            Name = name.Trim(TrimChars);
            //get the values, split em and trim em.
            if ((text.IndexOf(';')) != -1)
                text = text.Substring(0, text.IndexOf(';'));
            text = text.Trim(TrimChars);
            AddRange(
                (
                text
                .Split(Delimiters, StringSplitOptions.None) //split multivalues
                )
                .Select(s => s.Trim()) //remove spaces just to be safe
                );


        }




    }
}
