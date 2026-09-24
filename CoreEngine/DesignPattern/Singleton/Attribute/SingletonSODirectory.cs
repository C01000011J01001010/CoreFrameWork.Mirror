using System;

namespace CoreEngine.DesignPattern.Singleton
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class SingletonSODirectory : Attribute
    {
        public string Derectory { get; }

        public SingletonSODirectory(string derectory)
        {
            Derectory = derectory;
        }
    }
}