using UnityEngine;

namespace Utils
{
    public static class GuiUtils
    {
        private static Vector2 mouseScreenPosition;
        
        public static void DisplayTextUnderMouse(Vector2 _mousePos, string _text, float _xOffsetFactor = 0.01f, float _yOffsetFactor = 0.02f)
        {
            mouseScreenPosition = _mousePos;
            Vector2 _offset = new Vector2(Screen.width * _xOffsetFactor, Screen.height * _yOffsetFactor);
            Vector2 _textPosition = new Vector2(mouseScreenPosition.x + _offset.x, (Screen.height - mouseScreenPosition.y) + _offset.y);
            GUIStyle _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(_textPosition, new Vector2(Screen.width * 0.3f, Screen.height * 0.03f)), _text, _style);
        }
    }
}