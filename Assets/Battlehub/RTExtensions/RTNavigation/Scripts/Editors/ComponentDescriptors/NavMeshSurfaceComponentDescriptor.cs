using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.AI;

namespace Battlehub.RTNavigation
{
    [BuiltInDescriptor]
    public class NavMeshSurfaceComponentDescriptor : ComponentDescriptorBase<NavMeshSurface, NavMeshSurfaceGizmo>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            ILocalization lc = IOC.Resolve<ILocalization>();

            MethodInfo bakeMethodInfo = Strong.MethodInfo((NavMeshSurface x) => x.BuildNavMesh());
            MethodInfo clearMethodInfo = Strong.MethodInfo((NavMeshSurface x) => x.RemoveData());

            List<PropertyDescriptor> descriptors = new List<PropertyDescriptor>();
            descriptors.Add(new PropertyDescriptor(lc.GetString("ID_RTNavigation_NavMeshAgentComponentDescriptor_Bake"), editor.Components, bakeMethodInfo) 
            { 
                ValueChangedCallback = () => editor.BuildEditor() 
            });
            descriptors.Add(new PropertyDescriptor(lc.GetString("ID_RTNavigation_NavMeshAgentComponentDescriptor_Clear"), editor.Components, clearMethodInfo)
            {
                ValueChangedCallback = () => editor.BuildEditor()
            });

            return descriptors.ToArray();
        }
    }
}
