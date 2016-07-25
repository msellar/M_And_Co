using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;

namespace SaveFeedContent
{
    static public class Logger
    {
        public static void Log(string p_Msg)
        {
            // Simpest method, creates log if required
            string strLogFile = ConfigurationSettings.AppSettings["AppDirectory"] + @"\log.txt";
            File.AppendAllText(strLogFile, string.Format("{0} : {1}{2}", DateTime.Now.ToString(), p_Msg, Environment.NewLine));
        }
    }
}
