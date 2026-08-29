using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace CoreEngine.Helpers
{
    public static class SceneManagementHelper
    {
        public static void SetActiveScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded && SceneManager.GetActiveScene() != scene)
            {
                SceneManager.SetActiveScene(scene);
                LogHelper.Log($"[SceneFlowDirector] 현재 콘텐츠 씬 등록 완료 : {scene.name}", LogColor.Green);
            }
        }
    }
}
