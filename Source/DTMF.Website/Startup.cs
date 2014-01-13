using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(DTMF.Startup))]
namespace DTMF
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}