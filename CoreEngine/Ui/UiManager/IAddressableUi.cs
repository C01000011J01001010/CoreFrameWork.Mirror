
using UnityEngine;

namespace CoreEngine.UI
{
    /// <summary>
    /// AddressableUi가 메모리에 남아있는 시간을 정하는 정책
    /// </summary>
    public interface IAddressableUi: IUi
    {
        /// <summary>
        /// <para>-1이면 로드 후 계속유지, </para>
        /// <para>Hide시 0이면 즉시 Release, n이면 n초동안 메모리 유지 후 Release</para>
        /// </summary>
        float ReleaseDelay { get; }

        public GameObject gameObject { get; }
    }
}
