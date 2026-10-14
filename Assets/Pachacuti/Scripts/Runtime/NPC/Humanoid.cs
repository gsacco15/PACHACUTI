using UnityEngine;

namespace Pachacuti
{
    public enum HeadGear { None, Llautu, SoldierCrest, Shawl, Hat }
    public enum HandProp { None, Torch, Mace }

    /// <summary>
    /// Grey-box body: capsule torso, sphere head, two hands, optional headgear, hand
    /// prop and porter's load. Posture is driven by a few parameters so behaviour
    /// reads without animation clips (glance = head turn, stare = face, head down,
    /// crouch, torch raised). Replaced by rigged characters from week 3.
    /// </summary>
    public sealed class Humanoid : MonoBehaviour
    {
        public float Height = 1.6f;
        public Transform Body, Head, HandL, HandR, Load, Prop;
        public Torch PropTorch;

        // Posture inputs (0..1 unless noted)
        public float Crouch;          // 1 = crouched
        public float HeadDown;        // 1 = chin to chest (porter)
        public float HeadYaw;         // degrees, relative to body
        public float RaiseProp;       // torch raised overhead
        public float Lying;           // 1 = on the ground (bodies)
        public float Seated;
        public Vector3? HandsOnWall;  // world point; hands reach to it (climbing)
        public float HandShake;       // breath < 10: hands shake

        Vector3 _bodyBase, _headBase, _handLBase, _handRBase, _propBase;
        float _bodyScaleY;

        public static Humanoid Build(Transform parent, string name, Color tunic, Color skin, float height = 1.6f,
                                     HeadGear gear = HeadGear.None, HandProp prop = HandProp.None, Color? gearColor = null)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var h = root.AddComponent<Humanoid>();
            h.Height = height;
            var tunicMat = MaterialKit.Lit(tunic, Surface.Plain, 0.05f);
            var skinMat = MaterialKit.Lit(skin, Surface.Plain, 0.2f);

            float bodyH = height * 0.82f;
            h.Body = Greybox.Primitive(root.transform, PrimitiveType.Capsule, "Body", Vector3.zero, new Vector3(0.42f, bodyH * 0.5f, 0.3f), tunicMat).transform;
            h.Body.localPosition = new Vector3(0, bodyH * 0.5f, 0);
            h.Head = Greybox.Primitive(root.transform, PrimitiveType.Sphere, "Head", Vector3.zero, Vector3.one * 0.22f, skinMat).transform;
            h.Head.localPosition = new Vector3(0, height - 0.12f, 0.02f);
            h.HandL = Greybox.Primitive(root.transform, PrimitiveType.Sphere, "HandL", Vector3.zero, Vector3.one * 0.09f, skinMat).transform;
            h.HandR = Greybox.Primitive(root.transform, PrimitiveType.Sphere, "HandR", Vector3.zero, Vector3.one * 0.09f, skinMat).transform;
            h.HandL.localPosition = new Vector3(-0.25f, height * 0.5f, 0.05f);
            h.HandR.localPosition = new Vector3(0.25f, height * 0.5f, 0.05f);

            var gearMat = MaterialKit.Lit(gearColor ?? new Color(0.5f, 0.1f, 0.08f), Surface.Plain, 0.05f);
            switch (gear)
            {
                case HeadGear.Llautu: // braided headband
                    Child(h.Head, Greybox.Primitive(root.transform, PrimitiveType.Cylinder, "Llautu", Vector3.zero, new Vector3(1.05f, 0.12f, 1.05f), gearMat).transform, new Vector3(0, 0.15f, 0));
                    break;
                case HeadGear.SoldierCrest: // Quizquiz's northern troops: tall distinct headgear (§12)
                    Child(h.Head, Greybox.Primitive(root.transform, PrimitiveType.Cylinder, "Helmet", Vector3.zero, new Vector3(1.1f, 0.35f, 1.1f), gearMat).transform, new Vector3(0, 0.3f, 0));
                    Child(h.Head, Greybox.Primitive(root.transform, PrimitiveType.Cube, "Crest", Vector3.zero, new Vector3(0.25f, 1.3f, 0.9f), MaterialKit.Lit(new Color(0.75f, 0.62f, 0.2f))).transform, new Vector3(0, 1.0f, -0.05f));
                    break;
                case HeadGear.Shawl:
                    Child(h.Head, Greybox.Primitive(root.transform, PrimitiveType.Sphere, "Shawl", Vector3.zero, new Vector3(1.25f, 1.1f, 1.25f), gearMat).transform, new Vector3(0, 0.05f, -0.08f));
                    break;
                case HeadGear.Hat:
                    Child(h.Head, Greybox.Primitive(root.transform, PrimitiveType.Cylinder, "Hat", Vector3.zero, new Vector3(1.8f, 0.08f, 1.8f), gearMat).transform, new Vector3(0, 0.4f, 0));
                    break;
            }

