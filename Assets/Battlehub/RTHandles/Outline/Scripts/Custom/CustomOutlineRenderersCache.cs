using Battlehub.RTCommon;
using Battlehub.UIControls.DockPanels;
using Battlehub.RTHandles;
using Battlehub.RTEditor;
using UnityEngine;
using System.Linq;
using Battlehub.Utils;
using System.Collections.Generic;

namespace Battlehub.RTHandles
{
    public class CustomOutlineRenderersCache : EditorExtension, ICustomOutlineRenderersCache
    {
        private List<ICustomOutlinePrepass> rendererItems = new List<ICustomOutlinePrepass>();
        private IRuntimeEditor editor;

        protected override void OnEditorExist()
        {
            editor = IOC.Resolve<IRuntimeEditor>();
            if (editor.IsOpened)
            {
                IOC.Register("CustomOutlineRenderersCache", (ICustomOutlineRenderersCache) this);

                TryToAddRenderers(editor.Selection);
                editor.Selection.SelectionChanged += OnRuntimeEditorSelectionChanged;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (editor != null)
            {
                editor.Selection.SelectionChanged -= OnRuntimeEditorSelectionChanged;
            }

            IOC.Unregister("CustomOutlineRenderersCache", (ICustomOutlineRenderersCache) this);
        }

        private void OnRuntimeEditorSelectionChanged(Object[] unselectedObjects)
        {
            if (unselectedObjects != null)
            {
                ICustomOutlinePrepass[] renderers = unselectedObjects.Select(go => go as GameObject).Where(go => go != null).SelectMany(go => go.GetComponentsInChildren<ICustomOutlinePrepass>(true)).ToArray();
                for(int i = 0; i < renderers.Length; ++i)
                {
                    rendererItems.Remove(renderers[i]);
                }
            }
            TryToAddRenderers(editor.Selection);
        }

        private void TryToAddRenderers(IRuntimeSelection selection)
        {
            if (selection.gameObjects != null)
            {
                ICustomOutlinePrepass[] renderers = selection.gameObjects.Where(go => go != null).Select(go => go.GetComponent<ExposeToEditor>()).Where(e => e != null && e.ShowSelectionGizmo && !e.gameObject.IsPrefab() && (e.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0).SelectMany(e => e.GetComponentsInChildren<ICustomOutlinePrepass>()).ToArray();
                for (int i = 0; i < renderers.Length; ++i)
                {
                    ICustomOutlinePrepass renderer = renderers[i];
                    rendererItems.Add(renderer);
                }
            }
        }

        public List<ICustomOutlinePrepass> GetOutlineRendererItems() {
            return rendererItems;
        }
    }
}