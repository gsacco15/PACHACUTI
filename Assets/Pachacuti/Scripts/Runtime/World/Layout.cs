using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// Key coordinates of the grey-box (pitch slice §4, "Zones in play order").
    /// +Z is north, +X is east, metres, ground at y = 0. One district of Inca Cusco
    /// (~250 × 250 m) plus Qenqo as a detached scene. Real anchors are approximate;
    /// the rest of the district is plausible, not reconstructed (§16).
    /// </summary>
    public static class Layout
    {
        // ------------------------------------------------------------- Zone A: Rooftops (Beat 1)
        // Three roofs of the palace block north of Hatunrumiyoc, ~80 m run, two gaps,
        // one 6 m climb that exceeds one breath, a torch patrol below.
        public const float BlockX0 = -8f, BlockX1 = 6f;
        public const float R1Top = 4.5f, R1Z0 = 148f, R1Z1 = 172f;
        public const float Gap1Z0 = 146f;                       // 2.0 m gap
        public const float R2Top = 5.0f, R2Z0 = 126f, R2Z1 = 146f;
        public const float TowerTop = 11.0f, TowerZ0 = 110f, TowerZ1 = 126f; // 6 m masonry climb from R2
        public const float TowerLedge = 8.0f;
        public const float Gap2Z0 = 107.8f;                     // 2.2 m gap, jump down 3 m
        public const float R3Top = 8.0f, R3Z0 = 92f, R3Z1 = 107.8f;
        public const float PorchTop = 4.6f, PorchZ0 = 86f, PorchZ1 = 92f;
        public static readonly Vector3 ColdOpenSpawn = new Vector3(-1f, R1Top, 167f);
        public static readonly float ColdOpenYaw = 180f;        // facing south, toward the Coricancha
        public static readonly Vector3[] RooftopPatrol =
        {
            new Vector3(-10f, 0f, 168f), new Vector3(-10f, 0f, 147f), new Vector3(8f, 0f, 147f),
            new Vector3(8f, 0f, 118f), new Vector3(8f, 0f, 147f), new Vector3(-10f, 0f, 147f), new Vector3(-10f, 0f, 104f),
        };

        // ------------------------------------------------------------- Zone B: Huáscar's compound (story beat)
        public const float CompoundX0 = -21f, CompoundX1 = 19f, CompoundZ0 = 50f, CompoundZ1 = 92f;
        public static readonly Bounds CompoundCourt = MinMax(new Vector3(-20f, -0.5f, 51f), new Vector3(18f, 2.5f, 86f));
        public static readonly Vector3 WomanPost = new Vector3(-12.6f, 0f, 63f);
        public static readonly Vector3 CompoundDoor = new Vector3(-10f, 0f, 50.3f);
        /// <summary>"capture triggers a short chase and a reset to the compound door".</summary>
        public static readonly Vector3 StreetResetSpawn = new Vector3(-10f, 0f, 53f);

        // ------------------------------------------------------------- Zone C: Hatunrumiyoc street (Beat 2)
        // ~120 m, 4 m wide; checkpoint at the midpoint; four doorways; the kinsman; the twelve-angled stone.
        public const float StreetX0 = -12f, StreetX1 = -8f, LaneX = -10f;
        public const float StreetNorthZ = 50f, StreetSouthZ = -70f;
        public const float CheckpointZ = -10f;
        public const float TupaDoorZ = 0f;                      // 10 m before the checkpoint
        public static readonly float[] WestDoorZ = { 30f, -24f };
        public static readonly float[] EastDoorZ = { TupaDoorZ, -46f };
        public const float TwelveAngledStoneZ = -32f;
        public static readonly Bounds StreetEntry = MinMax(new Vector3(-12f, -0.5f, 40f), new Vector3(-8f, 3f, 50.2f));
        public static readonly Bounds StreetExit = MinMax(new Vector3(-12f, -0.5f, -76f), new Vector3(0f, 3f, -70f));
        public static readonly Vector3 TorchSoldierPost = new Vector3(-8.7f, 0f, -8f);
        public static readonly Vector3 MaceSoldierWestPost = new Vector3(-11.4f, 0f, -10.5f);
        public static readonly Vector3 MaceSoldierEastPost = new Vector3(-8.6f, 0f, -11.5f);

        // ------------------------------------------------------------- Zone D: Storehouse yard (Beat 3)
        public const float YardX0 = -12f, YardX1 = 18f, YardZ0 = -100f, YardZ1 = -70f;
        public static readonly Vector3 NinanSeat = new Vector3(4f, 0f, -85f);
        public static readonly Vector3 Fire = new Vector3(5.9f, 0f, -84.2f);
        public static readonly Vector3 Drain = new Vector3(14f, 0f, -95f);
        public static readonly Bounds NinanApproach = MinMax(new Vector3(-1f, -0.5f, -83f), new Vector3(9f, 3f, -76f));
        public static readonly Vector3 PorterGather = new Vector3(-3f, 0f, -90f);

        // ------------------------------------------------------------- Zone E: The tunnel
        // Stone drain under the street, ~40 m; one grate with torchlight; one low crawl.
        public const float TunnelFloor = -3f, TunnelCeiling = -1.2f;
        public const float TunnelZ0 = -96f, TunnelZ1 = -94f, TunnelZ = -95f;
        public const float TunnelX0 = 12.4f, TunnelX1 = 47f;
        public const float GrateX = 30f;
        public const float CrawlX0 = 38f, CrawlX1 = 43f, CrawlCeiling = -2.15f;
        public const float RampX0 = 47f, RampX1 = 51.5f;
        public static readonly Vector3 TunnelStart = new Vector3(14f, TunnelFloor, TunnelZ);
        public static readonly Bounds TunnelExit = MinMax(new Vector3(49.5f, -1.5f, -97f), new Vector3(56f, 3f, -93f));

        // ------------------------------------------------------------- Zone F: Coricancha wall & roof (Beat 4)
        // 15 m climb (~2 breaths with a ledge), 20 × 20 m roof, view over the whole district.
        public const float CorX0 = 56f, CorX1 = 76f, CorZ0 = -106f, CorZ1 = -86f, CorTop = 15f, CorLedge = 7.5f;
        public static readonly Bounds CoricanchaRoof = MinMax(new Vector3(56.3f, 14.5f, -105.7f), new Vector3(75.7f, 18f, -86.3f));
        public static readonly Vector3 CoricanchaZenith = new Vector3(66f, CorTop, -96f);

        // ------------------------------------------------------------- Zone G: Qenqo (Beat 5)
        // Detached 60 × 60 m scene. Real bearing Coricancha → Qenqo ≈ 49.5°, ~1.7 km, ~180 m higher.
        public static readonly Vector3 QenqoOrigin = new Vector3(5000f, 0f, 0f);
        /// <summary>The zigzag channel runs toward the Pleiades (azimuth ~21° at 22:00 on the slice night).</summary>
        public const float QenqoChannelAzimuth = 21f;
        public static readonly Vector3 CajamarcaOrigin = new Vector3(-6000f, 0f, 0f);
        public static readonly Vector3 GoldRoomOrigin = new Vector3(-6000f, 0f, 700f);

        public static Bounds MinMax(Vector3 min, Vector3 max)
        {
            var b = new Bounds();
            b.SetMinMax(min, max);
            return b;
        }

        public static Vector3 Dir(float azimuthDeg) => new Vector3(Mathf.Sin(azimuthDeg * Mathf.Deg2Rad), 0f, Mathf.Cos(azimuthDeg * Mathf.Deg2Rad));
    }
}
