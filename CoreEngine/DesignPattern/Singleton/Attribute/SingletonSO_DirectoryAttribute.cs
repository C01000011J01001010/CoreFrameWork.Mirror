using System;

namespace CoreEngine.DesignPattern.Singleton
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class SingletonSO_DirectoryAttribute : Attribute
    {
        public string Derectory { get; }

        public SingletonSO_DirectoryAttribute(string derectory)
        {
            Derectory = derectory;
        }
    }
}