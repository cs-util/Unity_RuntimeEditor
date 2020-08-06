using System;
using System.Reflection;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class OptionsEditorFloat : OptionsEditor<float>
    {
        protected override void SetInputField(float value)
        {
            if (HasMixedValues())
            {
                m_mixedValuesIndicator.text = "-";
            }
            else
            {
                m_input.value = Mathf.RoundToInt(value); 
                m_mixedValuesIndicator.text = m_input.options[Mathf.RoundToInt(ToIndex(value))].text;
            }
        }

        protected override void OnValueChanged(int index)
        {
            SetValue(ToValue(index));
            SetInputField(ToValue(index));
            EndEdit();
        }

        protected override void InitOverride(object[] target, object[] accessor, MemberInfo memberInfo, Action<object, object> eraseTargetCallback, string label = null)
        {
            base.InitOverride(target, accessor, memberInfo, eraseTargetCallback, label);
            m_currentValue = -1;
        }
    }
}
