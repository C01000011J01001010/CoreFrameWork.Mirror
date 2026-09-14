using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEngine.Pool
{
    public interface ISpawnable
    {
        public void OnSpawn();
        public void OnDespawn();
    }
}
