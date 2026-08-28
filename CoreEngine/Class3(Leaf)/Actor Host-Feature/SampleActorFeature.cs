
namespace CoreEngine.Actor
{
    /// <summary>
    /// IActorRoot에 조립될 수 있는 모든 순수 C# 부품의 예시
    /// </summary>
    public class SampleActorFeature : BaseActorFeature
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();
            // 필요에 따라 override 후 로직 추가
        }

        /// <summary>
        /// 개별 스크립트에서 필요에 따라 정의
        /// </summary>
        public void Tick(float deltaTime) { }
    }
}

