using nugex.cmdline;
using System;
using System.Diagnostics;
using Switch = nugex.cmdline.Switch;

namespace nugex
{
    partial class Program
    {
        public static void InitCommands() { 
            CmdLine.Parser.InitCommands(new[]
            {
                new Command("search", "search for package(s) on known feeds (from nuget.config)", () => Search(),
                        new Parameter(_SEARCH_TERM_, "search term (^ and $ can be used to make the phrase more specific)", mandatory: true),
                        new Parameter(_VPATTERN_, "version number pattern (common regex)"),
                        new Parameter(_VSPEC_, "exact version number"),
                        new Switch(_ALL_FEEDS_, "...even those without matching packages")
                    ),
                new Command("download", "download a package (a .nupkg file) into a local folder.", () => Download(),
                        new Parameter(_SEARCH_TERM_, "exact package id", mandatory: true),
                        new Parameter(_VSPEC_, "exact version number", mandatory: true),
                        new Parameter(_SOURCE_FEED_, $"download from this feed, default: {utils.FeedSelector.DefaultFeedName}"),
                        new Parameter(_TARGET_PATH_, "target folder for the package")
                    ),
                new Command("copy", "copies a package from nuget.org or internal feed to another one.", () => Copy(),
                        new Parameter(_SEARCH_TERM_, "exact package id", mandatory: true),
                        new Parameter(_VSPEC_, "exact version number", mandatory: true),
                        new Parameter(_SOURCE_FEED_, $"search on this feed, default: {utils.FeedSelector.DefaultFeedName}. Enables copying from internal feeds."),
                        new Parameter(_TARGET_FEED_, "the internal location for the package", mandatory: true),
                        new Parameter(_API_KEY_, "for the case the default value would not work")
                    ),
                new Command("explore", "...the dependencies of a given package. Internal exploration tells if (and how) we can satisfy a usage or an installation.", () => Explore(),
                        new Parameter(_SOURCE_FEED_, "specifies where to start the search, thus it enables to explore internal only. Default: nuget.org.", mandatory: true),
                        new Parameter(_SEARCH_TERM_, "exact package id", mandatory: true),
                        new Parameter(_VSPEC_, "version number"),
                        new Parameter(_FWSPEC_, ".Net \"framework\" like netcoreapp3.1 or net5.0. If not specified one will be chosen automatically, which is supported by the asked package."),
                        new Switch(_NO_EXACT_, "blend out feeds with exact matching"),
                        new Switch(_NO_PARTIALS_, "blend out feeds where the package exists with different version"),
                        new Switch(_NO_MISSINGS_, "blend out feeds where the package doesn't exist"),
                        new Switch(_CONSIDER_DISABLED_FEEDS_, "search also on internal feeds which are disabled in the configuration")),
            });
        }

        static void Main(string[] args)
        {
            InitCommands();

            try
            {
                CmdLine.Parser.Parse(args);

                CmdLine.Parser.ExecuteCommand();
            }
            catch (Exception exc)
            {
                var error = ExceptionProcessor.GetSupportedExcepton(exc);
                if (error != null) Console.Error.WriteLine($"ERROR: {error.Message}");
                else throw;
            }
            finally
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("press a key to terminate");
                    Console.ReadKey();
                }
            }
        }

        // option names in alphabetical order
        public static readonly string _API_KEY_ = "apiKey";
        public static readonly string _SEARCH_TERM_ = "name";
        public static readonly string _ALL_FEEDS_ = "show-all-feeds";
        public static readonly string _SOURCE_FEED_ = "source";
        public static readonly string _TARGET_FEED_ = "targetFeed";
        public static readonly string _TARGET_PATH_ = "targetDir";
        public static readonly string _VPATTERN_ = "version-pattern";
        public static readonly string _VSPEC_ = "version";
        public static readonly string _FWSPEC_ = "framework";
        public static readonly string _NO_EXACT_ = "no-exact";
        public static readonly string _NO_PARTIALS_ = "no-partials";
        public static readonly string _NO_MISSINGS_ = "no-missings";
        public static readonly string _CONSIDER_DISABLED_FEEDS_ = "consider-disabled-feeds";

    }
}
