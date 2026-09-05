using UnityEditor;
using UnityEngine;

namespace LP.HexTileTDR.Core
{
    [CustomEditor(typeof(HexMapGenerator))]
    public class HexMapGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default inspector fields
            DrawDefaultInspector();

            HexMapGenerator mapGenerator = (HexMapGenerator)target;

            GUILayout.Space(10);

            // Regenerate Button
            if (GUILayout.Button("Regenerate Map", GUILayout.Height(30)))
            {
                mapGenerator.GenerateMap();
                EditorUtility.SetDirty(mapGenerator);
            }

            // Clear Map Button
            if (GUILayout.Button("Clear Map", GUILayout.Height(30)))
            {
                mapGenerator.ClearMap();

                // Re-sync Placement Manager so dictionary/ghost tracking updates immediately
                HexPlacementManager placementManager = FindAnyObjectByType<HexPlacementManager>();
                if (placementManager != null)
                {
                    placementManager.ClearAndRegisterTiles();
                }

                EditorUtility.SetDirty(mapGenerator);
            }
        }
    }
}