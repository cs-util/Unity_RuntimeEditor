﻿using UnityEngine;

using System.Reflection;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

using Battlehub.RTCommon;
using Battlehub.RTSL;
using TMPro;
using System.Linq;

namespace Battlehub.RTEditor
{
    public class MaterialPropertyDescriptor
    {
        public object[] Targets;
        public object[] Accessors;

        public string Label;
        public RTShaderPropertyType Type;
        public Action<object, object> EraseTargetCallback;

        public PropertyInfo PropertyInfo;
        public RuntimeShaderInfo.RangeLimits Limits;
        public TextureDimension TexDims;
        
        public PropertyEditorCallback ValueChangedCallback;

        [Obsolete]
        public object Target
        {
            get { return Targets != null && Targets.Length > 0 ? Targets[0] : null; }
        }

        public object Accessor
        {
            get { return Accessors != null && Accessors.Length > 0 ? Accessors[0] : null; }
        }

        [Obsolete]
        public MaterialPropertyDescriptor(object target, object acessor, string label, RTShaderPropertyType type, PropertyInfo propertyInfo, RuntimeShaderInfo.RangeLimits limits, TextureDimension dims, PropertyEditorCallback callback, Action<object, object> eraseTargetCallback)
            : this(new [] { target }, new[] { acessor }, label, type, propertyInfo, limits, dims, callback, eraseTargetCallback)
        {
        }

        public MaterialPropertyDescriptor(object[] targets, object[] acessors, string label, RTShaderPropertyType type, PropertyInfo propertyInfo, RuntimeShaderInfo.RangeLimits limits, TextureDimension dims, PropertyEditorCallback callback, Action<object, object> eraseTargetCallback)
        {
            Targets = targets;
            Accessors = acessors;
            Label = label;
            Type = type;
            PropertyInfo = propertyInfo;
            Limits = limits;
            TexDims = dims;
            ValueChangedCallback = callback;
            EraseTargetCallback = eraseTargetCallback;
        }
    }


    public interface IMaterialDescriptor
    {
        string ShaderName
        {
            get;
        }

        object CreateConverter(MaterialEditor editor);

        MaterialPropertyDescriptor[] GetProperties(MaterialEditor editor, object converter);
    }

    public class MaterialEditor : MonoBehaviour
    {
        public readonly static Dictionary<string, IMaterialDescriptor> PropertyDescriptors;
        static MaterialEditor()
        {
            var type = typeof(IMaterialDescriptor);
            var types = Reflection.GetAssignableFromTypes(type);

            PropertyDescriptors = new Dictionary<string, IMaterialDescriptor>();
            foreach (Type t in types)
            {
                IMaterialDescriptor descriptor = (IMaterialDescriptor)Activator.CreateInstance(t);
                if (descriptor == null)
                {
                    Debug.LogWarningFormat("Unable to instantiate descriptor of type " + t.FullName);
                    continue;
                }
                if (descriptor.ShaderName == null)
                {
                    Debug.LogWarningFormat("ComponentType is null. ShaderName is null {0}", t.FullName);
                    continue;
                }
                if (PropertyDescriptors.ContainsKey(descriptor.ShaderName))
                {
                    Debug.LogWarningFormat("Duplicate component descriptor for {0} found. Type name {1}. Using {2} instead", descriptor.ShaderName, descriptor.GetType().FullName, PropertyDescriptors[descriptor.ShaderName].GetType().FullName);
                }
                else
                {
                    PropertyDescriptors.Add(descriptor.ShaderName, descriptor);
                }
            }
        }
        
        [SerializeField]
        private RangeEditor RangeEditor = null;
        [SerializeField]
        private Image m_image = null;
        [SerializeField]
        private TextMeshProUGUI TxtMaterialName = null;
        [SerializeField]
        private TextMeshProUGUI TxtShaderName = null;
        [SerializeField]
        private Transform EditorsPanel = null;
     
        [HideInInspector]
        public Material[] Materials = null;
        public Material Material
        {
            get { return Materials != null && Materials.Length > 0 ? Materials[0] : null; }
            set
            {
                if (value != null)
                {
                    Materials = new[] { value };
                }
                else
                {
                    Materials = null;
                }
            }
        }

        private IRuntimeEditor m_editor;
        private IResourcePreviewUtility m_resourcePreviewUtility;
        private Texture2D m_previewTexture;
        private IEditorsMap m_editorsMap;

        private void Start()
        {
            m_editor = IOC.Resolve<IRuntimeEditor>();
            m_editor.Undo.UndoCompleted += OnUndoCompleted;
            m_editor.Undo.RedoCompleted += OnRedoCompleted;
            m_resourcePreviewUtility = IOC.Resolve<IResourcePreviewUtility>();
            m_editorsMap = IOC.Resolve<IEditorsMap>();

            if ((Materials == null || Materials.Length == 0) && m_editor.Selection.Length > 0)
            {
                Materials = m_editor.Selection.objects.Cast<Material>().ToArray();
            }

            if (Materials == null || Materials.Length == 0 || Materials[0] == null)
            {
                Debug.LogError("Select material");
                return;
            }

            m_previewTexture = new Texture2D(1, 1, TextureFormat.ARGB32, true);

            TxtMaterialName.text = GetMaterialName(Materials);
            TxtShaderName.text = GetShaderName(Materials);

            UpdatePreview();
            BuildEditor();
        }

