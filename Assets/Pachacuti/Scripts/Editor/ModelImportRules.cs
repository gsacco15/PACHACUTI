using UnityEditor;
using UnityEngine;

namespace Pachacuti.EditorTools
{
    /// <summary>
    /// Import rules for art under Assets/Pachacuti/Art (from week 3, when the contract artist's
    /// meshes replace grey-box primitives). Pitch slice §13: "Climbable tagging by material name
    /// (inca_masonry_* = climbable) so artists control it without code."
    ///
    ///   inca_masonry_*        → Climbable layer (gripable polygonal masonry)
    ///   adobe_* / plaster_*   → Default layer (smooth: Manco slides off with a scrape)
    ///   thatch_*              → Default layer (walkable roofs, not climbable)
    ///   *_nocollide           → no collider
    ///
    /// FBX is Unity's native path. If the pipeline stays on GLB (as in the Three.js plan),
    /// add com.unity.cloud.gltfast and mirror these rules in a glTFast import hook.
    /// </summary>
    public sealed class ModelImportRules : AssetPostprocessor
    {
        const string ArtRoot = "Assets/Pachacuti/Art/";
        public const string ClimbablePrefix = "inca_masonry_";

        static bool InScope(string path) => path.Replace('\\', '/').StartsWith(ArtRoot);

        void OnPreprocessModel()
        {
            if (!InScope(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            importer.addCollider = true;          // mesh colliders for the level kit
            importer.isReadable = false;          // keep CPU copies out of the 60 MB budget
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.importCameras = false;
            importer.importLights = false;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!InScope(assetPath)) return;
            int climbable = LayerMask.NameToLayer("Climbable");
            int tagged = 0;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                bool masonry = false, noCollide = false;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    string n = m.name.ToLowerInvariant();
                    if (n.StartsWith(ClimbablePrefix)) masonry = true;
                    if (n.EndsWith("_nocollide")) noCollide = true;
                }
                if (masonry && climbable >= 0)
                {
                    r.gameObject.layer = climbable;
                    tagged++;
                }
                if (noCollide)
                    foreach (var c in r.GetComponents<Collider>()) Object.DestroyImmediate(c, true);
            }
            if (tagged > 0) Debug.Log($"[PACHACUTI] {assetPath}: {tagged} masonry mesh(es) tagged Climbable.");
        }
    }
}
