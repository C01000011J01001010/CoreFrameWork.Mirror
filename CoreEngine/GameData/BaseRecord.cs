using System;

namespace CoreEngine.GameData
{
    public interface IRecord { int Index { get; } }
    [Serializable]
    public class BaseRecord: IRecord
    {
        private int index;

        public int Index => index;
    }
}

