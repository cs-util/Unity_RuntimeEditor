using Battlehub.RTEditor;
using UnityEngine.AI;

namespace Battlehub.RTNavigation
{
    [BuiltInDescriptor]
    public class NavMeshSurfaceComponentDescriptor : ComponentDescriptorBase<NavMeshSurface>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            return new PropertyDescriptor[0];
        }
    }
}
