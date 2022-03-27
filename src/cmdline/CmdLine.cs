using System;
using System.Collections.Generic;
using System.Linq;

namespace nugex.cmdline
{
    public interface ICmdLine
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>value of parameter or 'default'</returns>
        string GetParam(string name);
        bool GetSwitch(string name);
        void InitCommands(IEnumerable<Command> commands);
        void Parse(params string[] args);
        void ExecuteCommand();
    }

    public class CmdLine : ICmdLine
    {
        private static ICmdLine _parser = null;
        public static ICmdLine Parser {
            get {
                if (_parser == null) _parser = new CmdLine();
                return _parser;
            }
            internal set { _parser = value; }
        }

        private List<string> Args;
        private string CommandRef;
        private readonly Dictionary<string, Command> Commands = new();

        public void Parse(params string[] args)
        {
            if (!args.Any()) { Help(); throw new ErrorMessage("please specify one of the above commands"); }
            CommandRef = args[0].ToLowerInvariant();
            Args = new List<string>(args).Skip(1).ToList();
        }

        int FindOption(string name)
        {
            var arg = Args
                .LastOrDefault(a => a.Trim(new[] { '/', '-' }).ToLowerInvariant().Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (arg == default) return -1;
            return Args.IndexOf(arg);
        }

        public string GetParam(string name)
        {
            var valueIdx = FindOption(name) + 1;
            if (valueIdx == 0 || valueIdx >= Args.Count) return default;
            return Args[valueIdx];
        }

        public bool GetSwitch(string name) => FindOption(name) >= 0;

        private void Help() {
            foreach (var cmd in Commands)
            {
                Console.WriteLine($"{cmd.Key,-29}{cmd.Value.Description}");
                foreach (var opt in cmd.Value.Options)
                {
                    var mndStr = (opt is Parameter) && (opt as Parameter).IsMandatory? " - mandatory":"";
                    Console.WriteLine($"  --{opt.Name,-25}{opt.Description}{mndStr}");
                }
            }
        }

        public void InitCommands(IEnumerable<Command> commands)
        {
            commands.ToList().ForEach(c => Commands[c.Name.ToLowerInvariant()] = c);
            Commands["help"] = new Command("help", "this text", () => Help());
        }

        public void ExecuteCommand()
        {
            if (!Commands.ContainsKey(CommandRef)) { Help(); throw new ErrorMessage($"'{CommandRef}' is not a supported command"); }
            var cmd = Commands[CommandRef];
            cmd.Execute();
        }

    }
}
