﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Battlehub.RTCommon;
using Battlehub.Utils;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Battlehub.RTHandles
{
    [System.Serializable]
    public class BaseHandleUnityEvent : UnityEvent<BaseHandle> { }

    /// <summary>
    /// Base class for all handles (Position, Rotation and Scale)
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public abstract class BaseHandle : RTEComponent
    {
        private const float SelectionMarginPixels = 10;
        /// <summary>
        /// current size of grid 
        /// </summary>
        protected float EffectiveGridUnitSize
        {
            get;
            private set;
        }

        /// <summary>
        /// HighlightOnHover
        /// </summary>
        public bool HightlightOnHover = true;
        public bool EnableUndo = true;
        public RuntimeHandlesHitTester HitTester;
        public RuntimeHandlesComponent Appearance;
        /// <summary>
        /// Configurable model
        /// </summary>
        public BaseHandleModel Model;

        private LockObject m_lockObject;
        public virtual LockObject LockObject
        {
            get { return m_lockObject; }
            set
            {
                m_lockObject = value;
                if(m_lockObject != null && Editor != null && Editor.Tools.LockAxes != null)
                {
                    m_lockObject.SetGlobalLock(Editor.Tools.LockAxes);
                }
                
                if (Model != null && !Model.gameObject.IsPrefab())
                {
                    Model.SetLock(LockObject);
                }
            }
        }

        public virtual Vector3 Position
        {
            get { return transform.position; }
            protected set { transform.position = value; }
        }

        /// <summary>
        /// Target objects which will be affected by handle (for example if m_targets array contains O1 and O2 objects and O1 is parent of O2 then m_activeTargets array will contain only O1 object)
        /// </summary>
        private Transform[] m_activeTargets;
        public virtual Transform[] ActiveTargets
        {
            get { return m_activeTargets; }
        }

        private Transform[] m_activeRealTargets;
        protected virtual Transform[] ActiveRealTargets
        {
            get { return m_activeRealTargets; }
        }
        private Transform[] m_realTargets;
        public virtual Transform[] RealTargets
        {
            get
            {
                if(m_realTargets == null)
                {
                    return Targets;
                }
                return m_realTargets;
            }
        }

        private Transform[] m_commonCenter;
        private Transform[] m_commonCenterTarget;

        private static List<BaseHandle> m_allHandles = new List<BaseHandle>();
        private BaseHandleInput m_input;

        private void GetActiveRealTargets()
        {
            if(m_realTargets == null)
            {
                m_activeRealTargets = null;
                return;
            }

            m_realTargets = m_realTargets.Where(t => t != null && ((t.hideFlags & HideFlags.DontSave) == 0)).ToArray();
            HashSet<Transform> targetsHS = new HashSet<Transform>();
            for (int i = 0; i < m_realTargets.Length; ++i)
            {
                if (m_realTargets[i] != null && !targetsHS.Contains(m_realTargets[i]))
                {
                    targetsHS.Add(m_realTargets[i]);
                }
            }
            m_realTargets = targetsHS.ToArray();
            if (m_realTargets.Length == 0)
            {
                m_activeRealTargets = new Transform[0];
                return;
            }
            else if (m_realTargets.Length == 1)
            {
                m_activeRealTargets = new[] { m_realTargets[0] };
            }

            for (int i = 0; i < m_realTargets.Length; ++i)
            {
                Transform target = m_realTargets[i];
                Transform p = target.parent;
                while (p != null)
                {
                    if (targetsHS.Contains(p))
                    {
                        targetsHS.Remove(target);
                        break;
                    }

                    p = p.parent;
                }
            }

            m_activeRealTargets = targetsHS.ToArray();
        }
        /// <summary>
        /// All Target objects
        /// </summary>
        [SerializeField]
        private Transform[] m_targets;
        public virtual Transform[] Targets
        {
            get
            {
                return Targets_Internal;
            }
            set
            {
                DestroyCommonCenter(true);
                m_realTargets = value;
                GetActiveRealTargets();
                Targets_Internal = value;
                if (Targets_Internal == null || Targets_Internal.Length == 0)
                {
                    return;
                }

                if (PivotMode == RuntimePivotMode.Center && ActiveTargets.Length > 1)
                {
                    Vector3 centerPosition = GetCommonCenterPosition();
                    m_commonCenter = new Transform[1];
                    m_commonCenter[0] = new GameObject { name = "CommonCenter" }.transform;
                    m_commonCenter[0].SetParent(transform.parent, true);
                    m_commonCenter[0].position = centerPosition;
                    m_commonCenter[0].rotation = Rotation;
                    m_commonCenterTarget = new Transform[m_realTargets.Length];
                    for (int i = 0; i < m_commonCenterTarget.Length; ++i)
                    {
                        GameObject target = new GameObject { name = "ActiveTarget " + m_realTargets[i].name };
                        target.transform.SetParent(m_commonCenter[0]);

                        target.transform.position = m_realTargets[i].position;
                        target.transform.rotation = m_realTargets[i].rotation;
                        target.transform.localScale = m_realTargets[i].localScale;

                        m_commonCenterTarget[i] = target.transform;
                    }
                    LockObject lockObject = LockObject;
                    Targets_Internal = m_commonCenter;
                    LockObject = lockObject;
                }
            }
        }

        protected virtual RuntimePivotMode PivotMode
        {
            get
            {
                LockObject lockObject = LockObject;
                if(lockObject != null && lockObject.PivotMode != null)
                {
                    return lockObject.PivotMode.Value;
                }

                return Editor.Tools.PivotMode;
            }
        }

        protected virtual RuntimePivotRotation PivotRotation
        {
            get
            {
                LockObject lockObject = LockObject;
                if (lockObject != null && lockObject.PivotRotation != null)
                {
                    return lockObject.PivotRotation.Value;
                }

                return Editor.Tools.PivotRotation;
            }
        }

        protected virtual Vector3 GetCommonCenterPosition()
        {
            Vector3 centerPosition = GetCenterPosition(Targets_Internal[0]);
            for (int i = 1; i < Targets_Internal.Length; ++i)
            {
                Transform target = Targets_Internal[i];
                centerPosition += GetCenterPosition(target);
            }

            centerPosition = centerPosition / Targets_Internal.Length;
            return centerPosition;
        }

        public virtual void Refresh()
        {
            if (m_commonCenter != null && m_commonCenter.Length > 0)
            {
                if (RealTargets == null || RealTargets.Length == 0)
                {
                    return;
                }

                Vector3 centerPosition = GetCenterPosition(RealTargets[0]);
                for (int i = 1; i < RealTargets.Length; ++i)
                {
                    Transform target = RealTargets[i];
                    centerPosition += GetCenterPosition(target);
                }

                centerPosition = centerPosition / RealTargets.Length;

                m_commonCenter[0].position = centerPosition;
                m_commonCenter[0].rotation = Rotation;

                for (int i = 0; i < m_allHandles.Count; ++i)
                {
                    BaseHandle handle = m_allHandles[i];
                    if (handle.Editor == Editor && handle.gameObject.activeSelf)
                    {
                        handle.m_commonCenter[0].position = m_commonCenter[0].position;
                        handle.m_commonCenter[0].rotation = m_commonCenter[0].rotation;
                        handle.m_commonCenter[0].localScale = m_commonCenter[0].localScale;
                    }

                    transform.position = m_commonCenter[0].position;
                }

                if(Model != null)
                {
                    SyncModelTransform();    
                }
            }
        }

        protected virtual Vector3 GetCenterPosition(Transform target)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                return target.TransformPoint(filter.sharedMesh.bounds.center);
            }

            SkinnedMeshRenderer smr = target.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                return target.TransformPoint(smr.sharedMesh.bounds.center);
            }

            return target.position;
        }

        protected virtual Transform[] Targets_Internal
        {
            get { return m_targets; }
            set
            {
             
                m_targets = value;
                if(m_targets == null)
                {
                    LockObject = LockAxes.Eval(null);
                    m_activeTargets = null;
                    return;
                }

                m_targets = m_targets.Where(t => t != null && ((t.hideFlags & HideFlags.DontSave) == 0)).ToArray();
                HashSet<Transform> targetsHS = new HashSet<Transform>();
                for (int i = 0; i < m_targets.Length; ++i)
                {
                    if (m_targets[i] != null && !targetsHS.Contains(m_targets[i]))
                    {
                        targetsHS.Add(m_targets[i]);
                    }
                }
                m_targets = targetsHS.ToArray();
                if (m_targets.Length == 0)
                {
                    LockObject = LockAxes.Eval(new LockAxes[0]);
                    m_activeTargets = new Transform[0];
                    return;
                }
                else if(m_targets.Length == 1)
                {
                    m_activeTargets = new [] { m_targets[0] };
                }

                for(int i = 0; i < m_targets.Length; ++i)
                {
                    Transform target = m_targets[i];
                    Transform p = target.parent;
                    while(p != null)
                    {
                        if(targetsHS.Contains(p))
                        {
                            targetsHS.Remove(target);
                            break;
                        }

                        p = p.parent;
                    }
                }

                m_activeTargets = targetsHS.ToArray();
                LockObject = LockAxes.Eval(m_activeTargets.Where(t => t.GetComponent<LockAxes>() != null).Select(t => t.GetComponent<LockAxes>()).ToArray());
                if(m_activeTargets.Any(target => target.gameObject.isStatic))
                {
                    LockObject = new LockObject();
                    LockObject.PositionX = LockObject.PositionY = LockObject.PositionZ = true;
                    LockObject.RotationX = LockObject.RotationY = LockObject.RotationZ = true;
                    LockObject.ScaleX = LockObject.ScaleY = LockObject.ScaleZ = true;
                    LockObject.RotationScreen = true;
                    LockObject.RotationFree = true;
                }

                if(m_activeTargets != null && m_activeTargets.Length > 0)
                {
                    transform.position = m_activeTargets[0].position;
                }

                if(IsAwaked && Model != null)
                {
                    SyncModelTransform();
                }
            }
        }

        public Transform Target
        {
            get
            {
                if(Targets == null || Targets.Length == 0)
                {
                    return null;
                }
                return Targets[0];
            }
        }

        /// <summary>
        /// Selected axis
        /// </summary>
        private RuntimeHandleAxis m_selectedAxis;

        /// <summary>
        /// Whether drag operation in progress
        /// </summary>
        private bool m_isDragging;
        
        /// <summary>
        /// Drag plane
        /// </summary>
        private Plane m_dragPlane;

        public virtual bool IsDragging
        {
            get { return m_isDragging; }
        }

        /// <summary>
        /// Tool type
        /// </summary>
        public virtual RuntimeTool Tool
        {
            get { return RuntimeTool.Custom; }
        }

        /// <summary>
        /// Quaternion Rotation based on selected coordinate system (local or global)
        /// </summary>
        protected virtual Quaternion Rotation
        {
            get
            {
                if(ActiveRealTargets == null || ActiveRealTargets.Length <= 0 || ActiveRealTargets == null)
                {
                    return Quaternion.identity;
                }
                
                return PivotRotation == RuntimePivotRotation.Local ? ActiveRealTargets[0].rotation : Quaternion.identity;
            }
        }

        protected virtual RuntimeHandleAxis SelectedAxis
        {
            get { return m_selectedAxis; }
            set
            {
                if(m_selectedAxis != value)
                {
                    m_selectedAxis = value;
                    if (Model != null)
                    {
                        Model.Select(SelectedAxis);
                    }
                    else
                    {
                        if(m_rteCamera != null)
                        {
                            m_rteCamera.RefreshCommandBuffer();
                        }
                    }
                }
            }
        }

        protected virtual Plane DragPlane
        {
            get { return m_dragPlane; }
            set { m_dragPlane = value; }
        }

        protected virtual float CurrentGridUnitSize
        {
            get { return 0.0f; }
        }

        private bool m_unitSnapping;
        public virtual bool UnitSnapping
        {
            get { return m_unitSnapping; }
            set { m_unitSnapping = value; }
        }

        public virtual bool SnapToGrid
        {
            get;
            set;
        }

        public virtual float SizeOfGrid
        {
            get;
            set;
        }

        public BaseHandleUnityEvent BeforeDrag = new BaseHandleUnityEvent();
        public BaseHandleUnityEvent Drag = new BaseHandleUnityEvent();
        public BaseHandleUnityEvent Drop = new BaseHandleUnityEvent();

        private IRTECamera m_rteCamera;
        protected IRTECamera RTECamera
        {
            get { return m_rteCamera; }
        }

        private Vector3 m_prevScale;
        private Vector3 m_prevCamPosition;
        private Quaternion m_prevCamRotation;
        private bool m_prevCamOrthographic;
        private float m_prevCamOrthographicsSize;
        private Rect m_prevCamRect;

        private bool m_refreshOnCameraChanged = true;
        protected bool RefreshOnCameraChanged
        {
            get { return m_refreshOnCameraChanged; }
            set { m_refreshOnCameraChanged = value; }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();

            m_allHandles.Add(this);

            RuntimeHandlesComponent.InitializeIfRequired(ref Appearance);
            RuntimeHandlesHitTester.InitializeIfRequired(Window, ref HitTester);
          
            if (m_targets != null && m_targets.Length > 0 )
            {
                var lockObject = LockObject;
                if(m_commonCenter == null || m_commonCenter.Length == 0 || m_commonCenter[0] != m_targets[0])
                {
                    Targets = m_targets;
                }
                if(lockObject != null)
                {
                    LockObject = lockObject;
                }
            }

            if (Targets == null || Targets.Length == 0)
            {
                var lockObject = LockObject;
                Targets = new[] { transform };
                if(lockObject != null)
                {
                    LockObject = lockObject;
                }
            }

            if (Model != null)
            {
                bool activeSelf = Model.gameObject.activeSelf;
                Model.gameObject.SetActive(false);
                BaseHandleModel model = Instantiate(Model, transform.parent);
                
                model.name = Model.name;
                model.Appearance = Appearance;
                model.Window = Window;

                Model.gameObject.SetActive(activeSelf);

                if(enabled)
                {
                    model.gameObject.SetActive(true);
                    Model = model;
                    Model.SetLock(LockObject);
                }
                else
                {
                    Model = model;
                }
            
                Model.ModelScale = Appearance.HandleScale;
                Model.SelectionMargin = Appearance.SelectionMargin;
            }
        }

        private void Start()
        {
            m_input = GetComponent<BaseHandleInput>();
            if (m_input == null || m_input.Handle != this)
            {
                m_input = gameObject.AddComponent<BaseHandleInput>();
                m_input.Handle = this;
            }

            IRTEGraphicsLayer graphicsLayer = Window.IOCContainer.Resolve<IRTEGraphicsLayer>();
            if (graphicsLayer != null)
            {
                m_rteCamera = graphicsLayer.Camera;
            }

            if (m_rteCamera == null && Window.Camera != null)
            {
                IRTEGraphics graphics = IOC.Resolve<IRTEGraphics>();
                if(graphics != null)
                {
                    m_rteCamera = graphics.GetOrCreateCamera(Window.Camera, CameraEvent.AfterImageEffectsOpaque);
                }
                
                if (m_rteCamera == null)
                {
                    m_rteCamera = Window.Camera.gameObject.AddComponent<RTECamera>();
                    m_rteCamera.Event = CameraEvent.AfterImageEffectsOpaque;
                }
            }

            if (Model == null && m_rteCamera != null)
            {
                m_prevScale = transform.localScale;

                m_prevCamPosition = m_rteCamera.Camera.transform.position;
                m_prevCamRotation = m_rteCamera.Camera.transform.rotation;
                m_prevCamOrthographic = m_rteCamera.Camera.orthographic;
                m_prevCamOrthographicsSize = m_rteCamera.Camera.orthographicSize;
                m_prevCamRect = m_rteCamera.Camera.rect;

                m_rteCamera.CommandBufferRefresh += OnCommandBufferRefresh;
                m_rteCamera.RefreshCommandBuffer();
            }

            OnStartOverride();
        }

        protected virtual void OnStartOverride()
        {

        }

        private void OnEnable()
        {
            Editor.Tools.PivotRotationChanged += OnPivotRotationChanged;
            Editor.Tools.PivotModeChanged += OnPivotModeChanged;
            Editor.Tools.ToolChanged += OnRuntimeToolChanged;
            Editor.Tools.LockAxesChanged += OnLockAxesChanged;
            Editor.Undo.UndoCompleted += OnUndoCompleted;
            Editor.Undo.RedoCompleted += OnRedoCompleted;

            OnEnableOverride();

            if(HitTester != null)
            {
                HitTester.Add(this);
            }
           
            if(m_input != null)
            {
                m_input.enabled = true;
            }

            if (Model != null)
            {
                SyncModelTransform();
                Model.gameObject.SetActive(true);
                if (!Model.gameObject.IsPrefab())
                {
                    Model.SetLock(LockObject);
                }
            }
            else
            {
                if (m_rteCamera != null)
                {
                    m_rteCamera.CommandBufferRefresh += OnCommandBufferRefresh;
                    m_rteCamera.RefreshCommandBuffer();
                }
            }
        }

        protected virtual void OnEnableOverride()
        {

        }

        private void OnDisable()
        {
            if (m_rteCamera != null)
            {
                m_rteCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                m_rteCamera.RefreshCommandBuffer();
            }

            if (HitTester != null)
            {
                HitTester.Remove(this);
            }

            if (Editor != null)
            {
                Editor.Tools.PivotModeChanged -= OnPivotModeChanged;
                Editor.Tools.PivotRotationChanged -= OnPivotRotationChanged;
                Editor.Tools.ToolChanged -= OnRuntimeToolChanged;
                Editor.Tools.LockAxesChanged -= OnLockAxesChanged;
                Editor.Undo.UndoCompleted -= OnUndoCompleted;
                Editor.Undo.RedoCompleted -= OnRedoCompleted;
            }

            DestroyCommonCenter(false);

            if (Model != null)
            {
                Model.gameObject.SetActive(false);
            }

            if (Editor != null && Editor.Tools != null && Editor.Tools.ActiveTool == this)
            {
                Editor.Tools.ActiveTool = null;
            }

            if (m_input != null)
            {
                m_input.enabled = false;
            }

            OnDisableOverride();
        }

        protected virtual void OnDisableOverride()
        {
            
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            m_allHandles.Remove(this);

            if (m_input != null && m_input.Handle == this)
            {
                Destroy(m_input);
            }

            if (m_rteCamera != null)
            {
                m_rteCamera.CommandBufferRefresh -= OnCommandBufferRefresh;
                m_rteCamera.RefreshCommandBuffer();
            }

            DestroyCommonCenter(false);

            if (Model != null && Model.gameObject != null)
            {
                if (!Model.gameObject.IsPrefab())
                {
                    Destroy(Model.gameObject);
                }
            }

            if (Editor != null && Editor.Tools != null && Editor.Tools.ActiveTool == this)
            {
                Editor.Tools.ActiveTool = null;
            }
        }

        private void OnTransformParentChanged()
        {
            OnTransformParentChangedOverride();
        }

        protected virtual void OnTransformParentChangedOverride()
        {
            if(Model != null && !Model.gameObject.IsPrefab())
            {
                Model.transform.SetParent(transform.parent, true);
                if (IsAwaked)
                {
                    SyncModelTransform();
                }
            }
        }

        protected override void OnWindowDeactivated()
        {
            base.OnWindowDeactivated();

            EndDrag();

            if (Editor != null && Editor.Tools != null && Editor.Tools.ActiveTool == this)
            {
                Editor.Tools.ActiveTool = null;
            }
        }

        private void DestroyCommonCenter(bool destroyImmediate)
        {
            if (m_commonCenter != null)
            {
                for (int i = 0; i < m_commonCenter.Length; ++i)
                {
                    if(m_commonCenter[i])
                    {
                        if(destroyImmediate)
                        {
                            DestroyImmediate(m_commonCenter[i].gameObject);
                        }
                        else
                        {
                            Destroy(m_commonCenter[i].gameObject);
                        }
                        
                    }
                    
                }
            }

            if (m_commonCenterTarget != null)
            {
                for (int i = 0; i < m_commonCenterTarget.Length; ++i)
                {
                    if(m_commonCenterTarget[i])
                    {
                        if (destroyImmediate)
                        {
                            DestroyImmediate(m_commonCenterTarget[i].gameObject);
                        }
                        else
                        {
                            Destroy(m_commonCenterTarget[i].gameObject);
                        }
                    }
                }
            }

            m_commonCenter = null;
            m_commonCenterTarget = null;
        }

        protected virtual void Update()
        {
            if(Model != null)
            {
                Model.ModelScale = Appearance.HandleScale;
                Model.SelectionMargin = Appearance.SelectionMargin;
            }

            if (m_isDragging)
            {
                if (Editor.Tools.IsViewing)
                {
                    m_isDragging = false;
                }
                else
                {
                    if (m_unitSnapping)
                    {
                        EffectiveGridUnitSize = CurrentGridUnitSize;
                    }
                    else
                    {
                        EffectiveGridUnitSize = 0;
                    }

                    OnDrag();
                }
            }
            else
            {
                if (!Window.IsPointerOver)
                {
                    SelectedAxis = RuntimeHandleAxis.None;
                }
            }
            
            UpdateOverride();

            if (m_isDragging)
            {
                if (PivotMode == RuntimePivotMode.Center && m_commonCenterTarget != null && m_realTargets != null && m_realTargets.Length > 1)
                {
                    for (int i = 0; i < m_commonCenterTarget.Length; ++i)
                    {
                        Transform commonCenterTarget = m_commonCenterTarget[i];
                        Transform target = m_realTargets[i];
                        target.transform.position = commonCenterTarget.position;
                        target.transform.rotation = commonCenterTarget.rotation;
                        target.transform.localScale = commonCenterTarget.lossyScale;
                    }
                }

                if (Drag != null)
                {
                    Drag.Invoke(this);
                }

                if (m_commonCenter != null && m_commonCenter.Length > 0)
                {
                    for (int i = 0; i < m_allHandles.Count; ++i)
                    {
                        BaseHandle handle = m_allHandles[i];
                        if (handle.Editor == Editor && handle.gameObject.activeSelf)
                        {
                            handle.m_commonCenter[0].position = m_commonCenter[0].position;
                            handle.m_commonCenter[0].rotation = m_commonCenter[0].rotation;
                            handle.m_commonCenter[0].localScale = m_commonCenter[0].localScale;
                        }
                    }
                }
            }

            if(Window != null)
            {
                if (Model != null)
                {
                    SyncModelTransform();
                }
            }
        }

        protected virtual void UpdateOverride()
        {
            //bool refreshCommandBuffer = false;
            Transform target = Targets != null && Targets.Length > 0 && Targets[0] != null ? Targets[0] : null;
            if (target != null && (target.position != transform.position || target.rotation != transform.rotation || target.localScale != m_prevScale))
            {
                m_prevScale = transform.localScale;
                if (IsDragging)
                {
                    Vector3 offset = transform.position - Targets[0].position;
                    for (int i = 0; i < ActiveTargets.Length; ++i)
                    {
                        if (ActiveTargets[i] != null)
                        {
                            ActiveTargets[i].position += offset;
                        }
                    }
                }
                else
                {
                    transform.position = target.position;
                    transform.rotation = target.rotation;
                }

                if (Model == null && m_rteCamera != null)
                {
                    m_rteCamera.RefreshCommandBuffer();
                }
            }
        }

        protected virtual void LateUpdate()
        {            
            if(!m_isDragging)
            {
                if (Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
            }

            if(Window != null)
            {
                if (Model != null)
                {
                    SyncScale();
                }
                else
                {
                    if (m_refreshOnCameraChanged)
                    {
                        Camera camera = m_rteCamera.Camera;
                        if (m_prevCamPosition != camera.transform.position ||
                            m_prevCamRotation != camera.transform.rotation ||
                            m_prevCamOrthographic != camera.orthographic ||
                            m_prevCamOrthographicsSize != camera.orthographicSize ||
                            m_prevCamRect != camera.rect)
                        {
                            m_prevCamPosition = camera.transform.position;
                            m_prevCamRotation = camera.transform.rotation;
                            m_prevCamOrthographic = camera.orthographic;
                            m_prevCamOrthographicsSize = camera.orthographicSize;
                            m_prevCamRect = camera.rect;
                            if (m_rteCamera != null)
                            {
                                m_rteCamera.RefreshCommandBuffer();
                            }
                        }
                    }
                }
            }
        }

        protected virtual void SyncModelTransform()
        {
            Model.transform.position = Position;
            Model.transform.rotation = Rotation;
            SyncScale();
        }

        private void SyncScale()
        {
            float screenScale = RuntimeHandlesComponent.GetScreenScale(transform.position, Window.Camera);
            if (!float.IsInfinity(screenScale) && !float.IsNaN(screenScale))
            {
                screenScale = Mathf.Max(0, screenScale);

                Vector3 scale = Appearance.InvertZAxis ? new Vector3(1, 1, -1) * screenScale : Vector3.one * screenScale;
                Vector3 lossyScale = transform.lossyScale;
                lossyScale.x = 1 / Mathf.Max(0.00001f, lossyScale.x);
                lossyScale.y = 1 / Mathf.Max(0.00001f, lossyScale.y);
                lossyScale.z = 1 / Mathf.Max(0.00001f, lossyScale.z);

                Vector3 prevScale = Model.transform.localScale;
                Vector3 newScale = Vector3.Scale(scale, lossyScale);
                Model.transform.localScale = newScale;
                
                if (prevScale == Vector3.zero && prevScale != newScale)
                {
                    Model.UpdateModel();
                }
            }
        }

        public void BeginDrag()
        {
            if(Editor.Tools.IsViewing)
            {
                return;
            }

            if (!IsWindowActive )
            {
                return;
            }

            if (Editor.Tools.ActiveTool != null)
            {
                return;
            }

            if (Window.Camera != null && !Window.IsPointerOver)
            {
                return;
            }

            
            m_isDragging = OnBeginDrag();
            if (m_isDragging)
            {
                if (BeforeDrag != null)
                {
                    BeforeDrag.Invoke(this);
                }

                Editor.Tools.ActiveTool = this;
                BeginRecordTransform();
            }
            else
            {
                if(Editor.Tools.ActiveTool == this)
                {
                    Editor.Tools.ActiveTool = null;
                }
            }
        }

        public void EndDrag()
        {
            if (m_isDragging)
            {
                OnDrop();
                EndRecordTransform();
                m_isDragging = false;

                if (Model == null && m_rteCamera != null)
                {
                    m_rteCamera.RefreshCommandBuffer();
                }

                if (Drop != null)
                {
                    Drop.Invoke(this);
                }
            }
        }

        protected virtual bool OnBeginDrag()
        {
            if(!IsWindowActive)
            {
                return false;
            }

            return true;
        }

        protected virtual void OnDrag()
        {

        }

        protected virtual void OnDrop()
        {

        }

        protected virtual void OnRuntimeToolChanged()
        {
            EndDrag();
        }

        protected virtual void OnPivotModeChanged()
        {
            if (RealTargets != null)
            {
                Targets = RealTargets;
            }

            if (PivotMode != RuntimePivotMode.Center)
            {
                m_realTargets = null;   
            }
            
            if(Target != null)
            {
                transform.position = Target.position;
            }    
            
            if(Model == null && m_rteCamera != null)
            {
                m_rteCamera.RefreshCommandBuffer();
            }
        }

        private void OnPivotRotationChanged()
        {
            if (Model == null && m_rteCamera != null)
            {
                m_rteCamera.RefreshCommandBuffer();
            }

            if(m_commonCenter != null && m_commonCenter.Length > 0)
            {
                Targets = RealTargets;
            }
        }

        private void OnLockAxesChanged()
        {
            if(LockObject != null)
            {
                LockObject.SetGlobalLock(Editor.Tools.LockAxes);
            }

            if(Model != null)
            {
                if (!Model.gameObject.IsPrefab())
                {
                    Model.SetLock(LockObject);
                }
            }
            else
            {
                if(m_rteCamera != null)
                {
                    m_rteCamera.RefreshCommandBuffer();
                }
            }
        }

        protected virtual void BeginRecordTransform()
        {
            if (!EnableUndo)
            {
                return;
            }
            Editor.Undo.BeginRecord();
            for (int i = 0; i < m_activeRealTargets.Length; ++i)
            {
                Editor.Undo.BeginRecordTransform(m_activeRealTargets[i]);
            }
            Editor.Undo.EndRecord();
        }

        protected virtual void EndRecordTransform()
        {
            if(!EnableUndo)
            {
                return;
            }
            Editor.Undo.BeginRecord();
            for (int i = 0; i < m_activeRealTargets.Length; ++i)
            {
                Editor.Undo.EndRecordTransform(m_activeRealTargets[i]);
            }
            Editor.Undo.EndRecord();
        }


        private void OnRedoCompleted()
        {
            if (PivotMode == RuntimePivotMode.Center)
            {
                if(m_realTargets != null && (m_realTargets.Length != 1 || m_realTargets[0] != transform))
                {
                    Targets = m_realTargets;
                }
            }
        }

        private void OnUndoCompleted()
        {
            if (PivotMode == RuntimePivotMode.Center)
            {
                if (m_realTargets != null && (m_realTargets.Length != 1 || m_realTargets[0] != transform))
                {
                    Targets = m_realTargets;
                }
            }
        }

        public virtual RuntimeHandleAxis HitTest(out float distance)
        {
            distance = float.PositiveInfinity;
            return RuntimeHandleAxis.None;
        }

        protected virtual bool HitCenter(out float distance)
        {
            Vector2 screenCenter = Window.Camera.WorldToScreenPoint(transform.position);
            Vector2 mouse = Window.Pointer.ScreenPoint;

            distance = (mouse - screenCenter).magnitude;
            return distance <= Appearance.SelectionMargin * SelectionMarginPixels;
        }

        protected virtual bool HitAxis(Vector3 axis, Matrix4x4 matrix, out float distanceToAxis)
        {
            axis = matrix.MultiplyVector(axis);
            Vector2 screenVectorBegin = Window.Camera.WorldToScreenPoint(transform.position);
            Vector2 screenVectorEnd = Window.Camera.WorldToScreenPoint(axis + transform.position);

            Vector3 screenVector = screenVectorEnd - screenVectorBegin;
            float screenVectorMag = screenVector.magnitude;
            screenVector.Normalize();
            if (screenVector != Vector3.zero)
            {
                return HitScreenAxis(out distanceToAxis, screenVectorBegin, screenVector, screenVectorMag);
            }
            else
            {
                Vector2 mousePosition = Window.Pointer.ScreenPoint;

                distanceToAxis = (screenVectorBegin - mousePosition).magnitude;
                bool result = distanceToAxis <= Appearance.SelectionMargin * SelectionMarginPixels;
                if (!result)
                {
                    distanceToAxis = float.PositiveInfinity;
                }
                else
                {
                    distanceToAxis = 0.0f;
                }
                return result;
            }
        }

        protected virtual bool HitScreenAxis(out float distanceToAxis, Vector2 screenVectorBegin, Vector3 screenVector, float screenVectorMag)
        {
            Vector2 perp = PerpendicularClockwise(screenVector).normalized;
            Vector2 mousePosition = Editor.Input.GetPointerXY(0);
            Vector2 relMousePositon = mousePosition - screenVectorBegin;

            distanceToAxis = Mathf.Abs(Vector2.Dot(perp, relMousePositon));
            Vector2 hitPoint = (relMousePositon - perp * distanceToAxis);
            float vectorSpaceCoord = Vector2.Dot(screenVector, hitPoint);

            float selectionMargin = Appearance.SelectionMargin * SelectionMarginPixels;
            bool result = vectorSpaceCoord <= screenVectorMag + selectionMargin && vectorSpaceCoord >= -selectionMargin && distanceToAxis <= selectionMargin;
            if (!result)
            {
                distanceToAxis = float.PositiveInfinity;
            }
            else
            {
                if (screenVectorMag < selectionMargin)
                {
                    distanceToAxis = 0.0f;
                }
            }
            return result;
        }

        protected virtual Plane GetDragPlane(Matrix4x4 matrix, Vector3 axis)
        {
            Plane plane = new Plane(matrix.MultiplyVector(axis).normalized, matrix.MultiplyPoint(Vector3.zero));
            return plane;
        }

        protected virtual Plane GetDragPlane(Vector3 axis)
        {
            Vector3 toCam;
            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(Window.Camera.transform.forward, Rotation * axis)), 1))
            {
                toCam = Window.Camera.transform.position - transform.position;
            }
            else
            {
                 toCam = Window.Camera.cameraToWorldMatrix.MultiplyVector(Vector3.forward); 
            }
            
            Plane dragPlane = new Plane(toCam.normalized, transform.position);
            return dragPlane;
        }

        protected virtual bool GetPointOnDragPlane(Vector3 screenPos, out Vector3 point)
        {
            return GetPointOnDragPlane(m_dragPlane, screenPos, out point);
        }

        protected virtual bool GetPointOnDragPlane(Plane dragPlane, Vector3 screenPos, out Vector3 point)
        {
            Ray ray = Window.Camera.ScreenPointToRay(screenPos);
            return GetPointOnDragPlane(dragPlane, ray, out point);
        }

        protected virtual bool GetPointOnDragPlane(Ray ray, out Vector3 point)
        {
            return GetPointOnDragPlane(m_dragPlane, ray, out point);
        }

        protected virtual bool GetPointOnDragPlane(Plane dragPlane, Ray ray, out Vector3 point)
        {
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private static Vector2 PerpendicularClockwise(Vector2 vector2)
        {
            return new Vector2(-vector2.y, vector2.x);
        }

        protected Vector3 GetGridOffset(float gridSize, Vector3 position)
        {
            Vector3 currentPosition = position;
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.y = Mathf.Round(position.y / gridSize) * gridSize;
            position.z = Mathf.Round(position.z / gridSize) * gridSize;
            Vector3 offset = position - currentPosition;
            return offset;
        }

        public void SetModel(BaseHandleModel model)
        {
            model.Appearance = Appearance;
            model.Window = Window;
            model.ModelScale = Appearance.HandleScale;
            model.SelectionMargin = Appearance.SelectionMargin;
            model.gameObject.SetActive(gameObject.activeSelf && enabled);
            model.SetLock(LockObject);

            Model = model;
        }

        private void OnCommandBufferRefresh(IRTECamera camera)
        {
            if(Target != null)
            {
                RefreshCommandBuffer(camera);
            }
        }

        protected virtual void RefreshCommandBuffer(IRTECamera camera)
        {

        }

    }
}
