using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace Utils
{
    public static class UIUtils
    {
        static readonly List<RaycastResult> results = new();

        public static bool IsMouseOverUI()
        {
            results.Clear();

            PointerEventData _data = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            EventSystem.current.RaycastAll(_data, results);

            foreach (var r in results)
            {
                if (r.module is GraphicRaycaster)
                    return true;
            }

            return false;
        }
    }
}