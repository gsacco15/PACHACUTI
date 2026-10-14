using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    public enum CameraProfile { Close, Medium, Wide }

    public enum CameraMode
    {
        Follow,   // third person, over the shoulder, no lock-on
        Tilt,     // authored tilt to the zenith (3 s)
        Sky,      // sky-map: soft cursor; the camera pans with it up to ±40°
        Rail,     // a scripted pose with free look (Qenqo walk ±30°, the vision)
        Fixed,    // a scripted pose, no look (quipu close-up, cut shots)
    }

    /// <summary>Pitch slice §10 Camera.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        public CameraMode Mode { get; private set; } = CameraMode.Follow;
        public float Yaw;
        public float Pitch = 10f;   // degrees down positive
        public float BreathSway;    // 0..1 strain: 0.5 Hz sway below 30 breath (§5)
        public Vector2 FreeLook;    // rail offsets (yaw, pitch), degrees
        public Vector2 SkyCursor;   // sky-map cursor offset from the zenith frame, degrees

        Manco _target;
        CameraProfile _profile = CameraProfile.Close;
        float _distance = Tuning.CameraCloseDistance, _shoulder = 0.35f, _height = Tuning.CameraCloseShoulderHeight;
        Vector3 _railPos;
        Quaternion _railRot;
        float _railFreeLook;
        Vector3 _tiltFromPos;
        Quaternion _tiltFromRot, _skyBase;
        Vector3 _skyPos;
        float _tiltT;
        float _blend = 1f, _blendTime;
        Vector3 _blendFromPos;
        Quaternion _blendFromRot;

        public static CameraRig Create(Transform parent)
        {
            var go = new GameObject("Camera");
            go.transform.SetParent(parent, false);
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 1100f;
            cam.fieldOfView = 62f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Greybox.Palette.Night;
            go.AddComponent<AudioListener>();
            var rig = go.AddComponent<CameraRig>();
            rig.Cam = cam;
            return rig;
        }

        public void Follow(Manco target, bool snap)
        {
            _target = target;
            BlendTo(CameraMode.Follow, snap ? 0f : 0.8f);
        }

        public void SetProfile(CameraProfile p) => _profile = p;

        public void Look(Vector2 deltaDegrees)
        {
            switch (Mode)
            {
                case CameraMode.Follow:
                    Yaw += deltaDegrees.x;
                    Pitch = Mathf.Clamp(Pitch - deltaDegrees.y, -60f, 70f);
                    break;
                case CameraMode.Rail:
                    FreeLook.x = Mathf.Clamp(FreeLook.x + deltaDegrees.x, -_railFreeLook, _railFreeLook);
                    FreeLook.y = Mathf.Clamp(FreeLook.y + deltaDegrees.y, -_railFreeLook, _railFreeLook);
                    break;
                case CameraMode.Sky:
                    SkyCursor += deltaDegrees;
                    float lim = Tuning.SkyCameraPanLimitDegrees * 1.6f;
                    SkyCursor = new Vector2(Mathf.Clamp(SkyCursor.x, -lim, lim), Mathf.Clamp(SkyCursor.y, -lim, lim));
                    break;
            }
        }

        public void SetRail(Vector3 position, Quaternion rotation, float freeLookDegrees, float blendSeconds = 0f)
        {
            _railPos = position;
            _railRot = rotation;
            _railFreeLook = freeLookDegrees;
            if (Mode != CameraMode.Rail) FreeLook = Vector2.zero;
            BlendTo(CameraMode.Rail, blendSeconds);
        }

        public void SetFixed(Vector3 position, Vector3 lookAt, float blendSeconds = 0f)
        {
            _railPos = position;
            _railRot = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
            BlendTo(CameraMode.Fixed, blendSeconds);
        }

        /// <summary>§8: "Camera tilts to the zenith over 3 s. The roof stays at the bottom of the frame."</summary>
        public void BeginTilt(Vector3 eye, float northYaw = 0f)
        {
            _tiltFromPos = transform.position;
            _tiltFromRot = transform.rotation;
            _skyPos = eye;
            // Look straight up with north at the top of the frame, so the north road runs up the screen.
            _skyBase = Quaternion.Euler(0f, northYaw, 0f) * Quaternion.Euler(-82f, 0f, 0f);
            _tiltT = 0f;
            SkyCursor = Vector2.zero;
            Mode = CameraMode.Tilt;
        }

        public bool TiltDone => Mode != CameraMode.Tilt;

        public void BeginTiltDown(float seconds)
        {
            _blendFromPos = transform.position;
            _blendFromRot = transform.rotation;
            _blend = 0f;
            _blendTime = seconds;
            Mode = CameraMode.Follow;
        }

        /// <summary>Direction of the soft cursor in world space (sky-map hover tests).</summary>
        public Vector3 CursorDirection => SkyRotation(SkyCursor) * Vector3.forward;

        Quaternion SkyRotation(Vector2 offset) => _skyBase * Quaternion.Euler(-offset.y, offset.x, 0f);

        void BlendTo(CameraMode mode, float seconds)
        {
            _blendFromPos = transform.position;
            _blendFromRot = transform.rotation;
            _blend = seconds <= 0f ? 1f : 0f;
            _blendTime = seconds;
            Mode = mode;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            Vector3 pos;
            Quaternion rot;
            switch (Mode)
            {
                case CameraMode.Tilt:
                    _tiltT += dt / Tuning.SkyTiltSeconds;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_tiltT));
                    pos = Vector3.Lerp(_tiltFromPos, _skyPos, k);
                    rot = Quaternion.Slerp(_tiltFromRot, _skyBase, k);
                    if (_tiltT >= 1f) Mode = CameraMode.Sky;
                    break;
                case CameraMode.Sky:
                    // The camera pans with the cursor, up to ±40°.
                    Vector2 pan = new Vector2(Mathf.Clamp(SkyCursor.x * 0.6f, -Tuning.SkyCameraPanLimitDegrees, Tuning.SkyCameraPanLimitDegrees),
                                              Mathf.Clamp(SkyCursor.y * 0.6f, -Tuning.SkyCameraPanLimitDegrees, Tuning.SkyCameraPanLimitDegrees));
                    pos = _skyPos;
                    rot = Quaternion.Slerp(transform.rotation, SkyRotation(pan), 1f - Mathf.Exp(-dt * 6f));
                    break;
                case CameraMode.Rail:
                    pos = _railPos;
                    rot = _railRot * Quaternion.Euler(-FreeLook.y, FreeLook.x, 0f);
                    break;
                case CameraMode.Fixed:
                    pos = _railPos;
                    rot = _railRot;
                    break;
                default:
                    FollowPose(dt, out pos, out rot);
                    break;
            }

            if (BreathSway > 0f && (Mode == CameraMode.Follow || Mode == CameraMode.Rail))
            {
                float s = Mathf.Sin(Time.time * Mathf.PI * 2f * Tuning.BreathSwayHz);
                rot *= Quaternion.Euler(s * 0.9f * BreathSway, 0f, s * 0.6f * BreathSway);
                pos += Vector3.up * (s * 0.025f * BreathSway);
            }

            if (_blend < 1f)
            {
                _blend = Mathf.Min(1f, _blend + dt / Mathf.Max(0.01f, _blendTime));
                float b = Mathf.SmoothStep(0f, 1f, _blend);
                pos = Vector3.Lerp(_blendFromPos, pos, b);
                rot = Quaternion.Slerp(_blendFromRot, rot, b);
            }
            transform.SetPositionAndRotation(pos, rot);
        }

        void FollowPose(float dt, out Vector3 pos, out Quaternion rot)
        {
            if (_target == null) { pos = transform.position; rot = transform.rotation; return; }
            float wantDist, wantHeight, wantShoulder;
            switch (_profile)
            {
                case CameraProfile.Medium: wantDist = Tuning.CameraMediumDistance; wantHeight = 0.45f; wantShoulder = 0.45f; break;
                case CameraProfile.Wide: wantDist = Tuning.CameraWideDistance; wantHeight = 0.8f; wantShoulder = 0.3f; break;
                default: wantDist = Tuning.CameraCloseDistance; wantHeight = Tuning.CameraCloseShoulderHeight; wantShoulder = 0.35f; break;
            }
            if (_target.Mode == MancoMode.Climb && _profile == CameraProfile.Wide) wantDist += 1.5f; // pulls back on the Coricancha climb
            float k = 1f - Mathf.Exp(-dt * 3f);
            _distance = Mathf.Lerp(_distance, wantDist, k);
            _height = Mathf.Lerp(_height, wantHeight, k);
            _shoulder = Mathf.Lerp(_shoulder, wantShoulder, k);

            rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 pivot = _target.Head + Vector3.up * _height;
            Vector3 right = rot * Vector3.right;
            Vector3 shoulderPivot = pivot + right * _shoulder;
            // Keep the shoulder offset itself out of walls.
            if (Physics.SphereCast(pivot, 0.15f, right, out var sh, _shoulder, Layers.World, QueryTriggerInteraction.Ignore))
                shoulderPivot = pivot + right * Mathf.Max(0f, sh.distance - 0.05f);
            Vector3 back = rot * Vector3.back;
            float dist = _distance;
            if (Physics.SphereCast(shoulderPivot, 0.18f, back, out var hit, _distance, Layers.World, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.25f, hit.distance - 0.05f);
            pos = shoulderPivot + back * dist;
        }
    }
}
