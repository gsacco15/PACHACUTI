using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;
using P = Pachacuti.Greybox.Palette;

namespace Pachacuti
{
    /// <summary>References into the built district that the beats need.</summary>
    public sealed class District
    {
        public Transform Root;
        public GameObject CompoundDoor;     // opens when Manco has the load
        public GameObject DrainCover;       // Ninan points to it
        public Vector3 TupaDoorway;         // inside the east recess
        public Vector3 TupaStepOut;         // in the street
        public readonly List<Vector3> Doorways = new List<Vector3>();
        public readonly List<Transform> Commons = new List<Transform>();
        public Transform CordRack;
        public Transform CoricanchaRoofline;
        public ClimbSurface ZoneAWall;
        public ClimbSurface CoricanchaWall;
    }

    /// <summary>
    /// Builds pitch slice §4 zones A–F from primitives. Week 1's "grey-box the whole
    /// thing": every zone at its spec'd size, walkable end to end, nothing pretty.
    /// </summary>
    public static class DistrictBuilder
    {
        public static District Build(Transform parent)
        {
            var d = new District { Root = new GameObject("District").transform };
            d.Root.SetParent(parent, false);

            Ground(d.Root);
            ZoneA_Rooftops(d);
            ZoneB_Compound(d);
            ZoneC_Street(d);
            ZoneD_Yard(d);
            ZoneE_Tunnel(d);
            ZoneF_Coricancha(d);
            Landmarks(d.Root);
            return d;
        }

        // ------------------------------------------------------------------ ground

        static void Ground(Transform root)
        {
            var holes = new List<Rect>
            {
                Rect.MinMaxRect(Layout.Drain.x - 0.8f, Layout.Drain.z - 0.8f, Layout.Drain.x + 0.8f, Layout.Drain.z + 0.8f),
                Rect.MinMaxRect(Layout.GrateX - 0.4f, Layout.TunnelZ0 + 0.2f, Layout.GrateX + 0.4f, Layout.TunnelZ1 - 0.2f),
                Rect.MinMaxRect(Layout.RampX0, Layout.TunnelZ0, Layout.RampX1, Layout.TunnelZ1),
            };
            Greybox.GroundWithHoles(root, Rect.MinMaxRect(-90f, -170f, 170f, 230f), -Layout.TunnelCeiling, holes, Surface.Earth, P.Earth);
        }

        // ------------------------------------------------------------------ Zone A — Rooftops (Beat 1)

