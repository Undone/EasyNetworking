using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace EasyNetworking
{
    public static class FrameworkString
    {
        public static string[] Explode(string str, string limiter)
        {
            if (str.Contains(limiter))
            {
                return Regex.Split(str, limiter);
            }
            else
            {
                return new string[] {str};
            }
        }

        public static string Concat(string[] str, string limiter)
        {
            string temp = "";

            for (int i = 0; i < str.Length; i++)
            {
                temp += str[i] + limiter;
            }

            return temp;
        }

        public static string[] SelectRange(string[] str, int startIndex, int endIndex)
        {
            List<string> strList = new List<string>();

            for (int i = startIndex; i < endIndex; i++)
            {
                strList.Add(str[i]);
            }

            return strList.ToArray();
        }
    }
}
