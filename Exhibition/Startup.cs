using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Exhibition.Startup))]
namespace Exhibition
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
