using nugex.cmdline;
using System;
using System.Diagnostics;
using Switch = nugex.cmdline.Switch;

namespace nugex
{
    partial class Program
    {
        static void Main(string[] args)
        {
            CmdLine.Parser.InitCommands(new[]
            {
                new Command("search", "search for package(s) on known feeds (from nuget.config)", () => Search(),
                        new Parameter(_SEARCH_TERM_, "search term (^ and $ can be used to make the phrase more specific)", mandatory: true),
                        new Parameter(_VSPEC_, "version number (common regex)"),
                        new Switch(_ALL_FEEDS_, "...even those without matching packages")
                    ),
                new Command("download", "... a package from nuget.org", () => Download(),
                        new Parameter(_SEARCH_TERM_, "exact package id", mandatory: true),
                        new Parameter(_VSPEC_, "exact version number", mandatory: true),
                        new Parameter(_TARGET_PATH_, "download folder for the package")
                    ),
                new Command("copy", "... a package from nuget.org", () => Copy(),
                        new Parameter(_SEARCH_TERM_, "exact package id", mandatory: true),
                        new Parameter(_VSPEC_, "exact version number", mandatory: true),
                        new Parameter(_TARGET_FEED_, "the internal location for the package", mandatory: true),
                        new Parameter(_API_KEY_, "for the case the default value would not work")
                    ),
                new Command("explore", "...the dependencies of a given package. Some dependencies may be listed with multiple versions. This can be handled differently by different resolve strategies.", () => Explore(),
                        new Parameter(_SEARCH_TERM_, "exact package id", mandatory: true),
                        new Parameter(_VSPEC_, "version number"),
                        new Parameter(_FWSPEC_, ".net \"framework\" like netcoreapp3.1 or net5.0"),
                        new Switch(_NO_EXACT_, "blend out feeds with exact matching"),
                        new Switch(_NO_PARTIALS_, "blend out feeds where the package exists with different version"),
                        new Switch(_NO_MISSINGS_, "blend out feeds where the package doesn't exist"))
            });

            CmdLine.Parser.Parse(args);

            CmdLine.Parser.ExecuteCommand();

            if (Debugger.IsAttached)
            {
                Console.WriteLine("press a key to terminate");
                Console.ReadKey();
            }
        }

        // option names in alphabetical order
        public static readonly string _API_KEY_ = "apiKey";
        public static readonly string _SEARCH_TERM_ = "name";
        public static readonly string _ALL_FEEDS_ = "show-all-feeds";
        public static readonly string _TARGET_FEED_ = "targetFeed";
        public static readonly string _TARGET_PATH_ = "targetDir";
        public static readonly string _VSPEC_ = "version";
        public static readonly string _FWSPEC_ = "framework";
        public static readonly string _NO_EXACT_ = "no-exact";
        public static readonly string _NO_PARTIALS_ = "no-partials";
        public static readonly string _NO_MISSINGS_ = "no-missings";

    }
}
