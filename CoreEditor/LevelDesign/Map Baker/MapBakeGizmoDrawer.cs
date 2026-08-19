#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace CoreEngine.LevelDesign
{
    public static class MapBakeGizmoDrawer
    {
        private static void CalculateDrawData(MapBakeSettingsSO settings, out Vector3 boundsCenter, out Vector3 boundsSize, out Vector3 tileCenter, out Vector3 tileSize, out Vector3 camPos, out Vector3 rayDir)
        {
            bool isXY = settings.projectionPlane == MapProjectionPlane.XY;

            boundsCenter = isXY
                ? settings.centerPosition + Vector3.forward * (settings.maxDepth * 0.5f - settings.captureOffset)
                : settings.centerPosition + Vector3.down * (settings.maxDepth * 0.5f - settings.captureOffset);
            boundsSize = isXY
                ? new Vector3(settings.totalMapSize.x, settings.totalMapSize.y, settings.maxDepth)
                : new Vector3(settings.totalMapSize.x, settings.maxDepth, settings.totalMapSize.y);

            tileCenter = isXY
                ? settings.centerPosition + Vector3.forward * (settings.maxDepth * 0.5f - settings.captureOffset)
                : settings.centerPosition + Vector3.down * (settings.maxDepth * 0.5f - settings.captureOffset);
            tileSize = isXY
                ? new Vector3(settings.tileSize.x, settings.tileSize.y, settings.maxDepth)
                : new Vector3(settings.tileSize.x, settings.maxDepth, settings.tileSize.y);

            camPos = isXY
                ? new Vector3(settings.centerPosition.x, settings.centerPosition.y, settings.centerPosition.z - settings.captureOffset)
                : new Vector3(settings.centerPosition.x, settings.centerPosition.y + settings.captureOffset, settings.centerPosition.z);
            rayDir = isXY ? Vector3.forward : Vector3.down;
        }

        public static void DrawWithHandles(MapBakeSettingsSO settings)
        {
            // 데이터 계산
            CalculateDrawData(settings, out var bc, out var bs, out var tc, out var ts, out var cp, out var rd);

            // 타일 경계선 렌더링
            DrawTileBounds(tc, ts);

            // 전체 맵 바운즈 렌더링
            DrawMapBounds(bc, bs);

            // 카메라 촬영 방향 렌더링 (토글 옵션 적용)
            if (settings.showCameraGizmo)
            {
                DrawCameraDirection(cp, rd, settings.maxDepth);
            }

            // 맵 크기 조절 인터랙티브 핸들 (토글로 켜고 끔)
            if (settings.showInteractiveGizmo)
            {
                DrawInteractiveHandles(settings, bc, bs);
            }
        }

        private static void DrawTileBounds(Vector3 center, Vector3 size)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireCube(center, size);
        }

        private static void DrawMapBounds(Vector3 center, Vector3 size)
        {
            Handles.color = Color.red;
            Handles.DrawWireCube(center, size);
        }

        private static void DrawCameraDirection(Vector3 camPos, Vector3 rayDir, float maxDepth)
        {
            Handles.color = new Color(0f, 1f, 1f, 0.9f); // 가시성 높은 형광 Cyan

            Vector3 endPos = camPos + rayDir * maxDepth;
            Vector3 midPos = Vector3.Lerp(camPos, endPos, 0.5f); // 맵 중앙 깊이 지점

            if (Event.current.type == EventType.Repaint)
            {
                float handleSize = HandleUtility.GetHandleSize(camPos);

                // ① 카메라 렌즈(시작점): 방향을 바라보는 사각형 프레임
                Handles.RectangleHandleCap(0, camPos, Quaternion.LookRotation(rayDir), handleSize * 0.2f, EventType.Repaint);

                // ② 굵고 눈에 띄는 레이저 점선 (시작점 -> 도착점)
                Handles.DrawDottedLine(camPos, endPos, 5f);

                // ③ 직관적인 방향 지시 원뿔 (중간 지점에 큼직하게 배치)
                Handles.ConeHandleCap(0, midPos, Quaternion.LookRotation(rayDir), handleSize * 0.5f, EventType.Repaint);
            }
        }

        private static void DrawInteractiveHandles(MapBakeSettingsSO settings, Vector3 boundsCenter, Vector3 boundsSize)
        {
            bool isXY = settings.projectionPlane == MapProjectionPlane.XY;
            Vector3 extents = boundsSize * 0.5f;

            // X축 (좌/우 늘리기)
            DrawFaceHandle(settings, boundsCenter + Vector3.right * extents.x, Vector3.right, (delta) => {
                float newSize = Mathf.Max(0.1f, settings.totalMapSize.x + delta);
                float actual = newSize - settings.totalMapSize.x;
                settings.totalMapSize = new Vector2(newSize, settings.totalMapSize.y);
                settings.centerPosition += Vector3.right * (actual * 0.5f);
            });
            DrawFaceHandle(settings, boundsCenter + Vector3.left * extents.x, Vector3.left, (delta) => {
                float newSize = Mathf.Max(0.1f, settings.totalMapSize.x + delta);
                float actual = newSize - settings.totalMapSize.x;
                settings.totalMapSize = new Vector2(newSize, settings.totalMapSize.y);
                settings.centerPosition += Vector3.left * (actual * 0.5f);
            });

            if (isXY)
            {
                // Y축 (위/아래 늘리기)
                DrawFaceHandle(settings, boundsCenter + Vector3.up * extents.y, Vector3.up, (delta) => {
                    float newSize = Mathf.Max(0.1f, settings.totalMapSize.y + delta);
                    float actual = newSize - settings.totalMapSize.y;
                    settings.totalMapSize = new Vector2(settings.totalMapSize.x, newSize);
                    settings.centerPosition += Vector3.up * (actual * 0.5f);
                });
                DrawFaceHandle(settings, boundsCenter + Vector3.down * extents.y, Vector3.down, (delta) => {
                    float newSize = Mathf.Max(0.1f, settings.totalMapSize.y + delta);
                    float actual = newSize - settings.totalMapSize.y;
                    settings.totalMapSize = new Vector2(settings.totalMapSize.x, newSize);
                    settings.centerPosition += Vector3.down * (actual * 0.5f);
                });

                // Z축 (깊이 및 카메라 위치 보정)
                DrawFaceHandle(settings, boundsCenter + Vector3.forward * extents.z, Vector3.forward, (delta) => {
                    settings.maxDepth = Mathf.Max(0.1f, settings.maxDepth + delta);
                });
                DrawFaceHandle(settings, boundsCenter + Vector3.back * extents.z, Vector3.back, (delta) => {
                    float newDepth = Mathf.Max(0.1f, settings.maxDepth + delta);
                    float actual = newDepth - settings.maxDepth;
                    settings.maxDepth = newDepth;
                    settings.captureOffset += actual;
                });
            }
            else
            {
                // Z축 (맵의 앞/뒤 늘리기)
                DrawFaceHandle(settings, boundsCenter + Vector3.forward * extents.z, Vector3.forward, (delta) => {
                    float newSize = Mathf.Max(0.1f, settings.totalMapSize.y + delta);
                    float actual = newSize - settings.totalMapSize.y;
                    settings.totalMapSize = new Vector2(settings.totalMapSize.x, newSize);
                    settings.centerPosition += Vector3.forward * (actual * 0.5f);
                });
                DrawFaceHandle(settings, boundsCenter + Vector3.back * extents.z, Vector3.back, (delta) => {
                    float newSize = Mathf.Max(0.1f, settings.totalMapSize.y + delta);
                    float actual = newSize - settings.totalMapSize.y;
                    settings.totalMapSize = new Vector2(settings.totalMapSize.x, newSize);
                    settings.centerPosition += Vector3.back * (actual * 0.5f);
                });

                // Y축 (깊이 및 카메라 위치 보정)
                DrawFaceHandle(settings, boundsCenter + Vector3.down * extents.y, Vector3.down, (delta) => {
                    settings.maxDepth = Mathf.Max(0.1f, settings.maxDepth + delta);
                });
                DrawFaceHandle(settings, boundsCenter + Vector3.up * extents.y, Vector3.up, (delta) => {
                    float newDepth = Mathf.Max(0.1f, settings.maxDepth + delta);
                    float actual = newDepth - settings.maxDepth;
                    settings.maxDepth = newDepth;
                    settings.captureOffset += actual;
                });
            }
        }

        private static void DrawFaceHandle(MapBakeSettingsSO settings, Vector3 faceCenter, Vector3 normal, Action<float> onDrag)
        {
            EditorGUI.BeginChangeCheck();

            // 🌟 큐브 대신 화살표를 렌더링하기 위해 핸들 크기를 0.1f에서 1.0f(표준 사이즈)로 키웠습니다.
            float handleSize = HandleUtility.GetHandleSize(faceCenter) * 1.0f;
            Handles.color = Color.white; //new Color(1f, 0.5f, 0f, 0.9f); // 시인성 좋은 주황색
            
            // 🌟 CubeHandleCap 대신 ArrowHandleCap 사용
            Vector3 newPos = Handles.Slider(faceCenter, normal, handleSize, Handles.ArrowHandleCap, 0f);

            if (EditorGUI.EndChangeCheck())
            {
                float delta = Vector3.Dot(newPos - faceCenter, normal);

                Undo.RecordObject(settings, "Resize Map Bounds");
                onDrag?.Invoke(delta);
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
#endif