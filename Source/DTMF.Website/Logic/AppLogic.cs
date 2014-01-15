using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DTMF.Models;

namespace DTMF.Logic
{
    public class AppLogic
    {

        public List<AppInfoExtended> GetAppList(bool getVersionInfo = false)
        {
            var appinfolist = new List<AppInfoExtended>();
            var filenames = Directory
                        .EnumerateFiles(HttpContext.Current.Server.MapPath("~/App_Data/Configurations/"), "*.xml", SearchOption.TopDirectoryOnly)
                        .Select(Path.GetFileNameWithoutExtension);
            foreach (var name in filenames)
            {
                appinfolist.Add(GetAppExtendedByName(name, getVersionInfo));
            }
            return appinfolist;
        }

        public AppInfo GetAppByName(string appName)
        {
            //need a way to clear the cache when changes happen before enabling
            //if (HttpContext.Current.Cache.Get(appName) != null)
            //{
            //    return (AppInfo)HttpContext.Current.Cache.Get(appName);
            //}

            var doc = new XmlDocument();
            doc.Load(HttpContext.Current.Server.MapPath(string.Format("~/App_Data/Configurations/{0}.xml", appName)));
            var xmlcontents = doc.InnerXml;
            var result = (AppInfo)Utilities.Deserialize(typeof(AppInfo), xmlcontents);
            //HttpContext.Current.Cache.Insert(appName, result);
            return result;
        }

        public AppInfoExtended GetAppExtendedByName(string appName, bool getVersionInfo = true)
        {
            Debug.WriteLine(appName + " 0 " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));
            var xmlresult = GetAppByName(appName);
            Debug.WriteLine(appName + " 1 " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));
            var result = new AppInfoExtended();
            result.AppName = xmlresult.AppName;
            //todo no trailing slash
            result.BuildOutputBasePath = xmlresult.BuildOutputBasePath;
            result.BuildOutputRelativeWebPath = xmlresult.BuildOutputRelativeWebPath;
            result.PendingRequest = xmlresult.PendingRequest;
            result.Powershell = xmlresult.Powershell;
            result.BuildOutputDatabases = xmlresult.BuildOutputDatabases;
            result.DestinationPaths = xmlresult.DestinationPaths;
            result.LastDeployed = xmlresult.LastDeployed;
            result.RobocopyExcludedFiles = xmlresult.RobocopyExcludedFiles;
            result.RobocopyExcludedFolders = xmlresult.RobocopyExcludedFolders;
            if (getVersionInfo)
            {
                result.LatestVersion = Utilities.GetVersion(Path.Combine(result.BuildOutputBasePath, result.BuildOutputRelativeWebPath), result.AppName);
                result.DestinationVersion = Utilities.GetVersion(result.DestinationPaths[0], result.AppName);

                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["BackupPath"]))
                    result.BackupVersion = Utilities.GetVersion(Path.Combine(System.Configuration.ConfigurationManager.AppSettings["BackupPath"], result.AppName), result.AppName);
            }
          
            var sb = new StringBuilder();
            result.IsValid = IsConfigurationValid(sb, result);
            result.InvalidMessage = sb.ToString();
            Debug.WriteLine(appName + " 2 " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));

            return result;
        }

        public void SaveConfiguration(AppInfo info)
        {
            var serializer = new XmlSerializer(typeof(AppInfo));
            var stringwriter = new Utf8StringWriter();
            var writer = XmlWriter.Create(stringwriter);
            serializer.Serialize(writer, info);
            var xml = stringwriter.ToString();
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            doc.Save(HttpContext.Current.Server.MapPath(string.Format("~/App_Data/Configurations/{0}.xml", info.AppName)));
        }

        public bool IsConfigurationValid(StringBuilder sb, AppInfoExtended appinfo)
        {
            if (appinfo == null)
            {
               Utilities.AppendAndSend(sb, "Configuration undefined");
               return false;
            }

            if (!Directory.Exists(appinfo.BuildOutputBasePath))
            {
                Utilities.AppendAndSend(sb, "Build output directory does not exist");
                return false;
            }

            if (appinfo.LatestVersion == "Unknown")
            {
                Utilities.AppendAndSend(sb, "Unable to locate latest version in build output folder");
                return false;
            }
       
            return true;
        }

        public void SetLastRunTime(string appName)
        {
            var appinfo = GetAppByName(appName);
            appinfo.LastDeployed = DateTime.Now + " by " + Utilities.CurrentUser;
            appinfo.PendingRequest = string.Empty;
            SaveConfiguration(appinfo);
        }

        public void SetPendingRequest(string appName, string message)
        {
            var appinfo = GetAppByName(appName);
            appinfo.PendingRequest = message;
            SaveConfiguration(appinfo);
        }

    }
    public sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding { get { return Encoding.UTF8; } }
    }
}