        static void ZoneA_Rooftops(District d)
        {
            var z = new GameObject("A Rooftops").transform;
            z.SetParent(d.Root, false);
            float x0 = Layout.BlockX0, x1 = Layout.BlockX1;

            // R1: a thatched house roof — the cold open.
            Greybox.Building(z, "R1", x0, x1, Layout.R1Z0, Layout.R1Z1, Layout.R1Top, Surface.Adobe);
            // Gap 1 (2.0 m) over the cross alley where the patrol passes.
            Greybox.Link(z, "Jump R1>R2", LinkKind.Jump, new Vector3(x0 + 0.3f, Layout.R1Top - 0.6f, Layout.R1Z0), new Vector3(x1 - 0.3f, Layout.R1Top + 1f, Layout.R1Z0 + 1.4f),
                         new Vector3(0, Layout.R2Top, Layout.R2Z1 - 1.2f), Vector3.back);
            Greybox.Link(z, "Jump R2>R1", LinkKind.Jump, new Vector3(x0 + 0.3f, Layout.R2Top - 0.6f, Layout.R2Z1 - 1.4f), new Vector3(x1 - 0.3f, Layout.R2Top + 1f, Layout.R2Z1),
                         new Vector3(0, Layout.R1Top, Layout.R1Z0 + 1.2f), Vector3.forward);

            // R2: the palace's lower range, masonry walls (climbable from the alleys).
            var r2Min = new Vector3(x0, 0, Layout.R2Z0);
            var r2Max = new Vector3(x1, Layout.R2Top - 0.25f, Layout.R2Z1);
            var r2 = Greybox.Block(z, "R2", r2Min, r2Max, Surface.Masonry);
            Greybox.ThatchTop(r2.transform, r2Min, r2Max);
            Greybox.ClimbFace(r2, r2Min, r2Max, Face.West, 0f, Layout.R2Top, label: "R2 west (from alley)");
            Greybox.ClimbFace(r2, r2Min, r2Max, Face.East, 0f, Layout.R2Top, label: "R2 east (from alley)");

            // The tower: the one 6 m masonry climb that exceeds one breath (§5), with a mid ledge.
            var tMin = new Vector3(x0, 0, Layout.TowerZ0);
            var tMax = new Vector3(x1, Layout.TowerTop, Layout.TowerZ1);
            var tower = Greybox.Block(z, "Tower", tMin, tMax, Surface.Masonry);
            d.ZoneAWall = Greybox.ClimbFace(tower, tMin, tMax, Face.North, Layout.R2Top, Layout.TowerTop,
                                            new[] { Layout.TowerLedge - Layout.R2Top }, Tuning.ZoneAWallDrainMultiplier, true, 0.6f, "Zone A 6 m wall");
            Greybox.ClimbFace(tower, tMin, tMax, Face.West, 0f, Layout.TowerTop, new[] { 4f, 8f }, 1f, true, 0.6f, "Tower west (from alley)");

            // Gap 2 (2.2 m), a jump down onto R3.
            Greybox.Link(z, "Jump Tower>R3", LinkKind.Jump, new Vector3(x0 + 0.3f, Layout.TowerTop - 0.6f, Layout.TowerZ0), new Vector3(x1 - 0.3f, Layout.TowerTop + 1f, Layout.TowerZ0 + 1.4f),
                         new Vector3(0, Layout.R3Top, Layout.Gap2Z0 - 1.3f), Vector3.back);

            // R3: the compound's north range; a porch below it on the court side.
            Greybox.Building(z, "R3", x0, x1, Layout.R3Z0, Layout.Gap2Z0, Layout.R3Top, Surface.Adobe);
            var porch = Greybox.Block(z, "Porch", new Vector3(x0, Layout.PorchTop - 0.25f, Layout.PorchZ0), new Vector3(x1, Layout.PorchTop, Layout.PorchZ1), Surface.Thatch);
            Greybox.Block(porch.transform, "Post W", new Vector3(x0 + 0.2f, 0, Layout.PorchZ0 + 0.2f), new Vector3(x0 + 0.5f, Layout.PorchTop - 0.25f, Layout.PorchZ0 + 0.5f), Surface.Plain, P.Wood);
            Greybox.Block(porch.transform, "Post E", new Vector3(x1 - 0.5f, 0, Layout.PorchZ0 + 0.2f), new Vector3(x1 - 0.2f, Layout.PorchTop - 0.25f, Layout.PorchZ0 + 0.5f), Surface.Plain, P.Wood);
            Greybox.Link(z, "Drop R3>Porch", LinkKind.Drop, new Vector3(x0 + 0.3f, Layout.R3Top - 0.6f, Layout.R3Z0), new Vector3(x1 - 0.3f, Layout.R3Top + 1f, Layout.R3Z0 + 1.3f),
                         new Vector3(0, Layout.PorchTop, Layout.PorchZ0 + 2.5f), Vector3.back);
            Greybox.Link(z, "Drop Porch>Court", LinkKind.Drop, new Vector3(x0 + 0.3f, Layout.PorchTop - 0.6f, Layout.PorchZ0), new Vector3(x1 - 0.3f, Layout.PorchTop + 1f, Layout.PorchZ0 + 1.3f),
                         new Vector3(0, 0f, Layout.PorchZ0 - 1.2f), Vector3.back);

            // House rows closing the alleys (adobe, not climbable) and the north end.
            Greybox.Building(z, "West houses", -20f, -12f, 92f, 176f, 7f, Surface.Adobe);
            Greybox.Building(z, "East houses", 10f, 18f, 92f, 176f, 7f, Surface.Adobe);
            Greybox.Building(z, "North houses", -20f, 18f, Layout.R1Z1, 180f, 7.5f, Surface.Adobe);
            for (float zz = 100f; zz < 170f; zz += 14f)
            {
                Greybox.FakeDoor(z, new Vector3(-12f, 0, zz), Face.East);
                Greybox.FakeDoor(z, new Vector3(10f, 0, zz + 7f), Face.West);
            }
        }

