//using CoreEngine.Helpers;
//using System;
//using System.Collections.Generic;

//namespace CoreEngine.GameData
//{
//    public interface IDataCollection<TData>
//    {
//        TData Get(int id);
//    }
//    public class DataRouter<TData, TDataCollection>
//        where TData : class
//        where TDataCollection : class, IDataCollection<TData>
//    {
//        public readonly Dictionary<Type, TDataCollection> DataCollectionMap = new();

//        // GameDataManager가 어드레서블로 로드한 뒤 이 메서드를 호출해 주입
//        public void Inject(TDataCollection dataCollection)
//        {
//            if (DataCollectionMap.ContainsKey(dataCollection.GetType()))
//            {
//                LogHelper.LogWarning($"{GetType().Name}:{dataCollection.GetType().Name} is already injected.");
//                return;
//            }
//            DataCollectionMap[dataCollection.GetType()] = dataCollection;
//        }

//        public TData Get(Type dataCollectionType, int id)
//        {
//            if (DataCollectionMap.TryGetValue(dataCollectionType, out TDataCollection dataCollection))
//            {
//                return dataCollection.Get(id);
//            }
//            LogHelper.LogWarning($"{dataCollectionType.Name} has no id({id}).");
//            return null;
//        }
//    }
//}
