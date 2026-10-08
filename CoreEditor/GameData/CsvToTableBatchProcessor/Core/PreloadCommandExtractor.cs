using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoreEngine.GameData;

namespace CoreEditor.GameData
{
    public static class PreloadCommandExtractor
    {
        private struct Field2Type
        {
            public readonly FieldInfo fieldInfo;
            public readonly Type cmdType;
            public Field2Type(FieldInfo fieldInfo, Type cmdType)
            {
                this.fieldInfo = fieldInfo;
                this.cmdType = cmdType;
            }
        }

        internal static _AssetPreloadCommand[] Extract<TRecord>(List<IRecord> records)
        {
            var singleAssetIdFields = new List<Field2Type>();
            var arrayAssetIdFields = new List<Field2Type>();

            var allFieldInfos = typeof(TRecord).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var fieldInfo in allFieldInfos)
            {
                Type ft = fieldInfo.FieldType;

                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(AssetId<,>))
                {
                    Type[] genArgs = ft.GetGenericArguments();
                    Type cmdType = typeof(AssetPreloadCommand<,>).MakeGenericType(genArgs[0], genArgs[1]);
                    singleAssetIdFields.Add(new Field2Type(fieldInfo, cmdType));
                }
                else if (ft.IsArray && ft.GetElementType().IsGenericType && ft.GetElementType().GetGenericTypeDefinition() == typeof(AssetId<,>))
                {
                    Type[] genArgs = ft.GetElementType().GetGenericArguments();
                    Type cmdType = typeof(AssetPreloadCommand<,>).MakeGenericType(genArgs[0], genArgs[1]);
                    arrayAssetIdFields.Add(new Field2Type(fieldInfo, cmdType));
                }
            }

            Dictionary<Type, HashSet<ulong>> typeToIdsMap = new Dictionary<Type, HashSet<ulong>>();

            foreach (IRecord record in records)
            {
                foreach (Field2Type field2Type in singleAssetIdFields)
                {
                    object assetIdObj = field2Type.fieldInfo.GetValue(record);
                    ulong id = ((IIdentifiable)assetIdObj).ID;

                    if (id > 0)
                    {
                        if (!typeToIdsMap.TryGetValue(field2Type.cmdType, out var idSet))
                        {
                            idSet = new HashSet<ulong>();
                            typeToIdsMap[field2Type.cmdType] = idSet;
                        }
                        idSet.Add(id);
                    }
                }

                foreach (Field2Type field2Type in arrayAssetIdFields)
                {
                    if (field2Type.fieldInfo.GetValue(record) is Array arr)
                    {
                        if (!typeToIdsMap.TryGetValue(field2Type.cmdType, out var idSet))
                        {
                            idSet = new HashSet<ulong>();
                            typeToIdsMap[field2Type.cmdType] = idSet;
                        }

                        foreach (var item in arr)
                        {
                            ulong id = ((IIdentifiable)item).ID;
                            if (id > 0) idSet.Add(id);
                        }
                    }
                }
            }

            var bakedCommands = new List<_AssetPreloadCommand>();
            foreach (var kvp in typeToIdsMap)
            {
                Type cmdType = kvp.Key;
                ulong[] uniqueIdsArray = kvp.Value.ToArray();
                Array.Sort(uniqueIdsArray);

                var cmd = (_AssetPreloadCommand)Activator.CreateInstance(cmdType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new object[] { uniqueIdsArray }, null);

                bakedCommands.Add(cmd);
            }

            return bakedCommands.ToArray();
        }
    }
}