        // ------------------------------------------------------------------ Zone B — Huáscar's compound

        static void ZoneB_Compound(District d)
        {
            var z = new GameObject("B Compound").transform;
            z.SetParent(d.Root, false);
            float h = 4.5f;
            float cx0 = Layout.CompoundX0, cx1 = Layout.CompoundX1, cz0 = Layout.CompoundZ0, cz1 = Layout.CompoundZ1;
            Greybox.Block(z, "Wall W", new Vector3(cx0, 0, cz0), new Vector3(cx0 + 0.6f, h, cz1), Surface.Adobe);
            Greybox.Block(z, "Wall E", new Vector3(cx1 - 0.6f, 0, cz0), new Vector3(cx1, h, cz1), Surface.Adobe);
            Greybox.Block(z, "Wall N-W", new Vector3(cx0, 0, cz1 - 0.6f), new Vector3(Layout.BlockX0, h, cz1), Surface.Adobe);
            Greybox.Block(z, "Wall N-E", new Vector3(Layout.BlockX1, 0, cz1 - 0.6f), new Vector3(cx1, h, cz1), Surface.Adobe);
            // South wall with the one exit onto the street.
            float doorX0 = Layout.StreetX0 + 0.5f, doorX1 = Layout.StreetX1 - 0.5f;
            Greybox.Block(z, "Wall S-W", new Vector3(cx0, 0, cz0), new Vector3(doorX0, h, cz0 + 0.6f), Surface.Adobe);
            Greybox.Block(z, "Wall S-E", new Vector3(doorX1, 0, cz0), new Vector3(cx1, h, cz0 + 0.6f), Surface.Adobe);
            Greybox.Block(z, "Lintel", new Vector3(doorX0, 2.4f, cz0), new Vector3(doorX1, h, cz0 + 0.6f), Surface.Masonry, P.MasonryDark);
            d.CompoundDoor = Greybox.Block(z, "Door", new Vector3(doorX0, 0, cz0 + 0.1f), new Vector3(doorX1, 2.4f, cz0 + 0.5f), Surface.Plain, P.Wood);

            // Three rooms round the court.
            Greybox.Building(z, "Room W", cx0 + 0.6f, -14f, 56f, 72f, 3.8f, Surface.Adobe);
            Greybox.FakeDoor(z, new Vector3(-14f, 0, 64f), Face.East);
            Greybox.Building(z, "Room E", 12f, cx1 - 0.6f, 56f, 72f, 3.8f, Surface.Adobe);
            Greybox.FakeDoor(z, new Vector3(12f, 0, 64f), Face.West);
            Greybox.Building(z, "Room S", -2f, 10f, cz0 + 0.6f, 56f, 3.6f, Surface.Adobe);
            Greybox.FakeDoor(z, new Vector3(4f, 0, 56f), Face.North);

            // Things left behind by a house emptying in a hurry.
            var cloth = MaterialKit.Lit(P.Cloth);
            for (int i = 0; i < 6; i++)
                Greybox.Box(z, "Bundle", new Vector3(-16f + i * 1.3f, 0.25f, 75f + (i % 2) * 1.5f), new Vector3(0.7f, 0.5f, 0.5f), cloth, Layers.Default, true, Quaternion.Euler(0, i * 37f, 0));
            Greybox.Primitive(z, PrimitiveType.Cylinder, "Jar", new Vector3(8f, 0.45f, 80f), new Vector3(0.6f, 0.45f, 0.6f), MaterialKit.Lit(new Color(0.5f, 0.3f, 0.2f)), true);
            Greybox.Primitive(z, PrimitiveType.Cylinder, "Jar", new Vector3(9f, 0.4f, 79f), new Vector3(0.5f, 0.4f, 0.5f), MaterialKit.Lit(new Color(0.45f, 0.28f, 0.18f)), true);
        }

