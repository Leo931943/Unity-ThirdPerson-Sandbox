#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonDemo.Editor
{
    [InitializeOnLoad]
    internal static class CharacterRigSceneInstaller
    {
        static CharacterRigSceneInstaller()
        {
            EditorApplication.delayCall += EnsureRigInActiveScene;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EditorApplication.delayCall += EnsureRigInActiveScene;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += EnsureRigInActiveScene;
        }

        private static void EnsureRigInActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            ThirdPersonController player = Object.FindFirstObjectByType<ThirdPersonController>();
            if (player == null) return;
            ProceduralCharacterRig rig = player.GetComponent<ProceduralCharacterRig>();
            if (rig == null) rig = Undo.AddComponent<ProceduralCharacterRig>(player.gameObject);
            rig.EnsureVisualRig();
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif

