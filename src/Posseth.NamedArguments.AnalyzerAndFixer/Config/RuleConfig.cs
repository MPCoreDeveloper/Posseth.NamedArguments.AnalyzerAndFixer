using System;
namespace Posseth.NamedArguments.AnalyzerAndFixer
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RuleConfigurationOptionAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string DefaultValue { get; }

        public RuleConfigurationOptionAttribute(string name, string description, string defaultValue)
        {
            Name = name;
            Description = description;
            DefaultValue = defaultValue;
        }
    }
}