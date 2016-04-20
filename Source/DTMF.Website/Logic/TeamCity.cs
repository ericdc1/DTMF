using System.Linq;
using System.Text;
using TeamCitySharp;
using TeamCitySharp.Locators;

namespace DTMF.Logic
{
    public class TeamCity
    {
        public static bool IsRunning(StringBuilder sb, string appName, string buildConfigurationID)
        {
            //change app name to march the buildtypeid by replacing . with underscore
            appName = appName.Replace(".", "_");

            //if a specific build config ID is set use that
            if (!string.IsNullOrEmpty(buildConfigurationID))
            {
                appName = buildConfigurationID;
            }
  

            //skip if not configured
            if (System.Configuration.ConfigurationManager.AppSettings["TeamCityServer"] == string.Empty) return false;
            //Check for running builds
            var client = new TeamCityClient(System.Configuration.ConfigurationManager.AppSettings["TeamCityServer"]);
            //client.ConnectAsGuest();
            client.Connect(System.Configuration.ConfigurationManager.AppSettings["TeamCityUser"], System.Configuration.ConfigurationManager.AppSettings["TeamCityPass"]);
            var builds = client.Builds.ByBuildLocator(BuildLocator.RunningBuilds());
            if (builds.Any(f=>f.BuildTypeId.ToLower().Contains(appName.ToLower())))
            {
                Utilities.AppendAndSend(sb, "Build in progress. Sync disabled.");
                foreach (var build in builds)
                {
                    Utilities.AppendAndSend(sb, "<li>" + build.BuildTypeId + " running</li>");
                }
                return true;
            }
            return false;
        }


      

    }
}