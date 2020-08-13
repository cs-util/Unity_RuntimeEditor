using Battlehub.RTCommon;
using UnityEngine;
using System.Linq;
namespace Battlehub.RTHandles
{
    public class RuntimeHighlightComponent : MonoBehaviour
    {
        private IRTE m_editor;
        private IRenderersCache m_cache;
        private bool m_updateRenderers = true;

        private Renderer[] m_renderers;
        private IRuntimeSelectionComponent m_selectionComponent;
        private RuntimeWindow m_activeWindow;
        private Ray m_prevRay;

        private void Start()
        {
            IOC.Register("HighlightRenderers", m_cache = gameObject.AddComponent<RenderersCache>());

            m_editor = IOC.Resolve<IRTE>();

            m_activeWindow = m_editor.ActiveWindow;
            if (m_activeWindow != null && m_editor.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                m_selectionComponent = m_activeWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
            }

            m_editor.Object.Enabled += OnObjectEnabled;
            m_editor.Object.Disabled += OnObjectDisabled;
            m_editor.Object.ComponentAdded += OnComponentAdded;
            m_editor.ActiveWindowChanged += OnActiveWindowChanged;
        }

        private void OnDestroy()
        {
            IOC.Unregister("HighlightRenderers", m_cache);

            if (m_editor != null && m_editor.Object != null)
            {
                m_editor.Object.Enabled -= OnObjectEnabled;
                m_editor.Object.Disabled -= OnObjectDisabled;
                m_editor.Object.ComponentAdded -= OnComponentAdded;
            }
        }

        private void Update()
        {
            if (m_activeWindow == null || m_selectionComponent == null)
            {
                return;
            }

            if (m_updateRenderers)
            {
                m_updateRenderers = false;
                m_renderers = m_editor.Object.Get(true).SelectMany(go => go.GetComponentsInChildren<Renderer>()).ToArray();
            }
            else
            {
                Ray ray = m_activeWindow.Pointer;
                if (m_prevRay.origin == ray.origin && m_prevRay.direction == ray.direction)
                {
                    return;
                }
            }
            m_prevRay = m_activeWindow.Pointer;

            Renderer[] renderers = m_selectionComponent.BoxSelection.Pick(m_renderers);
            m_cache.Clear();
            m_cache.Add(renderers, true, true);
        }

        private void OnObjectEnabled(ExposeToEditor obj)
        {
            m_updateRenderers = true;
        }

        private void OnObjectDisabled(ExposeToEditor obj)
        {
            m_updateRenderers = true;
        }

        private void OnComponentAdded(ExposeToEditor obj, Component arg)
        {
            m_updateRenderers = true;
        }

        private void OnActiveWindowChanged(RuntimeWindow window)
        {
            if (m_editor.ActiveWindow != null && m_editor.ActiveWindow.WindowType == RuntimeWindowType.Scene)
            {
                m_updateRenderers = true;
                m_activeWindow = m_editor.ActiveWindow;
                m_selectionComponent = m_activeWindow.IOCContainer.Resolve<IRuntimeSelectionComponent>();
            }
            else
            {
                m_activeWindow = null;
                m_selectionComponent = null;
                m_renderers = null;
            }
        }
    }
}
