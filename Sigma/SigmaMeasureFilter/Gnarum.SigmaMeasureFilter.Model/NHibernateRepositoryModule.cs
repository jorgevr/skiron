using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject.Modules;
using Gnarum.SigmaMeasureFilter.Model.Repositories;

namespace Gnarum.SigmaMeasureFilter.Model
{
    public class NHibernateRepositoryModule : NinjectModule
    {
        public override void Load()
        {
            bindSigmaMeasure();
        }

        private void bindSigmaMeasure()
        {
            Bind<ISigmaMeasureRepository>().To<SigmaMeasureRepository>();
        }
    }
}