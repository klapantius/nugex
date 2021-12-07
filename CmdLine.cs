using System;
using System.Collections.Generic;
using System.Linq;

namespace nugex
{
    public static class CmdLine
    {
        private static List<string> Args;

        public static void Parse(string[] args)
        {
            Args = new List<string>(args);
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

    }
}
