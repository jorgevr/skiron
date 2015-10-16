using System;
using Gnarum.Cryptography;

namespace Gnarum.SigmaMeasureFilter.Model.Util
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
