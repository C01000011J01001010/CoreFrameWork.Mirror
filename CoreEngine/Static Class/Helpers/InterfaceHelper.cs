using CoreEngine.Interface;
using System;

namespace CoreEngine.Helpers
{
    public static class InterfaceHelper
    {
        public static T GetInterface<T>(ref T iObj, InterfaceReceiver<T> receiver) where T : class
        {
            // 인터페이스가 이미 있으면 그걸 반환
            if (!SystemHelper.isUnityNull(iObj)) return iObj;
            
            // 없으면 대상이 있는 지 확인후 반환
            if (receiver == null)
            {
                LogHelper.Log($"[receiver of {typeof(T)}] is null", LogColor.Red);
                return null;
            }
            receiver.Bind();
            if(receiver.TryGet(out iObj)) return iObj;

            LogHelper.Log($"[{nameof(InterfaceHelper)}.{nameof(GetInterface)}] is Failed On {typeof(T)}", LogColor.Red);
            return null;
        }
    }

}
