using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Battlehub.RTCommon;

namespace Battlehub.RTEditor
{
    [DefaultExecutionOrder(-100)]
    public partial class EditorsMap : MonoBehaviour, IEditorsMap
    {
        private class EditorDescriptor
        {
            public int Index;
            public bool Enabled;
            public bool IsPropertyEditor;

            public EditorDescriptor(int index, bool enabled, bool isPropertyEditor)
            {
                Index = index;
                Enabled = enabled;
                IsPropertyEditor = isPropertyEditor;
            }
        }

        private class MaterialEditorDescriptor
        {
            public GameObject Editor;
            public bool Enabled;

            public MaterialEditorDescriptor(GameObject editor, bool enabled)
            {
                Editor = editor;
                Enabled = enabled;
            }
        }

        private GameObject m_defaultMaterialEditor;
        private Dictionary<Shader, MaterialEditorDescriptor> m_materialMap = new Dictionary<Shader, MaterialEditorDescriptor>();
        private Dictionary<Type, EditorDescriptor> m_map = new Dictionary<Type, EditorDescriptor>();
        private GameObject[] m_editors = new GameObject[0];
        private bool m_isLoaded = false;
        private Dictionary<Type, IComponentDescriptor> m_componentDescriptors;
        private ComponentEditor m_emptyComponentEditor;

        public Dictionary<Type, IComponentDescriptor> ComponentDescriptors
        {
            get { return m_componentDescriptors; }
        }

        public ComponentEditor VoidComponentEditor
        {
            get;
            set;
        }

        private void Awake()
        {
            IOC.RegisterFallback<IEditorsMap>(this);
            
            var type = typeof(IComponentDescriptor);
#if !UNITY_WSA || UNITY_EDITOR
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
#else
            var types = type.GetTypeInfo().Assembly.GetTypes().
                Where(p => type.IsAssignableFrom(p) && p.GetTypeInfo().IsClass && !p.IsAbstract);
#endif

            m_componentDescriptors = new Dictionary<Type, IComponentDescriptor>();
            foreach (Type t in types)
            {
                IComponentDescriptor descriptor = (IComponentDescriptor)Activator.CreateInstance(t);
                if (descriptor == null)
                {
                    Debug.LogWarningFormat("Unable to instantiate descriptor of type " + t.FullName);
                    continue;
                }
                if (descriptor.ComponentType == null)
                {
                    Debug.LogWarningFormat("ComponentType is null. Descriptor Type {0}", t.FullName);
                    continue;
                }
                if (m_componentDescriptors.ContainsKey(descriptor.ComponentType))
                {
                    IComponentDescriptor alreadyAddedDescriptor = m_componentDescriptors[descriptor.ComponentType];
                    if(IsBulitIn(alreadyAddedDescriptor.GetType()))
                    {
                        //Overwrite built-in component descriptor
                        m_componentDescriptors[descriptor.ComponentType] = descriptor;
                    }
                    else if(!IsBulitIn(descriptor.GetType()))
                    {
                        Debug.LogWarningFormat("Duplicate descriptor for {0} found. Type name {1}. Using {2} instead", descriptor.ComponentType.FullName, descriptor.GetType().FullName, m_componentDescriptors[descriptor.ComponentType].GetType().FullName);
                    }
                }
                else
                {
                    m_componentDescriptors.Add(descriptor.ComponentType, descriptor);
                }
            }

            LoadMap();
        }

        private bool IsBulitIn(Type type)
        {
            return type.GetCustomAttribute<BuiltInDescriptorAttribute>(false) != null;
        }

        private void Start()
        {
            GameObject voidComponentEditor = new GameObject("VoidComponentEditor");
            voidComponentEditor.transform.SetParent(transform, false);
            VoidComponentEditor = voidComponentEditor.AddComponent<VoidComponentEditor>();
        }

        private void OnDestroy()
        {
            IOC.UnregisterFallback<IEditorsMap>(this);
        }

        public PropertyDescriptor[] GetPropertyDescriptors(Type componentType, ComponentEditor componentEditor = null, object converter = null)
        {
            ComponentEditor editor = componentEditor != null ? componentEditor : VoidComponentEditor;

            IComponentDescriptor componentDescriptor;
            if (!ComponentDescriptors.TryGetValue(componentType, out componentDescriptor))
            {
                componentDescriptor = null;
            }

            if (componentDescriptor != null)
            {
                if (converter == null)
                {
                    converter = componentDescriptor.CreateConverter(editor);
                }

                PropertyDescriptor[] properties = componentDescriptor.GetProperties(editor, converter);
                return properties;
            }
            else
            {
                if (componentType.IsScript())
                {
                    FieldInfo[] serializableFields = componentType.GetSerializableFields(false);
                    return serializableFields.Select(f => new PropertyDescriptor(f.Name, editor.Components, f, f)).ToArray();
                }
                else
                {
                    PropertyInfo[] properties = componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite).ToArray();
                    return properties.Select(p => new PropertyDescriptor(p.Name, editor.Components, p, p)).ToArray();
                }
            }
        }

