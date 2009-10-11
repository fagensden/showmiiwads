﻿/* This file is part of ShowMiiWads
 * Copyright (C) 2009 Leathl
 * 
 * ShowMiiWads is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * ShowMiiWads is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

//Wii.py by icefire / Xuzz was the base for TPL conversion
//Zetsubou by SquidMan was a reference for TPL conversion
//gbalzss by Andre Perrot was the base for LZ77 decompression

#define NoCheckVC

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Wii
{
    public class Tools
    {
        public static event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public static void ChangeProgress(int ProgressPercent)
        {
            EventHandler<ProgressChangedEventArgs> progressChanged = ProgressChanged;
            if (progressChanged != null)
            {
                progressChanged(new object(), new ProgressChangedEventArgs(ProgressPercent));
            }
        }

        /// <summary>
        /// Writes the small Byte Array into the big one at the given offset
        /// </summary>
        /// <param name="big"></param>
        /// <param name="small"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static byte[] InsertByteArray(byte[] big, byte[] small, int offset)
        {
            for (int i = 0; i < small.Length; i++)
                big[offset + i] = small[i];
            return big;
        }

        /// <summary>
        /// Creates a new Byte Array out of the given one
        /// from the given offset with the specified length
        /// </summary>
        /// <param name="array"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] GetPartOfByteArray(byte[] array, int offset, int length)
        {
            byte[] ret = new byte[length];
            for (int i = 0; i < length; i++)
                ret[i] = array[offset + i];
            return ret;
        }

        /// <summary>
        /// Converts UInt32 Array into Byte Array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static byte[] UInt32ArrayToByteArray(UInt32[] array)
        {
            List<byte> results = new List<byte>();
            foreach (UInt32 value in array)
            {
                byte[] converted = BitConverter.GetBytes(value);
                results.AddRange(converted);
            }
            return results.ToArray();
        }

        /// <summary>
        /// Converts UInt16 Array into Byte Array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static byte[] UInt16ArrayToByteArray(UInt16[] array)
        {
            List<byte> results = new List<byte>();
            foreach (UInt16 value in array)
            {
                byte[] converted = BitConverter.GetBytes(value);
                results.AddRange(converted);
            }
            return results.ToArray();
        }

        /// <summary>
        /// Converts Byte Array into UInt16 Array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static UInt32[] ByteArrayToUInt32Array(byte[] array)
        {
            UInt32[] converted = new UInt32[array.Length / 2];
            int j = 0;
            for (int i = 0; i < array.Length; i += 4)
            {
                converted[j] = BitConverter.ToUInt32(array, i);
                j++;
            }
            return converted;
        }

        /// <summary>
        /// Converts Byte Array into UInt16 Array
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static UInt16[] ByteArrayToUInt16Array(byte[] array)
        {
            UInt16[] converted = new UInt16[array.Length / 2];
            int j = 0;
            for (int i = 0; i < array.Length; i += 2)
            {
                converted[j] = BitConverter.ToUInt16(array, i);
                j++;
            }
            return converted;
        }

        /// <summary>
        /// Returns the file length as a Byte Array
        /// </summary>
        /// <param name="filelength"></param>
        /// <returns></returns>
        public static byte[] FileLengthToByteArray(int filelength)
        {
            byte[] length = BitConverter.GetBytes(filelength);
            Array.Reverse(length);
            return length;
        }

        /// <summary>
        /// Adds a padding to the next 64 bytes, if necessary
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static int AddPadding(int value)
        {
            return AddPadding(value, 64);
        }

        /// <summary>
        /// Adds a padding to the given value, if necessary
        /// </summary>
        /// <param name="value"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public static int AddPadding(int value, int padding)
        {
            if (value % padding != 0)
            {
                value = value + (padding - (value % padding));
            }

            return value;
        }

        /// <summary>
        /// Converts a Hex-String to Int
        /// </summary>
        /// <param name="hexstring"></param>
        /// <returns></returns>
        public static int HexStringToInt(string hexstring)
        {
            try { return int.Parse(hexstring, System.Globalization.NumberStyles.HexNumber); }
            catch { throw new Exception("An Error occured, maybe the Wad file is corrupt!"); }
        }

        /// <summary>
        /// Loads a file into a Byte Array
        /// </summary>
        /// <param name="sourcefile"></param>
        /// <returns></returns>
        public static byte[] LoadFileToByteArray(string sourcefile)
        {
            if (File.Exists(sourcefile))
            {
                using (FileStream fs = new FileStream(sourcefile, FileMode.Open))
                {
                    byte[] filearray = new byte[fs.Length];
                    fs.Read(filearray, 0, filearray.Length);
                    return filearray;
                }
            }
            else throw new FileNotFoundException("File couldn't be found:\r\n" + sourcefile);
        }

        /// <summary>
        /// Creates the Common Key
        /// </summary>
        /// <param name="fat">Must be "45e"</param>
        /// <param name="destination">Destination Path</param>
        public static void CreateCommonKey(string fat, string destination)
        {
            //What an effort, lol
            byte[] encryptedwater = new byte[] { 0x4d, 0x89, 0x21, 0x34, 0x62, 0x81, 0xe4, 0x02, 0x37, 0x36, 0xc4, 0xb4, 0xde, 0x40, 0x32, 0xab };
            byte[] key = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, byte.Parse(fat.Remove(2), System.Globalization.NumberStyles.HexNumber), byte.Parse(fat.Remove(0, 2) + "0", System.Globalization.NumberStyles.HexNumber) };
            byte[] decryptedwater = new byte[10];

            RijndaelManaged decryptkey = new RijndaelManaged();
            decryptkey.Mode = CipherMode.CBC;
            decryptkey.Padding = PaddingMode.None;
            decryptkey.KeySize = 128;
            decryptkey.BlockSize = 128;
            decryptkey.Key = key;
            Array.Reverse(key);
            decryptkey.IV = key;

            ICryptoTransform cryptor = decryptkey.CreateDecryptor();

            using (MemoryStream memory = new MemoryStream(encryptedwater))
            {
                using (CryptoStream crypto = new CryptoStream(memory, cryptor, CryptoStreamMode.Read))
                    crypto.Read(decryptedwater, 0, 10);
            }

            string water = BitConverter.ToString(decryptedwater).Replace("-", "").ToLower() + " ";

            water = water.Insert(0, fat[2].ToString());
            water = water.Insert(2, fat[2].ToString());
            water = water.Insert(7, fat[2].ToString());
            water = water.Insert(11, fat[2].ToString());

            water = water.Insert(7, fat[1].ToString());
            water = water.Insert(10, fat[1].ToString());
            water = water.Insert(18, fat[1].ToString());
            water = water.Insert(19, fat[1].ToString());

            water = water.Insert(3, fat[0].ToString());
            water = water.Insert(15, fat[0].ToString());
            water = water.Insert(16, fat[0].ToString());
            water = water.Insert(22, fat[0].ToString());

            byte[] cheese = new byte[16];
            int count = -1;

            for (int i = 0; i < 32; i += 2)
                cheese[++count] = byte.Parse(water.Remove(0, i).Remove(2), System.Globalization.NumberStyles.HexNumber);

            if (destination[destination.Length - 1] != '\\') destination = destination + "\\";
            using (FileStream keystream = new FileStream(destination + "\\common-key.bin", FileMode.Create))
            {
                keystream.Write(cheese, 0, cheese.Length);
            }
        }
    }

    public class WadInfo
    {
        public const int Headersize = 64;
        public static string[] RegionCode = new string[4] { "Japan", "USA", "Europe", "Region Free" };

        /// <summary>
        /// Returns the Header of a Wadfile
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static byte[] GetHeader(byte[] wadfile)
        {
            byte[] Header = new byte[0x20];

            for (int i = 0; i < Header.Length; i++)
            {
                Header[i] = wadfile[i];
            }

            return Header;
        }

        /// <summary>
        /// Returns the size of the Certificate
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetCertSize(byte[] wadfile)
        {
            int size = int.Parse(wadfile[0x08].ToString("x2") + wadfile[0x09].ToString("x2") + wadfile[0x0a].ToString("x2") + wadfile[0x0b].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
            return size;
        }

        /// <summary>
        /// Returns the size of the Ticket
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetTikSize(byte[] wadfile)
        {
            int size = int.Parse(wadfile[0x10].ToString("x2") + wadfile[0x11].ToString("x2") + wadfile[0x12].ToString("x2") + wadfile[0x13].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
            return size;
        }

        /// <summary>
        /// Returns the size of the TMD
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetTmdSize(byte[] wadfile)
        {
            int size = int.Parse(wadfile[0x14].ToString("x2") + wadfile[0x15].ToString("x2") + wadfile[0x16].ToString("x2") + wadfile[0x17].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
            return size;
        }

        /// <summary>
        /// Returns the size of all Contents
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetContentSize(byte[] wadfile)
        {
            int size = int.Parse(wadfile[0x18].ToString("x2") + wadfile[0x19].ToString("x2") + wadfile[0x1a].ToString("x2") + wadfile[0x1b].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
            return size;
        }

        /// <summary>
        /// Returns the size of the Footer
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetFooterSize(byte[] wadfile)
        {
            int size = int.Parse(wadfile[0x1c].ToString("x2") + wadfile[0x1d].ToString("x2") + wadfile[0x1e].ToString("x2") + wadfile[0x1f].ToString("x2"), System.Globalization.NumberStyles.HexNumber);
            return size;
        }

        /// <summary>
        /// Returns the position of the tmd in the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetTmdPos(byte[] wadfile)
        {
            return Headersize + Tools.AddPadding(GetCertSize(wadfile)) + Tools.AddPadding(GetTikSize(wadfile));
        }

        /// <summary>
        /// Returns the position of the ticket in the wad file, ticket or tmd
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetTikPos(byte[] wadfile)
        {
            return Headersize + Tools.AddPadding(GetCertSize(wadfile));
        }

        /// <summary>
        /// Returns the title ID of the wad file.
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="type">0 = Tik, 1 = Tmd</param>
        /// <returns></returns>
        public static string GetTitleID(byte[] wadtiktmd, int type)
        {
            string channeltype = GetChannelType(wadtiktmd, type);
            int tikpos = 0;
            int tmdpos = 0;

            if (IsThisWad(wadtiktmd) == true)
            {
                //It's a wad
                tikpos = GetTikPos(wadtiktmd);
                tmdpos = GetTmdPos(wadtiktmd);
            }

            if (type == 1)
            {
                if (channeltype.Contains("Channel"))
                {
                    string tmdid = Convert.ToChar(wadtiktmd[tmdpos + 0x190]).ToString() + Convert.ToChar(wadtiktmd[tmdpos + 0x191]).ToString() + Convert.ToChar(wadtiktmd[tmdpos + 0x192]).ToString() + Convert.ToChar(wadtiktmd[tmdpos + 0x193]).ToString();
                    return tmdid;
                }
                else if (channeltype.Contains("IOS"))
                {
                    int tmdid = Tools.HexStringToInt(wadtiktmd[tmdpos + 0x190].ToString("x2") + wadtiktmd[tmdpos + 0x191].ToString("x2") + wadtiktmd[tmdpos + 0x192].ToString("x2") + wadtiktmd[tmdpos + 0x193].ToString("x2"));               
                    return "IOS" + tmdid;
                }
                else if (channeltype.Contains("System")) return "SYSTEM";
                else return "";
            }
            else
            {
                if (channeltype.Contains("Channel"))
                {
                    string tikid = Convert.ToChar(wadtiktmd[tikpos + 0x1e0]).ToString() + Convert.ToChar(wadtiktmd[tikpos + 0x1e1]).ToString() + Convert.ToChar(wadtiktmd[tikpos + 0x1e2]).ToString() + Convert.ToChar(wadtiktmd[tikpos + 0x1e3]).ToString();
                    return tikid;
                }
                else if (channeltype.Contains("IOS"))
                {
                    int tikid = Tools.HexStringToInt(wadtiktmd[tikpos + 0x1e0].ToString("x2") + wadtiktmd[tikpos + 0x1e1].ToString("x2") + wadtiktmd[tikpos + 0x1e2].ToString("x2") + wadtiktmd[tikpos + 0x1e3].ToString("x2"));
                    return "IOS" + tikid;
                }
                else if (channeltype.Contains("System")) return "SYSTEM";
                else return "";
            }
        }

        /// <summary>
        /// Returns the title for each language of a wad file.
        /// Order: Jap, Eng, Ger, Fra, Spa, Ita, Dut
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string[] GetChannelTitles(string wadfile)
        {
            byte[] wadarray = Tools.LoadFileToByteArray(wadfile);
            return GetChannelTitles(wadarray);
        }

        /// <summary>
        /// Returns the title for each language of a wad file.
        /// Order: Jap, Eng, Ger, Fra, Spa, Ita, Dut
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string[] GetChannelTitles(byte[] wadfile)
        {
            if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\common-key.bin") || File.Exists(System.Windows.Forms.Application.StartupPath + "\\key.bin"))
            {
                string channeltype = GetChannelType(wadfile, 0);

                if (channeltype.Contains("Channel") && !channeltype.Contains("Hidden"))
                {
                    string[] titles = new string[7];

                    //Detection from footer is turned off, cause the footer
                    //can be easily edited and thus the titles in it could be simply wrong

                    //int footer = GetFooterSize(wadfile);
                    //if (footer > 0)
                    //{
                    //    int footerpos = wadfile.Length - footer;
                    //    int count = 0;
                    //    int imetpos = 0;

                    //    if ((wadfile.Length - (wadfile.Length - footer)) < 250) return new string[7];

                    //    for (int z = 0; z < 250; z++)
                    //    {
                    //        if (Convert.ToChar(wadfile[footerpos + z]) == 'I')
                    //            if (Convert.ToChar(wadfile[footerpos + z + 1]) == 'M')
                    //                if (Convert.ToChar(wadfile[footerpos + z + 2]) == 'E')
                    //                    if (Convert.ToChar(wadfile[footerpos + z + 3]) == 'T')
                    //                    {
                    //                        imetpos = footerpos + z;
                    //                        break;
                    //                    }
                    //    }

                    //    int jappos = imetpos + 29;

                    //    for (int i = jappos; i < jappos + 588; i += 84)
                    //    {
                    //        for (int j = 0; j < 40; j += 2)
                    //        {
                    //            if (wadfile[i + j] != 0x00)
                    //            {
                    //                char temp = Convert.ToChar(wadfile[i + j]);
                    //                titles[count] += temp;
                    //            }
                    //        }

                    //        count++;
                    //    }

                    //    return titles;
                    //}

                    if (channeltype.Contains("Channel") && !channeltype.Contains("Hidden"))
                    {
                        string[,] conts = GetContentInfo(wadfile);
                        byte[] titlekey = GetTitleKey(wadfile);
                        int nullapp = 0;

                        for (int i = 0; i < conts.GetLength(0); i++)
                        {
                            if (conts[i, 1] == "00000000")
                                nullapp = i;
                        }

                        byte[] contenthandle = WadEdit.DecryptContent(wadfile, nullapp, titlekey);
                        int imetpos = 0;

                        if (contenthandle.Length < 400) return new string[7];

                        for (int z = 0; z < 400; z++)
                        {
                            if (Convert.ToChar(contenthandle[z]) == 'I')
                                if (Convert.ToChar(contenthandle[z + 1]) == 'M')
                                    if (Convert.ToChar(contenthandle[z + 2]) == 'E')
                                        if (Convert.ToChar(contenthandle[z + 3]) == 'T')
                                        {
                                            imetpos = z;
                                            break;
                                        }
                        }

                        int jappos = imetpos + 29;
                        int count = 0;

                        for (int i = jappos; i < jappos + 588; i += 84)
                        {
                            for (int j = 0; j < 40; j += 2)
                            {
                                if (contenthandle[i + j] != 0x00)
                                {
                                    char temp = Convert.ToChar(contenthandle[i + j]);
                                    titles[count] += temp;
                                }
                            }

                            count++;
                        }
                    }


                    return titles;
                }
                else return new string[7];
            }
            else return new string[7];
        }

        /// <summary>
        /// Returns the title for each language of a 00.app file
        /// Order: Jap, Eng, Ger, Fra, Spa, Ita, Dut
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static string[] GetChannelTitlesFromApp(byte[] app)
        {
            string[] titles = new string[7];

            int imetpos = 0;
            int length = 400;

            if (app.Length < 400) length = app.Length;

            for (int z = 0; z < length; z++)
            {
                if (Convert.ToChar(app[z]) == 'I')
                    if (Convert.ToChar(app[z + 1]) == 'M')
                        if (Convert.ToChar(app[z + 2]) == 'E')
                            if (Convert.ToChar(app[z + 3]) == 'T')
                            {
                                imetpos = z;
                                break;
                            }
            }

            if (imetpos != 0)
            {
                int jappos = imetpos + 29;
                int count = 0;

                for (int i = jappos; i < jappos + 588; i += 84)
                {
                    for (int j = 0; j < 40; j += 2)
                    {
                        if (app[i + j] != 0x00)
                        {
                            char temp = Convert.ToChar(app[i + j]);
                            titles[count] += temp;
                        }
                    }

                    count++;
                }
            }

            return titles;
        }

        /// <summary>
        /// Returns the Type of the Channel as a string
        /// Wad or Tik needed for WiiWare / VC detection!
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string GetChannelType(byte[] wadtiktmd, int type)
        {
            int tikpos = 0;
            int tmdpos = 0;

            if (IsThisWad(wadtiktmd) == true)
            {
                //It's a wad
                tikpos = GetTikPos(wadtiktmd);
                tmdpos = GetTmdPos(wadtiktmd);
            }

            string thistype = "";

            if (type == 0)
            { thistype = wadtiktmd[tikpos + 0x1dc].ToString("x2") + wadtiktmd[tikpos + 0x1dd].ToString("x2") + wadtiktmd[tikpos + 0x1de].ToString("x2") + wadtiktmd[tikpos + 0x1df].ToString("x2"); }
            else { thistype = wadtiktmd[tmdpos + 0x18c].ToString("x2") + wadtiktmd[tmdpos + 0x18d].ToString("x2") + wadtiktmd[tmdpos + 0x18e].ToString("x2") + wadtiktmd[tmdpos + 0x18f].ToString("x2"); }
            string channeltype = "Unknown";

            if (thistype == "00010001")
            {
#if CheckVC
                //Unfortunately, this returns some channel (e.g. official) to be VC / WW, so it's turned off...
                if (CheckWiiWareVC(wadtiktmd) == true) channeltype = "VC / WW Channel";
                else channeltype = "Channel Title";
#elif NoCheckVC
                channeltype = "Channel Title";
#endif
            }
            else if (thistype == "00010002") channeltype = "System Channel";
            else if (thistype == "00010004" || thistype == "00010000") channeltype = "Game Channel";
            else if (thistype == "00010005") channeltype = "Downloaded Content";
            else if (thistype == "00010008") channeltype = "Hidden Channel";
            else if (thistype == "00000001")
            {
                channeltype = "System: IOS";

                string thisid = "";
                if (type == 0) { thisid = wadtiktmd[tikpos + 0x1e0].ToString("x2") + wadtiktmd[tikpos + 0x1e1].ToString("x2") + wadtiktmd[tikpos + 0x1e2].ToString("x2") + wadtiktmd[tikpos + 0x1e3].ToString("x2"); }
                else { thisid = wadtiktmd[tmdpos + 0x190].ToString("x2") + wadtiktmd[tmdpos + 0x191].ToString("x2") + wadtiktmd[tmdpos + 0x192].ToString("x2") + wadtiktmd[tmdpos + 0x193].ToString("x2"); }

                if (thisid == "00000001") channeltype = "System: Boot2";
                else if (thisid == "00000002") channeltype = "System: Menu";
                else if (thisid == "00000100") channeltype = "System: BC";
                else if (thisid == "00000101") channeltype = "System: MIOS";
            }

            return channeltype;
        }

        /// <summary>
        /// Returns the amount of included Contents (app-files)
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetContentNum(byte[] wadtmd)
        {
            int tmdpos = 0;

            if (IsThisWad(wadtmd) == true)
            {
                //It's a wad file, so get the tmd position
                tmdpos = GetTmdPos(wadtmd);
            }

            int contents = Tools.HexStringToInt(wadtmd[tmdpos + 0x1de].ToString("x2") + wadtmd[tmdpos + 0x1df].ToString("x2"));

            return contents;
        }

        /// <summary>
        /// Returns the approx. destination size on the Wii
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string GetNandSize(byte[] wadtmd, bool ConvertToMB)
        {
            int tmdpos = 0;
            int minsize = 0;
            int maxsize = 0;
            int numcont = GetContentNum(wadtmd);

            if (IsThisWad(wadtmd) == true)
            {
                //It's a wad
                tmdpos = GetTmdPos(wadtmd);
            }

            for (int i = 0; i < numcont; i++)
            {
                int cont = 36 * i;
                int contentsize = Tools.HexStringToInt(wadtmd[tmdpos + 0x1e4 + 8 + cont].ToString("x2") +
                    wadtmd[tmdpos + 0x1e5 + 8 + cont].ToString("x2") +
                    wadtmd[tmdpos + 0x1e6 + 8 + cont].ToString("x2") +
                    wadtmd[tmdpos + 0x1e7 + 8 + cont].ToString("x2") +
                    wadtmd[tmdpos + 0x1e8 + 8 + cont].ToString("x2") +
                    wadtmd[tmdpos + 0x1e9 + 8 + cont].ToString("x2") +
                    wadtmd[tmdpos + 0x1ea + 8 + cont].ToString("x2") +
                    wadtmd[tmdpos + 0x1eb + 8 + cont].ToString("x2"));

                string type = wadtmd[tmdpos + 0x1e4 + 6 + cont].ToString("x2") + wadtmd[tmdpos + 0x1e5 + 6 + cont].ToString("x2");

                if (type == "0001")
                {
                    minsize += contentsize;
                    maxsize += contentsize;
                }
                else if (type == "8001")
                    maxsize += contentsize;
            }

            string size = "";

            if (maxsize == minsize) size = maxsize.ToString();
            else size = minsize.ToString() + " - " + maxsize.ToString();

            if (ConvertToMB == true)
            {
                if (size.Contains("-"))
                {
                    string teil1 = size.Remove(size.IndexOf(' '));
                    string teil2 = size.Remove(0, size.IndexOf('-') + 2);

                    teil1 = Convert.ToString(Math.Round(Convert.ToDouble(teil1) * 0.0009765625 * 0.0009765625, 2));
                    teil2 = Convert.ToString(Math.Round(Convert.ToDouble(teil2) * 0.0009765625 * 0.0009765625, 2));
                    if (teil1.Length > 4) { teil1 = teil1.Remove(4); } //Round besser?!
                    if (teil2.Length > 4) { teil2 = teil2.Remove(4); }
                    size = teil1 + " - " + teil2 + " MB";
                }
                else
                {
                    size = Convert.ToString(Math.Round(Convert.ToDouble(size) * 0.0009765625 * 0.0009765625, 2));
                    if (size.Length > 4) { size = size.Remove(4); }
                    size = size + " MB";
                }
            }

            return size;
        }

        /// <summary>
        /// Returns the approx. destination block on the Wii
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string GetNandBlocks(byte[] wadtmd)
        {
            string size = GetNandSize(wadtmd, false);

            if (size.Contains("-"))
            {
                string size1 = size.Remove(size.IndexOf(' '));
                string size2 = size.Remove(0, size.LastIndexOf(' ') + 1);

                double blocks1 = (double)((Convert.ToDouble(size1) / 1024) / 128);
                double blocks2 = (double)((Convert.ToDouble(size2) / 1024) / 128);

                return Math.Ceiling(blocks1) + " - " + Math.Ceiling(blocks2);
            }
            else
            {
                double blocks = (double)((Convert.ToDouble(size) / 1024) / 128);

                return Math.Ceiling(blocks).ToString();
            }
        }

        /// <summary>
        /// Returns the title version of the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static int GetTitleVersion(byte[] wadtmd)
        {
            int tmdpos = 0;

            if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }
            return Tools.HexStringToInt(wadtmd[tmdpos + 0x1dc].ToString("x2") + wadtmd[tmdpos + 0x1dd].ToString("x2"));
        }

        /// <summary>
        /// Returns the IOS that is needed by the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string GetIosFlag(byte[] wadtmd)
        {
            string type = GetChannelType(wadtmd, 1);

            if (!type.Contains("IOS") && !type.Contains("BC"))
            {
                int tmdpos = 0;
                if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }
                return "IOS" + Tools.HexStringToInt(wadtmd[tmdpos + 0x188].ToString("x2") + wadtmd[tmdpos + 0x189].ToString("x2") + wadtmd[tmdpos + 0x18a].ToString("x2") + wadtmd[tmdpos + 0x18b].ToString("x2"));
            }
            else return "";
        }

        /// <summary>
        /// Returns the region of the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string GetRegionFlag(byte[] wadtmd)
        {
            int tmdpos = 0;

            if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }

            if (GetChannelType(wadtmd, 1).Contains("Channel"))
            {
                int region = Tools.HexStringToInt(wadtmd[tmdpos + 0x19d].ToString("x2"));
                return RegionCode[region];
            }
            else return "";
        }

        /// <summary>
        /// Returns the Path where the wad will be installed on the Wii
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string GetNandPath(string wadfile)
        {
            byte[] wad = Tools.LoadFileToByteArray(wadfile);
            return GetNandPath(wad, 0);
        }

        /// <summary>
        /// Returns the Path where the wad will be installed on the Wii
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="type">0 = Tik, 1 = Tmd</param>
        /// <returns></returns>
        public static string GetNandPath(byte[] wadtiktmd, int type)
        {
            int tikpos = 0;
            int tmdpos = 0;

            if (IsThisWad(wadtiktmd) == true)
            {
                tikpos = GetTikPos(wadtiktmd);
                tmdpos = GetTmdPos(wadtiktmd);
            }

            string thispath = "";

            if (type == 0)
            {
                thispath = wadtiktmd[tikpos + 0x1dc].ToString("x2") +
                    wadtiktmd[tikpos + 0x1dd].ToString("x2") +
                    wadtiktmd[tikpos + 0x1de].ToString("x2") +
                    wadtiktmd[tikpos + 0x1df].ToString("x2") +
                    wadtiktmd[tikpos + 0x1e0].ToString("x2") +
                    wadtiktmd[tikpos + 0x1e1].ToString("x2") +
                    wadtiktmd[tikpos + 0x1e2].ToString("x2") +
                    wadtiktmd[tikpos + 0x1e3].ToString("x2");
            }
            else
            {
                thispath = wadtiktmd[tmdpos + 0x18c].ToString("x2") +
                    wadtiktmd[tmdpos + 0x18d].ToString("x2") +
                    wadtiktmd[tmdpos + 0x18e].ToString("x2") +
                    wadtiktmd[tmdpos + 0x18f].ToString("x2") +
                    wadtiktmd[tmdpos + 0x190].ToString("x2") +
                    wadtiktmd[tmdpos + 0x191].ToString("x2") +
                    wadtiktmd[tmdpos + 0x192].ToString("x2") +
                    wadtiktmd[tmdpos + 0x193].ToString("x2");
            }

            thispath = thispath.Insert(8, "\\");
            return thispath;
        }

        /// <summary>
        /// Returns true, if the wad file is a WiiWare / VC title
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static bool CheckWiiWareVC(byte[] wadtik)
        {
            int tikpos = 0;

            if (IsThisWad(wadtik) == true) { tikpos = GetTikPos(wadtik); }

            if (wadtik[tikpos + 0x221] == 0x01) return true;
            else return false;
        }

        /// <summary>
        /// Returns all information stored in the tmd for all contents in the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static string[,] GetContentInfo(byte[] wadtmd)
        {
            int tmdpos = 0;

            if (IsThisWad(wadtmd) == true) { tmdpos = GetTmdPos(wadtmd); }
            int contentcount = GetContentNum(wadtmd);
            string[,] contentinfo = new string[contentcount, 5];

            for (int i = 0; i < contentcount; i++)
            {
                contentinfo[i, 0] = wadtmd[tmdpos + 0x1e4 + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1e5 + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1e6 + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1e7 + (36 * i)].ToString("x2");
                contentinfo[i, 1] = "0000" +
                    wadtmd[tmdpos + 0x1e8 + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1e9 + (36 * i)].ToString("x2");
                contentinfo[i, 2] = wadtmd[tmdpos + 0x1ea + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1eb + (36 * i)].ToString("x2");
                contentinfo[i, 3] = Tools.HexStringToInt(
                    wadtmd[tmdpos + 0x1ec + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1ed + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1ee + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1ef + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1f0 + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1f1 + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1f2 + (36 * i)].ToString("x2") +
                    wadtmd[tmdpos + 0x1f3 + (36 * i)].ToString("x2")).ToString();

                for (int j = 0; j < 20; j++)
                {
                    contentinfo[i, 4] += wadtmd[tmdpos + 0x1f4 + (36 * i) + j].ToString("x2");
                }
            }

            return contentinfo;
        }

        /// <summary>
        /// Returns the Tik of the wad file as a Byte-Array
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static byte[] ReturnTik(byte[] wadfile)
        {
            int tikpos = GetTikPos(wadfile);
            int tiksize = GetTikSize(wadfile);

            byte[] tik = new byte[tiksize];

            for (int i = 0; i < tiksize; i++)
            {
                tik[i] = wadfile[tikpos + i];
            }

            return tik;
        }

        /// <summary>
        /// Returns the Tmd of the wad file as a Byte-Array
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static byte[] ReturnTmd(byte[] wadfile)
        {
            int tmdpos = GetTmdPos(wadfile);
            int tmdsize = GetTmdSize(wadfile);

            byte[] tmd = new byte[tmdsize];

            for (int i = 0; i < tmdsize; i++)
            {
                tmd[i] = wadfile[tmdpos + i];
            }

            return tmd;
        }

        /// <summary>
        /// Checks, if the given file is a wad
        /// </summary>
        /// <param name="wadtiktmd"></param>
        /// <returns></returns>
        public static bool IsThisWad(byte[] wadtiktmd)
        {
            if (wadtiktmd[0] == 0x00 &&
                wadtiktmd[1] == 0x00 &&
                wadtiktmd[2] == 0x00 &&
                wadtiktmd[3] == 0x20 &&
                wadtiktmd[4] == 0x49 &&
                wadtiktmd[5] == 0x73)
            { return true; }

            return false;
        }

        /// <summary>
        /// Returns the decrypted TitleKey
        /// </summary>
        /// <param name="wadtik"></param>
        /// <returns></returns>
        public static byte[] GetTitleKey(byte[] wadtik)
        {
            byte[] commonkey = new byte[16];

            if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\common-key.bin"))
            { commonkey = Tools.LoadFileToByteArray(System.Windows.Forms.Application.StartupPath + "\\common-key.bin"); }
            else if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\key.bin"))
            { commonkey = Tools.LoadFileToByteArray(System.Windows.Forms.Application.StartupPath + "\\key.bin"); }
            else { throw new FileNotFoundException("The (common-)key.bin must be in the application directory!"); }

            byte[] encryptedkey = new byte[16];
            byte[] iv = new byte[16];
            int tikpos = 0;

            if (IsThisWad(wadtik) == true)
            {
                //It's a wad file, so get the tik position
                tikpos = GetTikPos(wadtik);
            }

            for (int i = 0; i < 16; i++)
            {
                encryptedkey[i] = wadtik[tikpos + 0x1bf + i];
            }

            for (int j = 0; j < 8; j++)
            {
                iv[j] = wadtik[tikpos + 0x1dc + j];
                iv[j + 8] = 0x00;
            }

            RijndaelManaged decrypt = new RijndaelManaged();
            decrypt.Mode = CipherMode.CBC;
            decrypt.Padding = PaddingMode.None;
            decrypt.KeySize = 128;
            decrypt.BlockSize = 128;
            decrypt.Key = commonkey;
            decrypt.IV = iv;

            ICryptoTransform cryptor = decrypt.CreateDecryptor();

            MemoryStream memory = new MemoryStream(encryptedkey);
            CryptoStream crypto = new CryptoStream(memory, cryptor, CryptoStreamMode.Read);

            byte[] decryptedkey = new byte[16];
            crypto.Read(decryptedkey, 0, decryptedkey.Length);

            crypto.Close();
            memory.Close();

            return decryptedkey;
        }
    }

    public class WadEdit
    {
        /// <summary>
        /// Changes the region of the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="region">0 = JAP, 1 = USA, 2 = EUR, 3 = FREE</param>
        /// <returns></returns>
        public static byte[] ChangeRegion(byte[] wadfile, int region)
        {

            int tmdpos = WadInfo.GetTmdPos(wadfile);

            if (region == 0) wadfile[tmdpos + 0x19d] = 0x00;
            else if (region == 1) wadfile[tmdpos + 0x19d] = 0x01;
            else if (region == 2) wadfile[tmdpos + 0x19d] = 0x02;
            else wadfile[tmdpos + 0x19d] = 0x03;

            wadfile = TruchaSign(wadfile, 1);

            return wadfile;
        }

        /// <summary>
        /// Changes the region of the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="region"></param>
        public static void ChangeRegion(string wadfile, int region)
        {
            byte[] wadarray = Tools.LoadFileToByteArray(wadfile);
            wadarray = ChangeRegion(wadarray, region);

            using (FileStream fs = new FileStream(wadfile, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(wadarray, 0, wadarray.Length);
            }
        }

        /// <summary>
        /// Changes the Channel Title of the wad file
        /// All languages have the same title
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static byte[] ChangeChannelTitle(byte[] wadfile, string title)
        {
            return ChangeChannelTitle(wadfile, title, title, title, title, title, title, title);
        }

        /// <summary>
        /// Changes the Channel Title of the wad file
        /// Each language has a specific title
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="jap"></param>
        /// <param name="eng"></param>
        /// <param name="ger"></param>
        /// <param name="fra"></param>
        /// <param name="spa"></param>
        /// <param name="ita"></param>
        /// <param name="dut"></param>
        public static void ChangeChannelTitle(string wadfile, string jap, string eng, string ger, string fra, string spa, string ita, string dut)
        {
            byte[] wadarray = Tools.LoadFileToByteArray(wadfile);
            wadarray = ChangeChannelTitle(wadarray, jap, eng, ger, fra, spa, ita, dut);

            using (FileStream fs = new FileStream(wadfile, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(wadarray, 0, wadarray.Length);
            }
        }

        /// <summary>
        /// Changes the Channel Title of the wad file
        /// Each language has a specific title
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="jap">Japanese Title</param>
        /// <param name="eng">English Title</param>
        /// <param name="ger">German Title</param>
        /// <param name="fra">French Title</param>
        /// <param name="spa">Spanish Title</param>
        /// <param name="ita">Italian Title</param>
        /// <param name="dut">Dutch Title</param>
        /// <returns></returns>
        public static byte[] ChangeChannelTitle(byte[] wadfile, string jap, string eng, string ger, string fra, string spa, string ita, string dut)
        {
            Tools.ChangeProgress(0);

            char[] japchars = jap.ToCharArray();
            char[] engchars = eng.ToCharArray();
            char[] gerchars = ger.ToCharArray();
            char[] frachars = fra.ToCharArray();
            char[] spachars = spa.ToCharArray();
            char[] itachars = ita.ToCharArray();
            char[] dutchars = dut.ToCharArray();

            byte[] titlekey = WadInfo.GetTitleKey(wadfile);
            string[,] conts = WadInfo.GetContentInfo(wadfile);
            int tmdpos = WadInfo.GetTmdPos(wadfile);
            int tmdsize = WadInfo.GetTmdSize(wadfile);
            int nullapp = 0;
            int contentpos = 64 + Tools.AddPadding(WadInfo.GetCertSize(wadfile)) + Tools.AddPadding(WadInfo.GetTikSize(wadfile)) + Tools.AddPadding(WadInfo.GetTmdSize(wadfile));
            SHA1Managed sha1 = new SHA1Managed();

            Tools.ChangeProgress(10);
            
            for (int i = 0; i < conts.GetLength(0); i++)
            {
                if (conts[i, 1] == "00000000")
                {
                    nullapp = i;
                    break;
                }
                else
                    contentpos += Tools.AddPadding(Convert.ToInt32(conts[i, 3]));
            }
            
            byte[] contenthandle = DecryptContent(wadfile, nullapp, titlekey);
            
            Tools.ChangeProgress(25);

            int imetpos = 0;

            for (int z = 0; z < 400; z++)
            {
                if (Convert.ToChar(contenthandle[z]) == 'I')
                    if (Convert.ToChar(contenthandle[z + 1]) == 'M')
                        if (Convert.ToChar(contenthandle[z + 2]) == 'E')
                            if (Convert.ToChar(contenthandle[z + 3]) == 'T')
                            {
                                imetpos = z;
                                break;
                            }
            }

            Tools.ChangeProgress(40);

            int count = 0;

            for (int x = imetpos; x < imetpos + 40; x += 2)
            {
                if (japchars.Length > count) { contenthandle[x + 29] = Convert.ToByte(japchars[count]); }
                else { contenthandle[x + 29] = 0x00; }
                if (engchars.Length > count) { contenthandle[x + 29 + 84] = Convert.ToByte(engchars[count]); }
                else { contenthandle[x + 29 + 84] = 0x00; }
                if (gerchars.Length > count) { contenthandle[x + 29 + 84 * 2] = Convert.ToByte(gerchars[count]); }
                else { contenthandle[x + 29 + 84 * 2] = 0x00; }
                if (frachars.Length > count) { contenthandle[x + 29 + 84 * 3] = Convert.ToByte(frachars[count]); }
                else { contenthandle[x + 29 + 84 * 3] = 0x00; }
                if (spachars.Length > count) { contenthandle[x + 29 + 84 * 4] = Convert.ToByte(spachars[count]); }
                else { contenthandle[x + 29 + 84 * 4] = 0x00; }
                if (itachars.Length > count) { contenthandle[x + 29 + 84 * 5] = Convert.ToByte(itachars[count]); }
                else { contenthandle[x + 29 + 84 * 5] = 0x00; }
                if (dutchars.Length > count) { contenthandle[x + 29 + 84 * 6] = Convert.ToByte(dutchars[count]); }
                else { contenthandle[x + 29 + 84 * 6] = 0x00; }

                count++;
            }

            Tools.ChangeProgress(50);

            byte[] newmd5 = new byte[16];
            contenthandle = FixMD5InImet(contenthandle, out newmd5);
            byte[] newsha = sha1.ComputeHash(contenthandle);

            contenthandle = EncryptContent(contenthandle, WadInfo.ReturnTmd(wadfile), nullapp, titlekey, false);

            Tools.ChangeProgress(70);

            for (int y = 0; y < contenthandle.Length; y++)
            {
                wadfile[contentpos + y] = contenthandle[y];
            }

            //SHA1 in TMD
            byte[] tmd = Tools.GetPartOfByteArray(wadfile, tmdpos, tmdsize);
            for (int i = 0; i < 20; i++)
                tmd[0x1f4 + (36 * nullapp) + i] = newsha[i];
            TruchaSign(tmd, 1);
            wadfile = Tools.InsertByteArray(wadfile, tmd, tmdpos);

            int footer = WadInfo.GetFooterSize(wadfile);

            Tools.ChangeProgress(80);

            if (footer > 0)
            {
                int footerpos = wadfile.Length - footer;
                int imetposfoot = 0;

                for (int z = 0; z < 200; z++)
                {
                    if (Convert.ToChar(wadfile[footerpos + z]) == 'I')
                        if (Convert.ToChar(wadfile[footerpos + z + 1]) == 'M')
                            if (Convert.ToChar(wadfile[footerpos + z + 2]) == 'E')
                                if (Convert.ToChar(wadfile[footerpos + z + 3]) == 'T')
                                {
                                    imetposfoot = footerpos + z;
                                    break;
                                }
                }

                Tools.ChangeProgress(90);

                int count2 = 0;

                for (int x = imetposfoot; x < imetposfoot + 40; x += 2)
                {
                    if (japchars.Length > count2) { wadfile[x + 29] = Convert.ToByte(japchars[count2]); }
                    else { wadfile[x + 29] = 0x00; }
                    if (engchars.Length > count2) { wadfile[x + 29 + 84] = Convert.ToByte(engchars[count2]); }
                    else { wadfile[x + 29 + 84] = 0x00; }
                    if (gerchars.Length > count2) { wadfile[x + 29 + 84 * 2] = Convert.ToByte(gerchars[count2]); }
                    else { wadfile[x + 29 + 84 * 2] = 0x00; }
                    if (frachars.Length > count2) { wadfile[x + 29 + 84 * 3] = Convert.ToByte(frachars[count2]); }
                    else { wadfile[x + 29 + 84 * 3] = 0x00; }
                    if (spachars.Length > count2) { wadfile[x + 29 + 84 * 4] = Convert.ToByte(spachars[count2]); }
                    else { wadfile[x + 29 + 84 * 4] = 0x00; }
                    if (itachars.Length > count2) { wadfile[x + 29 + 84 * 5] = Convert.ToByte(itachars[count2]); }
                    else { wadfile[x + 29 + 84 * 5] = 0x00; }
                    if (dutchars.Length > count2) { wadfile[x + 29 + 84 * 6] = Convert.ToByte(dutchars[count2]); }
                    else { wadfile[x + 29 + 84 * 6] = 0x00; }

                    count2++;
                }

                for (int i = 0; i < 16; i++)
                    wadfile[imetposfoot + 1456 + i] = newmd5[i];
            }

            Tools.ChangeProgress(100);
            return wadfile;
        }

        /// <summary>
        /// Changes the title ID of the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="titleid"></param>
        /// <returns></returns>
        public static byte[] ChangeTitleID(byte[] wadfile, string titleid)
        {
            Tools.ChangeProgress(0);

            int tikpos = WadInfo.GetTikPos(wadfile);
            int tmdpos = WadInfo.GetTmdPos(wadfile);
            char[] id = titleid.ToCharArray();

            byte[] oldtitlekey = WadInfo.GetTitleKey(wadfile);

            Tools.ChangeProgress(20);

            //Change the ID in the ticket
            wadfile[tikpos + 0x1e0] = (byte)id[0];
            wadfile[tikpos + 0x1e1] = (byte)id[1];
            wadfile[tikpos + 0x1e2] = (byte)id[2];
            wadfile[tikpos + 0x1e3] = (byte)id[3];

            //Change the ID in the tmd
            wadfile[tmdpos + 0x190] = (byte)id[0];
            wadfile[tmdpos + 0x191] = (byte)id[1];
            wadfile[tmdpos + 0x192] = (byte)id[2];
            wadfile[tmdpos + 0x193] = (byte)id[3];

            Tools.ChangeProgress(40);

            //Trucha-Sign both
            TruchaSign(wadfile, 0);

            Tools.ChangeProgress(50);

            TruchaSign(wadfile, 1);

            Tools.ChangeProgress(60);

            byte[] newtitlekey = WadInfo.GetTitleKey(wadfile);
            byte[] tmd = WadInfo.ReturnTmd(wadfile);

            int contentcount = WadInfo.GetContentNum(wadfile);

            wadfile = ReEncryptAllContents(wadfile, oldtitlekey, newtitlekey);

            Tools.ChangeProgress(100);
            return wadfile;
        }

        /// <summary>
        /// Changes the title ID of the wad file
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="titleid"></param>
        public static void ChangeTitleID(string wadfile, string titleid)
        {
            byte[] wadarray = Tools.LoadFileToByteArray(wadfile);
            wadarray = ChangeTitleID(wadarray, titleid);

            using (FileStream fs = new FileStream(wadfile, FileMode.Open, FileAccess.Write))
            {
                fs.Seek(0, SeekOrigin.Begin);
                fs.Write(wadarray, 0, wadarray.Length);
            }
        }

        /// <summary>
        /// Clears the Signature of the Tik / Tmd to 0x00
        /// </summary>
        /// <param name="wadtiktmd">Wad, Tik or Tmd</param>
        /// <param name="type">0 = Tik, 1 = Tmd</param>
        /// <returns></returns>
        public static byte[] ClearSignature(byte[] wadtiktmd, int type)
        {
            int tmdtikpos = 0;
            int tmdtiksize = wadtiktmd.Length; ;

            if (WadInfo.IsThisWad(wadtiktmd) == true)
            {
                //It's a wad file, so get the tik or tmd position and length
                switch (type)
                {
                    case 1:
                        tmdtikpos = WadInfo.GetTmdPos(wadtiktmd);
                        tmdtiksize = WadInfo.GetTmdSize(wadtiktmd);
                        break;
                    default:
                        tmdtikpos = WadInfo.GetTikPos(wadtiktmd);
                        tmdtiksize = WadInfo.GetTikSize(wadtiktmd);
                        break;
                }
            }

            for (int i = 4; i < 260; i++)
            {
                wadtiktmd[tmdtikpos + i] = 0x00;
            }

            return wadtiktmd;
        }

        /// <summary>
        /// Trucha-Signs the Tik or Tmd
        /// </summary>
        /// <param name="wadortmd">Wad or Tik or Tmd</param>
        /// <param name="type">0 = Tik, 1 = Tmd</param>
        /// <returns></returns>
        public static byte[] TruchaSign(byte[] wadtiktmd, int type)
        {
            SHA1Managed sha = new SHA1Managed();
            int[] position = new int[2] { 0x1f1, 0x1d4 }; //0x104 0x1c1
            int[] tosign = new int[2] { 0x140, 0x140 }; //0x104 0x140
            int tiktmdpos = 0;
            int tiktmdsize = wadtiktmd.Length;

            if (sha.ComputeHash(wadtiktmd, tiktmdpos + tosign[type], tiktmdsize - tosign[type])[0] != 0x00)
            {
                ClearSignature(wadtiktmd, type);

                if (WadInfo.IsThisWad(wadtiktmd) == true)
                {
                    //It's a wad file
                    if (type == 0) //Get Tik position and size
                    {
                        tiktmdpos = WadInfo.GetTikPos(wadtiktmd);
                        tiktmdsize = WadInfo.GetTikSize(wadtiktmd);
                    }
                    else //Get Tmd position and size
                    {
                        tiktmdpos = WadInfo.GetTmdPos(wadtiktmd);
                        tiktmdsize = WadInfo.GetTmdSize(wadtiktmd);
                    }
                }

                byte[] sha1 = new byte[20];

                for (UInt16 i = 0; i < 65535; i++)
                {
                    byte[] hex = BitConverter.GetBytes(i);
                    wadtiktmd[tiktmdpos + position[type]] = hex[0];
                    wadtiktmd[tiktmdpos + position[type] + 1] = hex[1];

                    sha1 = sha.ComputeHash(wadtiktmd, tiktmdpos + tosign[type], tiktmdsize - tosign[type]);
                    if (sha1[0] == 0x00) break;
                }

                return wadtiktmd;
            }
            else return wadtiktmd;
        }

        /// <summary>
        /// Decrypts the given content
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static byte[] DecryptContent(byte[] wadfile, int contentcount, byte[] titlekey)
        {
            int tmdpos = WadInfo.GetTmdPos(wadfile);
            byte[] iv = new byte[16];
            string[,] continfo = WadInfo.GetContentInfo(wadfile);
            int contentsize = Convert.ToInt32(continfo[contentcount, 3]);
            int paddedsize = Tools.AddPadding(contentsize, 16);

            int contentpos = 64 + Tools.AddPadding(WadInfo.GetCertSize(wadfile)) + Tools.AddPadding(WadInfo.GetTikSize(wadfile)) + Tools.AddPadding(WadInfo.GetTmdSize(wadfile));

            for (int x = 0; x < contentcount; x++)
            {
                contentpos += Tools.AddPadding(Convert.ToInt32(continfo[x, 3]));
            }

            iv[0] = wadfile[tmdpos + 0x1e8 + (0x24 * contentcount)];
            iv[1] = wadfile[tmdpos + 0x1e9 + (0x24 * contentcount)];

            RijndaelManaged decrypt = new RijndaelManaged();
            decrypt.Mode = CipherMode.CBC;
            decrypt.Padding = PaddingMode.None;
            decrypt.KeySize = 128;
            decrypt.BlockSize = 128;
            decrypt.Key = titlekey;
            decrypt.IV = iv;

            ICryptoTransform cryptor = decrypt.CreateDecryptor();

            MemoryStream memory = new MemoryStream(wadfile, contentpos, paddedsize);
            CryptoStream crypto = new CryptoStream(memory, cryptor, CryptoStreamMode.Read);

            bool fullread = false;
            byte[] buffer = new byte[16384];
            byte[] cont = new byte[1];

            using (MemoryStream ms = new MemoryStream())
            {
                while (fullread == false)
                {
                    int len = 0;
                    if ((len = crypto.Read(buffer, 0, buffer.Length)) <= 0)
                    {
                        fullread = true;
                        cont = ms.ToArray();
                    }
                    ms.Write(buffer, 0, len);
                }
            }

            memory.Close();
            crypto.Close();

            Array.Resize(ref cont, contentsize);

            return cont;
        }

        /// <summary>
        /// Decrypts the given content
        /// </summary>
        /// <param name="content"></param>
        /// <param name="tmd"></param>
        /// <param name="contentcount"></param>
        /// <param name="titlekey"></param>
        /// <returns></returns>
        public static byte[] DecryptContent(byte[] content, byte[] tmd, int contentcount, byte[] titlekey)
        {
            byte[] iv = new byte[16];
            string[,] continfo = WadInfo.GetContentInfo(tmd);
            int contentsize = content.Length;
            int paddedsize = Tools.AddPadding(contentsize, 16);
            Array.Resize(ref content, paddedsize);

            iv[0] = tmd[0x1e8 + (0x24 * contentcount)];
            iv[1] = tmd[0x1e9 + (0x24 * contentcount)];

            RijndaelManaged decrypt = new RijndaelManaged();
            decrypt.Mode = CipherMode.CBC;
            decrypt.Padding = PaddingMode.None;
            decrypt.KeySize = 128;
            decrypt.BlockSize = 128;
            decrypt.Key = titlekey;
            decrypt.IV = iv;

            ICryptoTransform cryptor = decrypt.CreateDecryptor();

            MemoryStream memory = new MemoryStream(content, 0, paddedsize);
            CryptoStream crypto = new CryptoStream(memory, cryptor, CryptoStreamMode.Read);

            bool fullread = false;
            byte[] buffer = new byte[memory.Length];
            byte[] cont = new byte[1];

            using (MemoryStream ms = new MemoryStream())
            {
                while (fullread == false)
                {
                    int len = 0;
                    if ((len = crypto.Read(buffer, 0, buffer.Length)) <= 0)
                    {
                        fullread = true;
                        cont = ms.ToArray();
                    }
                    ms.Write(buffer, 0, len);
                }
            }

            memory.Close();
            crypto.Close();

            return cont;
        }

        /// <summary>
        /// Encrypts the given content and adds a padding to the next 64 bytes
        /// </summary>
        /// <param name="content"></param>
        /// <param name="tmd"></param>
        /// <param name="contentcount"></param>
        /// <param name="titlekey"></param>
        /// <returns></returns>
        public static byte[] EncryptContent(byte[] content, byte[] tmd, int contentcount, byte[] titlekey, bool addpadding)
        {
            byte[] iv = new byte[16];
            string[,] continfo = WadInfo.GetContentInfo(tmd);
            int contentsize = content.Length;
            int paddedsize = Tools.AddPadding(contentsize, 16);
            Array.Resize(ref content, paddedsize);

            iv[0] = tmd[0x1e8 + (0x24 * contentcount)];
            iv[1] = tmd[0x1e9 + (0x24 * contentcount)];

            RijndaelManaged encrypt = new RijndaelManaged();
            encrypt.Mode = CipherMode.CBC;
            encrypt.Padding = PaddingMode.None;
            encrypt.KeySize = 128;
            encrypt.BlockSize = 128;
            encrypt.Key = titlekey;
            encrypt.IV = iv;

            ICryptoTransform cryptor = encrypt.CreateEncryptor();

            MemoryStream memory = new MemoryStream(content, 0, paddedsize);
            CryptoStream crypto = new CryptoStream(memory, cryptor, CryptoStreamMode.Read);

            bool fullread = false;
            byte[] buffer = new byte[memory.Length];
            byte[] cont = new byte[1];

            using (MemoryStream ms = new MemoryStream())
            {
                while (fullread == false)
                {
                    int len = 0;
                    if ((len = crypto.Read(buffer, 0, buffer.Length)) <= 0)
                    {
                        fullread = true;
                        cont = ms.ToArray();
                    }
                    ms.Write(buffer, 0, len);
                }
            }

            memory.Close();
            crypto.Close();

            if (addpadding == true) { Array.Resize(ref cont, Tools.AddPadding(cont.Length)); }
            return cont;
        }

        /// <summary>
        /// Re-Encrypts the given content
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static byte[] ReEncryptAllContents(byte[] wadfile, byte[] oldtitlekey, byte[] newtitlekey)
        {
            int contentnum = WadInfo.GetContentNum(wadfile);
            int certsize = WadInfo.GetCertSize(wadfile);
            int tiksize = WadInfo.GetTikSize(wadfile);
            int tmdsize = WadInfo.GetTmdSize(wadfile);
            int contentpos = 64 + Tools.AddPadding(certsize) + Tools.AddPadding(tiksize) + Tools.AddPadding(tmdsize);

            for (int i = 0; i < contentnum; i++)
            {
                byte[] tmd = WadInfo.ReturnTmd(wadfile);
                byte[] decryptedcontent = DecryptContent(wadfile, i, oldtitlekey);
                byte[] encryptedcontent = EncryptContent(decryptedcontent, tmd, i, newtitlekey, true);
                for (int j = 0; j < encryptedcontent.Length; j++)
                {
                    wadfile[contentpos + j] = encryptedcontent[j];
                }
                contentpos += Tools.AddPadding(encryptedcontent.Length);
            }

            return wadfile;
        }

        /// <summary>
        /// Fixes the MD5 Sum in the IMET Header
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static byte[] FixMD5InImet(byte[] file, out byte[] newmd5)
        {
            if (Convert.ToChar(file[128]) == 'I' &&
                Convert.ToChar(file[129]) == 'M' &&
                Convert.ToChar(file[130]) == 'E' &&
                Convert.ToChar(file[131]) == 'T')
            {
                byte[] buffer = new byte[1536];

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(file, 0x40, 1536);
                    buffer = ms.ToArray();
                }

                for (int i = 0; i < 16; i++)
                    buffer[1520 + i] = 0x00;

                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] hash = md5.ComputeHash(buffer);

                for (int i = 0; i < 16; i++)
                    file[1584 + i] = hash[i];

                newmd5 = hash;
                return file;
            }
            else
            {
                byte[] oldmd5 = new byte[16];

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(file, 1584, 16);
                    oldmd5 = ms.ToArray();
                }

                newmd5 = oldmd5;
                return file;
            }
        }

        public static byte[] FixMD5InImet(byte[] file)
        {
            byte[] tmp = new byte[16];
            return FixMD5InImet(file, out tmp);
        }
    }

    public class WadUnpack
    {
        /// <summary>
        /// Unpacks the the wad file
        /// </summary>
        public static void UnpackWad(string pathtowad, string destinationpath)
        {
            byte[] wadfile = Tools.LoadFileToByteArray(pathtowad);
            UnpackWad(wadfile, destinationpath);
        }

        /// <summary>
        /// Unpacks the wad file to *wadpath*\wadunpack\
        /// </summary>
        /// <param name="pathtowad"></param>
        public static void UnpackWad(string pathtowad)
        {
            string destinationpath = pathtowad.Remove(pathtowad.LastIndexOf('\\'));
            byte[] wadfile = Tools.LoadFileToByteArray(pathtowad);
            UnpackWad(wadfile, destinationpath);
        }

        /// <summary>
        /// Unpacks the 00000000.app of a wad
        /// </summary>
        /// <param name="wadfile"></param>
        /// <returns></returns>
        public static byte[] UnpackNullApp(byte[] wadfile)
        {
            int certsize = WadInfo.GetCertSize(wadfile);
            int tiksize = WadInfo.GetTikSize(wadfile);
            int tmdpos = WadInfo.GetTmdPos(wadfile);
            int tmdsize = WadInfo.GetTmdSize(wadfile);
            int contentpos = 64 + Tools.AddPadding(certsize) + Tools.AddPadding(tiksize) + Tools.AddPadding(tmdsize);

            byte[] titlekey = WadInfo.GetTitleKey(wadfile);
            string[,] contents = WadInfo.GetContentInfo(wadfile);

            for (int i = 0; i < contents.GetLength(0); i++)
            {
                if (contents[i, 1] == "00000000")
                {
                    return WadEdit.DecryptContent(wadfile, i, titlekey);
                }
            }

            throw new Exception("00000000.app couldn't be found in the Wad");
        }

        /// <summary>
        /// Unpacks the wad file
        /// </summary>
        public static void UnpackWad(byte[] wadfile, string destinationpath)
        {
            if (destinationpath[destinationpath.Length - 1] != '\\')
            { destinationpath = destinationpath + "\\"; }

            if (!Directory.Exists(destinationpath))
            { Directory.CreateDirectory(destinationpath); }
            if (Directory.GetFiles(destinationpath, "*.app").Length > 1 ||
                Directory.GetFiles(destinationpath, "*.cert").Length > 1 ||
                Directory.GetFiles(destinationpath, "*.tik").Length > 1 ||
                Directory.GetFiles(destinationpath, "*.tmd").Length > 1 ||
                Directory.GetFiles(destinationpath, "*.trailer").Length > 1)
            {
                throw new Exception("One of the files to unpack already exists!");
            }

            int certpos = 0x40;
            int certsize = WadInfo.GetCertSize(wadfile);
            int tikpos = WadInfo.GetTikPos(wadfile);
            int tiksize = WadInfo.GetTikSize(wadfile);
            int tmdpos = WadInfo.GetTmdPos(wadfile);
            int tmdsize = WadInfo.GetTmdSize(wadfile);
            int contentlength = WadInfo.GetContentSize(wadfile);
            int footersize = WadInfo.GetFooterSize(wadfile);
            int footerpos = 64 + Tools.AddPadding(certsize) + Tools.AddPadding(tiksize) + Tools.AddPadding(tmdsize) + Tools.AddPadding(contentlength);
            string wadpath = WadInfo.GetNandPath(wadfile, 0).Remove(8, 1);
            string[,] contents = WadInfo.GetContentInfo(wadfile);
            byte[] titlekey = WadInfo.GetTitleKey(wadfile);
            int contentpos = 64 + Tools.AddPadding(certsize) + Tools.AddPadding(tiksize) + Tools.AddPadding(tmdsize);

            //unpack cert
            using (FileStream cert = new FileStream(destinationpath + wadpath + ".cert", FileMode.Create))
            {
                cert.Seek(0, SeekOrigin.Begin);
                cert.Write(wadfile, certpos, certsize);
            }

            //unpack ticket
            using (FileStream tik = new FileStream(destinationpath + wadpath + ".tik", FileMode.Create))
            {
                tik.Seek(0, SeekOrigin.Begin);
                tik.Write(wadfile, tikpos, tiksize);
            }

            //unpack tmd
            using (FileStream tmd = new FileStream(destinationpath + wadpath + ".tmd", FileMode.Create))
            {
                tmd.Seek(0, SeekOrigin.Begin);
                tmd.Write(wadfile, tmdpos, tmdsize);
            }

            //unpack trailer
            if (footersize > 0)
            {
                using (FileStream trailer = new FileStream(destinationpath + wadpath + ".trailer", FileMode.Create))
                {
                    trailer.Seek(0, SeekOrigin.Begin);
                    trailer.Write(wadfile, footerpos, footersize);
                }
            }

            Tools.ChangeProgress(0);

            //unpack contents
            for (int i = 0; i < contents.GetLength(0); i++)
            {
                Tools.ChangeProgress((i + 1) * 100 / contents.GetLength(0));
                byte[] thiscontent = WadEdit.DecryptContent(wadfile, i, titlekey);
                FileStream content = new FileStream(destinationpath + contents[i, 1] + ".app", FileMode.Create);

                content.Write(thiscontent, 0, thiscontent.Length);
                content.Close();

                contentpos += Tools.AddPadding(thiscontent.Length);
            }
        }

        /// <summary>
        /// Unpacks the wad file to the given directory
        /// Shared contents will not be unpacked
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="nandpath"></param>
        public static void UnpackToNand(string wadfile, string nandpath)
        {
            byte[] wadarray = Tools.LoadFileToByteArray(wadfile);
            UnpackToNand(wadarray, nandpath);
        }

        /// <summary>
        /// Unpacks the wad file to the given directory
        /// Shared contents will not be unpacked
        /// </summary>
        /// <param name="wadfile"></param>
        /// <param name="nandpath"></param>
        public static void UnpackToNand(byte[] wadfile, string nandpath)
        {
            string path = WadInfo.GetNandPath(wadfile, 0);
            string path1 = path.Remove(path.IndexOf('\\'));
            string path2 = path.Remove(0, path.IndexOf('\\') + 1);

            if (nandpath[nandpath.Length - 1] != '\\') { nandpath = nandpath + "\\"; }

            if (!Directory.Exists(nandpath + "ticket")) { Directory.CreateDirectory(nandpath + "ticket"); }
            if (!Directory.Exists(nandpath + "title")) { Directory.CreateDirectory(nandpath + "title"); }
            if (!Directory.Exists(nandpath + "ticket\\" + path1)) { Directory.CreateDirectory(nandpath + "ticket\\" + path1); }
            if (!Directory.Exists(nandpath + "title\\" + path1)) { Directory.CreateDirectory(nandpath + "title\\" + path1); }
            if (!Directory.Exists(nandpath + "title\\" + path1 + "\\" + path2)) { Directory.CreateDirectory(nandpath + "title\\" + path1 + "\\" + path2); }
            if (!Directory.Exists(nandpath + "title\\" + path1 + "\\" + path2 + "\\content")) { Directory.CreateDirectory(nandpath + "title\\" + path1 + "\\" + path2 + "\\content"); }
            if (!Directory.Exists(nandpath + "title\\" + path1 + "\\" + path2 + "\\data")) { Directory.CreateDirectory(nandpath + "title\\" + path1 + "\\" + path2 + "\\data"); }
            if (!Directory.Exists(nandpath + "shared1")) Directory.CreateDirectory(nandpath + "shared1");

            int certsize = WadInfo.GetCertSize(wadfile);
            int tikpos = WadInfo.GetTikPos(wadfile);
            int tiksize = WadInfo.GetTikSize(wadfile);
            int tmdpos = WadInfo.GetTmdPos(wadfile);
            int tmdsize = WadInfo.GetTmdSize(wadfile);
            int contentlength = WadInfo.GetContentSize(wadfile);
            string[,] contents = WadInfo.GetContentInfo(wadfile);
            byte[] titlekey = WadInfo.GetTitleKey(wadfile);
            int contentpos = 64 + Tools.AddPadding(certsize) + Tools.AddPadding(tiksize) + Tools.AddPadding(tmdsize);

            //unpack ticket
            using (FileStream tik = new FileStream(nandpath + "ticket\\" + path1 + "\\" + path2 + ".tik", FileMode.Create))
            {
                tik.Seek(0, SeekOrigin.Begin);
                tik.Write(wadfile, tikpos, tiksize);
            }

            //unpack tmd
            using (FileStream tmd = new FileStream(nandpath + "title\\" + path1 + "\\" + path2 + "\\content\\title.tmd", FileMode.Create))
            {
                tmd.Seek(0, SeekOrigin.Begin);
                tmd.Write(wadfile, tmdpos, tmdsize);
            }

            Tools.ChangeProgress(0);

            //unpack contents
            for (int i = 0; i < contents.GetLength(0); i++)
            {
                Tools.ChangeProgress((i + 1) * 100 / contents.GetLength(0));
                byte[] thiscontent = WadEdit.DecryptContent(wadfile, i, titlekey);

                if (contents[i, 2] == "8001")
                {
                    if (File.Exists(nandpath + "shared1\\content.map"))
                    {
                        byte[] contmap = Tools.LoadFileToByteArray(nandpath + "shared1\\content.map");

                        if (ContentMap.CheckSharedContent(contmap, contents[i, 4]) == false)
                        {
                            string newname = ContentMap.GetNewSharedContentName(contmap);

                            FileStream content = new FileStream(nandpath + "shared1\\" + newname + ".app", FileMode.Create);
                            content.Write(thiscontent, 0, thiscontent.Length);
                            content.Close();
                            ContentMap.AddSharedContent(nandpath + "shared1\\content.map", newname, contents[i, 4]);
                        }
                    }
                    else
                    {
                        FileStream content = new FileStream(nandpath + "shared1\\00000000.app", FileMode.Create);
                        content.Write(thiscontent, 0, thiscontent.Length);
                        content.Close();
                        ContentMap.AddSharedContent(nandpath + "shared1\\content.map", "00000000", contents[i, 4]);
                    }
                }
                else
                {
                    FileStream content = new FileStream(nandpath + "title\\" + path1 + "\\" + path2 + "\\content\\" + contents[i, 0] + ".app", FileMode.Create);

                    content.Write(thiscontent, 0, thiscontent.Length);
                    content.Close();
                }

                contentpos += Tools.AddPadding(thiscontent.Length);
            }
        }
    }

    public class WadPack
    {
        public static byte[] wadheader = new byte[8] { 0x00, 0x00, 0x00, 0x20, 0x49, 0x73, 0x00, 0x00 };

        /// <summary>
        /// Packs the contents in the given directory and creates the destination wad file 
        /// </summary>
        /// <param name="directory"></param>
        public static void PackWad(string contentdirectory, string destinationfile, bool includefooter)
        {
            if (contentdirectory[contentdirectory.Length - 1] != '\\') { contentdirectory = contentdirectory + "\\"; }

            if (!Directory.Exists(contentdirectory)) throw new DirectoryNotFoundException("The directory doesn't exists:\r\n" + contentdirectory);
            if (Directory.GetFiles(contentdirectory, "*.app").Length < 1) throw new Exception("No *.app file was found");
            if (Directory.GetFiles(contentdirectory, "*.cert").Length < 1) throw new Exception("No *.cert file was found");
            if (Directory.GetFiles(contentdirectory, "*.tik").Length < 1) throw new Exception("No *.tik file was found");
            if (Directory.GetFiles(contentdirectory, "*.tmd").Length < 1) throw new Exception("No *.tmd file was found");
            if (File.Exists(destinationfile)) throw new Exception("The destination file already exists!");

            string[] certfile = Directory.GetFiles(contentdirectory, "*.cert");
            string[] tikfile = Directory.GetFiles(contentdirectory, "*.tik");
            string[] tmdfile = Directory.GetFiles(contentdirectory, "*.tmd");
            string[] trailerfile = Directory.GetFiles(contentdirectory, "*.trailer");

            byte[] cert = Tools.LoadFileToByteArray(certfile[0]);
            byte[] tik = Tools.LoadFileToByteArray(tikfile[0]);
            byte[] tmd = Tools.LoadFileToByteArray(tmdfile[0]);

            string[,] contents = WadInfo.GetContentInfo(tmd);

            FileStream wadstream = new FileStream(destinationfile, FileMode.Create);

            //Trucha-Sign Tik and Tmd, if they aren't already
            WadEdit.TruchaSign(tik, 0);
            WadEdit.TruchaSign(tmd, 1);

            //Write Cert
            wadstream.Seek(64, SeekOrigin.Begin);
            wadstream.Write(cert, 0, cert.Length);

            //Write Tik
            wadstream.Seek(64 + Tools.AddPadding(cert.Length), SeekOrigin.Begin);
            wadstream.Write(tik, 0, tik.Length);

            //Write Tmd
            wadstream.Seek(64 + Tools.AddPadding(cert.Length) + Tools.AddPadding(tik.Length), SeekOrigin.Begin);
            wadstream.Write(tmd, 0, tmd.Length);

            //Write Content
            int allcont = 0;
            int contpos = 64 + Tools.AddPadding(cert.Length) + Tools.AddPadding(tik.Length) + Tools.AddPadding(tmd.Length);
            int contcount = WadInfo.GetContentNum(tmd);

            Tools.ChangeProgress(0);
            byte[] titlekey = WadInfo.GetTitleKey(tik);

            for (int i = 0; i < contents.GetLength(0); i++)
            {
                Tools.ChangeProgress((i + 1) * 100 / contents.GetLength(0));
                byte[] thiscont = Tools.LoadFileToByteArray(contentdirectory + contents[i, 1] + ".app");
                if (i == contents.GetLength(0) - 1) { thiscont = WadEdit.EncryptContent(thiscont, tmd, i, titlekey, false); }
                else { thiscont = WadEdit.EncryptContent(thiscont, tmd, i, titlekey, true); }
                wadstream.Seek(contpos, SeekOrigin.Begin);
                wadstream.Write(thiscont, 0, thiscont.Length);
                contpos += thiscont.Length;
                allcont += thiscont.Length;
            }

            //Write Trailer, if exists and includefooter = true
            int trailerlength = 0;
            if (trailerfile.Length > 0 && includefooter == true)
            {
                byte[] trailer = Tools.LoadFileToByteArray(trailerfile[0]);
                trailerlength = trailer.Length;
                Array.Resize(ref trailer, Tools.AddPadding(trailer.Length));
                wadstream.Seek(contpos, SeekOrigin.Begin);
                wadstream.Write(trailer, 0, trailer.Length);
            }

            //Write Header
            byte[] certsize = Tools.FileLengthToByteArray(cert.Length);
            byte[] tiksize = Tools.FileLengthToByteArray(tik.Length);
            byte[] tmdsize = Tools.FileLengthToByteArray(tmd.Length);
            byte[] allcontsize = Tools.FileLengthToByteArray(allcont);
            byte[] trailersize = Tools.FileLengthToByteArray(trailerlength);

            wadstream.Seek(0x00, SeekOrigin.Begin);
            wadstream.Write(wadheader, 0, wadheader.Length);
            wadstream.Seek(0x08, SeekOrigin.Begin);
            wadstream.Write(certsize, 0, certsize.Length);
            wadstream.Seek(0x10, SeekOrigin.Begin);
            wadstream.Write(tiksize, 0, tiksize.Length);
            wadstream.Seek(0x14, SeekOrigin.Begin);
            wadstream.Write(tmdsize, 0, tmdsize.Length);
            wadstream.Seek(0x18, SeekOrigin.Begin);
            wadstream.Write(allcontsize, 0, allcontsize.Length);
            wadstream.Seek(0x1c, SeekOrigin.Begin);
            wadstream.Write(trailersize, 0, trailersize.Length);

            wadstream.Close();
        }

        /// <summary>
        /// Packs a Wad from a title installed on Nand
        /// Returns: 0 = OK, 1 = Files missing, 2 = Shared contents missing, 3 = Cert missing
        /// </summary>
        /// <param name="nandpath"></param>
        /// <param name="path">XXXXXXXX\XXXXXXXX</param>
        /// <param name="destinationfile"></param>
        /// <returns></returns>
        public static void PackWadFromNand(string nandpath, string path, string destinationfile)
        {
            if (nandpath[nandpath.Length - 1] != '\\') { nandpath = nandpath + "\\"; }
            string path1 = path.Remove(8);
            string path2 = path.Remove(0, 9);
            string ticketdir = nandpath + "ticket\\" + path1 + "\\";
            string contentdir = nandpath + "title\\" + path1 + "\\" + path2 + "\\content\\";
            string sharedir = nandpath + "shared1\\";
            string certdir = nandpath + "sys\\";

            if (!Directory.Exists(ticketdir) ||
                !Directory.Exists(contentdir)) throw new DirectoryNotFoundException("Directory doesn't exist:\r\n" + contentdir);
            if (!Directory.Exists(sharedir)) throw new DirectoryNotFoundException("Directory doesn't exist:\r\n" + sharedir);
            if (!File.Exists(certdir + "cert.sys")) throw new FileNotFoundException("File doesn't exist:\r\n" + certdir + "cert.sys");

            byte[] cert = Tools.LoadFileToByteArray(certdir + "cert.sys");
            byte[] tik = Tools.LoadFileToByteArray(ticketdir + path2 + ".tik");
            byte[] tmd = Tools.LoadFileToByteArray(contentdir + "title.tmd");

            string[,] contents = WadInfo.GetContentInfo(tmd);

            FileStream wadstream = new FileStream(destinationfile, FileMode.Create);

            //Trucha-Sign Tik and Tmd, if they aren't already
            WadEdit.TruchaSign(tik, 0);
            WadEdit.TruchaSign(tmd, 1);

            //Write Cert
            wadstream.Seek(64, SeekOrigin.Begin);
            wadstream.Write(cert, 0, cert.Length);

            //Write Tik
            wadstream.Seek(64 + Tools.AddPadding(cert.Length), SeekOrigin.Begin);
            wadstream.Write(tik, 0, tik.Length);

            //Write Tmd
            wadstream.Seek(64 + Tools.AddPadding(cert.Length) + Tools.AddPadding(tik.Length), SeekOrigin.Begin);
            wadstream.Write(tmd, 0, tmd.Length);

            //Write Content
            int allcont = 0;
            int contpos = 64 + Tools.AddPadding(cert.Length) + Tools.AddPadding(tik.Length) + Tools.AddPadding(tmd.Length);
            int contcount = WadInfo.GetContentNum(tmd);

            Tools.ChangeProgress(0);
            byte[] titlekey = WadInfo.GetTitleKey(tik);
            byte[] contentmap = Tools.LoadFileToByteArray(sharedir + "content.map");

            for (int i = 0; i < contents.GetLength(0); i++)
            {
                Tools.ChangeProgress((i + 1) * 100 / contents.GetLength(0));
                byte[] thiscont = new byte[1];

                if (contents[i, 2] == "8001")
                {
                    string contname = ContentMap.GetSharedContentName(contentmap, contents[i, 4]);
                    
                    if (contname == "fail")
                    {
                        wadstream.Close();
                        File.Delete(destinationfile);
                        throw new FileNotFoundException("At least one shared content is missing!");
                    }
                    
                    thiscont = Tools.LoadFileToByteArray(sharedir + contname + ".app");
                }
                else thiscont = Tools.LoadFileToByteArray(contentdir + contents[i, 0] + ".app");

                if (i == contents.GetLength(0) - 1) { thiscont = WadEdit.EncryptContent(thiscont, tmd, i, titlekey, false); }
                else { thiscont = WadEdit.EncryptContent(thiscont, tmd, i, titlekey, true); }
                wadstream.Seek(contpos, SeekOrigin.Begin);
                wadstream.Write(thiscont, 0, thiscont.Length);
                contpos += thiscont.Length;
                allcont += thiscont.Length;
            }

            //Write Header
            byte[] certsize = Tools.FileLengthToByteArray(cert.Length);
            byte[] tiksize = Tools.FileLengthToByteArray(tik.Length);
            byte[] tmdsize = Tools.FileLengthToByteArray(tmd.Length);
            byte[] allcontsize = Tools.FileLengthToByteArray(allcont);
            byte[] trailersize = new byte[] { 0x00, 0x00, 0x00, 0x00 };

            wadstream.Seek(0x00, SeekOrigin.Begin);
            wadstream.Write(wadheader, 0, wadheader.Length);
            wadstream.Seek(0x08, SeekOrigin.Begin);
            wadstream.Write(certsize, 0, certsize.Length);
            wadstream.Seek(0x10, SeekOrigin.Begin);
            wadstream.Write(tiksize, 0, tiksize.Length);
            wadstream.Seek(0x14, SeekOrigin.Begin);
            wadstream.Write(tmdsize, 0, tmdsize.Length);
            wadstream.Seek(0x18, SeekOrigin.Begin);
            wadstream.Write(allcontsize, 0, allcontsize.Length);
            wadstream.Seek(0x1c, SeekOrigin.Begin);
            wadstream.Write(trailersize, 0, trailersize.Length);

            wadstream.Close();
        }
    }

    public class ContentMap
    {
        /// <summary>
        /// Gets the name of the shared content in /shared1/.
        /// Returns "fail", if the content doesn't exist
        /// </summary>
        /// <param name="contentmap"></param>
        /// <param name="sha1ofcontent"></param>
        /// <returns></returns>
        public static string GetSharedContentName(byte[] contentmap, string sha1ofcontent)
        {
            int contindex = 0;
            string result = "";

            for (int i = 0; i < contentmap.Length - 19; i++)
            {
                string tmp = "";
                for (int y = 0; y < 20; y++)
                {
                    tmp += contentmap[i + y].ToString("x2");
                }

                if (tmp == sha1ofcontent)
                {
                    contindex = i;
                    break;
                }
            }

            if (contindex == 0) return "fail";

            result += Convert.ToChar(contentmap[contindex - 8]);
            result += Convert.ToChar(contentmap[contindex - 7]);
            result += Convert.ToChar(contentmap[contindex - 6]);
            result += Convert.ToChar(contentmap[contindex - 5]);
            result += Convert.ToChar(contentmap[contindex - 4]);
            result += Convert.ToChar(contentmap[contindex - 3]);
            result += Convert.ToChar(contentmap[contindex - 2]);
            result += Convert.ToChar(contentmap[contindex - 1]);

            return result;
        }

        /// <summary>
        /// Checks, if the shared content exists
        /// </summary>
        /// <param name="contentmap"></param>
        /// <param name="sha1ofcontent"></param>
        /// <returns></returns>
        public static bool CheckSharedContent(byte[] contentmap, string sha1ofcontent)
        {
            for (int i = 0; i < contentmap.Length - 19; i++)
            {
                string tmp = "";
                for (int y = 0; y < 20; y++)
                {
                    tmp += contentmap[i + y].ToString("x2");
                }

                if (tmp == sha1ofcontent) return true;
            }

            return false;
        }

        public static string GetNewSharedContentName(byte[] contentmap)
        {
            string name = "";

            name += Convert.ToChar(contentmap[contentmap.Length - 28]);
            name += Convert.ToChar(contentmap[contentmap.Length - 27]);
            name += Convert.ToChar(contentmap[contentmap.Length - 26]);
            name += Convert.ToChar(contentmap[contentmap.Length - 25]);
            name += Convert.ToChar(contentmap[contentmap.Length - 24]);
            name += Convert.ToChar(contentmap[contentmap.Length - 23]);
            name += Convert.ToChar(contentmap[contentmap.Length - 22]);
            name += Convert.ToChar(contentmap[contentmap.Length - 21]);

            string newname = (Convert.ToInt32(name) + 1).ToString();
            string result = "";

            for (int i = 0; i < 8 - newname.Length; i++)
            {
                result += "0";
            }

            return result + newname;
        }

        public static void AddSharedContent(string contentmap, string contentname, string sha1ofcontent)
        {
            byte[] name = new byte[8];
            byte[] sha1 = new byte[20];

            for (int i = 0; i < 8; i++)
            {
                name[i] = (byte)contentname[i];
            }

            for (int i = 0; i < sha1ofcontent.Length; i += 2)
            {
                sha1[i / 2] = Convert.ToByte(sha1ofcontent.Substring(i, 2), 16);
            }

            using (FileStream map = new FileStream(contentmap, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                map.Seek(0, SeekOrigin.End);
                map.Write(name, 0, name.Length);
                map.Write(sha1, 0, sha1.Length);
            }
        }
    }

    public class U8Unpack
    {
        /// <summary>
        /// Checks if the given file is a U8 Archive
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool CheckU8(byte[] file)
        {
            for (int i = 0; i < 2500; i++)
            {
                if (file[i] == 0x55 && file[i + 1] == 0xAA && file[i + 2] == 0x38 && file[i + 3] == 0x2D)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the given file is a U8 Archive
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static bool CheckU8(string file)
        {
            using (FileStream load = new FileStream(file, FileMode.Open))
            {
                byte[] buff = new byte[load.Length];
                load.Read(buff, 0, buff.Length);
                return CheckU8(buff);
            }
        }

        /// <summary>
        /// Unpacks the given U8 archive
        /// If the archive is Lz77 compressed, it will be decompressed first!
        /// </summary>
        /// <param name="u8archive"></param>
        /// <param name="unpackpath"></param>
        public static void UnpackU8(byte[] u8archive, string unpackpath)
        {
            int lz77offset = Lz77.GetLz77Offset(u8archive);
            if (lz77offset != -1) { u8archive = Lz77.Decompress(u8archive, lz77offset); }

            if (unpackpath[unpackpath.Length - 1] != '\\') { unpackpath = unpackpath + "\\"; }
            if (!Directory.Exists(unpackpath)) Directory.CreateDirectory(unpackpath);

            int u8offset = -1;
            for (int i = 0; i < 2500; i++)
            {
                if (u8archive[i] == 0x55 && u8archive[i + 1] == 0xAA && u8archive[i + 2] == 0x38 && u8archive[i + 3] == 0x2D)
                {
                    u8offset = i;
                    break;
                }
            }

            if (u8offset == -1) throw new Exception("File is not a valid U8 Archive!");

            int nodecount = Tools.HexStringToInt(u8archive[u8offset + 0x28].ToString("x2") + u8archive[u8offset + 0x29].ToString("x2") + u8archive[u8offset + 0x2a].ToString("x2") + u8archive[u8offset + 0x2b].ToString("x2"));
            int nodeoffset = 0x20;

            string[,] nodes = new string[nodecount, 5];

            for (int j = 0; j < nodecount; j++)
            {
                nodes[j, 0] = u8archive[u8offset + nodeoffset].ToString("x2") + u8archive[u8offset + nodeoffset + 1].ToString("x2");
                nodes[j, 1] = u8archive[u8offset + nodeoffset + 2].ToString("x2") + u8archive[u8offset + nodeoffset + 3].ToString("x2");
                nodes[j, 2] = u8archive[u8offset + nodeoffset + 4].ToString("x2") + u8archive[u8offset + nodeoffset + 5].ToString("x2") + u8archive[u8offset + nodeoffset + 6].ToString("x2") + u8archive[u8offset + nodeoffset + 7].ToString("x2");
                nodes[j, 3] = u8archive[u8offset + nodeoffset + 8].ToString("x2") + u8archive[u8offset + nodeoffset + 9].ToString("x2") + u8archive[u8offset + nodeoffset + 10].ToString("x2") + u8archive[u8offset + nodeoffset + 11].ToString("x2");

                nodeoffset += 12;
            }

            int stringtablepos = u8offset + nodeoffset;

            for (int x = 0; x < nodecount; x++)
            {
                bool end = false;
                int nameoffset = Tools.HexStringToInt(nodes[x, 1]);
                string thisname = "";

                while (end == false)
                {
                    if (u8archive[stringtablepos + nameoffset] != 0x00)
                    {
                        char tempchar = Convert.ToChar(u8archive[stringtablepos + nameoffset]);
                        thisname += tempchar.ToString();
                        nameoffset++;
                    }
                    else end = true;
                }

                nodes[x, 4] = thisname;
            }

            string[] dirs = new string[nodecount];
            dirs[0] = unpackpath;
            int dircount = 0;
            int dirindex = 0;
            int count = -1;
            bool recursive = false;

            for (int y = 1; y < nodecount; y++)
            {
                switch (nodes[y, 0])
                {
                    case "0100":
                        if (count == dircount || recursive == true) { dirindex--; }

                        dircount = Tools.HexStringToInt(nodes[y, 3]);
                        count = 0;

                        if (dirs[dirindex][dirs[dirindex].Length - 1] != '\\') { dirs[dirindex] = dirs[dirindex] + "\\"; }
                        Directory.CreateDirectory(dirs[dirindex] + nodes[y, 4]);
                        dirs[dirindex + 1] = dirs[dirindex] + nodes[y, 4];
                        dirindex++;

                        if (nodes[y, 2] == "00000001") recursive = true;
                        else recursive = false;
                        break;
                    default:
                        int filepos = u8offset + Tools.HexStringToInt(nodes[y, 2]);
                        int filesize = Tools.HexStringToInt(nodes[y, 3]);

                        using (FileStream fs = new FileStream(dirs[dirindex] + "\\" + nodes[y, 4], FileMode.Create))
                        {
                            fs.Write(u8archive, filepos, filesize);
                        }
                        break;
                }

                count++;
            }
        }

        /// <summary>
        /// Gets the Banner.bin out of the 00000000.app
        /// </summary>
        /// <param name="nullapp"></param>
        /// <returns></returns>
        public static byte[] GetBannerBin(byte[] nullapp)
        {
            int lz77offset = Lz77.GetLz77Offset(nullapp);
            if (lz77offset != -1) { nullapp = Lz77.Decompress(nullapp, lz77offset); }

            int u8offset = -1;
            for (int i = 0; i < 2500; i++)
            {
                if (nullapp[i] == 0x55 && nullapp[i + 1] == 0xAA && nullapp[i + 2] == 0x38 && nullapp[i + 3] == 0x2D)
                {
                    u8offset = i;
                    break;
                }
            }

            if (u8offset == -1) throw new Exception("File is not a valid U8 Archive!");

            int nodecount = Tools.HexStringToInt(nullapp[u8offset + 0x28].ToString("x2") + nullapp[u8offset + 0x29].ToString("x2") + nullapp[u8offset + 0x2a].ToString("x2") + nullapp[u8offset + 0x2b].ToString("x2"));
            int nodeoffset = 0x20;

            string[,] nodes = new string[nodecount, 5];

            for (int j = 0; j < nodecount; j++)
            {
                nodes[j, 0] = nullapp[u8offset + nodeoffset].ToString("x2") + nullapp[u8offset + nodeoffset + 1].ToString("x2");
                nodes[j, 1] = nullapp[u8offset + nodeoffset + 2].ToString("x2") + nullapp[u8offset + nodeoffset + 3].ToString("x2");
                nodes[j, 2] = nullapp[u8offset + nodeoffset + 4].ToString("x2") + nullapp[u8offset + nodeoffset + 5].ToString("x2") + nullapp[u8offset + nodeoffset + 6].ToString("x2") + nullapp[u8offset + nodeoffset + 7].ToString("x2");
                nodes[j, 3] = nullapp[u8offset + nodeoffset + 8].ToString("x2") + nullapp[u8offset + nodeoffset + 9].ToString("x2") + nullapp[u8offset + nodeoffset + 10].ToString("x2") + nullapp[u8offset + nodeoffset + 11].ToString("x2");

                nodeoffset += 12;
            }

            int stringtablepos = u8offset + nodeoffset;

            for (int x = 0; x < nodecount; x++)
            {
                bool end = false;
                int nameoffset = Tools.HexStringToInt(nodes[x, 1]);
                string thisname = "";

                while (end == false)
                {
                    if (nullapp[stringtablepos + nameoffset] != 0x00)
                    {
                        char tempchar = Convert.ToChar(nullapp[stringtablepos + nameoffset]);
                        thisname += tempchar.ToString();
                        nameoffset++;
                    }
                    else end = true;
                }

                nodes[x, 4] = thisname;
            }

            for (int y = 1; y < nodecount; y++)
            {
                if (nodes[y, 4] == "banner.bin")
                {
                    int filepos = u8offset + Tools.HexStringToInt(nodes[y, 2]);
                    int filesize = Tools.HexStringToInt(nodes[y, 3]);

                    MemoryStream ms = new MemoryStream(nullapp);
                    byte[] banner = new byte[filesize];
                    ms.Seek(filepos, SeekOrigin.Begin);
                    ms.Read(banner, 0, filesize);
                    ms.Close();

                    return banner;
                }
            }

            throw new Exception("This file doesn't contain banner.bin!");
        }

        /// <summary>
        /// Gets the Icon.bin out of the 00000000.app
        /// </summary>
        /// <param name="nullapp"></param>
        /// <returns></returns>
        public static byte[] GetIconBin(byte[] nullapp)
        {
            int lz77offset = Lz77.GetLz77Offset(nullapp);
            if (lz77offset != -1) { nullapp = Lz77.Decompress(nullapp, lz77offset); }

            int u8offset = -1;
            for (int i = 0; i < 2500; i++)
            {
                if (nullapp[i] == 0x55 && nullapp[i + 1] == 0xAA && nullapp[i + 2] == 0x38 && nullapp[i + 3] == 0x2D)
                {
                    u8offset = i;
                    break;
                }
            }

            if (u8offset == -1) throw new Exception("File is not a valid U8 Archive!");

            int nodecount = Tools.HexStringToInt(nullapp[u8offset + 0x28].ToString("x2") + nullapp[u8offset + 0x29].ToString("x2") + nullapp[u8offset + 0x2a].ToString("x2") + nullapp[u8offset + 0x2b].ToString("x2"));
            int nodeoffset = 0x20;

            string[,] nodes = new string[nodecount, 5];

            for (int j = 0; j < nodecount; j++)
            {
                nodes[j, 0] = nullapp[u8offset + nodeoffset].ToString("x2") + nullapp[u8offset + nodeoffset + 1].ToString("x2");
                nodes[j, 1] = nullapp[u8offset + nodeoffset + 2].ToString("x2") + nullapp[u8offset + nodeoffset + 3].ToString("x2");
                nodes[j, 2] = nullapp[u8offset + nodeoffset + 4].ToString("x2") + nullapp[u8offset + nodeoffset + 5].ToString("x2") + nullapp[u8offset + nodeoffset + 6].ToString("x2") + nullapp[u8offset + nodeoffset + 7].ToString("x2");
                nodes[j, 3] = nullapp[u8offset + nodeoffset + 8].ToString("x2") + nullapp[u8offset + nodeoffset + 9].ToString("x2") + nullapp[u8offset + nodeoffset + 10].ToString("x2") + nullapp[u8offset + nodeoffset + 11].ToString("x2");

                nodeoffset += 12;
            }

            int stringtablepos = u8offset + nodeoffset;

            for (int x = 0; x < nodecount; x++)
            {
                bool end = false;
                int nameoffset = Tools.HexStringToInt(nodes[x, 1]);
                string thisname = "";

                while (end == false)
                {
                    if (nullapp[stringtablepos + nameoffset] != 0x00)
                    {
                        char tempchar = Convert.ToChar(nullapp[stringtablepos + nameoffset]);
                        thisname += tempchar.ToString();
                        nameoffset++;
                    }
                    else end = true;
                }

                nodes[x, 4] = thisname;
            }

            for (int y = 1; y < nodecount; y++)
            {
                if (nodes[y, 4] == "icon.bin")
                {
                    int filepos = u8offset + Tools.HexStringToInt(nodes[y, 2]);
                    int filesize = Tools.HexStringToInt(nodes[y, 3]);

                    MemoryStream ms = new MemoryStream(nullapp);
                    byte[] icon = new byte[filesize];
                    ms.Seek(filepos, SeekOrigin.Begin);
                    ms.Read(icon, 0, filesize);
                    ms.Close();

                    return icon;
                }
            }

            throw new Exception("This file doesn't contain icon.bin!");
        }

        /// <summary>
        /// Extracts all Tpl's to the given path
        /// </summary>
        /// <param name="u8archive"></param>
        /// <param name="path"></param>
        public static void UnpackTpls(byte[] u8archive, string unpackpath)
        {
            int lz77offset = Lz77.GetLz77Offset(u8archive);
            if (lz77offset != -1) { u8archive = Lz77.Decompress(u8archive, lz77offset); }

            if (unpackpath[unpackpath.Length - 1] != '\\') { unpackpath = unpackpath + "\\"; }
            if (!Directory.Exists(unpackpath)) Directory.CreateDirectory(unpackpath);

            int u8offset = -1;
            int length = 2500;
            if (u8archive.Length < 2500) length = u8archive.Length - 4;

            for (int i = 0; i < 2500; i++)
            {
                if (u8archive[i] == 0x55 && u8archive[i + 1] == 0xAA && u8archive[i + 2] == 0x38 && u8archive[i + 3] == 0x2D)
                {
                    u8offset = i;
                    break;
                }
            }

            if (u8offset == -1) throw new Exception("File is not a valid U8 Archive!");

            int nodecount = Tools.HexStringToInt(u8archive[u8offset + 0x28].ToString("x2") + u8archive[u8offset + 0x29].ToString("x2") + u8archive[u8offset + 0x2a].ToString("x2") + u8archive[u8offset + 0x2b].ToString("x2"));
            int nodeoffset = 0x20;

            string[,] nodes = new string[nodecount, 5];

            for (int j = 0; j < nodecount; j++)
            {
                nodes[j, 0] = u8archive[u8offset + nodeoffset].ToString("x2") + u8archive[u8offset + nodeoffset + 1].ToString("x2");
                nodes[j, 1] = u8archive[u8offset + nodeoffset + 2].ToString("x2") + u8archive[u8offset + nodeoffset + 3].ToString("x2");
                nodes[j, 2] = u8archive[u8offset + nodeoffset + 4].ToString("x2") + u8archive[u8offset + nodeoffset + 5].ToString("x2") + u8archive[u8offset + nodeoffset + 6].ToString("x2") + u8archive[u8offset + nodeoffset + 7].ToString("x2");
                nodes[j, 3] = u8archive[u8offset + nodeoffset + 8].ToString("x2") + u8archive[u8offset + nodeoffset + 9].ToString("x2") + u8archive[u8offset + nodeoffset + 10].ToString("x2") + u8archive[u8offset + nodeoffset + 11].ToString("x2");

                nodeoffset += 12;
            }

            int stringtablepos = u8offset + nodeoffset;

            for (int x = 0; x < nodecount; x++)
            {
                bool end = false;
                int nameoffset = Tools.HexStringToInt(nodes[x, 1]);
                string thisname = "";

                while (end == false)
                {
                    if (u8archive[stringtablepos + nameoffset] != 0x00)
                    {
                        char tempchar = Convert.ToChar(u8archive[stringtablepos + nameoffset]);
                        thisname += tempchar.ToString();
                        nameoffset++;
                    }
                    else end = true;
                }

                nodes[x, 4] = thisname;
            }

            for (int y = 1; y < nodecount; y++)
            {
                if (nodes[y, 4].Contains("."))
                {
                    if (nodes[y, 4].Remove(0, nodes[y, 4].LastIndexOf('.')) == ".tpl")
                    {
                        int filepos = u8offset + Tools.HexStringToInt(nodes[y, 2]);
                        int filesize = Tools.HexStringToInt(nodes[y, 3]);

                        using (FileStream fs = new FileStream(unpackpath + nodes[y, 4], FileMode.Create))
                        {
                            fs.Write(u8archive, filepos, filesize);
                        }
                    }
                }
            }
        }
    }

    public class Lz77
    {
        public const int N = 4096;
        public const int F = 18;
        public const int threshold = 2;

        /// <summary>
        /// Returns the Offset to the Lz77 Header
        /// -1 will be returned, if the file is not Lz77 compressed
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int GetLz77Offset(byte[] data)
        {
            int length = 5000;
            if (data.Length < 5000) length = data.Length - 4;

            for (int i = 0; i < length; i++)
            {
                if (data[i] == 0x55 && data[i + 1] == 0xAA && data[i + 2] == 0x38 && data[i + 3] == 0x2D)
                {
                    break;
                }

                UInt32 tmp = BitConverter.ToUInt32(data, i);
                if (tmp == 0x37375a4c) return i;
            }

            return -1;
        }

        /// <summary>
        /// Decompresses the given data
        /// </summary>
        /// <param name="compressed"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] compressed, int offset)
        {
            int i, j, k, r, c, z;
            uint flags;
            UInt32 decomp_size;
            UInt32 cur_size = 0;

            MemoryStream infile = new MemoryStream(compressed);
            MemoryStream outfile = new MemoryStream();

            UInt32 gbaheader = new UInt32();
            byte[] temp = new byte[4];
            infile.Seek(offset + 4, SeekOrigin.Begin);
            infile.Read(temp, 0, 4);
            gbaheader = BitConverter.ToUInt32(temp, 0);

            decomp_size = gbaheader >> 8;
            byte[] text_buf = new byte[N + 17];

            for (i = 0; i < N - F; i++) text_buf[i] = 0xff;
            r = N - F; flags = 7; z = 7;

            while (true)
            {
                flags <<= 1;
                z++;
                if (z == 8)
                {
                    if ((c = (char)infile.ReadByte()) == -1) break;
                    flags = (uint)c;
                    z = 0;
                }
                if ((flags & 0x80) == 0)
                {
                    if ((c = infile.ReadByte()) == infile.Length - 1) break;
                    if (cur_size < decomp_size) outfile.WriteByte((byte)c);
                    text_buf[r++] = (byte)c;
                    r &= (N - 1);
                    cur_size++;
                }
                else
                {
                    if ((i = infile.ReadByte()) == -1) break;
                    if ((j = infile.ReadByte()) == -1) break;
                    j = j | ((i << 8) & 0xf00);
                    i = ((i >> 4) & 0x0f) + threshold;
                    for (k = 0; k <= i; k++)
                    {
                        c = text_buf[(r - j - 1) & (N - 1)];
                        if (cur_size < decomp_size) outfile.WriteByte((byte)c); text_buf[r++] = (byte)c; r &= (N - 1); cur_size++;
                    }
                }
            }

            return outfile.ToArray();
        }
    }

    public class TPL
    {
        public static System.Drawing.Bitmap ConvertTPL(byte[] tpl)
        {
            if (GetTextureCount(tpl) > 1) throw new Exception("Tpl's containing more than one Texture are not supported!");

            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int format = GetTextureFormat(tpl);
            if (format == -1) throw new Exception("The Texture has an unsupported format!");

            switch (format)
            {
                case 0:
                    byte[] temp0 = FromI4(tpl);
                    return ConvertPixelToBitmap(temp0, width, height);
                case 1:
                    byte[] temp1 = FromI8(tpl);
                    return ConvertPixelToBitmap(temp1, width, height);
                case 2:
                    byte[] temp2 = FromIA4(tpl);
                    return ConvertPixelToBitmap(temp2, width, height);
                case 3:
                    byte[] temp3 = FromIA8(tpl);
                    return ConvertPixelToBitmap(temp3, width, height);
                case 4:
                    byte[] temp4 = FromRGB565(tpl);
                    return ConvertPixelToBitmap(temp4, width, height);
                case 5:
                    byte[] temp5 = FromRGB5A3(tpl);
                    return ConvertPixelToBitmap(temp5, width, height);
                case 6:
                    byte[] temp6 = FromRGBA8(tpl);
                    return ConvertPixelToBitmap(temp6, width, height);
                case 14:
                    byte[] temp14 = FromCMP(tpl);
                    return ConvertPixelToBitmap(temp14, width, height);
                default:
                    throw new Exception("The Texture has an unsupported format!");
            }
        }

        /// <summary>
        /// Converts the Pixel Data into a Png Image
        /// </summary>
        /// <param name="data">Byte array with pixel data</param>
        public static System.Drawing.Bitmap ConvertPixelToBitmap(byte[] data, int width, int height)
        {
            if (width == 0) width = 1;
            if (height == 0) height = 1;

            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(
                                 new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                                 System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);

            System.Runtime.InteropServices.Marshal.Copy(data, 0, bmpData.Scan0, data.Length);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        /// <summary>
        /// Gets the Number of Textures in a Tpl
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static int GetTextureCount(byte[] tpl)
        {
            byte[] tmp = new byte[4];
            tmp[3] = tpl[4];
            tmp[2] = tpl[5];
            tmp[1] = tpl[6];
            tmp[0] = tpl[7];
            UInt32 count = BitConverter.ToUInt32(tmp, 0);
            return (int)count;
        }

        /// <summary>
        /// Gets the Format of the Texture in the Tpl
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static int GetTextureFormat(byte[] tpl)
        {
            byte[] tmp = new byte[4];
            tmp[3] = tpl[24];
            tmp[2] = tpl[25];
            tmp[1] = tpl[26];
            tmp[0] = tpl[27];
            UInt32 format = BitConverter.ToUInt32(tmp, 0);

            if (format == 0 ||
                format == 1 ||
                format == 2 ||
                format == 3 ||
                format == 4 ||
                format == 5 ||
                format == 6 ||
                format == 14) return (int)format;

            else return -1; //Unsupported Format
        }

        /// <summary>
        /// Gets the Format Name of the Texture in the Tpl
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static string GetTextureFormatName(byte[] tpl)
        {
            switch (GetTextureFormat(tpl))
            {
                case 0:
                    return "I4";
                case 1:
                    return "I8";
                case 2:
                    return "IA4";
                case 3:
                    return "IA8";
                case 4:
                    return "RGB565";
                case 5:
                    return "RGB5A3";
                case 6:
                    return "RGBA8";
                case 14:
                    return "CMP";
                default:
                    return "Unknown";
            }
        }

        public static int avg(int w0, int w1, int c0, int c1)
        {
            int a0 = c0 >> 11;
            int a1 = c1 >> 11;
            int a = (w0 * a0 + w1 * a1) / (w0 + w1);
            int c = (a << 11) & 0xffff;

            a0 = (c0 >> 5) & 63;
            a1 = (c1 >> 5) & 63;
            a = (w0 * a0 + w1 * a1) / (w0 + w1);
            c = c | ((a << 5) & 0xffff);

            a0 = c0 & 31;
            a1 = c1 & 31;
            a = (w0 * a0 + w1 * a1) / (w0 + w1);
            c = c | a;

            return c;
        }

        /// <summary>
        /// Gets the Width of the Texture in the Tpl
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static int GetTextureWidth(byte[] tpl)
        {
            byte[] tmp = new byte[2];
            tmp[1] = tpl[22];
            tmp[0] = tpl[23];
            UInt16 width = BitConverter.ToUInt16(tmp, 0);
            return (int)width;
        }

        /// <summary>
        /// Gets the Height of the Texture in the Tpl
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static int GetTextureHeight(byte[] tpl)
        {
            byte[] tmp = new byte[2];
            tmp[1] = tpl[20];
            tmp[0] = tpl[21];
            UInt16 height = BitConverter.ToUInt16(tmp, 0);
            return (int)height;
        }

        /// <summary>
        /// Gets the offset to the Texturedata in the Tpl
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static int GetTextureOffset(byte[] tpl)
        {
            byte[] tmp = new byte[4];
            tmp[3] = tpl[28];
            tmp[2] = tpl[29];
            tmp[1] = tpl[30];
            tmp[0] = tpl[31];
            UInt32 offset = BitConverter.ToUInt32(tmp, 0);
            return (int)offset;
        }

        /// <summary>
        /// Converts RGBA8 Tpl Array to RGBA Byte Array
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static byte[] FromRGBA8(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            int inp = 0;
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        for (int y1 = y; y1 < y + 4; y1++)
                        {
                            for (int x1 = x; x1 < x + 4; x1++)
                            {
                                byte[] pixelbytes = new byte[2];
                                pixelbytes[1] = tpl[offset + inp * 2];
                                pixelbytes[0] = tpl[offset + inp * 2 + 1];
                                UInt16 pixel = BitConverter.ToUInt16(pixelbytes, 0);
                                inp++;

                                if ((x1 >= width) || (y1 >= height))
                                    continue;

                                if (k == 0)
                                {
                                    int a = (pixel >> 8) & 0xff;
                                    int r = (pixel >> 0) & 0xff;
                                    output[x1 + (y1 * width)] |= (UInt32)((r << 16) | (a << 24));
                                }
                                else
                                {
                                    int g = (pixel >> 8) & 0xff;
                                    int b = (pixel >> 0) & 0xff;
                                    output[x1 + (y1 * width)] |= (UInt32)((g << 8) | (b << 0));
                                }
                            }
                        }
                    }
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }

        /// <summary>
        /// Converts RGB5A3 Tpl Array to RGBA Byte Array
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static byte[] FromRGB5A3(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            int inp = 0;
            int r, g, b;
            int a = 0;
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    for (int y1 = y; y1 < y + 4; y1++)
                    {
                        for (int x1 = x; x1 < x + 4; x1++)
                        {
                            byte[] pixelbytes = new byte[2];
                            pixelbytes[1] = tpl[offset + inp * 2];
                            pixelbytes[0] = tpl[offset + inp * 2 + 1];
                            UInt16 pixel = BitConverter.ToUInt16(pixelbytes, 0);
                            inp++;

                            if (y1 >= height || x1 >= width)
                                continue;

                            if ((pixel & (1 << 15)) != 0)
                            {
                                b = (((pixel >> 10) & 0x1F) * 255) / 31;
                                g = (((pixel >> 5) & 0x1F) * 255) / 31;
                                r = (((pixel >> 0) & 0x1F) * 255) / 31;
                                a = 255;
                            }
                            else
                            {
                                a = (((pixel >> 12) & 0x07) * 255) / 7;
                                b = (((pixel >> 8) & 0x0F) * 255) / 15;
                                g = (((pixel >> 4) & 0x0F) * 255) / 15;
                                r = (((pixel >> 0) & 0x0F) * 255) / 15;
                            }

                            int rgba = (r << 0) | (g << 8) | (b << 16) | (a << 24);
                            output[(y1 * width) + x1] = (UInt32)rgba;
                        }
                    }
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }

        /// <summary>
        /// Converts RGB565 Tpl Array to RGBA Byte Array
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static byte[] FromRGB565(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            int inp = 0;
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    for (int y1 = y; y1 < y + 4; y1++)
                    {
                        for (int x1 = x; x1 < x + 4; x1++)
                        {
                            byte[] pixelbytes = new byte[2];
                            pixelbytes[1] = tpl[offset + inp * 2];
                            pixelbytes[0] = tpl[offset + inp * 2 + 1];
                            UInt16 pixel = BitConverter.ToUInt16(pixelbytes, 0);
                            inp++;

                            if (y1 >= height || x1 >= width)
                                continue;

                            int b = (((pixel >> 11) & 0x1F) << 3) & 0xff;
                            int g = (((pixel >> 5) & 0x3F) << 2) & 0xff;
                            int r = (((pixel >> 0) & 0x1F) << 3) & 0xff;
                            int a = 255;

                            int rgba = (r << 0) | (g << 8) | (b << 16) | (a << 24);
                            output[y1 * width + x1] = (UInt32)rgba;
                        }
                    }
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }

        /// <summary>
        /// Converts I4 Tpl Array to RGBA Byte Array
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static byte[] FromI4(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            int inp = 0;
            for (int y = 0; y < height; y += 8)
            {
                for (int x = 0; x < width; x += 8)
                {
                    for (int y1 = y; y1 < y + 8; y1++)
                    {
                        for (int x1 = x; x1 < x + 8; x1 += 2)
                        {
                            int pixel = tpl[offset + inp];

                            if (y1 >= height || x1 >= width)
                                continue;

                            int r = (pixel >> 4) * 255 / 15;
                            int g = (pixel >> 4) * 255 / 15;
                            int b = (pixel >> 4) * 255 / 15;
                            int a = (pixel >> 4) * 255 / 15;

                            int rgba = (r << 0) | (g << 8) | (b << 16) | (a << 24);
                            output[y1 * width + x1] = (UInt32)rgba;

                            pixel = tpl[offset + inp];
                            inp++;

                            if (y1 >= height || x1 >= width)
                                continue;

                            r = (pixel & 0x0F) * 255 / 15;
                            g = (pixel & 0x0F) * 255 / 15;
                            b = (pixel & 0x0F) * 255 / 15;
                            a = (pixel & 0x0F) * 255 / 15;

                            rgba = (r << 0) | (g << 8) | (b << 16) | (a << 24);
                            output[y1 * width + x1 + 1] = (UInt32)rgba;
                        }
                    }
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }

        /// <summary>
        /// Converts IA4 Tpl Array to RGBA Byte Array
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static byte[] FromIA4(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            int inp = 0;
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 8)
                {
                    for (int y1 = y; y1 < y + 4; y1++)
                    {
                        for (int x1 = x; x1 < x + 8; x1++)
                        {
                            int pixel = tpl[offset + inp];
                            inp++;

                            if (y1 >= height || x1 >= width)
                                continue;

                            int r = ((pixel & 0x0F) * 255 / 15) & 0xff;
                            int g = ((pixel & 0x0F) * 255 / 15) & 0xff;
                            int b = ((pixel & 0x0F) * 255 / 15) & 0xff;
                            int a = (((pixel >> 4) * 255) / 15) & 0xff;

                            int rgba = (r << 0) | (g << 8) | (b << 16) | (a << 24);
                            output[y1 * width + x1] = (UInt32)rgba;
                        }
                    }
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }

        /// <summary>
        /// Converts I8 Tpl Array to RGBA Byte Array
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static byte[] FromI8(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            int inp = 0;
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 8)
                {
                    for (int y1 = y; y1 < y + 4; y1++)
                    {
                        for (int x1 = x; x1 < x + 8; x1++)
                        {
                            int pixel = tpl[offset + inp];
                            inp++;

                            if (y1 >= height || x1 >= width)
                                continue;

                            int r = pixel;
                            int g = pixel;
                            int b = pixel;
                            int a = 255;

                            int rgba = (r << 0) | (g << 8) | (b << 16) | (a << 24);
                            output[y1 * width + x1] = (UInt32)rgba;
                        }
                    }
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }

        /// <summary>
        /// Converts IA8 Tpl Array to RGBA Byte Array
        /// </summary>
        /// <param name="tpl"></param>
        /// <returns></returns>
        public static byte[] FromIA8(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            int inp = 0;
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    for (int y1 = y; y1 < y + 4; y1++)
                    {
                        for (int x1 = x; x1 < x + 4; x1++)
                        {
                            byte[] pixelbytes = new byte[2];
                            pixelbytes[1] = tpl[offset + inp * 2];
                            pixelbytes[0] = tpl[offset + inp * 2 + 1];
                            UInt16 pixel = BitConverter.ToUInt16(pixelbytes, 0);
                            inp++;

                            if (y1 >= height || x1 >= width)
                                continue;

                            int r = (pixel >> 8);// &0xff;
                            int g = (pixel >> 8);// &0xff;
                            int b = (pixel >> 8);// &0xff;
                            int a = pixel & 0xff;

                            int rgba = (r << 0) | (g << 8) | (b << 16) | (a << 24);
                            output[y1 * width + x1] = (UInt32)rgba;
                        }
                    }
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }

        public static byte[] FromCMP(byte[] tpl)
        {
            int width = GetTextureWidth(tpl);
            int height = GetTextureHeight(tpl);
            int offset = GetTextureOffset(tpl);
            UInt32[] output = new UInt32[width * height];
            UInt16[] c = new UInt16[4];
            int[] pix = new int[3];
            int inp = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ww = Tools.AddPadding(width, 8);

                    int x0 = x & 0x03;
                    int x1 = (x >> 2) & 0x01;
                    int x2 = x >> 3;

                    int y0 = y & 0x03;
                    int y1 = (y >> 2) & 0x01;
                    int y2 = y >> 3;

                    int off = (8 * x1) + (16 * y1) + (32 * x2) + (4 * ww * y2);

                    byte[] tmp1 = new byte[2];
                    tmp1[1] = tpl[offset + off];
                    tmp1[0] = tpl[offset + off + 1];
                    c[0] = BitConverter.ToUInt16(tmp1, 0);
                    tmp1[1] = tpl[offset + off + 2];
                    tmp1[0] = tpl[offset + off + 3];
                    c[1] = BitConverter.ToUInt16(tmp1, 0);

                    if (c[0] > c[1])
                    {
                        c[2] = (UInt16)avg(2, 1, c[0], c[1]);
                        c[3] = (UInt16)avg(1, 2, c[0], c[1]);
                    }
                    else
                    {
                        c[2] = (UInt16)avg(1, 1, c[0], c[1]);
                        c[3] = 0;
                    }

                    byte[] pixeldata = new byte[4];
                    pixeldata[3] = tpl[offset + off + 4];
                    pixeldata[2] = tpl[offset + off + 5];
                    pixeldata[1] = tpl[offset + off + 6];
                    pixeldata[0] = tpl[offset + off + 7];
                    UInt32 pixel = BitConverter.ToUInt32(pixeldata, 0);

                    int ix = x0 + (4 * y0);
                    int raw = c[(pixel >> (30 - (2 * ix))) & 0x03];

                    pix[0] = (raw >> 8) & 0xf8;
                    pix[1] = (raw >> 3) & 0xf8;
                    pix[2] = (raw << 3) & 0xf8;

                    int intout = (pix[0] << 16) | (pix[1] << 8) | (pix[2] << 0) | (255 << 24);
                    output[inp] = (UInt32)intout;
                    inp++;
                }
            }

            return Tools.UInt32ArrayToByteArray(output);
        }
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        private readonly int p_Percent = 0;

        public int PercentProgress
        {
            get { return p_Percent; }
        }

        internal ProgressChangedEventArgs(int PercentProgress)
            : base()
        {
            this.p_Percent = PercentProgress;
        }
    }
}