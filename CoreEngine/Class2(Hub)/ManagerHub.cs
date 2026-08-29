
using System.Collections;
using CoreEngine.Helpers;

namespace CoreEngine.Hub
{
    internal sealed class ManagerHub : BaseModuleHub<IManager>
    {
        protected override bool moduleEnabled => true;

        public override IEnumerator Initialize()
        {
            LogHelper.LogFunctionCallStart(this);
            return base.Initialize();
        }

        public override IEnumerator LateInitialize()
        {
            LogHelper.LogFunctionCallStart(this);
            return base.LateInitialize();
        }
    }
}
