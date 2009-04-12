using System;
using System.IO;
using System.Linq;
using RT.Util;

namespace RemoveEmptyDirs
{
    class Program
    {
        static ConsoleLogger Log = new ConsoleLogger();
        static int WarningsCount = 0;

        static void Main(string[] args)
        {
            CmdLineParser parser = new CmdLineParser();
            parser.DefineDefaultHelpOptions();
            parser.Parse(args);

            parser.ErrorIfPositionalArgsCountNot(1);

            var dir = new DirectoryInfo(parser.OptPositional[0]);
            Log.Info("Scanning \"{0}\" for empty directories to delete...", dir.FullName);
            DeleteEmpty(dir);

            if (WarningsCount > 0)
                Log.Warn("There were {0} warning(s); see log for details.", WarningsCount);

            Log.Info("Finished.");
        }

        /// <summary>
        /// Returns true if the directory was empty.
        /// </summary>
        static bool DeleteEmpty(DirectoryInfo path)
        {
            bool subdirsLeft = false;
            foreach (var dir in path.GetDirectories())
            {
                if (DeleteEmpty(dir))
                {
                    try
                    {
                        dir.Delete(false);
                        Log.Info("Deleting empty directory \"{0}\"", dir.FullName);
                    }
                    catch (Exception e)
                    {
                        subdirsLeft = true;
                        Log.Warn("Could not delete directory \"{0}\" - see exception below", dir.FullName);
                        Log.Exception(e, LogType.Warning);
                        WarningsCount++;
                    }
                }
                else
                    subdirsLeft = true;
            }

            return !subdirsLeft && !path.GetFiles().Any();
        }
    }
}