        // ------------------------------------------------------------------ Zone C — Hatunrumiyoc street (Beat 2)

        static void ZoneC_Street(District d)
        {
            var z = new GameObject("C Street").transform;
            z.SetParent(d.Root, false);
            float n = Layout.StreetNorthZ, s = Layout.StreetSouthZ;
            // Paving.
            Greybox.Block(z, "Paving", new Vector3(Layout.StreetX0, -0.05f, s), new Vector3(Layout.StreetX1, 0.01f, n), Surface.Masonry, P.Paving);

            // West wall: adobe over a masonry footing. East wall: palace masonry (the twelve-angled
            // stone's wall), gripable to 2.6 m only — adobe above refuses.
            WallWithDoors(z, -14f, -12f, s, n, Layout.WestDoorZ, 5f, isWest: true, d);
            WallWithDoors(z, -8f, -6f, s, n, Layout.EastDoorZ, 5f, isWest: false, d);
            d.TupaDoorway = new Vector3(-7.45f, 0f, Layout.TupaDoorZ);
            d.TupaStepOut = new Vector3(-8.7f, 0f, Layout.TupaDoorZ - 0.3f);

            // The twelve-angled stone, set in the east wall facing the street.
            var outline = new List<P2>();
            float[] radii = { 0.62f, 0.55f, 0.7f, 0.5f, 0.66f, 0.58f, 0.72f, 0.52f, 0.64f, 0.57f, 0.69f, 0.54f };
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + 0.2f;
                outline.Add(new P2(Mathf.Cos(a) * radii[i] * 1.25f, Mathf.Sin(a) * radii[i]));
            }
            var stone = new GameObject("Twelve-angled stone");
            stone.transform.SetParent(z, false);
            stone.transform.position = new Vector3(-8f - 0.02f, 1.3f, Layout.TwelveAngledStoneZ);
            stone.transform.rotation = Quaternion.LookRotation(Vector3.right);
            stone.AddComponent<MeshFilter>().sharedMesh = MeshKit.Polygon(outline, Color.white, Vector2.one, Vector2.zero);
            stone.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Lit(new Color(0.42f, 0.42f, 0.4f), Surface.Plain, 0.35f);

            // Checkpoint furniture.
            var crate = MaterialKit.Lit(P.Wood);
            Greybox.Box(z, "Checkpoint bench", new Vector3(-11.6f, 0.25f, Layout.CheckpointZ - 2f), new Vector3(0.6f, 0.5f, 1.8f), crate);
            Greybox.Box(z, "Checkpoint loads", new Vector3(-8.5f, 0.3f, Layout.CheckpointZ - 3f), new Vector3(0.8f, 0.6f, 0.8f), MaterialKit.Lit(P.Cloth));
        }

        static void WallWithDoors(Transform parent, float x0, float x1, float zs, float zn, float[] doors, float height, bool isWest, District d)
        {
            const float doorW = 1.3f, doorH = 2.2f, recess = 0.9f;
            var sorted = new List<float>(doors);
            sorted.Sort();
            float cursor = zs;
            Surface lower = isWest ? Surface.Adobe : Surface.Masonry;
            float split = isWest ? 0.8f : 2.6f;
            foreach (float dz in sorted)
            {
                Segment(parent, x0, x1, cursor, dz - doorW * 0.5f, height, lower, split, isWest);
                // Recess back, lintel above the opening.
                float backX0 = isWest ? x0 : x0 + recess, backX1 = isWest ? x1 - recess : x1;
                Greybox.Block(parent, "Recess back", new Vector3(backX0, 0, dz - doorW * 0.5f), new Vector3(backX1, height, dz + doorW * 0.5f), Surface.Adobe);
                Greybox.Block(parent, "Lintel", new Vector3(x0, doorH, dz - doorW * 0.5f), new Vector3(x1, height, dz + doorW * 0.5f), Surface.Masonry, P.MasonryDark);
                d.Doorways.Add(new Vector3(isWest ? x1 - recess * 0.5f : x0 + recess * 0.5f, 0, dz));
                cursor = dz + doorW * 0.5f;
            }
            Segment(parent, x0, x1, cursor, zn, height, lower, split, isWest);
        }

