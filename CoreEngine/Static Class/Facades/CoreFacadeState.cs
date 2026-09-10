using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEngine.Facades
{
    public static class CoreFacadeState
    {
        public static bool SceneInit => SceneContext.Inst != null && SceneContext.Inst.IsInit;
    }
}
