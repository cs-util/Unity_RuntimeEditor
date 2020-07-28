using UnityEngine;
using System.Reflection;
using Battlehub.Utils;
using Battlehub.RTCommon;

namespace Battlehub.RTEditor
{
    [BuiltInDescriptor]
    public class RectTransformComponentDescriptor : ComponentDescriptorBase<RectTransform>
    {
        public override object CreateConverter(ComponentEditor editor)
        {
            object[] converters = new object[editor.Components.Length];
            Component[] components = editor.Components;
            for (int i = 0; i < components.Length; ++i)
            {
                Transform transform = (Transform)components[i];
                if (transform != null)
                {
                    converters[i] = new TransformPropertyConverter
                    {
                        ExposeToEditor = transform.GetComponent<ExposeToEditor>()
                    };
                }
            }
            return converters;
        }

        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            object[] converters = (object[])converter;

            ILocalization lc = IOC.Resolve<ILocalization>();

            MemberInfo position = Strong.PropertyInfo((Transform x) => x.localPosition, "localPosition");
            MemberInfo positionConverted = Strong.PropertyInfo((TransformPropertyConverter x) => x.LocalPosition, "LocalPosition");
            MemberInfo rotation = Strong.PropertyInfo((Transform x) => x.localRotation, "localRotation");
            MemberInfo rotationConverted = Strong.PropertyInfo((TransformPropertyConverter x) => x.LocalEuler, "LocalEulerAngles");
            MemberInfo scale = Strong.PropertyInfo((Transform x) => x.localScale, "localScale");
            MemberInfo scaleConverted = Strong.PropertyInfo((TransformPropertyConverter x) => x.LocalScale, "LocalScale");

            return new[]
                {
                    new PropertyDescriptor( lc.GetString("ID_RTEditor_CD_Transform_Position", "Position"), converters, positionConverted, position) ,
                    new PropertyDescriptor( lc.GetString("ID_RTEditor_CD_Transform_Rotation", "Rotation"), converters, rotationConverted, rotation),
                    new PropertyDescriptor( lc.GetString("ID_RTEditor_CD_Transform_Scale", "Scale"), converters, scaleConverted, scale)
                };
        }
    }
}

