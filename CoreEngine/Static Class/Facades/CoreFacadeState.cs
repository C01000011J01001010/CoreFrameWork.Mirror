using System;
using System.Collections.Generic;
using System.Text;

namespace CoreEngine.Facades
{
    public static class CoreFacadeState
    {
        public static bool GetProjectInit() => ProjectContext.Inst != null && ProjectContext.Inst.IsInit;
        public static bool GetSceneInit() => SceneContext.Inst != null && SceneContext.Inst.IsInit;
    }
}