        private void DefaultEditorsMap()
        {
            m_map = new Dictionary<Type, EditorDescriptor>
            {
                { typeof(GameObject), new EditorDescriptor(0, true, false) },
                { typeof(object), new EditorDescriptor(1, true, true) },
                { typeof(UnityEngine.Object), new EditorDescriptor(2, true, true) },
                { typeof(bool), new EditorDescriptor(3, true, true) },
                { typeof(Enum), new EditorDescriptor(4, true, true) },
                { typeof(List<>), new EditorDescriptor(5, true, true) },
                { typeof(Array), new EditorDescriptor(6, true, true) },
                { typeof(string), new EditorDescriptor(7, true, true) },
                { typeof(int), new EditorDescriptor(8, true, true) },
                { typeof(float), new EditorDescriptor(9, true, true) },
                { typeof(Range), new EditorDescriptor(10, true, true) },
                { typeof(Vector2), new EditorDescriptor(11, true, true) },
                { typeof(Vector3), new EditorDescriptor(12, true, true) },
                { typeof(Vector4), new EditorDescriptor(13, true, true) },
                { typeof(Quaternion), new EditorDescriptor(14, true, true) },
                { typeof(Color), new EditorDescriptor(15, true, true) },
                { typeof(Bounds), new EditorDescriptor(16, true, true) },
                { typeof(RangeInt), new EditorDescriptor(17, true, true) },
                { typeof(RangeOptions), new EditorDescriptor(18, true, true) },
                { typeof(HeaderText), new EditorDescriptor(19, true, true) },
                { typeof(MethodInfo), new EditorDescriptor(20, true, true) },
                { typeof(Component), new EditorDescriptor(21, true, false) },
                { typeof(BoxCollider), new EditorDescriptor(22, true, false) },
                { typeof(Camera), new EditorDescriptor(21, true, false) },
                { typeof(CapsuleCollider), new EditorDescriptor(22, true, false) },
                { typeof(FixedJoint), new EditorDescriptor(21, true, false) },
                { typeof(HingeJoint), new EditorDescriptor(21, true, false) },
                { typeof(Light), new EditorDescriptor(21, true, false) },
                { typeof(MeshCollider), new EditorDescriptor(21, true, false) },
                { typeof(MeshFilter), new EditorDescriptor(21, true, false) },
                { typeof(MeshRenderer), new EditorDescriptor(21, true, false) },
                { typeof(MonoBehaviour), new EditorDescriptor(21, false, false) },
                { typeof(Rigidbody), new EditorDescriptor(21, true, false) },
                { typeof(SkinnedMeshRenderer), new EditorDescriptor(21, true, false) },
                { typeof(Skybox), new EditorDescriptor(21, true, false) },
                { typeof(SphereCollider), new EditorDescriptor(22, true, false) },
                { typeof(SpringJoint), new EditorDescriptor(21, true, false) },
                { typeof(Transform), new EditorDescriptor(23, true, false) },
                { typeof(RuntimeAnimation), new EditorDescriptor(21, true, false) },
                { typeof(AudioSource), new EditorDescriptor(21, true, false) },
                { typeof(AudioListener), new EditorDescriptor(21, true, false) },
            };
        }

        partial void InitEditorsMap();

        public void LoadMap()
        {
            if (m_isLoaded)
            {
                return;
            }
            m_isLoaded = true;

            DefaultEditorsMap();
            InitEditorsMap();

            EditorsMapStorage editorsMap = Resources.Load<EditorsMapStorage>(EditorsMapStorage.EditorsMapPrefabName);
            if (editorsMap == null)
            {
                editorsMap = Resources.Load<EditorsMapStorage>(EditorsMapStorage.EditorsMapTemplateName);
            }
            if (editorsMap != null)
            {
                m_editors = editorsMap.Editors;

                for (int i = 0; i < editorsMap.MaterialEditors.Length; ++i)
                {
                    GameObject materialEditor = editorsMap.MaterialEditors[i];
                    Shader shader = editorsMap.Shaders[i];
                    bool enabled = editorsMap.IsMaterialEditorEnabled[i];
                    if (!m_materialMap.ContainsKey(shader))
                    {
                        m_materialMap.Add(shader, new MaterialEditorDescriptor(materialEditor, enabled));
                    }
                    m_defaultMaterialEditor = editorsMap.DefaultMaterialEditor;
                }
            }
            else
            {
                Debug.LogError("Editors map is null");
            }
        }

