using System.IO;
using UnityEngine;
using BepInEx.Configuration;

namespace JiangHu
{
    public class DescriptionLoader : MonoBehaviour
    {
        private ConfigEntry<bool> showDescriptionConfig;
        private string descriptionText = "";
        private Rect windowRect = new Rect(100, 100, 1000, 700);
        private bool showDescriptionWindow = false;
        private Vector2 scrollPosition = Vector2.zero;
        private bool isMaximized = false;
        private Vector2 normalWindowSize = new Vector2(1000, 700);
        private Vector2 normalWindowPosition = new Vector2(100, 100);

        public void SetConfig(ConfigEntry<bool> showDescription)
        {
            showDescriptionConfig = showDescription;
        }

        private void LoadDescription(string languageCode)
        {
            string descriptionPath = Path.Combine(Directory.GetCurrentDirectory(), "BepInEx", "plugins", "JiangHu.Client", "description", $"{languageCode}.md");

            if (File.Exists(descriptionPath))
            {
                    descriptionText = File.ReadAllText(descriptionPath);
            }
        }

        void Update()
        {
            if (showDescriptionConfig.Value && !showDescriptionWindow)
            {
                LoadDescription("en");
                showDescriptionWindow = true;
            }
            else if (!showDescriptionConfig.Value && showDescriptionWindow)
            {
                showDescriptionWindow = false;
            }
        }

        void OnGUI()
        {
            if (!showDescriptionWindow) return;
            windowRect = GUI.Window(12350, windowRect, DrawDescriptionWindow, "About JiangHu");
        }

        void DrawDescriptionWindow(int windowID)
        {
            GUIStyle largeLabelStyle = new GUIStyle(GUI.skin.label);
            largeLabelStyle.fontSize = 20;
            largeLabelStyle.wordWrap = true;
            largeLabelStyle.normal.textColor = Color.white;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;

            if (GUI.Button(new Rect(windowRect.width - 120, 5, 100, 30), "CLOSE", buttonStyle))
            {
                showDescriptionWindow = false;
                showDescriptionConfig.Value = false;
                return;
            }

            string buttonText = isMaximized ? "NORMAL" : "MAXIMIZE";
            if (GUI.Button(new Rect(windowRect.width - 230, 5, 100, 30), buttonText, buttonStyle))
            {
                if (isMaximized)
                {
                    windowRect = new Rect(normalWindowPosition.x, normalWindowPosition.y, normalWindowSize.x, normalWindowSize.y);
                }
                else
                {
                    normalWindowPosition = new Vector2(windowRect.x, windowRect.y);
                    normalWindowSize = new Vector2(windowRect.width, windowRect.height);
                    windowRect = new Rect(Screen.width * 0.1f, Screen.height * 0.1f, Screen.width * 0.8f, Screen.height * 0.8f);
                }
                isMaximized = !isMaximized;
            }

            float buttonWidth = (windowRect.width - 40) / 7f;
            float buttonY = 40f;

            if (GUI.Button(new Rect(20, buttonY, buttonWidth, 35), "中文", buttonStyle))
            {
                LoadDescription("ch");
            }
            if (GUI.Button(new Rect(20 + buttonWidth, buttonY, buttonWidth, 35), "English", buttonStyle))
            {
                LoadDescription("en");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 2, buttonY, buttonWidth, 35), "Español", buttonStyle))
            {
                LoadDescription("es");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 3, buttonY, buttonWidth, 35), "Français", buttonStyle))
            {
                LoadDescription("fr");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 4, buttonY, buttonWidth, 35), "日本語", buttonStyle))
            {
                LoadDescription("jp");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 5, buttonY, buttonWidth, 35), "Português", buttonStyle))
            {
                LoadDescription("po");
            }
            if (GUI.Button(new Rect(20 + buttonWidth * 6, buttonY, buttonWidth, 35), "Русский", buttonStyle))
            {
                LoadDescription("ru");
            }

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 230, 25));

            float scrollViewY = buttonY + 45f;
            float scrollViewHeight = windowRect.height - scrollViewY - 10f;

            GUILayout.BeginArea(new Rect(10, scrollViewY, windowRect.width - 20, scrollViewHeight));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Label(descriptionText, largeLabelStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.DragWindow(new Rect(0, 0, windowRect.width - 230, 40));
        }
    }
}