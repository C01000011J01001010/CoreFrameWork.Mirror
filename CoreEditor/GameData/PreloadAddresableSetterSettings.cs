using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEditor.GameData
{
    [Serializable]
    public class PreloadTypeState
    {
        public string TypeFullName;
        public string TypeName;
        public bool IsEnabled = true;
    }

    public class PreloadAddresableSetterSettings : BaseToolSettings<PreloadAddresableSetterSettings>
    {
        public string TargetGroupName = "Preload Group";
        public string TargetLabel = "Preload";
        public List<PreloadTypeState> TypeStates = new List<PreloadTypeState>();

        public void SyncTypes(IEnumerable<Type> foundTypes)
        {
            bool isModified = false;

            // [Fix] 1. 더 이상 에셋이 존재하지 않는 타입 찌꺼기 청소
            for (int i = TypeStates.Count - 1; i >= 0; i--)
            {
                if (!foundTypes.Any(t => t.FullName == TypeStates[i].TypeFullName))
                {
                    TypeStates.RemoveAt(i);
                    isModified = true;
                }
            }

            // 2. 새로운 타입 추가
            foreach (var type in foundTypes)
            {
                if (!TypeStates.Any(t => t.TypeFullName == type.FullName))
                {
                    TypeStates.Add(new PreloadTypeState
                    {
                        TypeFullName = type.FullName,
                        TypeName = type.Name,
                        IsEnabled = true
                    });
                    isModified = true;
                }
            }

            if (isModified)
            {
                TypeStates = TypeStates.OrderBy(t => t.TypeName).ToList();
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
    }
}