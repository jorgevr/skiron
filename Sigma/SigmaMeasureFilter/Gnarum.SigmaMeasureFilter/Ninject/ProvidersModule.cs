using Gnarum.SigmaMeasureFilter.Providers;
using Ninject;
using Ninject.Modules;

namespace Gnarum.SigmaMeasureFilter.Ninject
{
    public class ProvidersModule : NinjectModule
    {
        public override void Load()
        {
            bindWebAPIProvider();
            bindSAPProvider();
        }

        private void bindSAPProvider()
        {
            Bind<ISAPProvider>().To<SAPProvider>();
        }

        private void bindWebAPIProvider()
        {
            Bind<IWebAPIProvider>().To<WebAPIProvider>();
        }
    }

    public static class ProvidersProvider
    {
        public static T GetDependency<T>()
        {
            return new StandardKernel(new ProvidersModule()).Get<T>();
        }
    }
}
