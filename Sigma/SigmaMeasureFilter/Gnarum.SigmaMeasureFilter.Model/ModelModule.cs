using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject.Modules;
using Gnarum.SigmaMeasureFilter.Model.Services;
using Ninject;

namespace Gnarum.SigmaMeasureFilter.Model
{
    public class ModelModule : NinjectModule
    {
        public override void Load()
        {
            bindSigmaMeasure();
        }

        private void bindSigmaMeasure()
        {
            Bind<ISigmaMeasureService>().To<SigmaMeasureService>();
        }
    }

    public static class ModelModuleProvider
    {
        public static T GetDependency<T>()
        {
            return new StandardKernel(new ModelModule()).Get<T>();
        }
    }
}
