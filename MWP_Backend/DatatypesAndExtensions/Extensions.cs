using System;
using System.Collections.Generic;
using System.Text;
using MWP_Backend.BackEnd;
using MWP.BackEnd;
using TagLib;
using TagLib.Id3v2;
using File = TagLib.File;
using Tag = TagLib.Id3v2.Tag;

namespace MWP
{
    /// <summary>
    /// Extensions to various custom objects
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Shuffles list
        /// </summary>
        /// <param name="list">list to be shuffled</param>
        /// <typeparam name="T">type</typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {
            //TODO: figure out where one of the elements disappears to;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = StateHandler.Rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Removes and returns element at <paramref name="index"/>
        /// </summary>
        /// <param name="list">List to be modified</param>
        /// <param name="index">Index of removal</param>
        /// <typeparam name="T">type</typeparam>
        /// <returns>removed <typeparamref name="T"/></returns>
        public static T Pop<T>(this IList<T> list, int index = 0)
        {
            T value = list[index];
            list.RemoveAt(index);
            return value;
        }

        ///<summary>
		///If current number is more than <paramref name="max"/> it loops over to <paramref name="min"/>
        ///<br/>
        ///If current number is less than <paramref name="min"/> it loops over to <paramref name="max"/>
		///</summary>
        public static int LoopOver(this int current, int max, int min = 0)
        {
            if(current > max)
            {
                return min;
            }
            if(current < min)
            {
                return max;
            }
            return current;
        }

        ///<summary>
        ///If current number is less than 0, returns 0 else returns current number
		///</summary>
        public static int KeepPositive(this int current)
        {
            if (current < 0)
            {
                return 0;
            }
            return current;
        }
        
        ///<summary>
        ///Keeps integer between <paramref name="min"/> and <paramref name="max"/>
        ///</summary>
        public static int Constraint(this int current, int min, int max)
        {
            if(current > max)
            {
                return max;
            }
            if(current < min)
            {
                return min;
            }
            return current;
        }
        
        ///<summary>
        ///Case-Insensitive comparison
        ///</summary>
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static void writePrivateFrame(this File tfile, string tagName, string value)
        {
            //https://stackoverflow.com/questions/34507982/adding-custom-tag-using-taglib-sharp-library
            Tag custom = (Tag) tfile.GetTag(TagTypes.Id3v2);
            PrivateFrame privateFrame = PrivateFrame.Get(custom, tagName, true);
            privateFrame.PrivateData = Encoding.UTF8.GetBytes(value);
        }
        
        public static void writePrivateFrame(this File tfile, string tagName, bool value)
        {
            //https://stackoverflow.com/questions/34507982/adding-custom-tag-using-taglib-sharp-library
            Tag custom = (Tag) tfile.GetTag(TagTypes.Id3v2);
            PrivateFrame privateFrame = PrivateFrame.Get(custom, tagName, true);
            privateFrame.PrivateData = Convert.ToByte(value);
        }

        public static string readPrivateFrame(this File tfile, string tagName, string defaultValue)
        {
            //https://stackoverflow.com/questions/34507982/adding-custom-tag-using-taglib-sharp-library
            Tag t = (Tag)tfile.GetTag(TagTypes.Id3v2);
            PrivateFrame? privateFrame = PrivateFrame.Get(t, tagName, false);
            return privateFrame != null ? Encoding.UTF8.GetString(privateFrame.PrivateData.Data) : defaultValue;
        }
        public static bool readPrivateFrame(this File tfile, string tagName, bool defaultValue)
        {
            //https://stackoverflow.com/questions/34507982/adding-custom-tag-using-taglib-sharp-library
            Tag t = (Tag)tfile.GetTag(TagTypes.Id3v2);
            PrivateFrame? privateFrame = PrivateFrame.Get(t, tagName, false);
            try
            {
                return privateFrame != null ? Convert.ToBoolean(privateFrame.PrivateData.Data) : defaultValue;
            }
            catch (Exception)
            {
                //ignored
            }
            return defaultValue;
        }
    }
}