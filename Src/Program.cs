using System;
using System.IO;
using System.Linq;
using RT.Util;
using RT.Util.CommandLine;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace RemoveEmptyDirs
{
#pragma warning disable 649
    [DocumentationLiteral("Finds empty directories and optionally deletes them. This includes directories which only have other empty directories inside them.")]
    class Args : ICommandLineValidatable
    {
        [Option("-d", "--delete")]
        [DocumentationLiteral("If specified, all empty folders will be deleted. Otherwise, only prints the paths to all empty folders that would have been deleted.")]
        public bool Delete;

        [Option("-f", "--force-readonly")]
        [DocumentationLiteral("If specified, empty directories marked as read-only will also be deleted.")]
        public bool ForceReadonly;

        [IsPositional]
        [DocumentationLiteral("Directories to be scanned.")]
        public string[] Directories;

        public ConsoleColoredString Validate()
        {
            if (Directories.Length == 0)
                return "Please specify at least one directory to scan.";
            return null;
        }
    }
#pragma warning restore 649

    class Program
    {
        static ConsoleLogger Log = new ConsoleLogger();
        static int WarningsCount = 0;
        static int EmptyCount = 0;
        static Args Args;

        static int Main(string[] args)
        {
            try
            {
                Args = CommandLineParser.Parse<Args>(args);
            }
            catch (CommandLineParseException e)
            {
                e.WriteUsageInfoToConsole();
                return 1;
            }

            foreach (var dir in Args.Directories.Select(path => new DirectoryInfo(path)))
            {
                Log.Info("Scanning \"{0}\" for empty directories...".Fmt(dir.FullName));
                DeleteEmpty(dir);
            }

            if (WarningsCount > 0)
                Log.Warn("There were {0} warning(s); see log for details.".Fmt(WarningsCount));

            Log.Info("Finished. There were {0} empty directories.".Fmt(EmptyCount));
            return 0;
        }

        /// <summary>
        /// Returns true if the directory was empty.
        /// </summary>
        static bool DeleteEmpty(DirectoryInfo path)
        {
            DirectoryInfo[] dirs;
            try
            {
                dirs = path.GetDirectories();
            }
            catch (Exception e)
            {
                Log.Warn("Could not list contents of \"{0}\", skipping. See details below.".Fmt(path.FullName));
                Log.Exception(e, LogType.Warning);
                WarningsCount++;
                return false;
            }

            bool subdirsLeft = false;
            foreach (var dir in dirs)
            {
                if (DeleteEmpty(dir))
                {
                    EmptyCount++;
                    retry:
                    try
                    {
                        if (Args.Delete)
                            dir.Delete(false);
                        Log.Info("{1} empty directory \"{0}\"".Fmt(dir.FullName, Args.Delete ? "Deleting" : "Would delete"));
                    }
                    catch (Exception e)
                    {
                        if (dir.Attributes.HasFlag(FileAttributes.ReadOnly))
                        {
                            if (Args.ForceReadonly)
                            {
                                dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
                                goto retry;
                            }
                            else
                            {
                                Log.Warn("Skipping empty directory \"{0}\" because it is read-only. Use -f to force deletion.".Fmt(dir.FullName));
                                subdirsLeft = true;
                                WarningsCount++;
                            }
                        }
                        else
                        {
                            subdirsLeft = true;
                            Log.Warn("Could not delete directory \"{0}\" - see exception below".Fmt(dir.FullName));
                            Log.Exception(e, LogType.Warning);
                            WarningsCount++;
                        }
                    }
                }
                else
                    subdirsLeft = true;
            }

            return !subdirsLeft && !path.GetFiles().Any();
        }
    }
}
