using nugex.cmdline;
namespace nugex
{
    partial class Program
    {
        static void Main(string[] args)
        {
            CmdLine.Parser.InitCommands(new[]
            {
                new Command("search", "search for package(s) on known feeds (from nuget.config)", () => Search())
            });

            CmdLine.Parser.Parse(args);

            CmdLine.Parser.ExecuteCommand();
        }
    }
}
