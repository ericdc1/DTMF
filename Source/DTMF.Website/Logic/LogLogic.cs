using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace DTMF.Logic
{
    public class LogLogic
    {

        public List<string> GetHistoryList(string appName)
        {
            var apphistorylist = new List<string>();
            if(!Directory.Exists(HttpContext.Current.Server.MapPath("~/App_Data/Logs/" + appName))) return apphistorylist;

            var filenames = Directory.EnumerateFiles(HttpContext.Current.Server.MapPath("~/App_Data/Logs/" + appName), "*.htm", SearchOption.TopDirectoryOnly)
                        .Select(Path.GetFileNameWithoutExtension);
            foreach (var name in filenames)
            {
                apphistorylist.Add(name);
            }
            return apphistorylist;
        }

        public string GetLogFileContents(string appName, string fileName)
        {
            return File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/Logs/" + appName + "/" + fileName + ".htm"));
        }

        public static void SaveLog(string baselogpath, string appName, string logtext)
        {
            var logfolder = Path.Combine(baselogpath, appName);
            var logfile = Path.Combine(logfolder, DateTime.Now.ToString("yyyy-MM-dd hh_mm") + ".htm");
            Directory.CreateDirectory(logfolder);
            File.WriteAllText(logfile, logtext);
        }
    }
}