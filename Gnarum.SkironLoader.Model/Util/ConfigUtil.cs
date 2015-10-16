using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gnarum.Cryptography;

namespace Gnarum.SkironLoader.Model.Util
{
    public static class ConfigUtil
    {
        private static string connectionStringNameFile = "wise.csconfig";
        private static string pathEnvironmentVariableKey = "Gnarum.Wise.Config.Path";

        public static string Path
        {
            get
            {
                return System.Environment.GetEnvironmentVariable(pathEnvironmentVariableKey, EnvironmentVariableTarget.Machine);
            }
        }

        public static string ConnectionStringPathFile
        {
            get
            {
                return System.IO.Path.Combine(ConfigUtil.Path, "DB\\" + connectionStringNameFile);
            }
        }

        public static string ReadDBConnectionString()
        {
            return TripleDESEncrypter.DecryptFileContent(ConnectionStringPathFile);
        }

    }
}
