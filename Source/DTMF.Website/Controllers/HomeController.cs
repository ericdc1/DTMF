using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using DTMF.Logic;
using DTMF.Models;

namespace DTMF.Controllers
{

    //todo button label stuff
    public class HomeController : Controller
    {
        private AppLogic appLogic = new AppLogic();
        private SyncLogic syncLogic = new SyncLogic();

        public ActionResult Index()
        {
            var appinfolist = new AppLogic().GetAppList().OrderBy(f => string.IsNullOrWhiteSpace(f.PendingRequest)).ThenBy(f => f.LatestVersion == f.ProductionVersion).ThenBy(f => f.AppName);
            return View(appinfolist);
        }

        public ActionResult Detailed()
        {
            var appinfolist = new AppLogic().GetAppList(true).OrderBy(f => string.IsNullOrWhiteSpace(f.PendingRequest)).ThenBy(f => f.LatestVersion == f.ProductionVersion).ThenBy(f => f.AppName);
            return View(appinfolist);
        }

        public ActionResult DetailedApp(string appName)
        {
            if (!System.IO.File.Exists(HttpContext.Server.MapPath(string.Format("~/App_Data/Configurations/{0}.xml", appName)))) return RedirectToAction("index");
            var appinfolist = new List<AppInfoExtended>();
            appinfolist.Add(appLogic.GetAppExtendedByName(appName));
            return View("Detailed", appinfolist);
        }

        public ActionResult RequestSync(string appName)
        {
            var message = Utilities.CurrentUser + " requested deployment of " + appName + " at " + DateTime.Now;
            HipChat.SendMessage(appName, message, "red");
            Utilities.SendEmailNotification(message);
            appLogic.SetPendingRequest(appName, message);
            return RedirectToAction("index");
        }

        [CanDeploy]
        public ActionResult Sync(string appName)
        {
            if (!string.IsNullOrEmpty(Utilities.GetRunningStatus()))
            {
                TempData["Message"] = "Another sync is currently in progress. Please try again in a few minutes. (" + Utilities.GetRunningStatus() + ")";
                Utilities.SetRunningStatus(string.Empty);
                return RedirectToAction("index");
            }


            ViewBag.AppName = appName;
            return View();
        }

        [CanDeploy]
        public void RunSync(string appName)
        {
            var runlog = new StringBuilder();

            Utilities.SetRunningStatus(appName);
            //Check for running builds
            if (TeamCity.IsRunning(runlog, appName))
            {
                Utilities.SetRunningStatus(string.Empty);
                return;
            }

            //set some variables
            var appinfo = appLogic.GetAppExtendedByName(appName);
            var binpath = HttpContext.ApplicationInstance.Server.MapPath("~/bin") + @"\";
            var tranformspath = HttpContext.ApplicationInstance.Server.MapPath("~/App_Data/Transforms/") + @"\";
            var baselogpath = HttpContext.ApplicationInstance.Server.MapPath("~/App_Data/Logs/") + @"\";

            if (!appLogic.IsConfigurationValid(runlog, appinfo))
            {
                Utilities.SetRunningStatus(string.Empty);
                return;
            }

            Utilities.AppendAndSend(runlog, "");
            Utilities.AppendAndSend(runlog, "Started at " + DateTime.Now);

            //show who we are running as
            Utilities.AppendAndSend(runlog, "Run as: " + syncLogic.ExecuteCode("whoami"), Utilities.WrapIn.H4);

            //rename build folder
            Utilities.AppendAndSend(runlog, "Rename build folder to _temp", Utilities.WrapIn.H4);
            if (!Directory.Exists(appinfo.BuildOutputBasePath))
            {
                Utilities.AppendAndSend(runlog, "Directory missing");
                Utilities.SetRunningStatus(string.Empty);
                return;
            }
            Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("Rename-Item '" + appinfo.BuildOutputBasePath + "' '" + appinfo.BuildOutputBasePathTemp + "'"), Utilities.WrapIn.Pre);

            //deploy web code to each server
            foreach (var prodpath in appinfo.ProductionPaths)
            {
                Utilities.AppendAndSend(runlog, "Running on Web Server " + prodpath, Utilities.WrapIn.H3);

                //only backup once since all targets will be the same
                if (appinfo.ProductionPaths.First() == prodpath && !string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["BackupPath"]))
                {
                    Utilities.AppendAndSend(runlog, "Backup application", Utilities.WrapIn.H4);
                    Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("& robocopy '" + prodpath + "' '" + Path.Combine(System.Configuration.ConfigurationManager.AppSettings["BackupPath"], appinfo.AppName) + "' /ETA /MIR /NP /W:2 /R:1"), Utilities.WrapIn.Pre);
                }