        private int m_skipUpdates;
        private void Update()
        {   
            if (Material == null)
            {
                return;
            }

            m_skipUpdates++;
            m_skipUpdates %= Materials.Length;
            if(m_skipUpdates == 0)
            {
                if(TxtMaterialName != null)
                {
                    string name = GetMaterialName(Materials);
                    if (TxtMaterialName.text != name)
                    {
                        TxtMaterialName.text = name;
                    }
                }    
            }
        }

        private void OnDestroy()
        {
            if(m_editor != null && m_editor.Undo != null)
            {
                m_editor.Undo.UndoCompleted -= OnUndoCompleted;
                m_editor.Undo.RedoCompleted -= OnRedoCompleted;
            }
            
            if (m_previewTexture != null)
            {
                Destroy(m_previewTexture);
            }
        }

        /// <summary>
        /// Get material name
        /// </summary>
        /// <param name="objects">materials</param>
        /// <returns>The name of the first material, if all materials have the same name. Otherwise returns null</returns>
        private static string GetMaterialName(Material[] materials)
        {
            string name = materials[0].name;
            for (int i = 1; i < materials.Length; ++i)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                if (material.name != name)
                {
                    return "-";
                }
            }
            return name;
        }

        private static string GetShaderName(Material[] materials)
        {
            Shader shader = materials[0].shader;
            for (int i = 1; i < materials.Length; ++i)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                if (material.shader != shader)
                {
                    return "-";
                }
            }

            if (shader == null)
            {
                return "Shader missing";
            }

            return shader.name;
        }

        public void BuildEditor()
        {
            foreach(Transform t in EditorsPanel)
            {
                Destroy(t.gameObject);
            }

            IMaterialDescriptor selector;
            if(!PropertyDescriptors.TryGetValue(Material.shader.name, out selector))
            {
                selector = new MaterialDescriptor();
            }

            object converter = selector.CreateConverter(this);
            MaterialPropertyDescriptor[] descriptors = selector.GetProperties(this, converter);
            if(descriptors == null)
            {
                Destroy(gameObject);
                return;
            }

            for(int i = 0; i < descriptors.Length; ++i)
            {
                MaterialPropertyDescriptor descriptor = descriptors[i];
                PropertyEditor editor = null;
                PropertyInfo propertyInfo = descriptor.PropertyInfo;

                RTShaderPropertyType propertyType = descriptor.Type;

                switch (propertyType)
                {
                    case RTShaderPropertyType.Range:
                        if (RangeEditor != null)
                        {
                            RangeEditor range = Instantiate(RangeEditor);
                            range.transform.SetParent(EditorsPanel, false);

                            var rangeLimits = descriptor.Limits;
                            range.Min = rangeLimits.Min;
                            range.Max = rangeLimits.Max;
                            editor = range;
                        }
                        break;
                    default:
                        if (m_editorsMap.IsPropertyEditorEnabled(propertyInfo.PropertyType))
                        {
                            GameObject editorPrefab = m_editorsMap.GetPropertyEditor(propertyInfo.PropertyType);
                            GameObject instance = Instantiate(editorPrefab);
                            instance.transform.SetParent(EditorsPanel, false);

                            if (instance != null)
                            {
                                editor = instance.GetComponent<PropertyEditor>();
                            }
                        }
                        break;
                }
                

                if (editor == null)
                {
                    continue;
                }

                editor.Init(descriptor.Targets, descriptor.Accessors, propertyInfo, descriptor.EraseTargetCallback, descriptor.Label, null, descriptor.ValueChangedCallback, () => 
                {
                    m_editor.IsDirty = true;
                    UpdatePreview();
                });
            }
        }


        private PropertyEditor InstantiateEditor( PropertyInfo propertyInfo)
        {
            PropertyEditor editor = null;
            if (m_editorsMap.IsPropertyEditorEnabled(propertyInfo.PropertyType))
            {
                GameObject prefab = m_editorsMap.GetPropertyEditor(propertyInfo.PropertyType);
                if (prefab != null)
                {
                    editor = Instantiate(prefab).GetComponent<PropertyEditor>();
                    editor.transform.SetParent(EditorsPanel, false);
                }
            }

            return editor;
        }

        private void OnRedoCompleted()
        {
            UpdatePreview();
        }

        private void OnUndoCompleted()
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if(Material == null)
            {
                return;
            }

            foreach(Material material in Materials)
            {
                if(material == null)
                {
                    continue;
                }
                m_editor.UpdatePreview(material, assetItem =>
                {
                    if (m_image != null && assetItem != null)
                    {
                        m_previewTexture.LoadImage(assetItem.Preview.PreviewData);
                        m_image.sprite = Sprite.Create(m_previewTexture, new Rect(0, 0, m_previewTexture.width, m_previewTexture.height), new Vector2(0.5f, 0.5f));
                    }
                });
            }
        }
    }
}