        static void Segment(Transform parent, float x0, float x1, float z0, float z1, float height, Surface lower, float split, bool isWest)
        {
            if (z1 - z0 < 0.05f) return;
            var lo = Greybox.Block(parent, "Wall", new Vector3(x0, 0, z0), new Vector3(x1, split, z1), lower);
            Greybox.Block(parent, "Wall upper", new Vector3(x0, split, z0), new Vector3(x1, height, z1), Surface.Adobe);
            if (lower == Surface.Masonry && z1 - z0 > 2f)
                Greybox.ClimbFace(lo, new Vector3(x0, 0, z0), new Vector3(x1, split, z1), isWest ? Face.East : Face.West, 0f, split, null, 1f, false, 0.5f, "Street footing");
        }

        // ------------------------------------------------------------------ Zone D — Storehouse yard (Beat 3)

        static void ZoneD_Yard(District d)
        {
            var z = new GameObject("D Yard").transform;
            z.SetParent(d.Root, false);
            float h = 4f;
            Greybox.Block(z, "Wall W", new Vector3(-14f, 0, Layout.YardZ0 - 2f), new Vector3(-12f, h, Layout.YardZ1), Surface.Adobe);
            Greybox.Block(z, "Wall S", new Vector3(-14f, 0, Layout.YardZ0 - 2f), new Vector3(Layout.YardX1 + 2f, h, Layout.YardZ0), Surface.Adobe);
            Greybox.Block(z, "Wall E", new Vector3(Layout.YardX1, 0, Layout.YardZ0 - 2f), new Vector3(Layout.YardX1 + 2f, h, Layout.YardZ1 + 2f), Surface.Adobe);
            Greybox.Block(z, "Wall N", new Vector3(-6f, 0, Layout.YardZ1), new Vector3(Layout.YardX1 + 2f, h, Layout.YardZ1 + 2f), Surface.Adobe);

            // Qollqas: round storehouses with conical thatch.
            var qollqaMat = MaterialKit.Lit(new Color(0.52f, 0.47f, 0.4f), Surface.Masonry);
            var thatch = MaterialKit.Lit(P.Thatch, Surface.Thatch);
            foreach (var q in new[] { new Vector3(-7f, 0, -95f), new Vector3(-1f, 0, -95.5f), new Vector3(5f, 0, -95.5f), new Vector3(-7.5f, 0, -77f) })
            {
                Greybox.Primitive(z, PrimitiveType.Cylinder, "Qollqa", q + Vector3.up * 1.8f, new Vector3(3.6f, 1.8f, 3.6f), qollqaMat, true);
                Greybox.Primitive(z, PrimitiveType.Sphere, "Qollqa roof", q + Vector3.up * 3.7f, new Vector3(4.3f, 1.8f, 4.3f), thatch);
                Greybox.FakeDoor(z, q + new Vector3(0, 0, 1.82f), Face.North, 0.8f, 1.4f);
            }

            // Cord racks by the fire.
            var rack = new GameObject("Cord rack").transform;
            rack.SetParent(z, false);
            rack.position = new Vector3(8f, 0, -81f);
            var wood = MaterialKit.Lit(P.Wood);
            Greybox.Box(rack, "Post", rack.position + new Vector3(-1.2f, 1f, 0), new Vector3(0.12f, 2f, 0.12f), wood);
            Greybox.Box(rack, "Post", rack.position + new Vector3(1.2f, 1f, 0), new Vector3(0.12f, 2f, 0.12f), wood);
            Greybox.Box(rack, "Bar", rack.position + new Vector3(0, 1.95f, 0), new Vector3(2.6f, 0.08f, 0.08f), wood, Layers.Default, false);
            var cordColors = new[] { new Color(0.7f, 0.6f, 0.4f), new Color(0.5f, 0.2f, 0.12f), new Color(0.25f, 0.3f, 0.45f), new Color(0.75f, 0.7f, 0.55f) };
            for (int i = 0; i < 16; i++)
            {
                float len = 0.6f + (i * 37 % 9) * 0.06f;
                Greybox.Box(rack, "Cord", rack.position + new Vector3(-1.1f + i * 0.145f, 1.95f - len * 0.5f, 0.02f), new Vector3(0.025f, len, 0.025f),
                            MaterialKit.Lit(cordColors[i % cordColors.Length]), Layers.Default, false);
            }
            d.CordRack = rack;

            // Ninan's fire: logs and a flame (light source for the quipu lesson).
            var fire = new GameObject("Fire").transform;
            fire.SetParent(z, false);
            fire.position = Layout.Fire;
            for (int i = 0; i < 4; i++)
                Greybox.Box(fire, "Log", Layout.Fire + new Vector3(0, 0.08f, 0), new Vector3(0.9f, 0.12f, 0.12f), wood, Layers.Default, false, Quaternion.Euler(0, i * 45f, 0));
            var flame = Torch.Create(fire, new Vector3(0, 0.45f, 0), 3.2f, 10f);
            flame.Radius = 5f;
            flame.transform.localScale = Vector3.one * 1.8f;

            // The drain Ninan points to: a stone slab with slits over a shaft into the tunnel.
            d.DrainCover = Greybox.Block(z, "Drain cover", Layout.Drain + new Vector3(-0.8f, -0.06f, -0.8f), Layout.Drain + new Vector3(0.8f, 0.06f, 0.8f), Surface.Masonry, P.MasonryDark);
            for (int i = 0; i < 4; i++)
                Greybox.Box(d.DrainCover.transform, "Slit", Layout.Drain + new Vector3(-0.5f + i * 0.33f, 0.065f, 0), new Vector3(0.06f, 0.01f, 1.2f), MaterialKit.Lit(Color.black), Layers.Default, false);
        }

