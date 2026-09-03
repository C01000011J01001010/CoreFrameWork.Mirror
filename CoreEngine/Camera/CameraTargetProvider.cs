using UnityEngine;

namespace CoreEngine.CameraSystem
{
    public class CameraTargetProvider : MonoBehaviour
    {
        [Tooltip("렌더링이 없는 GameObject를 할당할것")]
        [SerializeField] Transform _targetTransform;

        public Transform Target => _targetTransform;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(_targetTransform == null)
            {
                _targetTransform = gameObject.transform;
            }
        }
#endif
    }
}
