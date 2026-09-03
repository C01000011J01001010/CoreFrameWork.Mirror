using UnityEngine;

namespace CoreEngine.Manager.Input
{
    public interface ILookInput
    {
        /// <summary>
        /// 마우스 이동 입력
        /// <para>조이스틱 R 입력</para>
        /// </summary>
        Vector2 value { get; }
    }
}
