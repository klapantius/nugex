using nugex.cmdline;
namespace nugex
{
    partial class Program
    {
        static void Main(string[] args)
        {
            CmdLine.Parser.InitCommands(new[]
            {
                new Command("search", "search for package(s) on known feeds (from nuget.config)", () => Search(),
                        new Parameter(_SEARCH_TERM_, "search term", mandatory: true),
                        new Parameter(_VSPEC_, "version number (regex)"),
                        new Switch(_ALL_FEEDS_, "...even those without matching packages")
                    ),
            });

            CmdLine.Parser.Parse(args);

            CmdLine.Parser.ExecuteCommand();
        }

        // option names in alphabetical order
        public static readonly string _SEARCH_TERM_ = "name";
        public static readonly string _ALL_FEEDS_ = "show-all-feeds";
        public static readonly string _VSPEC_ = "version";

    }
}
