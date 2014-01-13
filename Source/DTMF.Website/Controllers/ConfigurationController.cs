using System.Web.Mvc;
using DTMF.Logic;

namespace DTMF.Controllers
{
    public class ConfigurationController : Controller
    {
        private AppLogic appLogic = new AppLogic();
        public ActionResult ViewConfig(string appName)
        {
            var app = appLogic.GetAppExtendedByName(appName);
            return View(app);
        }
    }
}
