using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DTMF.Logic
{
    public static class TeamCity
    {
        public static async Task<bool> IsRunningAsync(StringBuilder sb, string appName, string buildConfigurationID)
        {
            //change app name to march the buildtypeid by replacing . with underscore
            appName = appName.Replace(".", "_");

            //if a specific build config ID is set use that
            if (!string.IsNullOrEmpty(buildConfigurationID))
            {
                appName = buildConfigurationID;
            }

            var buildsResponse = await GetRunningBuildsAsync();
            if (buildsResponse == null) return false;
           
            if (buildsResponse.build.Any(f => f.buildTypeId.ToLowerInvariant().Contains(appName.ToLowerInvariant())))
            {
                Utilities.AppendAndSend(sb, "Build in progress. Sync disabled.");
                foreach (var build in buildsResponse.build)
                {
                    Utilities.AppendAndSend(sb, "<li>" + build.buildTypeId + " running</li>");
                }
                return true;
            }
            return false;
        }

        private static async Task<BuildsResponse> GetRunningBuildsAsync()
        {
            if (System.Configuration.ConfigurationManager.AppSettings["TeamCityServer"] == string.Empty) return null;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = CreateBasicAuthHeader(
                    System.Configuration.ConfigurationManager.AppSettings["TeamCityUser"],
                    System.Configuration.ConfigurationManager.AppSettings["TeamCityPass"]);

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync("https://" + System.Configuration.ConfigurationManager.AppSettings["TeamCityServer"] + "/httpAuth/app/rest/builds?locator=running:True");

                if (!response.IsSuccessStatusCode) return null;

                return JsonConvert.DeserializeObject<BuildsResponse>(await response.Content.ReadAsStringAsync());
            }
        }

        private static AuthenticationHeaderValue CreateBasicAuthHeader(string username, string password)
        {
            var unencodedAuth = Encoding.UTF8.GetBytes(username + ":" + password);

            var auth = System.Convert.ToBase64String(unencodedAuth);

            return new AuthenticationHeaderValue("Basic", auth);
        }


        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable UnusedMember.Global
        // ReSharper disable InconsistentNaming
        private class BuildsResponse
        {
            public int count { get; set; }
            public string href { get; set; }
            public Build[] build { get; set; }
        }

        private class Build
        {
            public int id { get; set; }
            public string buildTypeId { get; set; }
            public string number { get; set; }
            public string status { get; set; }
            public string state { get; set; }
            public int percentageComplete { get; set; }
            public string href { get; set; }
            public string webUrl { get; set; }
        }
        // ReSharper restore InconsistentNaming
        // ReSharper restore UnusedMember.Global
        // ReSharper restore ClassNeverInstantiated.Local
    }
}