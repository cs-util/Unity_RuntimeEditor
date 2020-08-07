using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Battlehub.RTSL.Internal
{
    public static class MappingsExportImportMenu 
    {
        [MenuItem("Tools/Runtime SaveLoad/Persistent Classes/Export Mappings")]
        public static void ExportMappings()
        {
            PersistentMappingsExporterWindow prevWindow = EditorWindow.GetWindow<PersistentMappingsExporterWindow>();
            if (prevWindow != null)
            {
                prevWindow.Close();
            }

            PersistentMappingsExporterWindow window = ScriptableObject.CreateInstance<PersistentMappingsExporterWindow>();
            window.titleContent = new GUIContent("Export Mappings");
            window.Show();
            window.position = new Rect(20, 40, 1280, 768);
        }

        [MenuItem("Tools/Runtime SaveLoad/Persistent Classes/Import Mappings")]
        public static void ImportMappings()
        {
            string path = EditorUtility.OpenFilePanel("Import Mappings", "", "txt");
            PersistentClassMapping[] classMappings = null;
            PersistentClassMapping[] surrogateMappings = null;

            try
            {
                MappingsUtility.Import(path, out classMappings, out surrogateMappings);
                MappingsUtility.MergeClassMappings(classMappings);
                MappingsUtility.MergeSurrogateMappings(surrogateMappings);
            }
            finally
            {
                Cleanup(classMappings);
                Cleanup(surrogateMappings);
            }
        }

        private static void Cleanup(PersistentClassMapping[] mappings)
        {
            if (mappings != null)
            {
                for (int i = 0; i < mappings.Length; ++i)
                {
                    if (mappings[i] != null)
                    {
                        Object.DestroyImmediate(mappings[i].gameObject);
                    }
                }
            }
        }
    }

}
