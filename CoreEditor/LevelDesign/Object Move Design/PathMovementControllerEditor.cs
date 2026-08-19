#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CoreEngine.LevelDesign.Editor
{
    [CustomEditor(typeof(PathMovementController))]
    public class PathMovementControllerEditor : UnityEditor.Editor
    {
        private PathMovementController _controller;
        private Material _blueGhostMat;
        private Material _redGhostMat;
        private MeshFilter[] _cachedMeshes;

        private int _selectedIndex = -1;
        private int _prevPointCount = -1;

        private Quaternion _globalHandleRot = Quaternion.identity;

        private void OnEnable()
        {
            _controller = (PathMovementController)target;
            _blueGhostMat = CreateGhostMaterial(new Color(0.2f, 0.6f, 1f, 0.35f));
            _redGhostMat = CreateGhostMaterial(new Color(1f, 0.2f, 0.2f, 0.4f));
            _cachedMeshes = _controller.GetComponentsInChildren<MeshFilter>();

            _prevPointCount = _controller.points != null ? _controller.points.Count : 0;
        }

        private void OnDisable()
        {
            if (_blueGhostMat != null) DestroyImmediate(_blueGhostMat);
            if (_redGhostMat != null) DestroyImmediate(_redGhostMat);
        }

        private Material CreateGhostMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = color;
            mat.hideFlags = HideFlags.DontSave;
            return mat;
        }

        private void OnSceneGUI()
        {
            if (_controller == null || _controller.rootPoint == null) return;

            if (_controller.points != null)
            {
                if (_prevPointCount != -1 && _controller.points.Count > _prevPointCount)
                {
                    _selectedIndex = _controller.points.Count - 1;
                    Repaint();
                }
                if (_selectedIndex >= _controller.points.Count)
                    _selectedIndex = _controller.points.Count - 1;
                _prevPointCount = _controller.points.Count;
            }

            if (!Application.isPlaying && _controller.transform.hasChanged)
            {
                _controller.SyncRootPointWithTransform();
                _controller.transform.hasChanged = false;
            }

            Vector3 rootPos = DrawRootPoint(_cachedMeshes);
            Vector3 lastPos = DrawPathPoints(rootPos, _cachedMeshes);
            DrawLoopConnections(lastPos, rootPos);
        }

        private Vector3 DrawRootPoint(MeshFilter[] childMeshes)
        {
            Vector3 rootPos = _controller.GetWorldPosition(_controller.rootPoint);
            Quaternion rootRot = _controller.GetWorldRotation(_controller.rootPoint);
            RenderGhostMesh(rootPos, rootRot, _redGhostMat, childMeshes);
            return rootPos;
        }

        private Vector3 DrawPathPoints(Vector3 rootPos, MeshFilter[] childMeshes)
        {
            Vector3 prevPos = rootPos;
            bool isPingPong = _controller.loopMode == LoopMode.PingPong;

            if (_controller.points == null) return prevPos;

            for (int i = 0; i < _controller.points.Count; i++)
            {
                TargetPoint pt = _controller.points[i];
                Vector3 worldPos = _controller.GetWorldPosition(pt);
                Quaternion worldRot = _controller.GetWorldRotation(pt);

                Handles.color = Color.yellow;
                Handles.DrawDottedLine(prevPos, worldPos, 4f);
                DrawDirectionCone(prevPos, worldPos, isPingPong);

                RenderGhostMesh(worldPos, worldRot, _blueGhostMat, childMeshes);

                float btnSize = HandleUtility.GetHandleSize(worldPos) * 0.15f;
                Handles.color = (_selectedIndex == i) ? Color.yellow : Color.cyan;
                if (Handles.Button(worldPos, worldRot, btnSize, btnSize, Handles.SphereHandleCap))
                {
                    _selectedIndex = i;
                    Repaint();
                }

                Handles.Label(worldPos + Vector3.up * 0.5f, $"Point [{i}]");

                if (_selectedIndex == i)
                {
                    DrawSelectionHandles(pt, worldPos, worldRot, i);
                }

                prevPos = worldPos;
            }
            return prevPos;
        }

        private void DrawSelectionHandles(TargetPoint pt, Vector3 worldPos, Quaternion worldRot, int index)
        {
            if (GUIUtility.hotControl == 0)
            {
                _globalHandleRot = Quaternion.identity;
            }

            EditorGUI.BeginChangeCheck();

            Quaternion handleRot = _controller.useLocalGizmo ? worldRot : _globalHandleRot;

            Vector3 newPos = Handles.PositionHandle(worldPos, handleRot);
            Quaternion newHandleRot = Handles.RotationHandle(handleRot, worldPos);

            if (EditorGUI.EndChangeCheck())
            {
                Quaternion finalRot = newHandleRot;
                if (!_controller.useLocalGizmo)
                {
                    Quaternion deltaRot = newHandleRot * Quaternion.Inverse(_globalHandleRot);
                    finalRot = deltaRot * worldRot;
                    _globalHandleRot = newHandleRot;
                }

                _controller.SetWorldPositionAndRotation(pt, newPos, finalRot);
            }
        }

        private void DrawLoopConnections(Vector3 lastPos, Vector3 rootPos)
        {
            if (_controller.points == null || _controller.points.Count == 0) return;

            if (_controller.loopMode == LoopMode.Restart)
            {
                Handles.color = Color.red;
                Handles.DrawDottedLine(lastPos, rootPos, 4f);
                DrawDirectionCone(lastPos, rootPos, false);

                Vector3 firstPos = _controller.GetWorldPosition(_controller.points[0]);
                Handles.color = new Color(1f, 0.5f, 0f);
                Handles.DrawDottedLine(rootPos, firstPos, 4f);
                DrawDirectionCone(rootPos, firstPos, false);
            }
            else if (_controller.loopMode == LoopMode.Circular && _controller.points.Count > 1)
            {
                Handles.color = Color.green;
                Vector3 firstPos = _controller.GetWorldPosition(_controller.points[0]);
                Handles.DrawDottedLine(lastPos, firstPos, 4f);
                DrawDirectionCone(lastPos, firstPos, false);
            }
        }

        private void RenderGhostMesh(Vector3 targetWorldPos, Quaternion targetWorldRot, Material ghostMat, MeshFilter[] childMeshes)
        {
            if (Event.current.type != EventType.Repaint || childMeshes == null) return;

            ghostMat.SetPass(0);
            Transform rootTransform = _controller.transform;
            Matrix4x4 ghostRootMatrix = Matrix4x4.TRS(targetWorldPos, targetWorldRot, rootTransform.lossyScale);
            Matrix4x4 rootWorldToLocal = rootTransform.worldToLocalMatrix;

            foreach (var mf in childMeshes)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                Matrix4x4 childRelativeMatrix = rootWorldToLocal * mf.transform.localToWorldMatrix;
                Matrix4x4 ghostChildMatrix = ghostRootMatrix * childRelativeMatrix;

                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    Graphics.DrawMeshNow(mf.sharedMesh, ghostChildMatrix, s);
                }
            }
        }

        private void DrawDirectionCone(Vector3 start, Vector3 end, bool isPingPong)
        {
            Vector3 dir = (end - start).normalized;
            if (dir.sqrMagnitude < 0.001f) return;

            Vector3 midPoint = Vector3.Lerp(start, end, 0.5f);
            float coneSize = HandleUtility.GetHandleSize(midPoint) * 0.2f;

            Handles.ConeHandleCap(0, midPoint, Quaternion.LookRotation(dir), coneSize, EventType.Repaint);

            if (isPingPong)
            {
                Handles.ConeHandleCap(0, midPoint, Quaternion.LookRotation(-dir), coneSize, EventType.Repaint);
            }
        }
    }
}
#endif