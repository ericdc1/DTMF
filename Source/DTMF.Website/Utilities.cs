using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DTMF.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace DTMF
{
    public class Utilities
    {
        public static bool CanDeploy
        {
            get
            {
                var deployers =
                    System.Configuration.ConfigurationManager.AppSettings["Deployers"].Split(Convert.ToChar(","));
                return deployers.Any(deployer => CurrentUser==deployer);
            }
        }

        public static string CurrentUser
        {
            get
            {
                return System.Web.HttpContext.Current.User.Identity.Name;
            }
        }

        public static string AppPoolUser
        {
            get
            {
                var user = System.Security.Principal.WindowsIdentity.GetCurrent().User;
                var userName = user.Translate(typeof(System.Security.Principal.NTAccount)).ToString();
                return userName;
            }
        }

        public static string GetVersion(string path, string appname)
        {
            //remove prefix and suffixes from app names so same app can go to multiple places
            appname = appname.Replace(".Production", "");
            appname = appname.Replace(".Development", "");
            appname = appname.Replace(".Staging", "");
            appname = appname.Replace(".Test", "");

            var binpath = Path.Combine(path, "bin");
            if (Directory.Exists(binpath))
            {
                var files = Directory.GetFiles(binpath, appname + "*.dll");
                var firstfile = files.FirstOrDefault();
                if (firstfile == null || !File.Exists(firstfile)) return "Not found";
                var info = FileVersionInfo.GetVersionInfo(firstfile);
                return info.FileVersion;
            }

            return "Unknown";

        }

        public static void SendHubMessage(string message)
        {
            GlobalHost
                .ConnectionManager
                .GetHubContext<MessageHub>().Clients.All.sendMessage(
                    message);
        }

        public static void AppendAndSend(StringBuilder sb, string message, WrapIn wrapin = WrapIn.None)
        {
            if (wrapin == WrapIn.Pre)
                message = string.Format("{0}{1}{2}", "<pre>", message, "</pre>");
            if (wrapin == WrapIn.H3)
                message = string.Format("{0}{1}{2}", "<h3>", message, "</h3>");
            if (wrapin == WrapIn.H4)
                message = string.Format("{0}{1}{2}", "<h4>", message, "</h4>");
            sb.AppendLine(message);
            SendHubMessage(message);
        }

        public static object Deserialize(Type typeToDeserialize, string xmlString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(xmlString);
            var mem = new MemoryStream(bytes);
            var ser = new XmlSerializer(typeToDeserialize);
            return ser.Deserialize(mem);
        }

        public static void SetRunningStatus(string appName)
        {
            HttpContext.Current.Application["RunningSync"] = appName;
        }
        public static string GetRunningStatus()
        {
            return HttpContext.Current.Application["RunningSync"] as string;
        }
       public  static void SendEmailNotification(string messagebody)
       {
           if (string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["RequestEmailAddress"]))
               return;

            var message = new System.Net.Mail.MailMessage();
            message.To.Add(System.Configuration.ConfigurationManager.AppSettings["RequestEmailAddress"]);
            message.Subject = "DTMF Sync Request";
            message.From = new System.Net.Mail.MailAddress(System.Configuration.ConfigurationManager.AppSettings["RequestEmailAddress"]);
            message.Body = messagebody;
            var smtp = new System.Net.Mail.SmtpClient();
            smtp.Send(message);
        }

        public enum WrapIn
        {
            None,
            Pre,
            H3,
            H4
        }

 
    }
}


public class XmlToDynamic
{
    public static void Parse(dynamic parent, XElement node)
    {
        if (node.HasElements)
        {
            if (node.Elements(node.Elements().First().Name.LocalName).Count() > 1)
            {
                //list
                var item = new ExpandoObject();
                var list = new List<dynamic>();
                foreach (var element in node.Elements())
                {
                    Parse(list, element);
                }

                AddProperty(item, node.Elements().First().Name.LocalName, list);
                AddProperty(parent, node.Name.ToString(), item);
            }
            else
            {
                var item = new ExpandoObject();

                foreach (var attribute in node.Attributes())
                {
                    AddProperty(item, attribute.Name.ToString(), attribute.Value.Trim());
                }

                //element
                foreach (var element in node.Elements())
                {
                    Parse(item, element);
                }

                AddProperty(parent, node.Name.ToString(), item);
            }
        }
        else
        {
            AddProperty(parent, node.Name.ToString(), node.Value.Trim());
        }
    }

    private static void AddProperty(dynamic parent, string name, object value)
    {
        if (parent is List<dynamic>)
        {
            (parent as List<dynamic>).Add(value);
        }
        else
        {
            (parent as IDictionary<String, object>)[name] = value;
        }
    }
}