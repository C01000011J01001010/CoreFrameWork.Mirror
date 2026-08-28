using UnityEngine;

namespace CoreEngine.Data
{
    public class BaseObjectContainer<TObject>
    {
        [SerializeField]
        protected TObject connectData;

        public BaseObjectContainer()
        {
            connectData = default;
        }

        public BaseObjectContainer(TObject InitialObject)
        {
            Set(InitialObject);
        }

        public virtual TObject Get() => connectData;
        public virtual TObject Set(TObject newObject)
        {
            return connectData = newObject;
        }
    }
}