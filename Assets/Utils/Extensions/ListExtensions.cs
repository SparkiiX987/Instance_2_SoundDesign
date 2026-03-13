using System.Collections.Generic;

namespace Utils.Extensions
{
    public static class ListExtensions
    {
        public static T PickRandom<T>(this List<T> _list)
        {
            if (_list == null || _list.Count == 0)
                return default(T);
            
            return _list[UnityEngine.Random.Range(0, _list.Count)];
        }
    }
}