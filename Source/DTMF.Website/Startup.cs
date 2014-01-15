using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(DTMF.Startup))]
namespace DTMF
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
            ConfigureAuthentication(app);
        }
    }
}