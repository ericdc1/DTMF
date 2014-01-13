using System.Linq;
using System.Text;
using TeamCitySharp;
using TeamCitySharp.Locators;

namespace DTMF.Logic
{
    public class TeamCity
    {
        public static bool IsRunning(StringBuilder sb, string appName)
        {
            //skip if not configured
            if (System.Configuration.ConfigurationManager.AppSettings["TeamCityServer"] == string.Empty) return false;
            //Check for running builds
            var client = new TeamCityClient(System.Configuration.ConfigurationManager.AppSettings["TeamCityServer"]);
            client.ConnectAsGuest();
            var builds = client.Builds.ByBuildLocator(BuildLocator.RunningBuilds());
            if (builds.Any(f=>f.BuildTypeId.Contains(appName)))
            {
                Utilities.AppendAndSend(sb, "Build in progress. Sync disabled");
                //foreach (var build in builds)
                //{
                //    Utilities.AppendAndSend(sb, "<li>" + build.BuildTypeId + " running</li>");
                //}
                return true;
            }
            return false;
        }


      

    }
}