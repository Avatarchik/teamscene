using UnityEngine;
using UnityEditor;

namespace Teamscene.Editor
{
    [InitializeOnLoad]
    public class ToolbarExtension
    {
        private static string currentMessage;
        private static bool growing;
        private static GUIStyle style;
        private static bool wasCompiling;

        static ToolbarExtension()
        {
            UnityToolbarExtender.ToolbarExtender.RightToolbarGUI.Add(OnGUI);

            TeamsceneAPI.Refreshed -= RefreshMessage;
            TeamsceneAPI.FinishedRecompile -= RefreshMessage;

            TeamsceneAPI.Refreshed += RefreshMessage;
            TeamsceneAPI.FinishedRecompile += RefreshMessage;
        }

        private static void RefreshMessage()
        {
            // UnityToolbarExtender.ToolbarCallback.container.MarkDirtyRepaint();
            currentMessage = TeamsceneAPI.IsSceneTaken && !TeamsceneAPI.IsSceneTakenByMe ? $"{TeamsceneAPI.CurrentOwner} is working on this scene." : "You're working on this scene.";
        }

        private static void OnGUI()
        {
            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginHorizontal();
            {
                GUI.enabled = TeamsceneAPI.State == TeamsceneAPIState.Connected;
                if (GUILayout.Button(new GUIContent("Refresh & Take", "Update the scene usage status and take it if available.")))
                {
                    TeamsceneAPI.RefreshAndTakeCurrent();
                }
                GUI.enabled = true;

                EditorGUILayout.BeginVertical();
                {
                    var width = EditorGUIUtility.currentViewWidth;

                    if (style == null)
                    {
                        InitializeLabelStyle();
                    }

                    var prevcolor = GUI.color;
                    GUI.contentColor = Color.white;
                    GUI.color = TeamsceneAPI.IsSceneTaken && !TeamsceneAPI.IsSceneTakenByMe ? Color.red : Color.green;

                    GUILayout.Label(new GUIContent("", currentMessage), style, GUILayout.Width(30), GUILayout.Height(20));

                    GUI.color = prevcolor;
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void InitializeLabelStyle()
        {
            style = new GUIStyle(GUI.skin.button);
            style.font = GUI.skin.label.font;
            style.fontStyle = GUI.skin.label.fontStyle;
            style.fontSize = 10;
            style.normal.textColor = Color.white;
            style.normal.background = Texture2D.whiteTexture;

            // Texture2D bg = style.normal.background;
            // for (int x = 0; x < bg.width; x++)
            // {
            //     for (int y = 0; y < bg.height; y++)
            //     {
            //         if (bg.GetPixel(x, y).a > 0)
            //             bg.SetPixel(x, y, Color.green);
            //     }
            // }
            // style.normal.background = bg;
        }
    }
}