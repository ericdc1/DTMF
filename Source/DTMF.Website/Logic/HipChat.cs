using System.Collections.Specialized;
using System.Net;

namespace DTMF.Logic
{
    public class HipChat
    {

        public static void SendMessage(string message, string color)
        {       
            var room = System.Configuration.ConfigurationManager.AppSettings["HipChatRoomID"];
            SendMessage(room, message, color);
        }


        public static void SendMessage(string HipChatRoomID, string message, string color)
        {
            //skip if not configured
            if (System.Configuration.ConfigurationManager.AppSettings["HipChatAuthToken"] == string.Empty) return;

            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["message"] = message;
                data["from"] = System.Configuration.ConfigurationManager.AppSettings["HipChatMessageFrom"];
                data["notify"] = "1";
                data["color"] = color;
                var room = HipChatRoomID;
                var authtoken = System.Configuration.ConfigurationManager.AppSettings["HipChatAuthToken"];
                wb.UploadValues(string.Format("https://api.hipchat.com/v1/rooms/message?auth_token={0}&room_id={1}", authtoken, room), "POST", data);
            }
        }


    }
}