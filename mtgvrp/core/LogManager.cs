﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace mtgvrp.core
{
    public static class LogManager
    {
        private static Timer archiveTimer = null;

        public static void StartLogArchiveTimer()
        {
            ArchiveLogs();

            archiveTimer = new Timer {Interval = 3.6e+6};
            archiveTimer.Elapsed += ArchiveTimer_Elapsed;
            archiveTimer.Start();
        }

        private static void ArchiveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ArchiveLogs();
        }

        public static int GetTimeStamp => (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

        static void ArchiveLogs()
        {
            try
            {
                DebugManager.DebugMessage("** Starting log archive process.", 1);

                var path = "OldLogs/" + DateTime.Now.Year + "-" + DateTime.Now.Month;
                //Make sure folders exist.
                Directory.CreateDirectory(path);

                //Move the files.
                foreach (var file in Directory.GetFiles("Logs", "*.log"))
                {
                    File.Move(file, path + "/" + Path.GetFileName(file) + "-" + GetTimeStamp);
                }
                DebugManager.DebugMessage("** Logs has been archived.");
            }
            catch (Exception e)
            {
                DebugManager.DebugMessage(e.Message);
            }
        }

        public enum LogTypes
        {
            AdminActions,
            Bans,           //Logged
            Commands,       //Logged
            Connection,     //Logged
            Death,          //Logged
            GroupChat,      //Logged
            RadioChat,      //Logged
            GroupInvites,
            ICchat,         //Logged
            Phone,          //Logged
            Money,
            OOCchat,        //Logged
            PMchat,         //Logged
            Stats,
            Storage,
            Unbans,         //Logged
            Warns,          //Logged
        }

        public static void Log(LogTypes type, string log)
        {
            //prepare file name.
            var file = type.ToString() + ".log";

            //if dir not there, make it.
            Directory.CreateDirectory("Logs");

            //Append
            File.AppendAllText("Logs/" + file, $"[{GetTimeStamp}] " + log + "\r\n");
        }
    }
}