            if (prop == HandProp.Torch)
            {
                var stick = Greybox.Primitive(root.transform, PrimitiveType.Cylinder, "TorchStick", Vector3.zero, new Vector3(0.05f, 0.35f, 0.05f), MaterialKit.Lit(Greybox.Palette.Wood));
                h.Prop = stick.transform;
                // Parented to the root (not the scaled stick) so the flame keeps its shape; follows the stick tip in LateUpdate.
                h.PropTorch = Torch.Create(root.transform, Vector3.zero);
            }
            else if (prop == HandProp.Mace)
            {
                var stick = Greybox.Primitive(root.transform, PrimitiveType.Cylinder, "Macana", Vector3.zero, new Vector3(0.05f, 0.4f, 0.05f), MaterialKit.Lit(Greybox.Palette.Wood));
                var star = Greybox.Primitive(stick.transform, PrimitiveType.Sphere, "StarHead", Vector3.zero, Vector3.one, MaterialKit.Lit(new Color(0.45f, 0.42f, 0.38f), Surface.Plain, 0.3f));
                star.transform.localPosition = new Vector3(0, 1.05f, 0);
                star.transform.localScale = new Vector3(3.2f, 0.45f, 3.2f);
                h.Prop = stick.transform;
            }

            h.CaptureRest();
            return h;
        }

        static void Child(Transform parent, Transform child, Vector3 localPos)
        {
            child.SetParent(parent, false);
            child.localPosition = localPos;
        }

        void CaptureRest()
        {
            _bodyBase = Body.localPosition;
            _bodyScaleY = Body.localScale.y;
            _headBase = Head.localPosition;
            _handLBase = HandL.localPosition;
            _handRBase = HandR.localPosition;
            if (Prop != null)
            {
                Prop.localPosition = _handRBase + new Vector3(0.02f, 0.15f, 0.05f);
                _propBase = Prop.localPosition;
            }
        }

        /// <summary>Give this body a porter's load: a wrapped bundle on a tumpline (§6).</summary>
        public void SetLoad(bool carrying)
        {
            if (carrying && Load == null)
            {
                var bundle = Greybox.Primitive(transform, PrimitiveType.Cube, "Load", Vector3.zero, new Vector3(0.55f, 0.5f, 0.38f), MaterialKit.Lit(Greybox.Palette.Cloth));
                Load = bundle.transform;
                Load.localPosition = new Vector3(0, Height * 0.66f, -0.32f);
            }
            if (Load != null) Load.gameObject.SetActive(carrying);
        }

        void LateUpdate()
        {
            float crouch = Mathf.Clamp01(Crouch);
            float lying = Mathf.Clamp01(Lying);
            float seated = Mathf.Clamp01(Seated);
            float squash = Mathf.Lerp(1f, 0.68f, Mathf.Max(crouch, seated * 0.9f));
            Body.localScale = new Vector3(Body.localScale.x, _bodyScaleY * squash, Body.localScale.z);
            Body.localPosition = new Vector3(_bodyBase.x, _bodyBase.y * squash, _bodyBase.z + crouch * 0.08f);
            Body.localRotation = Quaternion.Euler(crouch * 18f, 0, 0);
            float headY = Mathf.Lerp(_headBase.y, _headBase.y * 0.7f, Mathf.Max(crouch, seated * 0.9f));
            Head.localPosition = new Vector3(_headBase.x, headY - HeadDown * 0.08f, _headBase.z + crouch * 0.18f + HeadDown * 0.1f);
            Head.localRotation = Quaternion.Euler(HeadDown * 35f, HeadYaw, 0);

            if (HandsOnWall.HasValue)
            {
                Vector3 local = transform.InverseTransformPoint(HandsOnWall.Value);
                float shake = HandShake > 0f ? Mathf.Sin(Time.time * 41f) * 0.02f * HandShake : 0f;
                float reach = Mathf.Sin(Time.time * 5f) * 0.12f;
                HandL.localPosition = local + new Vector3(-0.22f, 0.15f + reach + shake, 0f);
                HandR.localPosition = local + new Vector3(0.22f, 0.15f - reach - shake, 0f);
            }
            else
            {
                float hy = Mathf.Lerp(_handLBase.y, _handLBase.y * 0.7f, crouch);
                HandL.localPosition = new Vector3(_handLBase.x, hy, _handLBase.z + crouch * 0.2f);
                HandR.localPosition = new Vector3(_handRBase.x, hy + RaiseProp * (Height * 0.55f), _handRBase.z + crouch * 0.2f);
                if (Load != null && Load.gameObject.activeSelf)
                {
                    // Hands up at the tumpline across the forehead.
                    HandL.localPosition = new Vector3(-0.16f, Height * 0.84f, 0.08f);
                    HandR.localPosition = new Vector3(0.16f, Height * 0.84f, 0.08f);
                }
            }
            if (Prop != null) Prop.localPosition = HandR.localPosition + new Vector3(0.02f, 0.15f, 0.05f);
            if (PropTorch != null && Prop != null) PropTorch.transform.position = Prop.position + Prop.up * 0.42f;

            if (lying > 0f)
            {
                Body.localRotation = Quaternion.Euler(90f * lying, 0, 0);
                Body.localPosition = new Vector3(_bodyBase.x, Mathf.Lerp(_bodyBase.y, 0.15f, lying), _bodyBase.z + lying * 0.6f);
                Head.localPosition = new Vector3(_headBase.x, Mathf.Lerp(headY, 0.12f, lying), _headBase.z + lying * 1.35f);
            }
        }
    }
}
