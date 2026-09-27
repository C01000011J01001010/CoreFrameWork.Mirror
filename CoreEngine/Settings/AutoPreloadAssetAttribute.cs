using System;

namespace CoreEngine.Settings
{
    /// <summary>
    /// 이 어트리뷰트가 달린 ScriptableObject 클래스는 
    /// 에디터 컴파일 시 자동으로 PlayerSettings의 Preloaded Assets에 등록
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class AutoPreloadAssetAttribute : Attribute
    {
    }
}