using System;
using Microsoft.AspNet.SignalR;

namespace DTMF
{
    public class MessageHub : Hub
    {
        public string Activate()
        {
            return "Monitor Activated";
        }

    }
}