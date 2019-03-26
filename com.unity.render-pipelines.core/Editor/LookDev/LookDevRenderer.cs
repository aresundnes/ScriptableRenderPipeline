using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.LookDev
{
    enum ViewCompositionIndex
    {
        First = ViewIndex.FirstOrFull,
        Second = ViewIndex.Second,
        Composite
    };

    class RenderTextureCache
    {
        RenderTexture[] m_RTs = new RenderTexture[3];

        public RenderTexture this[ViewCompositionIndex index]
            => m_RTs[(int)index];

        public void UpdateSize(Rect rect, ViewCompositionIndex index)
        {
            int width = (int)rect.width;
            int height = (int)rect.height;
            if (m_RTs[(int)index] == null
                || width != m_RTs[(int)index].width
                || height != m_RTs[(int)index].height)
                m_RTs[(int)index] = new RenderTexture(
                    width, height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        }
    }

    //TODO: for automatising environment change
    // if Unsupported.SetOverrideRenderSettings
    // do not behave as expected (see Stage)
    struct EnvironmentScope : IDisposable
    {
        public EnvironmentScope(bool b)
        {
        }

        void IDisposable.Dispose()
        {
        }
    }


    /// <summary>
    /// Rendering logic
    /// TODO: extract SceneLogic elswhere
    /// </summary>
    internal class Renderer
    {
        IDisplayer displayer;
        Context contexts;
        RenderTextureCache m_RenderTextures = new RenderTextureCache();
        //PreviewRenderUtility previewUtility;
        Stage stage;

        GUIStyle background = "IN BigTitle inner";

        public Renderer(
            IDisplayer displayer,
            Context contexts)
        {
            this.displayer = displayer;
            this.contexts = contexts;

            //previewUtility = new PreviewRenderUtility();
            stage = new Stage("LookDevViewA");

            EditorApplication.update += Render;
        }

        bool cleaned = false;
        internal void CleanUp()
        {
            if (!cleaned)
            {
                cleaned = true;
                EditorApplication.update -= Render;
                //previewUtility.Cleanup();

            }
        }
        ~Renderer() => CleanUp();

        public void UpdateScene()
        {
            //previewUtility.Cleanup();
            //previewUtility = new PreviewRenderUtility();
            stage.Clear();
            var viewContent = contexts.GetViewContent(ViewIndex.FirstOrFull);
            if (viewContent == null)
            {
                viewContent.prefabInstanceInPreview = null;
                return;
            }

            //var obj =
            //    //PrefabUtility.LoadPrefabContentsIntoPreviewScene()
            //    previewUtility.InstantiatePrefabInScene(viewContent.contentPrefab);
            //obj.transform.position = Vector3.zero;
            //obj.transform.rotation = Quaternion.identity;
            //viewContent.prefabInstanceInPreview = obj;

            viewContent.prefabInstanceInPreview =
                stage.InstantiateInStage(viewContent.contentPrefab);
        }

        public void Render()
        {
            switch (contexts.layout.viewLayout)
            {
                case Layout.FullA:
                    RenderSingle(ViewIndex.FirstOrFull);
                    break;
                case Layout.FullB:
                    RenderSingle(ViewIndex.Second);
                    break;
                case Layout.HorizontalSplit:
                case Layout.VerticalSplit:
                    RenderSideBySide();
                    break;
                case Layout.CustomSplit:
                case Layout.CustomCircular:
                    RenderDualView();
                    break;
            }
        }

        bool IsNullArea(Rect r)
            => r.width == 0 || r.height == 0
            || float.IsNaN(r.width) || float.IsNaN(r.height);

        void RenderSingle(ViewIndex index)
        {
            Rect rect = displayer.GetRect(index);
            if (IsNullArea(rect))
                return;

            var cameraState = contexts.GetCameraState(index);
            var viewContext = contexts.GetViewContent(index);

            m_RenderTextures.UpdateSize(rect, (ViewCompositionIndex)index);

            var texture = RenderScene(
                rect,
                cameraState,
                viewContext);

            displayer.SetTexture(ViewIndex.FirstOrFull, texture);
        }

        void RenderSideBySide()
        {

        }

        void RenderDualView()
        {

        }

        private Texture RenderScene(Rect previewRect, CameraState cameraState, ViewContext context)
        {
            previewUtility.BeginPreview(previewRect, displayer.backgroundStyle);

            previewUtility.camera.renderingPath = RenderingPath.DeferredShading;
            previewUtility.camera.backgroundColor = Color.white;
            previewUtility.camera.allowHDR = true;

            previewUtility.camera.transform.position = cameraState.position;
            previewUtility.camera.transform.rotation = cameraState.rotation.value;
            previewUtility.camera.fieldOfView = cameraState.fov;
            previewUtility.camera.nearClipPlane = 0f;
            previewUtility.camera.farClipPlane = cameraState.farClip;
            previewUtility.camera.aspect = previewRect.width / previewRect.height;


            //for (int lightIndex = 0; lightIndex < 2; lightIndex++)
            //{
            //    previewUtility.lights[lightIndex].enabled = false;
            //    previewUtility.lights[lightIndex].intensity = 0.0f;
            //    previewUtility.lights[lightIndex].shadows = LightShadows.None;
            //}

            //previewUtility.ambientColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            //RenderSettings.ambientIntensity = 1.0f; // fix this to 1, this parameter should not exist!
            //RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
            //RenderSettings.reflectionIntensity = 1.0f;

            //if (sceneContent != null)
            //{
            //    foreach (Renderer renderer in sceneContent.GetComponentsInChildren<Renderer>())
            //        renderer.enabled = true;
            //}

            previewUtility.Render(true, false);

            //if (sceneContent != null)
            //{
            //    foreach (Renderer renderer in sceneContent.GetComponentsInChildren<Renderer>())
            //        renderer.enabled = false;
            //}

            return previewUtility.EndPreview();
        }

    }
}
