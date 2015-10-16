using Gnarum.SigmaMeasureFilter.Model.Repositories;
using Gnarum.SigmaMeasureFilter.Model.Sap.Converters;
using Gnarum.SigmaMeasureFilter.Model.Sap.OptionsGenerators;
using Gnarum.SigmaMeasureFilter.Model.Sap.Repositories;
using Ninject.Modules;
using Ninject;

namespace Gnarum.SigmaMeasureFilter.Model.Sap
{
    public class SapRepositoryModule : NinjectModule
    {
        public override void Load()
        {
            bindSigmaMeasure();
        }

        private void bindSigmaMeasure()
        {
            Bind<ISapSigmaMeasureOptionsGenerator>().To<SapSigmaMeasureOptionsGenerator>();
            Bind<ISapSigmaMeasureConverter>().To<SapSigmaMeasureConverter>();
            Bind<ISigmaMeasureRepository>().To<SapSigmaMeasureRepository>();
        }

    }

    public static class SapRepositoryModuleProvider
    {
        public static T GetDependency<T>()
        {
            return new StandardKernel(new SapRepositoryModule()).Get<T>();
        }
    }
}