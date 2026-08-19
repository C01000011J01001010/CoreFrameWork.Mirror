using System.Collections.Generic;
using UnityEngine.UIElements;

namespace CoreEngine.Interface
{
    public interface IBindable
    {
        void Bind();
        void Unbind();
    }

    /// <summary>
    /// 등록된 모든 Receiver와 Publisher의 Bind/Unbind를 한 번에 처리해주는 도구
    /// </summary>
    public class InterfaceBinderContainer
    {
        private readonly List<IBindable> _bindables = new List<IBindable>();

        // 부품 등록
        public void Add(IBindable bindable)
        {
            _bindables.Add(bindable);
        }

        // 일괄 켜기
        public void BindAll()
        {
            foreach (var b in _bindables) b.Bind();
        }

        // 일괄 끄기
        public void UnbindAll()
        {
            foreach (var b in _bindables) b.Unbind();
        }
    }
}