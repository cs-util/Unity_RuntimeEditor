using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Battlehub.RTCommon;
using System.Reflection;
using Battlehub.Utils;
using TMPro;
using System;
using System.Linq;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTEditor
{
    public class GameObjectEditor : MonoBehaviour
    {
        [SerializeField]
        private Toggle TogEnableDisable = null;
        [SerializeField]
        private TMP_InputField InputName = null;
        [SerializeField]
        private TMP_Dropdown LayerDropdown = null;
        [SerializeField]
        private Transform ComponentsPanel = null;
        
        private IRuntimeEditor m_editor;
        private IEditorsMap m_editorsMap;

        public bool IsGameObjectActive
        {
            get
            {
                GameObject go = m_editor.Selection.activeGameObject;
                if(go == null)
                {
                    return false;
                }
                return GetActiveSelf(m_editor.Selection.gameObjects);
            }
            set
            {
                GameObject[] gameObjects = m_editor.Selection.gameObjects;
                if (gameObjects != null)
                {
                    for(int i = 0; i < gameObjects.Length; ++i)
                    {
                        GameObject go = gameObjects[i];
                        if(go != null)
                        {
                            go.SetActive(value);
                        }
                    }

                    if (TogEnableDisable != null)
                    {
                        TogEnableDisable.onValueChanged.RemoveListener(OnEnableDisable);
                    }
                    TogEnableDisable.isOn = value;
                    if (TogEnableDisable != null)
                    {
                        TogEnableDisable.onValueChanged.AddListener(OnEnableDisable);
                    }
                }
            }
        }

        private Dictionary<int, int> m_layerToIndex;
        private Dictionary<int, int> m_indexToLayer;

        private void Awake()
        {
            m_editorsMap = IOC.Resolve<IEditorsMap>();
            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_editor.Object.ComponentAdded += OnComponentAdded;

            GameObject[] selectedObjects = m_editor.Selection.gameObjects;
            InputName.text = GetObjectName(selectedObjects);
            TogEnableDisable.isOn = GetActiveSelf(selectedObjects);

            InputName.onEndEdit.AddListener(OnEndEditName);
            TogEnableDisable.onValueChanged.AddListener(OnEnableDisable);

            List<List<Component>> groups = GetComponentGroups(selectedObjects);
            for(int i = 0; i < groups.Count; ++i)
            {
                List<Component> group = groups[i];
                CreateComponentEditor(group);
            }

            List<TMP_Dropdown.OptionData> layers = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("0: Default"),
                new TMP_Dropdown.OptionData("1: Transparent FX"),
                new TMP_Dropdown.OptionData("2: Ignore Raycast"),
                new TMP_Dropdown.OptionData("4: Water"),
                new TMP_Dropdown.OptionData("5: UI"),
            };

            m_layerToIndex = new Dictionary<int, int>();
            m_layerToIndex.Add(0, 0);
            m_layerToIndex.Add(1, 1);
            m_layerToIndex.Add(2, 2);
            m_layerToIndex.Add(4, 3);
            m_layerToIndex.Add(5, 4);

            int index = m_layerToIndex.Count;
            for (int i = 10; i <= 15; ++i, ++index)
            {
                m_layerToIndex.Add(i, index);
                layers.Add(new TMP_Dropdown.OptionData(string.Format("{0}: Layer {0}", i)));
            }

            for(int i = 25; i <= 30; ++i, ++index)
            {
                m_layerToIndex.Add(i, index);
                layers.Add(new TMP_Dropdown.OptionData(string.Format("{0}: Layer {0}", i)));
            }

            m_indexToLayer = m_layerToIndex.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

            LayerDropdown.options = layers;
            LayerDropdown.value = m_layerToIndex.ContainsKey(m_editor.Selection.activeGameObject.layer) ? m_layerToIndex[m_editor.Selection.activeGameObject.layer] : 0;
            LayerDropdown.onValueChanged.AddListener(OnLayerChanged);
        }

        private void OnDestroy()
        {
            if (InputName != null)
            {
                InputName.onEndEdit.RemoveListener(OnEndEditName);
            }
            if (TogEnableDisable != null)
            {
                TogEnableDisable.onValueChanged.RemoveListener(OnEnableDisable);
            }

            if (m_editor != null && m_editor.Object != null)
            {
                m_editor.Object.ComponentAdded -= OnComponentAdded;
            }

            if(LayerDropdown != null)
            {
                LayerDropdown.onValueChanged.RemoveListener(OnLayerChanged);
            }
        }

        private void Update()
        {
            GameObject go = m_editor.Selection.activeGameObject;
            if(go == null)
            {
                return;
            }

            UnityObject[] objects = m_editor.Selection.objects;
            if(objects[0] == null)
            {
                return;
            }

            if (InputName != null && !InputName.isFocused)
            {
                string objectName = GetObjectName(objects);
                if(InputName.text != objectName)
                {
                    InputName.text = objectName;
                }
            }
        }

        /// <summary>
        /// Get object name
        /// </summary>
        /// <param name="objects">objects</param>
        /// <returns>The name of the first object, if all objects have the same name. Otherwise returns null</returns>
        private static string GetObjectName(UnityObject[] objects)
        {
            string name = objects[0].name;
            for(int i = 1; i < objects.Length; ++i)
            {
                UnityObject go = objects[i];
                if(go == null)
                {
                    continue;
                }

                if(go.name != name)
                {
                    return null;
                }
            }
            return name;
        }

        /// <summary>
        /// Get object activeSelf value
        /// </summary>
        /// <param name="objects">objects</param>
        /// <returns>The activeSelf value of the first object, if all objects have the same value of activeSelf. Otherwise returns false</returns>
        private static bool GetActiveSelf(UnityObject[] objects)
        {
            bool activeSelf = ((GameObject)objects[0]).activeSelf;
            for(int i = 1; i < objects.Length; ++i)
            {
                GameObject go = (GameObject)objects[i];
                if(go == null)
                {
                    continue;
                }

                if(go.activeSelf != activeSelf)
                {
                    return false;
                }
            }
            return activeSelf;
        }


        /// <summary>
        /// Check if component valid to be represented by component editor
        /// </summary>
        /// <param name="component">component</param>
        /// <param name="ignoreComponents">list of components which must be ignored</param>
        /// <returns>true if component is valid</returns>
        private static bool IsComponentValid(Component component, HashSet<Component> ignoreComponents)
        {
            if(component == null)
            {
                return false;
            }

            return !ignoreComponents.Contains(component) && (component.hideFlags & HideFlags.HideInInspector) == 0;
        }

        /// <summary>
        /// Find intersection of components of game objects
        /// </summary>
        /// <param name="gameObjects">game objects</param>
        /// <returns>list of groups of components with the same type</returns>
        private static List<List<Component>> GetComponentGroups(GameObject[] gameObjects)
        {
            List<List<Component>> groups = new List<List<Component>>();
            List<List<Component>> allComponents = new List<List<Component>>();
            for(int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject go = gameObjects[i];

                HashSet<Component> ignoreComponents = IgnoreComponents(go);
                allComponents.Add(go.GetComponents<Component>().Where(component => IsComponentValid(component, ignoreComponents)).ToList());
            }

            List<Component> primaryList = allComponents[0];
            for(int i = 0; i < primaryList.Count; ++i)
            {
                Component primary = primaryList[i];
                Type primaryType = primary.GetType();

                List<Component> group = new List<Component>();
                group.Add(primary);

                for(int j = 1; j < allComponents.Count; ++j)
                {
                    List<Component> secondaryList = allComponents[j];
                    if(secondaryList.Count == 0)
                    {
                        //one of the lists is exhausted -> break outer loop
                        i = primaryList.Count;
                        group = null;
                        break;
                    }

                    //find component of type
                    for(int k = 0; k < secondaryList.Count; k++)
                    {
                        Component secondary = secondaryList[k];
                        if(primaryType == secondary.GetType())
                        {
                            group.Add(secondary);
                            secondaryList.RemoveAt(k);
                            break;
                        }
                    }

                    if(group.Count != j + 1)
                    {
                        //not all game objects have a component with the same type
                        group = null;
                        break;
                    }
                }

                if(group != null)
                {
                    //all game objects have a component with the same type
                    groups.Add(group);
                }
            }

            return groups;
        }
        
        private static HashSet<Component> IgnoreComponents(GameObject go)
        {
            ExposeToEditor exposeToEditor = go.GetComponent<ExposeToEditor>();
            HashSet<Component> ignoreComponents = new HashSet<Component>();
            if (exposeToEditor != null)
            {
                if (exposeToEditor.Colliders != null)
                {
                    for (int i = 0; i < exposeToEditor.Colliders.Length; ++i)
                    {
                        Collider collider = exposeToEditor.Colliders[i];
                        if (!ignoreComponents.Contains(collider))
                        {
                            ignoreComponents.Add(collider);
                        }
                    }
                }

                ignoreComponents.Add(exposeToEditor);
            }

            return ignoreComponents;
        }
        
        private bool CreateComponentEditor(List<Component> components)
        {
            if(components.Count == 0)
            {
                return false;
            }

            Component component = components[0];
            Type componentType = component.GetType();
            if (m_editorsMap.IsObjectEditorEnabled(componentType))
            {
                GameObject editorPrefab = m_editorsMap.GetObjectEditor(componentType);
                if (editorPrefab != null)
                {
                    ComponentEditor componentEditorPrefab = editorPrefab.GetComponent<ComponentEditor>();
                    if (componentEditorPrefab != null)
                    {
                        ComponentEditor editor = Instantiate(componentEditorPrefab);
                        editor.EndEditCallback = () =>
                        {
                            UpdatePreviews(components);
                        };
                        editor.transform.SetParent(ComponentsPanel, false);
                        editor.Components = components.ToArray();
                        return true;
                    }
                    else
                    {
                        Debug.LogErrorFormat("editor prefab {0} does not have ComponentEditor script", editorPrefab.name);
                        return false;
                    }
                }
            }

            return false;
        }

        private void UpdatePreviews(List<Component> components)
        {
            for (int i = 0; i < components.Count; ++i)
            {
                Component component = components[i];
                if (component != null && component.gameObject != null)
                {
                    m_editor.UpdatePreview(component);
                }
            }
            m_editor.IsDirty = true;
        }

        private void OnEnableDisable(bool enable)
        {
            //Wrong? how does it work?
            PropertyInfo prop = Strong.PropertyInfo((GameObjectEditor x) => x.IsGameObjectActive, "IsGameObjectActive");

            m_editor.Undo.BeginRecord();
            GameObject[] gameObjects = m_editor.Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject go = gameObjects[i];
                if (go == null)
                {
                    continue;
                }

                m_editor.Undo.BeginRecordValue(go, this, prop);
                go.SetActive(enable);
                m_editor.Undo.EndRecordValue(go, this, prop, null);
            }
            m_editor.Undo.EndRecord();
        }

        private void OnEndEditName(string name)
        {
            GameObject[] gameObjects = m_editor.Selection.gameObjects;
            for(int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject go = gameObjects[i];
                if(go == null)
                {
                    continue;
                }

                ExposeToEditor exposeToEditor = go.GetComponent<ExposeToEditor>();
                if (exposeToEditor != null)
                {
                    exposeToEditor.SetName(name, true);
                }
                else
                {
                    go.name = name;
                }
            }    
        }

        private void OnComponentAdded(ExposeToEditor obj, Component component)
        {
            if(component == null)
            {
                IWindowManager wnd = IOC.Resolve<IWindowManager>();
                wnd.MessageBox("Unable to add component", "Component was not added");
            }
            else
            {
                if (!m_editor.Selection.IsSelected(component.gameObject))
                {
                    return;
                }

                if (m_editor.Selection.activeGameObject == null)
                {
                    return;
                }

                HashSet<Component> ignoreComponents = IgnoreComponents(obj.gameObject);
                if(!IsComponentValid(component, ignoreComponents))
                {
                    return;
                }

                GameObject[] gameObjects = m_editor.Selection.gameObjects;
                if (gameObjects.Length == 1)
                {
                    CreateComponentEditor(new List<Component> { component });
                }
                else
                {
                    if (gameObjects[gameObjects.Length - 1] != component.gameObject)
                    {
                        return;
                    }

                    List<List<Component>> groups = GetComponentGroups(gameObjects);
                    for (int i = 0; i < groups.Count; ++i)
                    {
                        List<Component> group = groups[i];

                        //This is to handle case when AddComponent called on multiple objects. 
                        //See InspectorView.cs void OnAddComponent(Type type) method for details.
                        if (group[group.Count - 1] == component)
                        {
                            CreateComponentEditor(group);
                            break;
                        }
                    }
                }
            }
        }

        private void OnLayerChanged(int value)
        {
            int layer = m_indexToLayer[value];
            foreach(GameObject go in m_editor.Selection.gameObjects)
            {
                go.layer = layer;
            }
        }
    }
}

