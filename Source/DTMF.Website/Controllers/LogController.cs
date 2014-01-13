using System.Linq;
using System.Web.Mvc;
using DTMF.Logic;

namespace DTMF.Controllers
{
    public class LogController : Controller
    {
        private LogLogic logLogic = new LogLogic();
        public ActionResult History(string appName)
        {
            ViewBag.AppName = appName;
            var apphistorylist = logLogic.GetHistoryList(appName).OrderByDescending(f=>f).ToList();
            return View(apphistorylist);
        }

        public ActionResult ViewLog(string appName, string fileName)
        {
            var filecontents = logLogic.GetLogFileContents(appName, fileName);
            ViewBag.FileContents = filecontents;
            return View();
        }
    }
}
