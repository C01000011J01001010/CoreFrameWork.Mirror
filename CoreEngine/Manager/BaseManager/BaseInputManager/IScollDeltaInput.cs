
namespace CoreEngine.Manager.Input
{
    internal interface IScollDeltaInput
    {
        /// <summary>
        /// 마우스 y축 휠 입력
        /// <para>조이스틱 조합입력  ex) B + 조이스틱R 위아래</para>
        /// </summary>
        float value { get; }
    }
}
