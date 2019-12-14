using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.SceneManagement;
using System.Linq;
using Teamscene.APIData;
using UnityEngine.SceneManagement;

namespace Teamscene
{
    [InitializeOnLoad]
    public class TeamsceneAPI
    {
        public static string Username { get; private set; }
        public static string ProjectName { get; private set; }
        public static bool IsInProject { get; private set; }
        public static string ProjectKey { get; private set; }
        public static bool IsSceneTaken => CurrentScene != null ? CurrentScene.inUse : false;
        public static bool IsSceneTakenByMe => CurrentScene != null ? CurrentScene.IsTakenByMe() : false;
        public static string CurrentOwner => CurrentScene.by;
        public static TeamsceneAPIState State { get; private set; }
        public static SceneUsageData CurrentScene
        {
            get
            {
                return _currentScene;
            }
        }
        private static SceneUsageData _currentScene;

        public static event Action<string> JoinedProject;
        public static event Action Refreshed;
        public static event Action FinishedRecompile;

        private static TeamsceneAPIResponse LastServerResponse = null;
        private static TeamsceneConfig _config;
        private static TeamsceneConfig Config => _config ?? (_config = TeamsceneConfig.FindOrCreate());
        // private static Scene currentUnityScene;
        private static Request currentRequest;
        private static string lastRequestedGuid;

        static TeamsceneAPI()
        {
            State = TeamsceneAPIState.Disconnected;

            EditorSceneManager.activeSceneChangedInEditMode -= HandleSceneChangedInEditMode;
            EditorSceneManager.activeSceneChangedInEditMode += HandleSceneChangedInEditMode;
            EditorSceneManager.sceneOpened -= HandleSceneOpened;
            EditorSceneManager.sceneOpened += HandleSceneOpened;
            EditorApplication.projectChanged -= HandleProjectChange;
            EditorApplication.projectChanged += HandleProjectChange;

            EditorApplication.quitting -= OnQuit;
            EditorApplication.quitting += OnQuit;
            ProjectKey = Config.ProjectKey;
            Username = Config.Username;
            // Debug.Log(ProjectKey);

            if (!string.IsNullOrEmpty(ProjectKey))
            {
                AttemptTake(AssetDatabase.AssetPathToGUID(EditorSceneManager.GetActiveScene().path));
            }
            // currentUnityScene =
        }

        private static void OnQuit()
        {
            Debug.Log("Quitting...");
            ReleaseScene(lastRequestedGuid);
        }

        private static void HandleSceneOpened(Scene scene, OpenSceneMode mode)
        {
            string guid = AssetDatabase.AssetPathToGUID(scene.path);
            AttemptTake(guid);
        }

        private static void AttemptTake(string guid)
        {
            var sequence = GetRefreshRequest(ProjectKey);
            sequence.OnComplete += () => GetTakeSceneRequest(guid, GetRefreshRequest(ProjectKey))?.Enqueue();
            sequence.Enqueue();
        }

        private static void HandleSceneChangedInEditMode(Scene current, Scene next)
        {
            var currentPath = lastRequestedGuid;
            var nextPath = next.path;
            string guid = null;
            guid = AssetDatabase.AssetPathToGUID(currentPath);

            ReleaseScene(lastRequestedGuid);
        }

        private static void HandleProjectChange()
        {
            if (LastServerResponse != null)
            {
                SetConnected(LastServerResponse);
            }
            else
            {
                SetDisconnected();
            }

            FinishedRecompile?.Invoke();
        }

        public static void JoinProject(string projectKey, string name)
        {
            State = TeamsceneAPIState.JoiningTeam;
            var request = GetNewResponse(projectKey);

            request.OnComplete += (() =>
            {
                TeamsceneAPIResponse response = JsonUtility.FromJson<TeamsceneAPIResponse>(request.Result);
                if (response != null)
                {
                    lastRequestedGuid = response.scenes.FirstOrDefault(x => x.IsTakenByMe())?.id;
                    response.Succeed = true;

                    Username = name;
                    ProjectKey = projectKey;

                    SetConnected(response);

                    JoinedProject?.Invoke(ProjectName);

                    Config.JoinProject(ProjectKey, Username);

                    if (_currentScene != null)
                    {
                        var req = GetTakeSceneRequest(_currentScene.id);
                        req?.Enqueue();
                    }
                }
                else
                {
                    SetDisconnected();
                }
            });
        }

