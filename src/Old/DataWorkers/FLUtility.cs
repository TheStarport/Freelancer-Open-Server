/*
 * Purpose: Miscellanous utility functions.
 * Author: Cannon
 * Date: Jan 2010
 * 
 * Item hash algorithm from flhash.exe by sherlog@t-online.de (2003-06-11)
 * Faction hash algorithm from flfachash.exe by Haenlomal (October 2006)
 * 
 * This is free software. Permission to copy, store and use granted as long
 * as this note remains intact.
 */

using System;
using System.IO;
using System.Text;

namespace FLServer.DataWorkers
{
    internal class FLUtility
    {
        public static DateTime InvalidDate = new DateTime(0);

        /// <summary>
        ///     Look up table for faction id creation.
        /// </summary>
        private static uint[] _createFactionIDTable;

        /// <summary>
        ///     Look up table for id creation.
        /// </summary>
        private static uint[] _createIDTable;

        /// <summary>
        ///     Decode an ascii hex string into unicode
        /// </summary>
        /// <param name="encodedValue">The encoded value</param>
        /// <returns>The deocded value</returns>
        public static string DecodeUnicodeHex(string encodedValue)
        {
            string name = "";
            while (encodedValue.Length > 0)
            {
                name += (char) Convert.ToUInt16(encodedValue.Substring(0, 4), 16);
                encodedValue = encodedValue.Remove(0, 4);
            }
            return name;
        }


        /// <summary>
        ///     Decode a unicode string into ascii hex
        /// </summary>
        /// <param name="value">The value string to encode</param>
        /// <returns>The encoded value</returns>
        public static string EncodeUnicodeHex(string value)
        {
            return BitConverter.ToString(Encoding.BigEndianUnicode.GetBytes(value)).Replace("-", "");
        }


        /// <summary>
        ///     Function for calculating the Freelancer data nickname hash.
        ///     Algorithm from flfachash.c by Haenlomal (October 2006).
        /// </summary>
        /// <param name="nickName"></param>
        /// <returns></returns>
        public static uint CreateFactionID(string nickName)
        {
            const uint flfachashPolynomial = 0x1021;
            const uint numBits = 8;
            const int hashTableSize = 256;

            if (_createFactionIDTable == null)
            {
                // The hash table used is the standard CRC-16-CCITT Lookup table 
                // using the standard big-endian polynomial of 0x1021.
                _createFactionIDTable = new uint[hashTableSize];
                for (uint i = 0; i < hashTableSize; i++)
                {
                    uint x = i << (16 - (int) numBits);
                    for (uint j = 0; j < numBits; j++)
                    {
                        x = ((x & 0x8000) == 0x8000) ? (x << 1) ^ flfachashPolynomial : (x << 1);
                        x &= 0xFFFF;
                    }
                    _createFactionIDTable[i] = x;
                }
            }

            byte[] tNickName = Encoding.ASCII.GetBytes(nickName.ToLowerInvariant());

            uint hash = 0xFFFF;
            for (uint i = 0; i < tNickName.Length; i++)
            {
                uint y = (hash & 0xFF00) >> 8;
                hash = y ^ (_createFactionIDTable[(hash & 0x00FF) ^ tNickName[i]]);
            }

            return hash;
        }

        /// <summary>
        ///     Function for calculating the Freelancer data nickname hash.
        ///     Algorithm from flhash.exe by sherlog@t-online.de (2003-06-11)
        /// </summary>
        /// <param name="nickName"></param>
        /// <returns></returns>
        public static uint CreateID(string nickName)
        {
            const uint flhashPolynomial = 0xA001;
            const int logicalBits = 30;
            const int physicalBits = 32;

            // Build the crc lookup table if it hasn't been created
            if (_createIDTable == null)
            {
                _createIDTable = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint x = i;
                    for (uint bit = 0; bit < 8; bit++)
                        x = ((x & 1) == 1) ? (x >> 1) ^ (flhashPolynomial << (logicalBits - 16)) : x >> 1;
                    _createIDTable[i] = x;
                }
                if (2926433351 != CreateID("st01_to_st03_hole"))
                    throw new Exception("Create ID hash algoritm is broken!");
                if (2460445762 != CreateID("st02_to_st01_hole"))
                    throw new Exception("Create ID hash algoritm is broken!");
                if (2263303234 != CreateID("st03_to_st01_hole"))
                    throw new Exception("Create ID hash algoritm is broken!");
                if (2284213505 != CreateID("li05_to_li01")) throw new Exception("Create ID hash algoritm is broken!");
                if (2293678337 != CreateID("li01_to_li05")) throw new Exception("Create ID hash algoritm is broken!");
            }

            byte[] tNickName = Encoding.ASCII.GetBytes(nickName.ToLowerInvariant());

            // Calculate the hash.
            uint hash = 0;
            for (int i = 0; i < tNickName.Length; i++)
                hash = (hash >> 8) ^ _createIDTable[(byte) hash ^ tNickName[i]];
            // b0rken because byte swapping is not the same as bit reversing, but 
            // that's just the way it is; two hash bits are shifted out and lost
            hash = (hash >> 24) | ((hash >> 8) & 0x0000FF00) | ((hash << 8) & 0x00FF0000) | (hash << 24);
            hash = (hash >> (physicalBits - logicalBits)) | 0x80000000;

            return hash;
        }

        /// <summary>
        ///     Escape a string for an expression.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EscapeLikeExpressionString(string value)
        {
            string escapedText = value;
            escapedText = escapedText.Replace("[", "[[]");
            //filter = filter.Replace("]", "[]]");
            escapedText = escapedText.Replace("%", "[%]");
            escapedText = escapedText.Replace("*", "[*]");
            escapedText = escapedText.Replace("'", "''");
            return escapedText;
        }

