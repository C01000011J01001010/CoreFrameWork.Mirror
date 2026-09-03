using UnityEngine;

namespace CoreEngine.Manager.Input
{
    public interface IMoveInput
    {
        /// <summary>
        /// wasd 입력
        /// <para>조이스틱L 입력</para>
        /// </summary>
        Vector2 value { get; }
    }
}
