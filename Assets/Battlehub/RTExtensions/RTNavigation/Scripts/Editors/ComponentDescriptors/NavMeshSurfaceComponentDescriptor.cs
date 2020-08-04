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
            
            MethodInfo bakeMethodInfo = Strong.MethodInfo((NavMeshSurface x) => x.BuildNavMesh());
            MethodInfo clearMethodInfo = Strong.MethodInfo((NavMeshSurface x) => x.RemoveData());

            List<PropertyDescriptor> descriptors = new List<PropertyDescriptor>();
            descriptors.Add(new PropertyDescriptor("Bake", editor.Components, bakeMethodInfo));
            descriptors.Add(new PropertyDescriptor("Clear", editor.Components, clearMethodInfo));

            return descriptors.ToArray();
        }
    }
}
