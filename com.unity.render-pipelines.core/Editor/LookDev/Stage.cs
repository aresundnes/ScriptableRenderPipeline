using System;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.LookDev
{
    //TODO: add undo support
    internal class Stage : IDisposable
    {
        const int k_PreviewCullingLayerIndex = 31; //Camera.PreviewCullingLayer; //TODO: expose or reflection

        private readonly Scene m_PreviewScene;
        private readonly List<GameObject> m_GameObjects = new List<GameObject>();
        private readonly Camera m_Camera;
        
        public Camera camera => m_Camera;

        public Scene scene => m_PreviewScene;

        public Stage(string sceneName)
        {
            m_PreviewScene = EditorSceneManager.NewPreviewScene();
            m_PreviewScene.name = sceneName;

            // Setup default render settings for this preview scene
            Unsupported.SetOverrideRenderSettings(m_PreviewScene);
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom; //TODO: gather data from SRP
            //RenderSettings.customReflection =                //TODO: gather data from SRP
            RenderSettings.skybox = null;                      //TODO: gather data from SRP         
            RenderSettings.ambientMode = AmbientMode.Trilight; //TODO: gather data from SRP
            Unsupported.RestoreOverrideRenderSettings();
            
            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            AddGameObject(camGO);
            m_Camera = camGO.GetComponent<Camera>();
            camera.cameraType = CameraType.Preview;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.fieldOfView = 15;
            camera.farClipPlane = 10.0f;
            camera.nearClipPlane = 2.0f;
            camera.cullingMask = k_PreviewCullingLayerIndex;
            camera.transform.position = new Vector3(0, 0, -6);
            
            camera.renderingPath = RenderingPath.DeferredShading;
            camera.useOcclusionCulling = false;
            camera.scene = m_PreviewScene;
        }

        public void AddGameObject(GameObject go)
            => AddGameObject(go, Vector3.zero, Quaternion.identity);
        public void AddGameObject(GameObject go, Vector3 position, Quaternion rotation)
        {
            if (m_GameObjects.Contains(go))
                return;

            SceneManager.MoveGameObjectToScene(go, m_PreviewScene);
            go.transform.position = position;
            go.transform.rotation = rotation;
            m_GameObjects.Add(go);

            InitAddedObjectsRecursively(go);
        }

        public GameObject InstantiateInStage(GameObject prefabOrSceneObject)
            => InstantiateInStage(prefabOrSceneObject, Vector3.zero, Quaternion.identity);
        public GameObject InstantiateInStage(GameObject prefabOrSceneObject, Vector3 position, Quaternion rotation)
        {
            var handle = GameObject.Instantiate(prefabOrSceneObject);
            AddGameObject(handle, position, rotation);
            return handle;
        }

        public void Dispose()
        {
            EditorSceneManager.ClosePreviewScene(m_PreviewScene);
            Clear();
        }

        public void Clear()
        {
            foreach (var go in m_GameObjects)
                UnityEngine.Object.DestroyImmediate(go);
            m_GameObjects.Clear();
        }

        static void InitAddedObjectsRecursively(GameObject go)
        {
            go.hideFlags = HideFlags.HideAndDontSave;
            go.layer = k_PreviewCullingLayerIndex;
            foreach (Transform child in go.transform)
                InitAddedObjectsRecursively(child.gameObject);
        }
    }
}
