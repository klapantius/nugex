using System;

namespace nugex.cmdline
{
    public class Command
    {
        public string Name { get; }
        public string Description { get; }
        public Action Action { get; }

        public Command(string name, string description, Action action)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }
            Name = name;
            Description = description;
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Execute() => Action();

    }
}
