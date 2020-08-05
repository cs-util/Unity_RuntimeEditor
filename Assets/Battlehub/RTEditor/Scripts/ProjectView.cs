using UnityEngine;
using Battlehub.RTCommon;
using TMPro;

namespace Battlehub.RTEditor
{
    public class ProjectView : RuntimeWindow
    {
        [SerializeField]
        private ProjectTreeView m_projectTree = null;
        public RuntimeWindow ProjectTree
        {
            get { return m_projectTree; }
        }
        [SerializeField]
        private ProjectFolderView m_projectResources = null;
        public RuntimeWindow ProjectFolder
        {
            get { return m_projectResources; }
        }

        [SerializeField]
        private TMP_InputField m_filterInput = null;
        public TMP_InputField FilterInput
        {
            get { return m_filterInput; }
        }

        //public override int Depth
        //{ 
        //    get { return base.Depth; }
        //    set
        //    { 
        //        base.Depth = value;
        //        ProjectTree.Depth = value + 1;
        //        ProjectFolder.Depth = value + 1;
        //    }
        //}

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.Project;
            base.AwakeOverride();

            //ProjectTree.Depth = Depth + 1;
           // ProjectFolder.Depth = Depth + 1;
        }

        private void Start()
        {
            ProjectViewImpl impl = GetComponent<ProjectViewImpl>();
            if (!impl)
            {
                gameObject.AddComponent<ProjectViewImpl>();
            }
        }

    }
}
