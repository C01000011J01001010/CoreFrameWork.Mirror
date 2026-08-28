using System.Collections;
using UnityEngine;

namespace CoreEngine.Actor
{
    public interface IActorHost
    {
        #region MonoBehaviour 내장 프로퍼티
        Transform transform { get; }
        GameObject gameObject { get; }
         string tag { get; }
        #endregion

        #region MonoBehaviour 내장 메서드

        // ==========================================
        bool TryGetComponent<T>(out T component);
        bool TryGetComponent(System.Type type, out Component component);

        // ==========================================
        T GetComponent<T>();
        T[] GetComponentsInChildren<T>(bool includeInactive = false);
        T GetComponentInChildren<T>(bool includeInactive = false);

        // ==========================================
        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine routine);
        void StopCoroutine(IEnumerator routine);

        // ==========================================
        bool isActiveAndEnabled { get; }
        string name { get; }
        bool CompareTag(string tag);
        #endregion

        bool TryGetFeature<T>(out T feature) where T : class, IActorFeature;
    }
}