        /// <summary>
        ///     Escape a string for an expression.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string EscapeEqualsExpressionString(string value)
        {
            string escapedText = value;
            escapedText = escapedText.Replace("'", "''");
            return escapedText;
        }

        ///// <summary>
        /////     Get the account id from the specified account directory.
        /////     Will throw file open exceptions if the 'name' file cannot be opened.
        ///// </summary>
        ///// <param name="accDirPath">The account directory to search.</param>
        //public static string GetAccountID(string accDirPath)
        //{
        //    string accountIdFilePath = accDirPath + Path.DirectorySeparatorChar + "name";

        //    // Read a 'name' file into memory.
        //    byte[] buf = File.ReadAllBytes(accDirPath);

        //    // Decode the account ID
        //    string accountID = "";
        //    for (int i = 0; i < buf.Length; i += 2)
        //    {
        //        switch (buf[i])
        //        {
        //            case 0x43:
        //                accountID += '-';
        //                break;
        //            case 0x0f:
        //                accountID += 'a';
        //                break;
        //            case 0x0c:
        //                accountID += 'b';
        //                break;
        //            case 0x0d:
        //                accountID += 'c';
        //                break;
        //            case 0x0a:
        //                accountID += 'd';
        //                break;
        //            case 0x0b:
        //                accountID += 'e';
        //                break;
        //            case 0x08:
        //                accountID += 'f';
        //                break;
        //            case 0x5e:
        //                accountID += '0';
        //                break;
        //            case 0x5f:
        //                accountID += '1';
        //                break;
        //            case 0x5c:
        //                accountID += '2';
        //                break;
        //            case 0x5d:
        //                accountID += '3';
        //                break;
        //            case 0x5a:
        //                accountID += '4';
        //                break;
        //            case 0x5b:
        //                accountID += '5';
        //                break;
        //            case 0x58:
        //                accountID += '6';
        //                break;
        //            case 0x59:
        //                accountID += '7';
        //                break;
        //            case 0x56:
        //                accountID += '8';
        //                break;
        //            case 0x57:
        //                accountID += '9';
        //                break;
        //            default:
        //                accountID += '?';
        //                break;
        //        }
        //    }

        //    return accountID;
        //}

        /// <summary>
        ///     Get the account id from the specified account directory.
        ///     Will throw file open exceptions if the 'name' file cannot be opened.
        /// TODO: OBSOLETE, look at it and remove this.
        /// </summary>
        /// <param name="accDirPath">The account directory to search.</param>
        /// <param name="accID"></param>
        public static void WriteAccountID(string accDirPath, string accID)
        {
            string accountIdFilePath = accDirPath + Path.DirectorySeparatorChar + "name";

            var buf = new byte[70];
            for (int i = 0, p = 0; i < accID.Length; i++)
            {
                switch (accID[i])
                {
                    case '-':
                        buf[p++] = 0x43;
                        break;
                    case 'a':
                    case 'A':
                        buf[p++] = 0x0f;
                        break;
                    case 'b':
                    case 'B':
                        buf[p++] += 0x0C;
                        break;
                    case 'c':
                    case 'C':
                        buf[p++] = 0x0D;
                        break;
                    case 'd':
                    case 'D':
                        buf[p++] = 0x0a;
                        break;
                    case 'e':
                    case 'E':
                        buf[p++] = 0x0b;
                        break;
                    case 'f':
                    case 'F':
                        buf[p++] = 0x08;
                        break;
                    case '0':
                        buf[p++] = 0x5e;
                        break;
                    case '1':
                        buf[p++] = 0x5f;
                        break;
                    case '2':
                        buf[p++] = 0x5c;
                        break;
                    case '3':
                        buf[p++] = 0x5d;
                        break;
                    case '4':
                        buf[p++] = 0x5a;
                        break;
                    case '5':
                        buf[p++] = 0x5b;
                        break;
                    case '6':
                        buf[p++] = 0x58;
                        break;
                    case '7':
                        buf[p++] = 0x59;
                        break;
                    case '8':
                        buf[p++] = 0x56;
                        break;
                    case '9':
                        buf[p++] = 0x57;
                        break;
                    default:
                        buf[p++] = 0x00;
                        break;
                }
                buf[p++] = 0x2E;
            }

            // Decode the account ID
            string accountID = "";
            for (int i = 0; i < buf.Length; i += 2)
            {
                switch (buf[i])
                {
                    case 0x43:
                        accountID += '-';
                        break;
                    case 0x0f:
                        accountID += 'a';
                        break;
                    case 0x0c:
                        accountID += 'b';
                        break;
                    case 0x0d:
                        accountID += 'c';
                        break;
                    case 0x0a:
                        accountID += 'd';
                        break;
                    case 0x0b:
                        accountID += 'e';
                        break;
                    case 0x08:
                        accountID += 'f';
                        break;
                    case 0x5e:
                        accountID += '0';
                        break;
                    case 0x5f:
                        accountID += '1';
                        break;
                    case 0x5c:
                        accountID += '2';
                        break;
                    case 0x5d:
                        accountID += '3';
                        break;
                    case 0x5a:
                        accountID += '4';
                        break;
                    case 0x5b:
                        accountID += '5';
                        break;
                    case 0x58:
                        accountID += '6';
                        break;
                    case 0x59:
                        accountID += '7';
                        break;
                    case 0x56:
                        accountID += '8';
                        break;
                    case 0x57:
                        accountID += '9';
                        break;
                    default:
                        accountID += '?';
                        break;
                }
            }


            File.WriteAllBytes(accountIdFilePath, buf);
        }
    }
}