        // ------------------------------------------------------------------ Zone E — the tunnel

        static void ZoneE_Tunnel(District d)
        {
            var z = new GameObject("E Tunnel").transform;
            z.SetParent(d.Root, false);
            float f = Layout.TunnelFloor, c = Layout.TunnelCeiling;
            float z0 = Layout.TunnelZ0, z1 = Layout.TunnelZ1;
            var stone = Surface.Masonry;
            Greybox.Block(z, "Floor", new Vector3(Layout.TunnelX0 - 0.4f, f - 0.3f, z0 - 0.4f), new Vector3(Layout.RampX0 + 0.2f, f, z1 + 0.4f), stone, P.MasonryDark);
            Greybox.Block(z, "Wall N", new Vector3(Layout.TunnelX0 - 0.4f, f, z1), new Vector3(Layout.RampX1, 0f, z1 + 0.4f), stone, P.MasonryDark);
            Greybox.Block(z, "Wall S", new Vector3(Layout.TunnelX0 - 0.4f, f, z0 - 0.4f), new Vector3(Layout.RampX1, 0f, z0), stone, P.MasonryDark);
            Greybox.Block(z, "End W", new Vector3(Layout.TunnelX0 - 0.4f, f, z0), new Vector3(Layout.TunnelX0, c, z1), stone, P.MasonryDark);
            // The low crawl.
            Greybox.Block(z, "Crawl", new Vector3(Layout.CrawlX0, Layout.CrawlCeiling, z0), new Vector3(Layout.CrawlX1, c, z1), stone, P.MasonryDark);
            // Water in the channel floor.
            var water = new GameObject("Water");
            water.transform.SetParent(z, false);
            water.transform.position = new Vector3((Layout.TunnelX0 + Layout.RampX0) * 0.5f, f + 0.02f, Layout.TunnelZ);
            water.transform.rotation = Quaternion.Euler(90f, 0, 0);
            water.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(Layout.RampX0 - Layout.TunnelX0, 0.5f, Color.white);
            water.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Alpha(new Color(0.25f, 0.35f, 0.45f, 0.35f));

            // The grate: stone bars over an opening; a soldier's torch above.
            for (int i = 0; i < 4; i++)
                Greybox.Box(z, "Grate bar", new Vector3(Layout.GrateX - 0.3f + i * 0.2f, -0.1f, Layout.TunnelZ), new Vector3(0.07f, 0.2f, 1.7f), MaterialKit.Lit(P.MasonryDark));

            // Exit: a ramp up into the walled court at the foot of the Coricancha.
            float run = Layout.RampX1 - Layout.RampX0, rise = -f;
            float len = Mathf.Sqrt(run * run + rise * rise);
            float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            var rampCenter = new Vector3((Layout.RampX0 + Layout.RampX1) * 0.5f, f * 0.5f, Layout.TunnelZ) + Quaternion.Euler(0, 0, ang) * new Vector3(0, -0.15f, 0);
            Greybox.Box(z, "Ramp", rampCenter, new Vector3(len + 0.3f, 0.3f, z1 - z0), MaterialKit.Lit(P.MasonryDark, Surface.Masonry), Layers.Default, true, Quaternion.Euler(0, 0, ang));
            // Moonlight spilling down the outlet: the only other light down here.
            var moonSpill = Torch.Create(z, new Vector3(Layout.RampX0 + 1.5f, -0.4f, Layout.TunnelZ), 0.9f, 7f, false, new Color(0.45f, 0.55f, 0.8f));
            moonSpill.Radius = 0f; // light only; not "torchlight" for stealth
        }

