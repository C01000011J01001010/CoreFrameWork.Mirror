using UnityEngine;

namespace CoreEngine.Interface
{
    /// <summary>
    /// Polling으로 입력 데이터를 가져오는 처리
    /// </summary>
    public interface IPlayerInputProvider
    {
        /// <summary>
        /// wasd 입력
        /// <para>조이스틱L 입력</para>
        /// </summary>
        Vector2 Move { get; }

        /// <summary>
        /// 마우스 이동 입력
        /// <para>조이스틱 R 입력</para>
        /// </summary>
        Vector2 Look { get; }

        /// <summary>
        /// 마우스 y축 휠 입력
        /// <para>조이스틱 조합입력  ex) B + 조이스틱R 위아래</para>
        /// </summary>
        float ScrollDelta { get; }
    }
}
