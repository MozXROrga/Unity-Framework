using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mox
{
#if UNITY_EDITOR
    namespace Editor
    {
        [InitializeOnLoad]
        public class SceneHandler
        {
            private static List<string> _sceneGuids = new List<string>();

            private const string _sceneGuidsKey = "Mox.Editor.SceneHandler.SceneGuids";

            static SceneHandler()
            {
                LoadSceneGuids();
                EditorApplication.update += OnEditorUpdate;
            }

            private static void LoadSceneGuids()
            {
                _sceneGuids.Clear();

                string saved = EditorPrefs.GetString(_sceneGuidsKey, "");
                if (string.IsNullOrEmpty(saved) == false)
                {
                    foreach (string guid in saved.Split(';'))
                    {
                        if (string.IsNullOrEmpty(guid) == false)
                        {
                            _sceneGuids.Add(guid);
                        }
                    }
                }
            }

            private static void SaveSceneGuids()
            {
                EditorPrefs.SetString(_sceneGuidsKey, string.Join(";", _sceneGuids));
            }

            private static void OnEditorUpdate()
            {
                string[] guids = AssetDatabase.FindAssets("t:Scene");
                if (guids.Length > 0)
                {
                    if (_sceneGuids.Count <= 0)
                    {
                        foreach (string guid in guids)
                        {
                            _sceneGuids.Add(guid);
                            SaveSceneGuids();
                        }
                    }
                    else
                    {
                        List<string> deletedScenes = new List<string>();
                        foreach (string guid in _sceneGuids)
                        {
                            deletedScenes.Add(guid);
                        }

                        foreach (string guid in guids)
                        {
                            deletedScenes.Remove(guid);
                        }

                        foreach (string deletedScene in deletedScenes)
                        {
                            Debug.Log("Scene deleted: " + deletedScene);

                            _sceneGuids.Remove(deletedScene);
                            SaveSceneGuids();
                        }
                    }

                    string loadedSceneGuid = AssetDatabase.AssetPathToGUID(SceneManager.GetActiveScene().path);
                    if (_sceneGuids.Contains(loadedSceneGuid) == false)
                    {
                        List<SyncBehaviour> syncBehaviours = FindSyncBehavioursInActiveScene();

                        foreach(SyncBehaviour syncBehaviour in syncBehaviours)
                        {
                            Debug.Log("Reseting Sync ID for " + syncBehaviour.name);

                            syncBehaviour._syncId = "";
                        }

                        _sceneGuids.Add(loadedSceneGuid);
                        SaveSceneGuids();

                        Debug.Log("HandleProjectScenes: reset SyncIDs for newly opened scene " + SceneManager.GetActiveScene().path + " (" + loadedSceneGuid + ")");
                    }
                }
            }

            private static List<SyncBehaviour> FindSyncBehavioursInActiveScene()
            {
                List<SyncBehaviour> result = new List<SyncBehaviour>();

                GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

                foreach (GameObject rootObject in rootObjects)
                {
                    SyncBehaviour[] syncBehaviours = rootObject.GetComponentsInChildren<SyncBehaviour>(true);
                    result.AddRange(syncBehaviours);
                }

                return result;
            }
        }
    }
#endif
}

