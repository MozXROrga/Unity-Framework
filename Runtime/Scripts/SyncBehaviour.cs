using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Component = UnityEngine.Component;

[assembly: InternalsVisibleTo("MoxSyncManager")]
namespace Mox
{
    public class SyncBehaviour : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        internal string _syncId = string.Empty;

        public string SyncId
        {
            get { return _syncId; }
        }

        private GameObject _presentationObject = null;

        public GameObject PresentationObject
        {
            get { return _presentationObject; }
        }


        internal Transform internalTransform
        {
            get
            {
                if (_presentationObject != null)
                {
                    return _presentationObject.transform;
                }
                else
                {
                    return gameObject.transform;
                }
            }
        }

#if UNITY_EDITOR
        // Handle copying sync objects, diplicate sync IDs will cause issues for multi-node synchronization, level switching, and who knows what
        // source: https://discussions.unity.com/t/callback-when-object-is-created-in-editor/509531/5
        // When overriding OnValidate, make sure to call base.OnValidate()
        protected void OnValidate()
        {
            if (Event.current != null)
            {
                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Duplicate")
                {
                    _syncId = string.Empty;

                }
                else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Paste")
                {
                    _syncId = string.Empty;

                }
                else if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Paste")
                {
                    _syncId = string.Empty;
                }
            }
        }
#endif

        public void Start()
        {

        }

        public void Update()
        {

        }

        // When overriding OnDestroy, make sure to call base.OnDestroy()
        protected void OnDestroy()
        {
            Debug.Log(gameObject.name + " - OnDestroy");

            if (_presentationObject != null)
            {
                Destroy(_presentationObject);
            }

            Sync.SceneSyncManager.RemoveSyncObject(_syncId);
        }

        virtual protected object GetUserData()
        {
            return null;
        }

        virtual protected System.Type GetUserDataType()
        {
            throw new NotImplementedException();
        }

        virtual protected void SetUserData(object userData)
        {
        }

        virtual protected void SyncInitDone()
        {
        }

        internal object GetUserDataInternal()
        {
            return GetUserData();
        }

        internal System.Type GetUserDataTypeInternal()
        {
            return GetUserDataType();
        }

        internal void SetUserDataInternal(object userData)
        {
            SetUserData(userData);
        }

        internal void SyncInitDoneInternal()
        {
            SyncInitDone();
        }

        internal void PrepareForSync(bool isPrimary)
        {
            Debug.Log(gameObject.name + " - PrepareForSync");

            GeneratePresentationObject();

            if (isPrimary == false)
            {
                Rigidbody rigidBody = GetComponent<Rigidbody>();
                if (rigidBody != null)
                {
                    Destroy(rigidBody);
                }
            }

            for(int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).transform.SetParent(_presentationObject.transform, false);
            }
        }

        private void GeneratePresentationObject()
        {
            _presentationObject = new GameObject();
            _presentationObject.transform.parent = gameObject.transform.parent;
            _presentationObject.transform.localPosition = Vector3.zero;
            _presentationObject.transform.localRotation = Quaternion.identity;
            _presentationObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

            AudioSource[] audioSources = GetComponents<AudioSource>();
            AudioListener audioListener = GetComponent<AudioListener>();

            Camera camera = GetComponent<Camera>();
            UniversalAdditionalCameraData camData = GetComponent<UniversalAdditionalCameraData>();

            UniversalAdditionalLightData lightData = GetComponent<UniversalAdditionalLightData>();
            Light[] lights = GetComponents<Light>();

            if (meshFilter != null)
            {
                MoveComponentToGameObject(meshFilter, _presentationObject);
            }
            if (meshRenderer != null)
            {
                List<string> ignoredProperties = new List<string>();
                ignoredProperties.Add("bounds");

                MoveComponentToGameObject(meshRenderer, _presentationObject, ignoredProperties);
            }

            if (audioSources != null && audioSources.Length > 0)
            {
                foreach (AudioSource audioSource in audioSources)
                {
                    MoveComponentToGameObject(audioSource, _presentationObject);

                    AudioSource newSource = _presentationObject.GetComponent<AudioSource>();
                    if(newSource.playOnAwake == true)
                    {
                        newSource.Play();
                    }
                }
            }
            if (audioListener != null)
            {
                MoveComponentToGameObject(audioListener, _presentationObject);
            }

            if (camera != null)
            {
                MoveComponentToGameObject(camera, _presentationObject);
            }
            if (camData != null)
            {
                MoveComponentToGameObject(camData, _presentationObject);
            }

            if (lightData != null)
            {
                MoveComponentToGameObject(lightData, _presentationObject);

                Light[] newLights = _presentationObject.GetComponents<Light>();

                Destroy(lightData);

                for (int i = 0; i < newLights.Length; i++)
                {
                    CopyComponentValues(newLights[i], lights[i]);

                    Destroy(lights[i]);
                }
            }

            _presentationObject.name = "presentation_" + name;
        }

        private void MoveComponentToGameObject(Component component, GameObject targetObject, List<string> ignoredProperties = null)
        {
            Component comp = targetObject.AddComponent(component.GetType());
            if (comp != null)
            {
                try
                {
                    CopyComponentValues(comp, component, ignoredProperties);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to copy component values: " + e.Message);
                }

                Destroy(component);
            }
        }

        private void CopyComponentValues(Component target, Component source, List<string> ignoredProperties = null)
        {
            if (target == null
                || source == null)
            {
                Debug.LogError("Cannot copy component, target or source is null");
                return;
            }

            if (target.GetType() != source.GetType())
            {
                Debug.LogError("Cannot copy component, type missmatch :(");
                return;
            }

            PropertyInfo[] propertyInfos = target.GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name == "name")
                {
                    continue;
                }

                if (Attribute.IsDefined(propertyInfo, typeof(ObsoleteAttribute)))
                {
                    continue;
                }

                if (ignoredProperties != null
                    && ignoredProperties.Contains(propertyInfo.Name))
                {
                    continue;
                }

                if (propertyInfo.CanWrite == true)
                {
                    propertyInfo.SetValue(target, propertyInfo.GetValue(source, null), null);
                }
            }

            FieldInfo[] fieldInfos = target.GetType().GetFields();
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                if (fieldInfo.CanWrite() == true)
                {
                    fieldInfo.SetValue(target, fieldInfo.GetValue(source));
                }
            }
        }
    }
}
