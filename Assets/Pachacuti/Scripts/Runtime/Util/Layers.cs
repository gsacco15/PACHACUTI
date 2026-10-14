using UnityEngine;

namespace Pachacuti
{
    /// <summary>Physics layers (ProjectSettings/TagManager.asset).</summary>
    public static class Layers
    {
        public const int Default = 0;
        public const int IgnoreRaycast = 2;
        public const int Player = 6;
        public const int Characters = 7;
        /// <summary>Gripable masonry. Artists get it for free by naming materials inca_masonry_* (see ModelImportRules).</summary>
        public const int Climbable = 8;
        public const int Sky = 9;

        /// <summary>What blocks a soldier's line of sight: world geometry, not people.</summary>
        public static readonly int SightBlockers = (1 << Default) | (1 << Climbable);
        /// <summary>What Manco and the camera collide with.</summary>
        public static readonly int World = (1 << Default) | (1 << Climbable);

        public static void SetRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetRecursively(t.gameObject, layer);
        }
    }
}