        // ------------------------------------------------------------------ Zone F — Coricancha wall & roof (Beat 4)

        static void ZoneF_Coricancha(District d)
        {
            var z = new GameObject("F Coricancha").transform;
            z.SetParent(d.Root, false);
            var min = new Vector3(Layout.CorX0, 0, Layout.CorZ0);
            var max = new Vector3(Layout.CorX1, Layout.CorTop, Layout.CorZ1);
            var cor = Greybox.Block(z, "Coricancha", min, max, Surface.Masonry, new Color(0.6f, 0.57f, 0.52f));
            // The curved wall stands where Santo Domingo's base is today; flat in the grey-box.
            d.CoricanchaWall = Greybox.ClimbFace(cor, min, max, Face.West, 0f, Layout.CorTop, new[] { Layout.CorLedge }, 1f, true, 2f, "Coricancha curved wall");
            // Parapet on three sides (not over the climb).
            Greybox.Block(z, "Parapet N", new Vector3(Layout.CorX0, Layout.CorTop, Layout.CorZ1 - 0.5f), new Vector3(Layout.CorX1, Layout.CorTop + 0.5f, Layout.CorZ1), Surface.Masonry);
            Greybox.Block(z, "Parapet S", new Vector3(Layout.CorX0, Layout.CorTop, Layout.CorZ0), new Vector3(Layout.CorX1, Layout.CorTop + 0.5f, Layout.CorZ0 + 0.5f), Surface.Masonry);
            Greybox.Block(z, "Parapet E", new Vector3(Layout.CorX1 - 0.5f, Layout.CorTop, Layout.CorZ0), new Vector3(Layout.CorX1, Layout.CorTop + 0.5f, Layout.CorZ1), Surface.Masonry);

            // The gold band at the roofline: fog-free, faint, visible from every zone (§4).
            var band = new GameObject("Roofline gold");
            band.transform.SetParent(z, false);
            d.CoricanchaRoofline = band.transform;
            float y = Layout.CorTop + 0.55f;
            var corners = new[]
            {
                new Vector3(Layout.CorX0, y, Layout.CorZ0), new Vector3(Layout.CorX0, y, Layout.CorZ1), new Vector3(Layout.CorX1, y, Layout.CorZ1),
                new Vector3(Layout.CorX1, y, Layout.CorZ0), new Vector3(Layout.CorX0, y, Layout.CorZ0),
            };
            var parts = new List<CombineInstance>();
            for (int i = 0; i < 4; i++)
            {
                var a = corners[i];
                var b = corners[i + 1];
                var seg = MeshKit.Ribbon(new[] { a, b }, p => 0.5f, (a + b) * 0.5f + Vector3.up * 50f, new Color(1f, 0.75f, 0.35f));
                parts.Add(new CombineInstance { mesh = seg, transform = Matrix4x4.identity });
                var seg2 = MeshKit.Ribbon(new[] { a, b }, p => 0.5f, (a + b) * 0.5f + Vector3.Cross(b - a, Vector3.up) * 5f, new Color(1f, 0.75f, 0.35f));
                parts.Add(new CombineInstance { mesh = seg2, transform = Matrix4x4.identity });
            }
            band.AddComponent<MeshFilter>().sharedMesh = MeshKit.Combine(parts, "Roofline");
            band.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Glow(new Color(1f, 0.72f, 0.3f), 0.7f);

            // Walled court at the foot of the curved wall, where the tunnel comes out.
            Greybox.Block(z, "Court wall W", new Vector3(45f, 0, Layout.CorZ0 - 1f), new Vector3(46f, 4.5f, Layout.CorZ1 + 1f), Surface.Adobe);
            Greybox.Block(z, "Court wall N", new Vector3(45f, 0, Layout.CorZ1), new Vector3(Layout.CorX0, 4.5f, Layout.CorZ1 + 1f), Surface.Adobe);
            Greybox.Block(z, "Court wall S", new Vector3(45f, 0, Layout.CorZ0 - 1f), new Vector3(Layout.CorX0, 4.5f, Layout.CorZ0), Surface.Adobe);
        }

