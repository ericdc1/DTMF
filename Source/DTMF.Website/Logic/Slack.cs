using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;

namespace DTMF.Logic
{
    public class Slack
    {
        public static void SendMessage(string appName, string title, string message, string color, string link, string iconUrl = null, string imageUrl = null)
        {       
            var room = System.Configuration.ConfigurationManager.AppSettings["SlackRoomID"];
            SendMessage(room, appName, title, message, color, link, iconUrl, imageUrl);
        }

        public static void SendMessage(string slackRoomId, string appName, string title, string message, string color, string link, string iconUrl, string imageUrl)
        {
            //skip if not configured
            if (System.Configuration.ConfigurationManager.AppSettings["SlackAuthToken"] == string.Empty) 
                return;

            var authtoken = System.Configuration.ConfigurationManager.AppSettings["SlackAuthToken"];
            var from = System.Configuration.ConfigurationManager.AppSettings["SlackMessageFrom"];
            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["attachments"] = JsonConvert.SerializeObject(new List<object>
                {
                    new { color, fallback = message, text = message, title, title_link = link, image_url = imageUrl }
                });
                data["icon_url"] = iconUrl;
                wb.UploadValues(string.Format("https://slack.com/api/chat.postMessage?token={0}&channel={1}&username={2}&pretty=1", authtoken, slackRoomId, from), "POST", data);
            }
        }
    }
}