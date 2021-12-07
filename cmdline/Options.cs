namespace nugex.cmdline
{

    public interface IOption
    {
        string Name { get; }
        string Description { get; }
    }

    public class Parameter : IOption
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsMandatory { get; }

        public Parameter(string name, string description, bool mandatory = false)
        {
            Name = name;
            Description = description;
            IsMandatory = mandatory;
        }
    }

    public class Switch : IOption {
        public string Name { get; }
        public string Description { get; }

        public Switch(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

}