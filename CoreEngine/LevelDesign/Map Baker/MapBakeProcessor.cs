#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace CoreEngine.LevelDesign
{
    /// <summary>
    /// 맵 데이터를 캡처하고 렌더링하는 핵심 프로세서 (렌더링 파이프라인 제어)
    /// </summary>
    public static class MapBakeProcessor
    {
        private static readonly int BakeColorProperty = Shader.PropertyToID("_BakeColor");
        private static readonly int StepsProperty = Shader.PropertyToID("_Steps");

        #region [1] Public API (외부 노출 메서드)

        public static Texture2D CaptureTile(MapBakeSettingsSO settings, int col, int row)
        {
            if (settings == null) return null;
            Vector3 tileCenter = GetTileCenterPosition(settings, col, row);
            return ExecuteBakePipeline(settings, tileCenter, settings.tileSize, (int)settings.resolution);
        }

        public static Texture2D CaptureFullMapLOD(MapBakeSettingsSO settings, int overrideResolution = 2048)
        {
            if (settings == null) return null;
            Vector3 fullCenter = GetFullMapCenterPosition(settings);
            return ExecuteBakePipeline(settings, fullCenter, settings.totalMapSize, overrideResolution);
        }

        #endregion

        #region [2] 메인 렌더링 파이프라인 (Core Pipeline)

        private static Texture2D ExecuteBakePipeline(MapBakeSettingsSO settings, Vector3 camPos, Vector2 size, int targetResolution)
        {
            Shader depthShader = Shader.Find("Map/DepthStepShader");
            Shader outlineCompositeShader = Shader.Find("Map/OutlineComposite");

            if (depthShader == null)
            {
                Debug.LogError("[MapBakeProcessor] 'Map/DepthStepShader' 셰이더를 찾을 수 없습니다.");
                return null;
            }

            Material bakeMaterial = new Material(depthShader);
            Material depthBakeMat = new Material(depthShader);
            Material outlineMaterial = outlineCompositeShader != null ? new Material(outlineCompositeShader) : null;

            int resX = targetResolution;
            int resY = Mathf.RoundToInt(targetResolution * (size.y / size.x));
            Camera cam = SetupBakeCamera(settings, camPos, size);

            Renderer[] sceneRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            Terrain[] sceneTerrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);

            MaterialBackup backup = new MaterialBackup();
            backup.Backup(sceneRenderers, sceneTerrains);

            Dictionary<string, Color> colorDict = ConvertToDictionary(settings.layerColors);

            RenderTexture rtBase = null;
            RenderTexture rtFinal = null;
            Texture2D finalTexture = null;

            try
            {
                // [Pass 1] 원본 Base Map 렌더링 및 틴트 적용
                rtBase = BakeBaseMap(cam, settings, sceneRenderers, sceneTerrains, bakeMaterial, colorDict, resX, resY);

                // [Pass 2] 하이브리드 외곽선 렌더링 및 합성
                rtFinal = ApplyHybridOutlines(cam, settings, rtBase, sceneRenderers, sceneTerrains, depthBakeMat, outlineMaterial, resX, resY);

                // 최종 텍스처 추출
                finalTexture = ExtractTexture(rtFinal, resX, resY);
            }
            finally
            {
                backup.Restore();
                CleanupResources(cam.gameObject, bakeMaterial, depthBakeMat, outlineMaterial, rtBase, rtFinal);
            }

            return finalTexture;
        }

        #endregion

        #region [3] 렌더링 패스 (Pass 1: Base Map)

        private static RenderTexture BakeBaseMap(Camera cam, MapBakeSettingsSO settings, Renderer[] renderers, Terrain[] terrains, Material bakeMat, Dictionary<string, Color> colorDict, int resX, int resY)
        {
            cam.cullingMask = settings.renderMask;

            // 틴트 색깔 배경색상에 적용
            cam.backgroundColor = (settings.depthSteps != MapDepthSteps.None)
                ? settings.backgroundColor * settings.mapTintColor
                : settings.backgroundColor;

            RenderTexture rtBase = RenderTexture.GetTemporary(resX, resY, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rtBase;

            if (settings.depthSteps != MapDepthSteps.None)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();

                foreach (Renderer r in renderers)
                {
                    if (!IsValidTarget(r.gameObject, r.enabled, settings.renderMask)) continue;

                    Material[] tempMats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < tempMats.Length; i++) tempMats[i] = bakeMat;
                    r.sharedMaterials = tempMats;

                    ConfigureBakePropertyBlock(mpb, settings, r.gameObject.layer, colorDict);
                    r.SetPropertyBlock(mpb);
                }

                foreach (Terrain t in terrains)
                {
                    if (!IsValidTarget(t.gameObject, t.enabled, settings.renderMask)) continue;

                    t.materialTemplate = bakeMat;
                    ConfigureBakePropertyBlock(mpb, settings, t.gameObject.layer, colorDict);
                    t.SetSplatMaterialPropertyBlock(mpb);
                }
            }

            cam.Render();

            if (settings.depthSteps == MapDepthSteps.None && settings.mapTintColor != Color.white)
            {
                rtBase = ApplyPostProcessTint(rtBase, settings.mapTintColor, resX, resY);
            }

            return rtBase;
        }

        #endregion

        #region [4] 렌더링 패스 (Pass 2: Outlines)

        private static RenderTexture ApplyHybridOutlines(Camera cam, MapBakeSettingsSO settings, RenderTexture rtBase, Renderer[] renderers, Terrain[] terrains, Material depthBakeMat, Material outlineMaterial, int resX, int resY)
        {
            if (settings.outlineSettings == null || settings.outlineSettings.Count == 0 || outlineMaterial == null)
            {
                return rtBase;
            }

            RenderTexture rtCurrent = rtBase;
            RenderTexture rtGlobalDepth = RenderTexture.GetTemporary(resX, resY, 24, RenderTextureFormat.RFloat);
            RenderTexture rtTargetDepth = RenderTexture.GetTemporary(resX, resY, 24, RenderTextureFormat.RFloat);

            // 깊이 셰이더 세팅 (흰색 = 깊이 측정 대상 / 검은색 = 투명 취급 구멍 뚫기)
            depthBakeMat.SetColor(BakeColorProperty, Color.white);
            depthBakeMat.SetFloat(StepsProperty, 10000f);

            Material holeCutMat = new Material(depthBakeMat.shader);
            holeCutMat.SetColor(BakeColorProperty, Color.black);
            holeCutMat.SetFloat(StepsProperty, 10000f);

            foreach (var outline in settings.outlineSettings)
            {
                if (!outline.isUse || string.IsNullOrEmpty(outline.layerName)) continue;

                int targetLayer = LayerMask.NameToLayer(outline.layerName);
                if (targetLayer == -1 || (settings.renderMask.value & (1 << targetLayer)) == 0) continue;

                // 🌟 1. 전체 씬 깊이(Global Depth) 렌더링
                cam.cullingMask = settings.renderMask & ~outline.forceEdgeMask;
                cam.backgroundColor = Color.black;
                ClearAndSetRenderTarget(cam, rtGlobalDepth);

                // 원본 로직 완벽 복구: cullingMask 조건 없이 전체를 덮어씌움 (-1 전달)
                OverrideSceneMaterials(renderers, terrains, -1, -1, depthBakeMat, depthBakeMat);
                cam.Render();

                // 🌟 2. 타겟 마스크 렌더링
                cam.cullingMask = (1 << targetLayer) | outline.forceEdgeMask;
                ClearAndSetRenderTarget(cam, rtTargetDepth);
                OverrideSceneMaterials(renderers, terrains, cam.cullingMask, targetLayer, depthBakeMat, holeCutMat);
                cam.Render();

                // 🌟 3. 외곽선 합성 (Blit)
                RenderTexture rtNext = RenderTexture.GetTemporary(resX, resY, 0, RenderTextureFormat.ARGB32);
                outlineMaterial.SetTexture("_MaskTex", rtTargetDepth);
                outlineMaterial.SetTexture("_GlobalDepthTex", rtGlobalDepth);
                outlineMaterial.SetColor("_OutlineColor", outline.outlineColor);
                outlineMaterial.SetFloat("_OutlineThickness", outline.outlineThickness);
                outlineMaterial.SetFloat("_DepthThreshold", outline.depthThreshold);
                outlineMaterial.SetVector("_PixelSize", new Vector4(1f / resX, 1f / resY, resX, resY));

                Graphics.Blit(rtCurrent, rtNext, outlineMaterial);

                if (rtCurrent != rtBase) RenderTexture.ReleaseTemporary(rtCurrent);
                rtCurrent = rtNext;
            }

            RenderTexture.ReleaseTemporary(rtGlobalDepth);
            RenderTexture.ReleaseTemporary(rtTargetDepth);
            SafeDestroy(holeCutMat);

            return rtCurrent;
        }

        #endregion

        #region [5] 자원 백업 및 유틸리티 헬퍼 (Scene State Management)

        private class MaterialBackup
        {
            private Dictionary<Renderer, Material[]> _origRenderers = new Dictionary<Renderer, Material[]>();
            private Dictionary<Terrain, Material> _origTerrains = new Dictionary<Terrain, Material>();

            public void Backup(Renderer[] renderers, Terrain[] terrains)
            {
                foreach (var r in renderers)
                    if (r != null && r.sharedMaterials != null && r.sharedMaterials.Length > 0)
                        _origRenderers[r] = r.sharedMaterials;

                foreach (var t in terrains)
                    if (t != null)
                        _origTerrains[t] = t.materialTemplate;
            }

            public void Restore()
            {
                foreach (var pair in _origRenderers)
                    if (pair.Key != null) { pair.Key.sharedMaterials = pair.Value; pair.Key.SetPropertyBlock(null); }

                foreach (var pair in _origTerrains)
                    if (pair.Key != null) { pair.Key.materialTemplate = pair.Value; pair.Key.SetSplatMaterialPropertyBlock(new MaterialPropertyBlock()); }
            }
        }

        /// <summary>
        /// 렌더러와 터레인에 지정된 재질(Material)을 강제로 덮어씌웁니다. 
        /// (MaterialPropertyBlock 초기화를 통해 이전 패스의 양자화 오염 방지)
        /// </summary>
        private static void OverrideSceneMaterials(Renderer[] renderers, Terrain[] terrains, int cullingMask, int targetLayer, Material targetMat, Material nonTargetMat)
        {
            foreach (var r in renderers)
            {
                if (r != null && (cullingMask == -1 || ((1 << r.gameObject.layer) & cullingMask) != 0))
                {
                    bool isTarget = (targetLayer == -1) || (r.gameObject.layer == targetLayer);

                    Material[] tempMats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < tempMats.Length; i++) tempMats[i] = isTarget ? targetMat : nonTargetMat;
                    r.sharedMaterials = tempMats;

                    // 🚨 [핵심 복구] 1단계 렌더링 시 씌워진 MPB(색상/양자화값)를 제거해야 깊이 연산이 정상 작동합니다.
                    r.SetPropertyBlock(null);
                }
            }
            foreach (var t in terrains)
            {
                if (t != null && (cullingMask == -1 || ((1 << t.gameObject.layer) & cullingMask) != 0))
                {
                    bool isTarget = (targetLayer == -1) || (t.gameObject.layer == targetLayer);
                    t.materialTemplate = isTarget ? targetMat : nonTargetMat;

                    // 🚨 [핵심 복구] 터레인 역시 MPB를 비워주어 오염을 방지합니다.
                    t.SetSplatMaterialPropertyBlock(new MaterialPropertyBlock());
                }
            }
        }

        private static bool IsValidTarget(GameObject obj, bool isEnabled, LayerMask renderMask)
        {
            return obj != null && isEnabled && ((1 << obj.layer) & renderMask.value) != 0;
        }

        private static void ConfigureBakePropertyBlock(MaterialPropertyBlock mpb, MapBakeSettingsSO settings, int layer, Dictionary<string, Color> colorDict)
        {
            Color targetColor = settings.mapTintColor;

            if (settings.useLayerColor && colorDict.TryGetValue(LayerMask.LayerToName(layer), out Color mappedColor))
            {
                targetColor = mappedColor * settings.mapTintColor;
            }

            bool ignoreQuantization = (settings.ignoreDepthQuantizationMask.value & (1 << layer)) != 0;
            float currentSteps = ignoreQuantization ? 1f : (float)settings.depthSteps;

            mpb.SetColor(BakeColorProperty, targetColor);
            mpb.SetFloat(StepsProperty, currentSteps);
            mpb.SetFloat("_FinalDepthBrightness", settings.finalDepthBrightness);
        }

        private static RenderTexture ApplyPostProcessTint(RenderTexture source, Color tintColor, int width, int height)
        {
            RenderTexture tintedRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Material tintMat = new Material(Shader.Find("UI/Default")) { color = tintColor };

            Graphics.Blit(source, tintedRT, tintMat);
            RenderTexture.ReleaseTemporary(source);
            SafeDestroy(tintMat);

            return tintedRT;
        }

        private static void ClearAndSetRenderTarget(Camera cam, RenderTexture rt)
        {
            cam.targetTexture = rt;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }

        #endregion

        #region [6] 수학 및 일반 유틸리티 (Math & System)

        private static Camera SetupBakeCamera(MapBakeSettingsSO settings, Vector3 camPos, Vector2 size)
        {
            GameObject camObj = new GameObject("Temp_MapBakeCamera");
            camObj.transform.position = camPos;
            camObj.transform.rotation = settings.projectionPlane == MapProjectionPlane.XY
                ? Quaternion.Euler(0f, 0f, 0f)
                : Quaternion.Euler(90f, 0f, 0f);

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size.y * 0.5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = settings.maxDepth;
            cam.clearFlags = CameraClearFlags.SolidColor;
            return cam;
        }

        private static Vector3 GetTileCenterPosition(MapBakeSettingsSO settings, int col, int row)
        {
            float startX = settings.centerPosition.x - settings.totalMapSize.x / 2f + settings.tileSize.x / 2f;

            if (settings.projectionPlane == MapProjectionPlane.XY)
            {
                float startY = settings.centerPosition.y - settings.totalMapSize.y / 2f + settings.tileSize.y / 2f;
                float camZ = settings.centerPosition.z - settings.captureOffset;
                return new Vector3(startX + col * settings.tileSize.x, startY + row * settings.tileSize.y, camZ);
            }
            else
            {
                float startZ = settings.centerPosition.z - settings.totalMapSize.y / 2f + settings.tileSize.y / 2f;
                float camY = settings.centerPosition.y + settings.captureOffset;
                return new Vector3(startX + col * settings.tileSize.x, camY, startZ + row * settings.tileSize.y);
            }
        }

        private static Vector3 GetFullMapCenterPosition(MapBakeSettingsSO settings)
        {
            if (settings.projectionPlane == MapProjectionPlane.XY)
                return new Vector3(settings.centerPosition.x, settings.centerPosition.y, settings.centerPosition.z - settings.captureOffset);

            return new Vector3(settings.centerPosition.x, settings.centerPosition.y + settings.captureOffset, settings.centerPosition.z);
        }

        private static Dictionary<string, Color> ConvertToDictionary(List<LayerColorPair> list)
        {
            var dict = new Dictionary<string, Color>();
            if (list == null) return dict;
            foreach (var pair in list)
            {
                if (!string.IsNullOrEmpty(pair.layerName) && !dict.ContainsKey(pair.layerName))
                {
                    dict.Add(pair.layerName, pair.color);
                }
            }
            return dict;
        }

        private static Texture2D ExtractTexture(RenderTexture rtFinal, int resX, int resY)
        {
            RenderTexture.active = rtFinal;
            Texture2D texture = new Texture2D(resX, resY, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, resX, resY), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            return texture;
        }

        private static void CleanupResources(GameObject camObj, Material bakeMat, Material depthMat, Material outlineMat, RenderTexture rtBase, RenderTexture rtFinal)
        {
            if (rtBase != null) RenderTexture.ReleaseTemporary(rtBase);
            if (rtFinal != null && rtFinal != rtBase) RenderTexture.ReleaseTemporary(rtFinal);

            SafeDestroy(camObj);
            SafeDestroy(bakeMat);
            SafeDestroy(depthMat);
            if (outlineMat != null) SafeDestroy(outlineMat);
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        #endregion
    }
}
#endif