        public void RegisterEditor(ComponentEditor editor)
        {
            Array.Resize(ref m_editors, m_editors.Length + 1);
            m_editors[m_editors.Length - 1] = editor.gameObject;
        }

        public void RegisterEditor(PropertyEditor editor)
        {
            Array.Resize(ref m_editors, m_editors.Length + 1);
            m_editors[m_editors.Length - 1] = editor.gameObject;
        }

        public bool HasMapping(Type type)
        {
            return m_map.ContainsKey(type);
        }

        public void AddMapping(Type type, Type editorType, bool enabled, bool isPropertyEditor)
        {
            GameObject editor = m_editors.Where(ed => ed.GetComponents<Component>().Any(c => c.GetType() == editorType)).FirstOrDefault();
            if (editor == null)
            {
                throw new ArgumentException("editorType");
            }

            AddMapping(type, editor, enabled, isPropertyEditor);
        }

        public void RemoveMapping(Type type)
        {
            m_map.Remove(type);
        }

        public void AddMapping(Type type, GameObject editor, bool enabled, bool isPropertyEditor)
        {
            int index = Array.IndexOf(m_editors, editor);
            if (index < 0)
            {
                Array.Resize(ref m_editors, m_editors.Length + 1);
                index = m_editors.Length - 1;
                m_editors[index] = editor;
            }
            m_map.Add(type, new EditorDescriptor(index, enabled, isPropertyEditor));
        }

        public bool IsObjectEditorEnabled(Type type)
        {
            return IsEditorEnabled(type, false, true);
        }

        public bool IsPropertyEditorEnabled(Type type, bool strict = false)
        {
            return IsEditorEnabled(type, true, strict);
        }

        private bool IsEditorEnabled(Type type, bool isPropertyEditor, bool strict)
        {
            EditorDescriptor descriptor = GetEditorDescriptor(type, isPropertyEditor, strict);
            if (descriptor != null)
            {
                return descriptor.Enabled;
            }
            return false;
        }

        public bool IsMaterialEditorEnabled(Shader shader)
        {
            MaterialEditorDescriptor descriptor = GetEditorDescriptor(shader);
            if (descriptor != null)
            {
                return descriptor.Enabled;
            }

            return false;
        }

        public GameObject GetObjectEditor(Type type, bool strict = false)
        {
            return GetEditor(type, false, strict);
        }

        public GameObject GetPropertyEditor(Type type, bool strict = false)
        {
            return GetEditor(type, true, strict);
        }

        private GameObject GetEditor(Type type, bool isPropertyEditor, bool strict = false)
        {
            EditorDescriptor descriptor = GetEditorDescriptor(type, isPropertyEditor, strict);
            if (descriptor != null)
            {
                return m_editors[descriptor.Index];
            }
            return null;
        }

        public GameObject GetMaterialEditor(Shader shader, bool strict = false)
        {
            MaterialEditorDescriptor descriptor = GetEditorDescriptor(shader);
            if (descriptor != null)
            {
                return descriptor.Editor;
            }

            if (strict)
            {
                return null;
            }

            return m_defaultMaterialEditor;
        }

        private MaterialEditorDescriptor GetEditorDescriptor(Shader shader)
        {
            MaterialEditorDescriptor descriptor;
            if (m_materialMap.TryGetValue(shader, out descriptor))
            {
                return m_materialMap[shader];
            }

            return null;
        }

        private EditorDescriptor GetEditorDescriptor(Type type, bool isPropertyEditor, bool strict)
        {
            if (type == typeof(MethodInfo))
            {
                EditorDescriptor descriptor;
                if (m_map.TryGetValue(type, out descriptor))
                {
                    return descriptor;
                }
                return null;
            }

            do
            {
                EditorDescriptor descriptor;
                if (m_map.TryGetValue(type, out descriptor))
                {
                    if (descriptor.IsPropertyEditor == isPropertyEditor)
                    {
                        return descriptor;
                    }
                }
                else
                {
                    if (type.IsGenericType)
                    {
                        if (m_map.TryGetValue(type.GetGenericTypeDefinition(), out descriptor))
                        {
                            if (descriptor.IsPropertyEditor == isPropertyEditor)
                            {
                                return descriptor;
                            }
                        }
                    }
                }

                if (strict)
                {
                    break;
                }

                type = type.BaseType();
            }
            while (type != null);
            return null;
        }

        public Type[] GetEditableTypes()
        {
            return m_map.Where(kvp => kvp.Value != null && kvp.Value.Enabled).Select(kvp => kvp.Key).ToArray();
        }
    }
}
