using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Teamscene.Editor
{
    public class TeamSceneConfigWindow : EditorWindow
    {
        private Label projectNameLabel;
        private VisualElement projectInfo;
        private TextField projectKeyField;
        private TextField usernameField;


        [MenuItem("Window/Teamscene/Teamscene Config... %#E")]
        public static void ShowExample()
        {
            TeamSceneConfigWindow wnd = GetWindow<TeamSceneConfigWindow>();
            wnd.titleContent = new GUIContent("Teamscene Set-Up");
            wnd.minSize = new Vector2(500, 220);
            wnd.maxSize = new Vector2(Screen.width, 220);
        }

        public void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            // VisualElement label = new Label("Hello World! From C#");
            // root.Add(label);

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Teamscene/Editor/Team Configuration Window/TeamSceneConfigWindow.uxml");
            VisualElement labelFromUXML = visualTree.CloneTree();
            root.Add(labelFromUXML);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Teamscene/Editor/Team Configuration Window/TeamSceneConfigWindow.uss");
            root.styleSheets.Add(styleSheet);

            projectKeyField = root.Q<TextField>("connection-code");
            usernameField = root.Q("username-field", "setting-field") as TextField;
            projectInfo = root.Q("project-info");

            // root.Q<Button>("submit").clicked += () => JoinTeam(projectKeyField.value, usernameField.value);
            root.Q<Button>("create-team-button").clickable.clicked += OpenCreateTeamWebsite;
            root.Q<Button>("submit").clickable.clicked += () =>
            {
                JoinTeam(projectKeyField.value, usernameField.value);
            };

            projectNameLabel = root.Q<Label>("project-name");

            TeamsceneAPI.JoinedProject += OnJoinedProject;
            TeamsceneAPI.Refreshed += HandleAPIRefresh;
            TeamsceneAPI.Refresh();
        }

        private void HandleAPIRefresh()
        {
            projectKeyField.value = TeamsceneAPI.ProjectKey;
            usernameField.value = TeamsceneAPI.Username;
            projectInfo.visible = TeamsceneAPI.IsInProject;
            SetProjectName(TeamsceneAPI.ProjectName);
        }

        public void OnDisable()
        {
            TeamsceneAPI.JoinedProject -= OnJoinedProject;
            TeamsceneAPI.Refreshed -= HandleAPIRefresh;
        }

        private void OnJoinedProject(string projectName)
        {
            SetProjectName(projectName);
        }
        private void SetProjectName(string projectName)
        {
            projectNameLabel.text = $"Project Name: {projectName}";
        }

        private void OpenCreateTeamWebsite()
        {
            Application.OpenURL("https://team-scene.firebaseapp.com/app");
        }

        private void JoinTeam(string key, string name)
        {
            TeamsceneAPI.JoinProject(key, name);
        }
    }
}