        // ------------------------------------------------------------------ landmarks & backdrop

        static void Crest(List<Vector2> pts, float xFrom, float xTo, float y)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(xTo - xFrom) / 10f));
            for (int k = 0; k <= steps; k++)
                pts.Add(new Vector2(Mathf.Lerp(xFrom, xTo, k / (float)steps), y + (k % 2 == 0 ? 0f : 3f)));
        }

        static void Landmarks(Transform root)
        {
            var z = new GameObject("Landmarks").transform;
            z.SetParent(root, false);
            // Sacsayhuamán on its hill to the north: silhouette only, three zigzag tiers on the crest.
            var hill = new List<Vector2> { new Vector2(-520f, -40f), new Vector2(520f, -40f), new Vector2(520f, 30f), new Vector2(330f, 75f) };
            // Upper envelope, right to left: three stepped tiers with crenellated tops.
            Crest(hill, 260f, 220f, 102f);
            Crest(hill, 220f, 180f, 107f);
            Crest(hill, 180f, -180f, 112f);
            Crest(hill, -180f, -220f, 107f);
            Crest(hill, -220f, -260f, 102f);
            hill.Add(new Vector2(-330f, 75f));
            hill.Add(new Vector2(-520f, 30f));
            Greybox.Silhouette(z, "Sacsayhuaman", hill, new Vector3(0f, 0f, 640f), Vector3.right, new Color(0.012f, 0.014f, 0.022f));

            // Valley ridges all round, a little lighter than the hill so the hill reads in front.
            var rng = new System.Random(7);
            const int cards = 18;
            for (int i = 0; i < cards; i++)
            {
                float a0 = i / (float)cards * Mathf.PI * 2f, a1 = (i + 1) / (float)cards * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)) * 880f, p1 = new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)) * 880f;
                Vector3 right = (p1 - p0);
                float w = right.magnitude;
                right /= w;
                var pts = new List<Vector2> { new Vector2(-2f, -60f), new Vector2(w + 2f, -60f) };
                for (int k = 8; k >= 0; k--) pts.Add(new Vector2(w * k / 8f, 95f + (float)rng.NextDouble() * 105f));
                Greybox.Silhouette(z, "Ridge", pts, p0, right, new Color(0.02f, 0.026f, 0.036f));
            }
        }
    }
}
