using Battlehub.RTCommon;
using Battlehub.RTGizmos;
using Battlehub.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Battlehub.RTNavigation
{
    public class NavMeshSurfaceGizmo : BaseGizmo
    {
        private NavMeshSurface m_surface;
        private Mesh m_mesh;
        [SerializeField]
        private Material m_material;

        protected override CameraEvent CameraEvent
        {
            get { return CameraEvent.AfterForwardAlpha; }
        }

        protected override bool ForceCreateCamera
        {
            get { return true; }
        }


        protected override void Awake()
        {
            base.Awake();
            m_surface = GetComponent<NavMeshSurface>();
   
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            
            m_material = new Material(Shader.Find("Battlehub/RTNavigation/UnlitColor"));
            m_material.color = new Color(1, 1, 1, 0.5f);
            m_material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            m_material.SetFloat("_ZWrite", 0);

            m_mesh = new Mesh();
            m_mesh.vertices = triangulation.vertices;
            
            int[] tris = triangulation.indices;
            m_mesh.triangles = tris;

            int[] areas = triangulation.areas;
            Color32[] colors = new Color32[triangulation.vertices.Length];
            for(int i = 0; i < areas.Length; i++)
            {
                int area = areas[i];
                Color32 color = Colors.Kellys[(area + 3) % Colors.Kellys.Length];
                colors[tris[i * 3]] = color;
                colors[tris[i * 3 + 1]] = color;
                colors[tris[i * 3 + 2]] = color;
            }
            m_mesh.colors32 = colors;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if(m_mesh != null)
            {
                Destroy(m_mesh);
                m_mesh = null;
            }

            if(m_material != null)
            {
                Destroy(m_material);
                m_material = null;
            }
        }

        protected override void OnCommandBufferRefresh(IRTECamera camera)
        {
            if(m_mesh != null)
            {
                camera.CommandBuffer.DrawMesh(m_mesh, Matrix4x4.identity, m_material);
            }
        }

    }

}
