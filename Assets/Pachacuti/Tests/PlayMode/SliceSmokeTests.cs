using System.Collections;
using NUnit.Framework;
using Pachacuti.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Pachacuti.PlayModeTests
{
    /// <summary>
    /// In-engine smoke tests for the grey-box slice. They load Boot.unity and drive
    /// SliceDirector through its test hooks (DebugInput, RequestSkip). Run them from
    /// Window > General > Test Runner > PlayMode before every playable build.
    /// </summary>
    public class SliceSmokeTests
    {
        SliceDirector _director;

        [UnitySetUp]
        public IEnumerator LoadBoot()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            yield return null;
            _director = Object.FindAnyObjectByType<SliceDirector>();
            Assert.That(_director, Is.Not.Null, "Boot scene must contain the SliceDirector");
        }

        [UnityTearDown]
        public IEnumerator Clear()
        {
            if (_director != null) _director.DebugInput = null;
            yield return null;
        }

        IEnumerator SkipTo(Beat beat, float timeoutPerBeat = 15f)
        {
            float guard = 0f;
            while (_director.Beats.Current < beat)
            {
                var before = _director.Beats.Current;
                _director.RequestSkip();
                float t = 0f;
                while (_director.Beats.Current == before && t < timeoutPerBeat)
                {
                    t += Time.unscaledDeltaTime;
                    _director.RequestSkip();
                    yield return null;
                }
                Assert.That(_director.Beats.Current, Is.Not.EqualTo(before), $"stuck in {before}");
                guard += t;
            }
        }

        static InputFrame Frame(Vector2 move = default, bool context = false, InteractInput interact = default)
        {
            return new InputFrame { Move = move, ContextPressed = context, Interact = interact, Digit = -1 };
        }

        [UnityTest]
        public IEnumerator BootsIntoTheColdOpen()
        {
            yield return new WaitForSeconds(0.5f);
            Assert.That(_director.Beats.Current, Is.EqualTo(Beat.ColdOpen));
            Assert.That(_director.Manco, Is.Not.Null);
            Assert.That(ClimbSurface.All.Count, Is.GreaterThan(3), "district built with climbable faces");
            Assert.That(Soldier.All.Count, Is.EqualTo(8), "§2: 8 soldiers in the slice");
        }

        [UnityTest]
        public IEnumerator EveryBeatAdvancesToTheTitle()
        {
            yield return SkipTo(Beat.Title, 40f);
            Assert.That(_director.Beats.Current, Is.EqualTo(Beat.Title));
        }

        [UnityTest]
        public IEnumerator ClimbsTheZoneAWallOnAFullBreath()
        {
            yield return SkipTo(Beat.Rooftops);
            var m = _director.Manco;
            m.Teleport(new Vector3(-1f, Layout.R2Top, Layout.TowerZ1 + 0.45f), 180f);
            _director.Rig.Yaw = 180f;
            m.Breath.Reset();
            yield return null;
            _director.DebugInput = Frame(context: true);
            yield return null;
            _director.DebugInput = Frame(new Vector2(0f, 1f));
            yield return null;
            Assert.That(m.Mode, Is.EqualTo(MancoMode.Climb), "context key grabs the masonry");
            float t = 0f;
            while (m.transform.position.y < Layout.TowerTop - 0.2f && t < 12f) { t += Time.deltaTime; yield return null; }
            _director.DebugInput = Frame();
            yield return new WaitForSeconds(0.6f);
            Assert.That(m.transform.position.y, Is.GreaterThan(Layout.TowerTop - 0.3f), "mantled onto the tower");
            Assert.That(m.Breath.Value, Is.LessThan(45f), "§5: the climb should cost ~75 breath");
        }

        [UnityTest]
        public IEnumerator AWindedClimberFallsBackToTheLedge()
        {
            yield return SkipTo(Beat.Rooftops);
            var m = _director.Manco;
            m.Teleport(new Vector3(-1f, Layout.R2Top, Layout.TowerZ1 + 0.45f), 180f);
            _director.Rig.Yaw = 180f;
            m.Breath.Reset(50f); // as if he ran across the roof first
            _director.DebugInput = Frame(context: true);
            yield return null;
            _director.DebugInput = Frame(new Vector2(0f, 1f));
            float t = 0f;
            while (!m.Breath.IsGasping && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.That(m.Breath.IsGasping, Is.True);
            _director.DebugInput = Frame();
            yield return new WaitForSeconds(1f);
            Assert.That(m.Mode, Is.EqualTo(MancoMode.Climb), "still on the wall, on the ledge");
            Assert.That(m.transform.position.y, Is.EqualTo(Layout.TowerLedge).Within(0.3f), "§5: falls to the last ledge");
        }

        [UnityTest]
        public IEnumerator ContextJumpCrossesGapOne()
        {
            yield return SkipTo(Beat.Rooftops);
            var m = _director.Manco;
            m.Teleport(new Vector3(-1f, Layout.R1Top, Layout.R1Z0 + 0.6f), 180f);
            _director.Rig.Yaw = 180f;
            yield return null;
            _director.DebugInput = Frame(context: true);
            yield return null;
            _director.DebugInput = Frame();
            yield return new WaitForSeconds(1.2f);
            Assert.That(m.transform.position.z, Is.LessThan(Layout.Gap1Z0), "landed on R2");
            Assert.That(m.transform.position.y, Is.EqualTo(Layout.R2Top).Within(0.3f));
        }

        [UnityTest]
        public IEnumerator TappingInteractHidesTheKinsman()
        {
            yield return SkipTo(Beat.Street);
            var m = _director.Manco;
            yield return new WaitForSeconds(0.5f);
            m.Teleport(new Vector3(Layout.LaneX, 0f, Layout.TupaDoorZ + 1.5f), 180f);
            _director.Rig.Yaw = 180f;
            float t = 0f;
            while (_director.KinsmanPhase != KinsmanPhase.Window && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.That(_director.KinsmanPhase, Is.EqualTo(KinsmanPhase.Window), "Tupa steps out within 2 m");
            _director.DebugInput = Frame(interact: new InteractInput { Pressed = true, Held = true, HeldSeconds = 0.05f });
            yield return null;
            _director.DebugInput = Frame(interact: new InteractInput { Released = true, HeldSeconds = 0.1f });
            yield return null;
            _director.DebugInput = Frame();
            yield return new WaitForSeconds(0.5f);
            Assert.That(_director.World.Tupa, Is.EqualTo(KinsmanOutcome.Hidden));
            Assert.That(m.DroppedLoad, Is.Not.Null, "the load lies in the street");
        }
    }
}
