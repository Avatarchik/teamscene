using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teamscene.APIData;
using UnityEditor;

namespace Teamscene
{
    public class TeamsceneConfig : ScriptableObject
    {
        private static TeamsceneConfig _instance;

        [SerializeField] private TeamsceneAPIResponse _lastValidResponse = null;
        [SerializeField] private string _projectKey = null;
        [SerializeField] private List<SceneUsageData> _scenes = null;
        [SerializeField] private string _projectName = null;

        public TeamsceneAPIResponse LastValidResponse => _lastValidResponse ?? (_lastValidResponse = new TeamsceneAPIResponse(_projectName, _scenes, true));
        public string Username => _username;
        public string ProjectKey => _projectKey;

        [SerializeField] private string _username;

        public static TeamsceneConfig FindOrCreate()
        {
            _instance = LoadConfig() ?? CreateConfig();
            _instance._username = EditorPrefs.GetString($"Editor.Teamscene.{Application.productName}.Username");
            //config._lastValidResponse = new TeamsceneAPIResponse(config._projectName, config._scenes, true);//
            return _instance;
        }

        private static TeamsceneConfig CreateConfig()
        {
            var path = GetScriptPath();

            path = path.Substring(0, path.LastIndexOf('/'));
            path += "/TeamsceneConfig.asset";

            var instance = ScriptableObject.CreateInstance<TeamsceneConfig>();
            AssetDatabase.CreateAsset(instance, path);

            return AssetDatabase.LoadAssetAtPath<TeamsceneConfig>(path);
        }

        private static string GetScriptPath()
        {
            var paths = AssetDatabase.FindAssets("TeamsceneConfig");

            foreach (var p in paths)
            {
                var path = AssetDatabase.GUIDToAssetPath(p);
                var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                // Debug.Log(asset.GetType());
                if (asset == null) continue;

                if (asset != null && asset.GetClass() == typeof(TeamsceneConfig))
                {
                    return path;
                }
            }
            Debug.Log("For some reason this script (which is being executed right now) wasn't found in the project. This should only be possible with black magic.");
            return string.Empty;
        }
        private static TeamsceneConfig LoadConfig()
        {
            var paths = AssetDatabase.FindAssets("t:TeamsceneConfig");

            if (paths == null || paths.Length < 1) return null;
            var path = paths[0];
            path = AssetDatabase.GUIDToAssetPath(path);
            var config = AssetDatabase.LoadAssetAtPath<TeamsceneConfig>(path);

            return config;
        }

        public void JoinProject(string projectKey, string username)
        {
            SetProperty("_projectKey", projectKey);
            EditorPrefs.SetString($"Editor.Teamscene.{Application.productName}.Username", username);
        }

        public void SetLastValidResponse(TeamsceneAPIResponse response)
        {
            SetProperty("_projectName", response.name);
            SetProperty("_scenes", response.scenes);

            _lastValidResponse = new TeamsceneAPIResponse(_projectName, _scenes, true);
        }

        private string GetStringProperty(string propertyName)
        {
            SerializedObject so = new SerializedObject(this);
            so.Update();

            return so.FindProperty(propertyName).stringValue;
        }

        private void SetProperty<T>(string propertyName, T value)
        {
            if (_instance == null)
            {
                FindOrCreate();
            }

            SerializedObject so = new SerializedObject(_instance);
            so.Update();

            SerializedProperty property = so.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError("Invalid property name " + propertyName);
                return;
            }
            else
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.String:
                        property.stringValue = value as string;
                        break;

                    case SerializedPropertyType.ObjectReference:
                        property.objectReferenceValue = value as Object;
                        break;
                }
            }

            so.ApplyModifiedProperties();
        }
    }
}