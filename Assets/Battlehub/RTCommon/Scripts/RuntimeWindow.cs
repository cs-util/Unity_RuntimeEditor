﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Battlehub.RTCommon
{
    public enum RuntimeWindowType
    {
        None = 0,
        Game = 1,
        Scene = 2,
        Hierarchy = 3,
        Project = 4,
        ProjectTree = 5,
        ProjectFolder = 6,
        Inspector = 7,
        Console = 8,
        Animation = 9,

        ToolsPanel = 21,

        ImportFile = 50,
        OpenProject = 51,
        SelectAssetLibrary = 52,
        ImportAssets = 53,
        SaveScene = 54,
        About = 55,
        SaveAsset = 56,
        SaveFile = 70,
        OpenFile = 72,
                
        SelectObject = 101,
        SelectColor = 102,
        SelectAnimationProperties = 109,

        Custom = 1 << 16,
    }

    public enum RenderTextureUsage
    {
        UsePipelineSettings,
        Off,
        On
    }

    [DefaultExecutionOrder(-60)]
    public class RuntimeWindow : DragDropTarget
    {
        public event Action CameraResized;

        [SerializeField]
        private RenderTextureUsage m_renderTextureUsage = RenderTextureUsage.UsePipelineSettings;
        public RenderTextureUsage RenderTextureUsage
        {
            get { return m_renderTextureUsage; }
            set { m_renderTextureUsage = value; }
        }

        [SerializeField]
        private bool m_activateOnAnyKey = false;
        public bool ActivateOnAnyKey
        {
            get { return m_activateOnAnyKey; }
            set { m_activateOnAnyKey = true; }
        }

        [SerializeField]
        private bool m_canActivate = true;
        public bool CanActivate
        {
            get { return m_canActivate; }
            set { m_canActivate = value; }
        }

        private bool m_isActivated;
        private IOCContainer m_container = new IOCContainer();
        public IOCContainer IOCContainer
        {
            get { return m_container; }
        }

        [SerializeField]
        private RuntimeWindowType m_windowType = RuntimeWindowType.Scene;
        public virtual RuntimeWindowType WindowType
        {
            get { return m_windowType; }
            set
            {
                if(m_windowType != value)
                {
                    m_index = Editor.GetIndex(value);
                    m_windowType = value;
                }
            }
        }

        private int m_index;
        public virtual int Index
        {
            get { return m_index; }
        }

        private int m_depth;
        public int Depth
        {
            get { return m_depth; }
            set { m_depth = value; }
        }

        private Vector3 m_position;
        private Rect m_rect;
        private RectTransform m_rectTransform;
        private CanvasGroup m_canvasGroup;        
        protected CanvasGroup CanvasGroup
        {
            get { return m_canvasGroup; }
        }

        private Canvas m_canvas;
        protected Canvas Canvas
        {
            get { return m_canvas; }
        }
        [SerializeField]
        private Image m_background;
        public Image Background
        {
            get { return m_background; }
        }

        [SerializeField]
        protected Camera m_camera;
        public virtual Camera Camera
        {
            get { return m_camera; }
            set
            {
                if(m_camera == value)
                {
                    return;
                }

                if(m_camera != null)
                {
                    ResetCullingMask();

                    UnregisterGraphicsCamera();
                }

                m_camera = value;
                if(m_camera != null)
                {
                    SetCullingMask();
                    if (WindowType == RuntimeWindowType.Scene)
                    {
                        RegisterGraphicsCamera();
                    }

                    RenderPipelineInfo.XRFix(Camera);
                }
            }
        }

     

        private int m_cameraDepth;
        public int CameraDepth
        {
            get { return m_cameraDepth; }
        }

        public virtual void SetCameraDepth(int depth)
        {
            m_cameraDepth = depth;
            if (m_camera != null)
            {
                m_camera.depth = m_cameraDepth;
            }
        }

        [SerializeField]
        private Pointer m_pointer;
        public virtual Pointer Pointer
        {
            get { return m_pointer; }
        }

        public override bool IsPointerOver
        {
            get { return base.IsPointerOver; }
            set
            {
                if (base.IsPointerOver != value)
                {
                    if (value)
                    {
                        Editor.SetPointerOverWindow(this);
                    }
                    else
                    {
                        Editor.SetPointerOverWindow(null);
                    }
                    base.IsPointerOver = value;
                }
            }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            if (Camera != null)
            {
                Image windowBackground = GetComponent<Image>();
                if (windowBackground != null)
                {
                    Color color = windowBackground.color;
                    color.a = 0;
                    windowBackground.color = color;
                }

                if (RenderTextureUsage == RenderTextureUsage.Off || RenderTextureUsage == RenderTextureUsage.UsePipelineSettings && !RenderPipelineInfo.UseRenderTextures)
                {
                    RenderTextureCamera renderTextureCamera = Camera.GetComponent<RenderTextureCamera>();
                    if (renderTextureCamera != null)
                    {
                        DestroyImmediate(renderTextureCamera);
                    }
                }

                RenderPipelineInfo.XRFix(Camera);
            }
        

            if(m_background == null)
            {
                if(!Editor.IsVR)
                {
                    m_background = GetComponent<Image>();
                    if(m_background == null)
                    {
                        m_background = gameObject.AddComponent<Image>();
                        m_background.color = new Color(0, 0, 0, 0);
                        m_background.raycastTarget = true;
                    }
                    else
                    {
                        m_background.raycastTarget = true;
                    }
                }
            }

            if (m_pointer == null)
            {
                if(Editor.IsVR)
                {
                    //m_pointer = gameObject.AddComponent<VRPointer>();
                }
                else
                {
                    m_pointer = gameObject.AddComponent<Pointer>();
                }
            }
            
            m_rectTransform = GetComponent<RectTransform>();
            m_canvas = GetComponentInParent<Canvas>();
            m_canvasGroup = GetComponent<CanvasGroup>();
            if(m_canvasGroup == null)
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if(m_canvasGroup != null)
            {
                m_canvasGroup.blocksRaycasts = true;
                m_canvasGroup.ignoreParentGroups = true;
            }
            
            Editor.ActiveWindowChanged += OnActiveWindowChanged;

            if(WindowType != RuntimeWindowType.Custom)
            {
                m_index = Editor.GetIndex(WindowType);
            }
            else
            {
                m_index = 0;
            }
            
            if (m_camera != null)
            {
                SetCullingMask();
                if (WindowType == RuntimeWindowType.Scene)
                {
                    RegisterGraphicsCamera();
                }
            }

            Editor.RegisterWindow(this);
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            if(Editor != null)
            {
                Editor.ActiveWindowChanged -= OnActiveWindowChanged;
                Editor.UnregisterWindow(this);
            }

            if (m_camera != null)
            {
                ResetCullingMask();
                if (WindowType == RuntimeWindowType.Scene)
                {
                    UnregisterGraphicsCamera();
                }
            }
        }

        private void RegisterGraphicsCamera()
        {
            IRTEGraphics graphics = IOC.Resolve<IRTEGraphics>();
            if (graphics != null)
            {
                graphics.RegisterCamera(m_camera);
            }
        }

        private void UnregisterGraphicsCamera()
        {
            IRTEGraphics graphics = IOC.Resolve<IRTEGraphics>();
            if (graphics != null)
            {
                graphics.UnregisterCamera(m_camera);
            }
        }

        protected virtual void OnEnable()
        {
            TryResize();
        }

        protected virtual void OnDisable()
        {

        }

        protected virtual void Update()
        {
            UpdateOverride();
        }

        protected virtual void UpdateOverride()
        {
            TryResize();
        }

        private void TryResize()
        {
            if (m_camera != null && m_rectTransform != null)
            {
                if (m_rectTransform.rect != m_rect || m_rectTransform.position != m_position)
                {
                    HandleResize();

                    m_rect = m_rectTransform.rect;
                    m_position = m_rectTransform.position;
                }
            }
        }

        private void OnTransformParentChanged()
        {
            EnableRaycasts();
        }


        public void EnableRaycasts()
        {
            if(m_canvasGroup != null)
            {
                m_canvasGroup.blocksRaycasts = true;
            }
            
        }

        public void DisableRaycasts()
        {
            if(!m_isActivated)
            {
                if (m_canvasGroup != null)
                {
                    m_canvasGroup.blocksRaycasts = false;
                }
            }
        }

        protected virtual void OnActiveWindowChanged(RuntimeWindow deactivatedWindow)
        {
            if (Editor.ActiveWindow == this)
            {
                if (!m_isActivated)
                {                    
                    m_isActivated = true;
                    if(WindowType == RuntimeWindowType.Game)
                    {
                        if(m_background != null)
                        {
                            m_background.raycastTarget = false;  // allow to interact with world space ui
                        }
                    }
                    OnActivated();
                }
            }
            else
            {
                if (m_isActivated)
                {   
                    m_isActivated = false;
                    if(m_background != null)
                    {
                        m_background.raycastTarget = true;
                    }
                    OnDeactivated();
                }
            }
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
        }

        public virtual void HandleResize()
        {
            if (m_camera == null)
            {
                return;
            }
            if (m_rectTransform != null && m_canvas != null)
            {
                if (m_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    Vector3[] corners = new Vector3[4];
                    m_rectTransform.GetWorldCorners(corners);
                    ResizeCamera(new Rect(corners[0], new Vector2(corners[2].x - corners[0].x, corners[1].y - corners[0].y)));
                }
                else if (m_canvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    if (m_canvas.worldCamera != Camera)
                    {
                        Vector3[] corners = new Vector3[4];
                        m_rectTransform.GetWorldCorners(corners);

                        corners[0] = RectTransformUtility.WorldToScreenPoint(m_canvas.worldCamera, corners[0]);
                        corners[1] = RectTransformUtility.WorldToScreenPoint(m_canvas.worldCamera, corners[1]);
                        corners[2] = RectTransformUtility.WorldToScreenPoint(m_canvas.worldCamera, corners[2]);
                        corners[3] = RectTransformUtility.WorldToScreenPoint(m_canvas.worldCamera, corners[3]);

                        Vector2 size = new Vector2(corners[2].x - corners[0].x, corners[1].y - corners[0].y);
                        ResizeCamera(new Rect(corners[0], size));
                    }
                }
            }
        }

        protected virtual void ResizeCamera(Rect pixelRect)
        {
            m_camera.pixelRect = pixelRect;
            if (CameraResized != null)
            {
                CameraResized();
            }            
        }

        protected virtual void OnActivated()
        {
        }

        protected virtual void OnDeactivated()
        {
        }

        protected virtual void SetCullingMask()
        {
            SetCullingMask(m_camera);
        }

      
        protected virtual void ResetCullingMask()
        {
            ResetCullingMask(m_camera);
        }

        protected virtual void SetCullingMask(Camera camera)
        {
            CameraLayerSettings settings = Editor.CameraLayerSettings;
            camera.cullingMask &= (settings.RaycastMask | 1 << settings.AllScenesLayer);
        }

        protected virtual void ResetCullingMask(Camera camera)
        {
            CameraLayerSettings settings = Editor.CameraLayerSettings;
            camera.cullingMask |= ~(settings.RaycastMask | 1 << settings.AllScenesLayer);
        }
    }
}
