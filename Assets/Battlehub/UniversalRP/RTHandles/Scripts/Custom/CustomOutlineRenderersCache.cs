
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using Battlehub.RTCommon;
using Battlehub.Utils;

namespace Battlehub.RTHandles
{
    public class CustomOutlineRenderersCache : MonoBehaviour, ICustomOutlineRenderersCache
    {
        private List<ICustomOutlinePrepass> m_rendererItems = new List<ICustomOutlinePrepass>();
        private IRTE m_editor;

        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            IOC.Register<ICustomOutlineRenderersCache>("CustomOutlineRenderersCache", this);

            TryToAddRenderers(m_editor.Selection);
            m_editor.Selection.SelectionChanged += OnRuntimeEditorSelectionChanged;
        }

        private void OnDestroy()
        {
            if (m_editor != null)
            {
                m_editor.Selection.SelectionChanged -= OnRuntimeEditorSelectionChanged;
            }
            IOC.Unregister<ICustomOutlineRenderersCache>("CustomOutlineRenderersCache", this);
        }

        private void OnRuntimeEditorSelectionChanged(Object[] unselectedObjects)
        {
            if (unselectedObjects != null)
            {
                ICustomOutlinePrepass[] renderers = unselectedObjects.Select(go => go as GameObject).Where(go => go != null).SelectMany(go => go.GetComponentsInChildren<ICustomOutlinePrepass>(true)).ToArray();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    m_rendererItems.Remove(renderers[i]);
                }
            }
            TryToAddRenderers(m_editor.Selection);
        }

        private void TryToAddRenderers(IRuntimeSelection selection)
        {
            if (selection.gameObjects != null)
            {
                ICustomOutlinePrepass[] renderers = selection.gameObjects.Where(go => go != null).Select(go => go.GetComponent<ExposeToEditor>()).Where(e => e != null && e.ShowSelectionGizmo && !e.gameObject.IsPrefab() && (e.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0).SelectMany(e => e.GetComponentsInChildren<ICustomOutlinePrepass>()).ToArray();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    ICustomOutlinePrepass renderer = renderers[i];
                    m_rendererItems.Add(renderer);
                }
            }
        }

        public List<ICustomOutlinePrepass> GetOutlineRendererItems()
        {
            return m_rendererItems;
        }
    }
}