﻿using Battlehub.UIControls;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTEditor
{
    public class AnimationPropertyItem
    {
        public string ComponentType;
        public string ComponentDisplayName;
        public string PropertyName;
        public string PropertyDisplayName;
        public AnimationPropertyItem Parent;
        public List<AnimationPropertyItem> Children;
        public object Component;

        public AnimationPropertyItem() { }
        public AnimationPropertyItem(AnimationPropertyItem item)
        {
            ComponentType = item.ComponentType;
            ComponentDisplayName = item.ComponentDisplayName;
            PropertyName = item.PropertyName;
            PropertyDisplayName = item.PropertyDisplayName;
            Parent = item.Parent;
            Children = item.Children;
            Component = item.Component;
        }

        
        public object Value
        {
            get
            {
                if (Parent != null)
                {
                    return GetMemberValue(Parent.Value, PropertyName);
                }

                return GetMemberValue(Component, PropertyName);
            }
            set
            {
                if (Parent != null)
                {
                    object v = Parent.Value;
                    SetMemberValue(v, PropertyName, value);
                    Parent.Value = v;
                }
                else
                {
                    SetMemberValue(Component, PropertyName, value);
                }
            }
        }

        private static void SetMemberValue(object obj, string path, object value)
        {
            Type propertyType = obj.GetType();
            MemberInfo[] members = propertyType.GetMember(path);
            if (members[0].MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = (FieldInfo)members[0];
                fieldInfo.SetValue(obj, value);

            }
            else if (members[0].MemberType == MemberTypes.Property)
            {
                PropertyInfo propInfo = (PropertyInfo)members[0];
                propInfo.SetValue(obj, value);
            }
        }

        private static object GetMemberValue(object obj, string path)
        {
            Type propertyType = obj.GetType();
            MemberInfo[] members = propertyType.GetMember(path);
            if (members[0].MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = (FieldInfo)members[0];
                return fieldInfo.GetValue(obj);
            }
            else if (members[0].MemberType == MemberTypes.Property)
            {
                PropertyInfo propInfo = (PropertyInfo)members[0];
                return propInfo.GetValue(obj);
            }

            throw new InvalidOperationException("wrong property path " + path);
        }

        public void TryToCreateChildren()
        {
            Type type = Value.GetType();
            if (Reflection.IsPrimitive(type))
            {
                return;
            }

            if (!Reflection.IsValueType(type))
            {
                return;
            }

            List<AnimationPropertyItem> children = new List<AnimationPropertyItem>();
            FieldInfo[] fields = type.GetSerializableFields();
            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo field = fields[i];
                if(!Reflection.IsPrimitive(field.FieldType))
                {
                    continue;
                }

                AnimationPropertyItem child = new AnimationPropertyItem
                {
                    PropertyName = field.Name,
                    PropertyDisplayName = field.Name,
                    Parent = this,
                    Component = Component,

                    Value = GetMemberValue(Value, field.Name),
                };
                children.Add(child);
            }

            PropertyInfo[] properties = type.GetSerializableProperties();
            for (int i = 0; i < properties.Length; ++i)
            {
                PropertyInfo property = properties[i];
                if (!Reflection.IsPrimitive(property.PropertyType))
                {
                    continue;
                }

                AnimationPropertyItem child = new AnimationPropertyItem
                {
                    PropertyName = property.Name,
                    PropertyDisplayName = property.Name,
                    Parent = this,
                    Component = Component,

                    Value = GetMemberValue(Value, property.Name),
                };
                children.Add(child);
            }

            Children = children;
        }
    }

    public class AnimationPropertyView : MonoBehaviour
    {        
        [SerializeField]
        private TextMeshProUGUI m_label = null;

        [SerializeField]
        private TMP_InputField m_inputField = null;

        [SerializeField]
        private Toggle m_toggle = null;

        [SerializeField]
        private Button m_addPropertyButton = null;

        [SerializeField]
        private DragField m_dragField = null;

        private AnimationPropertyItem m_item;
        public AnimationPropertyItem Item
        {
            get { return m_item; }
            set
            {
                m_item = value;

                if(m_item != null)
                {
                    bool isBool = m_item.Value is bool;
                    bool hasChildren = m_item.Children != null && m_item.Children.Count > 0;

                    if (m_toggle != null)
                    {
                        m_toggle.gameObject.SetActive(isBool && !hasChildren);
                        if(isBool)
                        {
                            m_toggle.isOn = (bool)m_item.Value;
                        }
                    }

                    if (m_dragField != null)
                    {
                        m_dragField.enabled = !isBool && !hasChildren;
                    }

                    if (m_inputField != null)
                    {
                        if(!hasChildren)
                        {
                            m_inputField.gameObject.SetActive(!isBool);
                            m_inputField.DeactivateInputField();

                            if (!isBool && m_item.Value != null)
                            {
                                Type type = m_item.Value.GetType();
                                if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(byte))
                                {
                                    m_inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                                    if(m_dragField != null)
                                    {
                                        m_dragField.IncrementFactor = 1.0f;
                                    }
                                }
                                else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                                {
                                    m_inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                                    if(m_dragField != null)
                                    {
                                        m_dragField.IncrementFactor = 0.1f;
                                    }
                                }
                                else
                                {
                                    m_inputField.contentType = TMP_InputField.ContentType.Standard;
                                    if (m_dragField != null)
                                    {
                                        m_dragField.IncrementFactor = 1.0f;
                                    }
                                }

                                m_inputField.text = m_item.Value + "";
                            }
                            else
                            {
                                m_inputField.contentType = TMP_InputField.ContentType.Standard;
                                m_inputField.text = "";
                            }
                        }
                        else
                        {
                            m_inputField.gameObject.SetActive(false);
                            m_inputField.text = "";
                        }   
                    }

                    if (m_label != null)
                    {
                        if (m_item.Parent == null)
                        {
                            m_label.text = string.Format("{0} : {1}", m_item.ComponentDisplayName, m_item.PropertyDisplayName);
                        }
                        else
                        {
                            m_label.text = string.Format("{0} : {1}", m_item.Parent.PropertyDisplayName, m_item.PropertyDisplayName);
                        }

                        m_label.gameObject.SetActive(true);
                    }

                    if (m_addPropertyButton != null)
                    {
                        m_addPropertyButton.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if(m_inputField != null)
                    {
                        m_inputField.gameObject.SetActive(false);
                    }
                    if(m_toggle != null)
                    {
                        m_toggle.gameObject.SetActive(false);
                    }
                    if(m_label != null)
                    {
                        m_label.gameObject.SetActive(false);
                    }

                    if (m_addPropertyButton != null)
                    {
                        m_addPropertyButton.gameObject.SetActive(true);
                    }
                }
            }
        }

   
        public AnimationPropertiesView View
        {
            get;
            set;
        }

        private bool m_isDragging;

        private void Awake()
        {
            UnityEventHelper.AddListener(m_inputField, input => input.onEndEdit, OnInputFieldEndEdit);
            UnityEventHelper.AddListener(m_addPropertyButton, button => button.onClick, OnAddPropertyButtonClick);
            UnityEventHelper.AddListener(m_toggle, toggle => toggle.onValueChanged, OnToggleValueChange);
            UnityEventHelper.AddListener(m_dragField, dragField => dragField.BeginDrag, OnDragFieldBeginDrag);
            UnityEventHelper.AddListener(m_dragField, dragField => dragField.EndDrag, OnDragFieldEndDrag);
        }

        private void OnDestroy()
        {
            UnityEventHelper.RemoveListener(m_inputField, input => input.onEndEdit, OnInputFieldEndEdit);
            UnityEventHelper.RemoveListener(m_addPropertyButton, button => button.onClick, OnAddPropertyButtonClick);
            UnityEventHelper.RemoveListener(m_toggle, toggle => toggle.onValueChanged, OnToggleValueChange);
            UnityEventHelper.RemoveListener(m_dragField, dragField => dragField.BeginDrag, OnDragFieldBeginDrag);
            UnityEventHelper.RemoveListener(m_dragField, dragField => dragField.EndDrag, OnDragFieldEndDrag);
        }

        private void OnAddPropertyButtonClick()
        {
            View.AddProperty(Item);
        }

        private void OnInputFieldEndEdit(string value)
        {
            UpdateValue(value);
        }

        private void UpdateValue(string value)
        {
            if (m_item != null)
            {
                Type type = m_item.Value.GetType();

                object result;
                if (Reflection.TryConvert(value, type, out result))
                {
                    m_item.Value = result;
                }
            }
        }

        private void OnDragFieldBeginDrag()
        {
            m_isDragging = true;
        }

        private void OnDragFieldEndDrag()
        {
            m_isDragging = false;
            OnInputFieldEndEdit(m_inputField.text);
        }

        private void OnToggleValueChange(bool value)
        {
            m_item.Value = value;
        }

        private float m_nextUpdate = 0.0f;
        private void Update()
        {
            if (m_isDragging)
            {
                UpdateValue(m_inputField.text);
                return;
            }

            if (m_nextUpdate > Time.time)
            {
                return;
            }
            m_nextUpdate = Time.time + 0.2f;
            if(m_item == null)
            {
                return;
            }

           
            bool hasChildren = m_item.Children != null && m_item.Children.Count > 0;
            if(hasChildren)
            {
                return;
            }

            bool isBool = m_item.Value is bool;
            if (isBool)
            {
                if (m_toggle != null)
                {
                    m_toggle.isOn = (bool)m_item.Value;
                }
            }
            else
            {
                if (m_inputField != null)
                {
                    m_inputField.text = m_item.Value + "";
                }
            }
        }
    }
}
