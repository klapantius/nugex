using System;
using System.Collections.Generic;
using System.Linq;

namespace nugex.cmdline
{
    public class Command
    {
        public string Name { get; }
        public string Description { get; }
        public Action Action { get; }

        public List<IOption> Options { get; }
        
        public Command(string name, string description, Action action, params IOption[] options)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }
            Name = name;
            Description = description;
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Options = options != null ? options.ToList() : new List<IOption>();
        }

        public void Execute() => Action();

    }
}
