using System.Collections.Generic;

namespace Teamscene.APIData
{
    [System.Serializable]
    public class TeamsceneAPIResponse
    {
        public bool Succeed;
        public string name;
        public List<SceneUsageData> scenes;

        public TeamsceneAPIResponse(string projectName, List<SceneUsageData> scenes, bool succeed)
        {
            this.name = projectName;
            this.scenes = scenes;
            Succeed = succeed;
        }

        public override string ToString()
        {
            return $"APIResponse: Project name {name}";
        }
    }

    [System.Serializable]
    public class SceneUsageData
    {
        public SceneUsageData(string id, string by, bool inUse)
        {
            this.by = by;
            this.id = id;
            this.inUse = inUse;
        }

        public string by;
        public string id;
        public bool inUse;

        public bool IsTakenByMe()
        {
            return this.by == TeamsceneAPI.Username;
        }

    }

    [System.Serializable]
    public class SceneRequest
    {
        public string sceneId;
        public string by;

        public SceneRequest(string sceneId, string by = null)
        {
            this.sceneId = sceneId;
            this.by = by;
        }

        public string ToJson(string sceneId)
        {
            return $"{{\"sceneId\": \"{sceneId}\"}}";
        }
    }
}