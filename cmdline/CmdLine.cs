using System;
using System.Collections.Generic;
using System.Linq;

namespace nugex.cmdline
{
    public static partial class CmdLine
    {

        private static List<string> Args;
        private static string CommandRef;
        private static Dictionary<string, Command> Commands = new Dictionary<string, Command>();

        public static void Parse(string[] args)
        {
            CommandRef = args[0].ToLowerInvariant();
            Args = new List<string>(args).Skip(1).ToList();
        }

        static int FindOption(string name)
        {
            var arg = Args
                .LastOrDefault(a => a.Trim(new[] { '/', '-' }).ToLowerInvariant().Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (arg == default) return -1;
            return Args.IndexOf(arg);
        }

        public static string GetParam(string name)
        {
            var valueIdx = FindOption(name) + 1;
            if (valueIdx == 0 || valueIdx >= Args.Count) return default;
            return Args[valueIdx];
        }

        public static bool GetSwitch(string name) => FindOption(name) >= 0;

        public static void InitCommands(IEnumerable<Command> commands)
        {
            commands.ToList().ForEach(c => Commands[c.Name.ToLowerInvariant()] = c);
        }

        public static void ExecuteCommand() {
            var cmd = Commands[CommandRef];
            cmd.Execute();
        }

    }
}