                Utilities.AppendAndSend(runlog, "Take web app offline", Utilities.WrapIn.H4);
                Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("copy-item '" + binpath + @"\tools\app_offline.htm' '" + Path.Combine(prodpath, "app_offline.htm") + "'"), Utilities.WrapIn.Pre);

                Utilities.AppendAndSend(runlog, "Copy new application", Utilities.WrapIn.H4);
                Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("& robocopy '" + Path.Combine(appinfo.BuildOutputBasePathTemp, appinfo.BuildOutputRelativeWebPath) + "' '" + prodpath + "' /ETA /MIR /NP /W:2 /R:1 /XD " + appinfo.RobocopyExcludedFolders + " /XF app_offline.htm " + appinfo.RobocopyExcludedFiles), Utilities.WrapIn.Pre);


                if (System.IO.File.Exists(tranformspath + appinfo.AppName + ".web.config "))
                {
                    Utilities.AppendAndSend(runlog, "Transform web.config", Utilities.WrapIn.H4);
                    //usually we want web.config
                    var webconfigpath = Path.Combine(appinfo.BuildOutputBasePathTemp, appinfo.BuildOutputRelativeWebPath) + "\\web.config";
                    //sometimes we don't check in web.config and have a template file instead
                    if (!System.IO.File.Exists(webconfigpath))
                        webconfigpath = Path.Combine(appinfo.BuildOutputBasePathTemp, appinfo.BuildOutputRelativeWebPath) + "\\web.template.config";
                    Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("& '" + binpath + @"\tools\webconfigtransformrunner.exe' '" + webconfigpath + "' '" + tranformspath + appinfo.AppName + ".web.config' '" + prodpath + "\\web.config'"), Utilities.WrapIn.Pre);
                }
                else
                {
                    Utilities.AppendAndSend(runlog, "No transform named " + appinfo.AppName + ".web.config" + " found", Utilities.WrapIn.H4);
                }

                Utilities.AppendAndSend(runlog, "Bring app back online", Utilities.WrapIn.H4);
                Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("remove-item '" + Path.Combine(prodpath, "app_offline.htm") + "'"), Utilities.WrapIn.Pre);
                Utilities.AppendAndSend(runlog, "Total execution time: " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));
            }


            if (appinfo.BuildOutputDatabases.Any() && !string.IsNullOrEmpty(appinfo.BuildOutputDatabases[0].ServerName))
            {
                Utilities.AppendAndSend(runlog, "Run database scripts", Utilities.WrapIn.H3);
                foreach (var db in appinfo.BuildOutputDatabases)
                {
                    Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("& '" + binpath + "\\tools\\aliasql.exe' 'Update' '" + db.ServerName + "' '" + db.DatabaseName + "' '" + Path.Combine(appinfo.BuildOutputBasePathTemp, db.BuildOutputRelativeScriptPath) + "'"), Utilities.WrapIn.Pre);
                    Utilities.AppendAndSend(runlog, "Total execution time: " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));
                }
            }

            if (!string.IsNullOrEmpty(appinfo.Powershell))
            {
                Utilities.AppendAndSend(runlog, "Run custom powershell", Utilities.WrapIn.H3);
                Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode(appinfo.Powershell), Utilities.WrapIn.Pre);
                Utilities.AppendAndSend(runlog, "Total execution time: " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));
            }

            Utilities.AppendAndSend(runlog, "Rename build folder to original name", Utilities.WrapIn.H4);
            Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("Rename-Item '" + appinfo.BuildOutputBasePathTemp + "' '" + appinfo.BuildOutputBasePath + "'"), Utilities.WrapIn.Pre);
            Utilities.AppendAndSend(runlog, "Total execution time: " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));

            runlog.AppendLine("Finished at " + DateTime.Now);

            var message = Utilities.CurrentUser + " deployed " + appName + " version " + appinfo.LatestVersion + " at " + DateTime.Now;
            HipChat.SendMessage(appName, message, "green");

            //mark last ran time 
            appLogic.SetLastRunTime(appName);

            //log it
            LogLogic.SaveLog(baselogpath, appName, runlog.ToString());

            //all done
            Utilities.AppendAndSend(runlog, "Done!", Utilities.WrapIn.H3);

            Utilities.AppendAndSend(runlog, "<a href=\"index\" class=\"btn btn-primary\">Continue</a>");

            Utilities.SetRunningStatus(string.Empty);
        }

        [CanDeploy]
        public ActionResult Rollback(string appName)
        {
            if (!string.IsNullOrEmpty(Utilities.GetRunningStatus()))
            {
                TempData["Message"] = "Another sync is currently in progress. Please try again in a few minutes. (" + Utilities.GetRunningStatus() + ")";
                Utilities.SetRunningStatus(string.Empty);
                return RedirectToAction("index");
            }

            ViewBag.AppName = appName;
            return View();
        }

        [CanDeploy]
        public void RunRollback(string appName)
        {
            var runlog = new StringBuilder();
            Utilities.SetRunningStatus(appName);
            //set some variables
            var appinfo = appLogic.GetAppExtendedByName(appName);
            var binpath = HttpContext.ApplicationInstance.Server.MapPath("~/bin") + @"\";
            var baselogpath = HttpContext.ApplicationInstance.Server.MapPath("~/App_Data/Logs/") + @"\";
            var rollbackpath = Path.Combine(System.Configuration.ConfigurationManager.AppSettings["BackupPath"], appName);
            Utilities.AppendAndSend(runlog, "");
            Utilities.AppendAndSend(runlog, "Rollback started at " + DateTime.Now);

            //show who we are running as
            Utilities.AppendAndSend(runlog, "Run as: " + syncLogic.ExecuteCode("whoami"), Utilities.WrapIn.H4);

            if (!Directory.Exists(rollbackpath))
            {
                Utilities.AppendAndSend(runlog, "Directory missing");
                Utilities.SetRunningStatus(string.Empty);
                return;
            }

            //deploy web code to each server
            foreach (var prodpath in appinfo.ProductionPaths)
            {
                Utilities.AppendAndSend(runlog, "Running on Web Server " + prodpath, Utilities.WrapIn.H3);

                Utilities.AppendAndSend(runlog, "Take web app offline", Utilities.WrapIn.H4);
                Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("copy-item " + binpath + @"\tools\app_offline.htm " + Path.Combine(prodpath, "app_offline.htm")), Utilities.WrapIn.Pre);

                Utilities.AppendAndSend(runlog, "Copy new application", Utilities.WrapIn.H4);
                Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("& robocopy " + rollbackpath + " " + prodpath + " /ETA /MIR /NP /W:2 /R:1 /XD " + appinfo.RobocopyExcludedFolders + " /XF app_offline.htm web.production.config web.development.config"), Utilities.WrapIn.Pre);

                Utilities.AppendAndSend(runlog, "Bring app back online", Utilities.WrapIn.H4);
                Utilities.AppendAndSend(runlog, syncLogic.ExecuteCode("remove-item " + Path.Combine(prodpath, "app_offline.htm")), Utilities.WrapIn.Pre);
                Utilities.AppendAndSend(runlog, "Total execution time: " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));
            }

            Utilities.AppendAndSend(runlog, "Total execution time: " + Math.Round(DateTime.Now.Subtract(System.Web.HttpContext.Current.Timestamp).TotalSeconds, 3));

            runlog.AppendLine("Finished at " + DateTime.Now);

            var message = Utilities.CurrentUser + " rolled back " + appName + " to version " + appinfo.BackupVersion + " at " + DateTime.Now;
            HipChat.SendMessage(appName, message, "yellow");

            //mark last ran time 
            appLogic.SetLastRunTime(appName);

            //log it
            LogLogic.SaveLog(baselogpath, appName, runlog.ToString());

            //all done
            Utilities.AppendAndSend(runlog, "Done!", Utilities.WrapIn.H3);

            Utilities.AppendAndSend(runlog, "<a href=\"index\" class=\"btn btn-primary\">Continue</a>");

            Utilities.SetRunningStatus(string.Empty);
        }
        public ActionResult Unlock()
        {
            Utilities.SetRunningStatus(string.Empty);
            return RedirectToAction("index");
        }

        public ActionResult Denied()
        {
            return Content("You do not have access to this action!");
        }

    }
}