        public static void RefreshAndTakeCurrent()
        {
            string guid = AssetDatabase.AssetPathToGUID(EditorSceneManager.GetActiveScene().path);
            AttemptTake(guid);
            // RequestScene(guid);
        }

        public static Request Refresh()
        {
            if (string.IsNullOrEmpty(ProjectKey)) return null;

            var request = GetNewResponse(ProjectKey);
            request.OnComplete += (() =>
            {
                TeamsceneAPIResponse response = JsonUtility.FromJson<TeamsceneAPIResponse>(request.Result);
                if (response != null)
                {
                    SetConnected(response);
                }
                else
                {
                    SetDisconnected();
                }

                Refreshed?.Invoke();
            });
            return request;
        }

        private static Request ReleaseScene(string guid)
        {
            if (CurrentScene != null && CurrentScene.IsTakenByMe())
            {
                var requestScene = new SceneRequest(guid);
                var json = requestScene.ToJson(guid);
                return Post("releaseScene", json);
            }
            return null;
        }

        private static Request GetTakeSceneRequest(string guid, Request next = null)
        {
            var scene = LastServerResponse.scenes.FirstOrDefault(x => x.id == guid);
            if (scene != null && scene.inUse && !scene.IsTakenByMe())
            {
                Debug.Log($"Current scene is in use by {CurrentOwner}.");
                return null;
            }

            var requestScene = new SceneRequest(guid, Username);
            var json = JsonUtility.ToJson(requestScene);

            return CreatePostRequest("takeScene", json, next);
        }

        private static Request CreatePostRequest(string method, string json, Request next = null)
        {
            string url = $"https://us-central1-team-scene.cloudfunctions.net/webApi/api/{method}/project/{ProjectKey}";
            return TeamsceneAPIRequestHelper.CreatePOSTRequest(url, json, next);
        }

        private static Request Post(string method, string json)
        {
            string url = $"https://us-central1-team-scene.cloudfunctions.net/webApi/api/{method}/project/{ProjectKey}";
            return TeamsceneAPIRequestHelper.QueuePOST(url, json, Refresh());
        }

        private static Request GetRefreshRequest(string projectKey, Request next = null)
        {
            if (string.IsNullOrEmpty(projectKey)) return null;

            string destinationUrl = $"https://us-central1-team-scene.cloudfunctions.net/webApi/api/projects/{projectKey}";
            var request = TeamsceneAPIRequestHelper.CreateGETRequest(destinationUrl, next);
            request.OnComplete += (() =>
            {
                TeamsceneAPIResponse response = JsonUtility.FromJson<TeamsceneAPIResponse>(request.Result);
                if (response != null)
                {
                    SetConnected(response);
                }
                else
                {
                    SetDisconnected();
                }
                // Debug.Log(request.Result);
                Refreshed?.Invoke();
            });
            return request;
        }

        private static Request GetNewResponse(string projectKey)
        {
            if (string.IsNullOrEmpty(projectKey)) return null;

            string destinationUrl = $"https://us-central1-team-scene.cloudfunctions.net/webApi/api/projects/{projectKey}";
            var request = TeamsceneAPIRequestHelper.QueueGET(destinationUrl);
            return request;
        }

        private static SceneUsageData FindScene(List<SceneUsageData> scenes, string guid)
        {
            return scenes.FirstOrDefault(x => x.id == guid);
        }

        private static void SetDisconnected()
        {
            IsInProject = false;
            ProjectName = "No Project";
            State = TeamsceneAPIState.Disconnected;
        }

        private static void SetConnected(TeamsceneAPIResponse response)
        {
            State = TeamsceneAPIState.Connected;
            IsInProject = true;
            ProjectName = response.name;
            LastServerResponse = response;
            lastRequestedGuid = response.scenes.FirstOrDefault(x => x.IsTakenByMe())?.id;
            UpdateSceneUsage(response);

            Config.SetLastValidResponse(response);
        }

        private static void UpdateSceneUsage(TeamsceneAPIResponse response)
        {
            var guid = AssetDatabase.AssetPathToGUID(EditorSceneManager.GetActiveScene().path);
            var scene = FindScene(response.scenes, guid);
            _currentScene = scene;
        }
    }
}