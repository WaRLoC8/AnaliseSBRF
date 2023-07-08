using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnaliseSBRF
{
    internal class Log
    {
        public static Boolean LogWrite(string LogString, bool enableLog)

        {
            if (enableLog)
            {
                try
                {
                    string logpath = DateTime.Now.Date.ToString("yyyy.MM.dd");
                    logpath = "C:\\Robot\\LogRobot\\AnalizeSBRF" + logpath.Replace(".", String.Empty) + ".txt";
                    StreamWriter writelog = new StreamWriter(logpath, true);
                    writelog.WriteLine(LogString, true);
                    writelog.Close();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                    return false;
                }
            }
            else return true;
        }
    }
}
