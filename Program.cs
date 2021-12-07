using nugex.cmdline;
namespace nugex
{
    partial class Program
    {
        static void Main(string[] args)
        {
            CmdLine.InitCommands(new[]
            {
                new Command("search", "search for package(s) on known feeds (from nuget.config)", () => Search())
            });

            CmdLine.Parse(args);

            CmdLine.ExecuteCommand();
        }
    }
}
