using System;
using System.Configuration;
using Gnarum.SAP;
using Gnarum.SigmaMeasureFilter.Model;
using Gnarum.SigmaMeasureFilter.Model.Sap;
using Ninject;

namespace Gnarum.SigmaMeasureFilter.Ninject
{
    public static class ModelProvider
    {

        public static T GetDependency<T>()
        {

            var kernel = new StandardKernel();

            registerServices(kernel);

            return kernel.Get<T>();
        }

        private static void registerServices(StandardKernel kernel)
        {
            loadRepositoryModule(kernel);

            kernel.Load(new ModelModule());
        }

        private static void loadRepositoryModule(StandardKernel kernel)
        {
            string repositoryType = ConfigurationManager.AppSettings["RepositoryType"];

            if (repositoryType == "SAP")
            {
                kernel.Load(new SapModule());
                kernel.Load(new SapRepositoryModule());
            }
            else if (repositoryType == "NHIB")
                kernel.Load(new NHibernateRepositoryModule());
            else
                throw new ArgumentException("Repository Type is not recognized");
        }
    }
}

