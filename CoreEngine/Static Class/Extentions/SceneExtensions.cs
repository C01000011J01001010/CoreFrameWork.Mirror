using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreEngine.Extensions
{
    public static class SceneExtensions
    {
        public static void MoveScene(this GameObject obj, Scene targetScene)
        {
            SceneManager.MoveGameObjectToScene(obj, targetScene);
        }
    }
}
