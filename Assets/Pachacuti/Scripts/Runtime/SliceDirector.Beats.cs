using System.Collections;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>The playthrough (pitch slice §3 beat sheet). Times are design targets, not limits.</summary>
    public sealed partial class SliceDirector
    {
        IEnumerator RunSlice()
        {
            yield return ColdOpen();
            yield return Rooftops();
            yield return Compound();
            yield return Street();
            yield return Yard();
            yield return Tunnel();
            yield return Roof();
            yield return Qenqo();
            yield return Title();
        }

        // ------------------------------------------------------------------ helpers

        IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds && !_skipRequested)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        bool ConsumeSkip()
        {
            bool s = _skipRequested;
            _skipRequested = false;
            return s;
        }

        static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        // ------------------------------------------------------------------ 0:00 Cold open

        IEnumerator ColdOpen()
        {
            // Black. Breathing. Fade up on Manco crouched on a rooftop in fog; below, torches moving
            // in a column. A pututu horn. Subtitle: "Quizquiz's men are in the city." No menu, no title.
            Beats.Advance(Beat.ColdOpen);
            _playerControl = false;
            _manco.Teleport(Layout.ColdOpenSpawn, Layout.ColdOpenYaw);
            _manco.SetCrouched(true);
            _manco.SetScripted(true);
            _rig.SetFixed(Layout.ColdOpenSpawn + new Vector3(2.4f, 1.4f, 2.6f), new Vector3(-7f, 1.2f, 152f));
            _ui.FadeTo(1f, 0f);
            _audio.SetAmbience(Ambience.Silence);
            yield return Wait(0.6f);
            _audio.Play("breath_audible", 0.4f);
            yield return Wait(1.7f);
            _audio.Play("breath_audible", 0.35f);
            yield return Wait(1.2f);

            foreach (var h in _column) h.gameObject.SetActive(true);
            StartCoroutine(MarchColumn());
            _audio.SetAmbience(Ambience.District);
            _ui.FadeTo(0f, 4f);
            yield return Wait(3f);
            _audio.Play("pututu", 0.75f);
            yield return Wait(2f);
            _ui.Say(SliceScript.ColdOpen);
            yield return Wait(4.5f);
            ConsumeSkip();
        }

        IEnumerator MarchColumn()
        {
            float t = 0f;
            while (t < 70f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < _column.Count; i++)
                {
                    var h = _column[i];
                    var p = h.transform.position;
                    p.z -= 1.35f * Time.deltaTime;
                    p.y = Mathf.Abs(Mathf.Sin((t + i * 0.3f) * 5.5f)) * 0.04f;
                    h.transform.position = p;
                    if (p.z < 96f) h.gameObject.SetActive(false);
                }
                yield return null;
            }
            foreach (var h in _column) h.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ 0:30 Beat 1 — Rooftops

        IEnumerator Rooftops()
        {
            // Prompt: climb. Three roofs, a drop into the compound. Breath is audible; a long climb
            // forces a pause. A soldier passes below; the player waits or moves.
            Beats.Advance(Beat.Rooftops);
            _manco.SetScripted(false);
            _playerControl = true;
            _rig.Yaw = Layout.ColdOpenYaw;
            _rig.Pitch = 14f;
            _rig.Follow(_manco, snap: false);
            _rig.SetProfile(CameraProfile.Close);
            _ui.ShowPrompt(ControlPrompt.Climb);
            foreach (var s in _roofSoldiers) s.Active = true;
            float promptTime = 0f;
            while (true)
            {
                promptTime += Time.deltaTime;
                if (promptTime > 15f) _ui.HidePrompt();
                if (Layout.CompoundCourt.Contains(_manco.transform.position) && _manco.Mode == MancoMode.Ground && _manco.Grounded) break;
                if (ConsumeSkip())
                {
                    _manco.Teleport(new Vector3(-1f, 0f, 80f), 200f);
                    break;
                }
                yield return null;
            }
            _ui.HidePrompt();
            foreach (var s in _roofSoldiers) s.Active = false;
        }

        // ------------------------------------------------------------------ 3:00 Huáscar's compound

        IEnumerator Compound()
        {
            // Huáscar's lineage palace, emptying. A woman hands Manco a porter's load.
            Beats.Advance(Beat.Compound);
            _rig.SetProfile(CameraProfile.Medium);
            _line.gameObject.SetActive(true);
            _line.Place(Layout.StreetNorthZ - 3f);
            while (Flat(_woman.transform.position, _manco.transform.position) > 1.5f && !_skipRequested)
            {
                Vector3 to = _manco.transform.position - _woman.transform.position;
                to.y = 0f;
                _woman.transform.rotation = Quaternion.LookRotation(to.normalized);
                if (to.magnitude > 1.5f) _woman.transform.position += to.normalized * (1.3f * Time.deltaTime);
                _line.Tick(0f);
                yield return null;
            }
            ConsumeSkip();
            _playerControl = false;
            _woman.HandsOnWall = _manco.Chest;
            yield return Wait(0.7f);
            _manco.SetCarrying(true);
            World.CarryingLoad = true;
            _woman.HandsOnWall = null;
            _ui.Say(SliceScript.Woman);
            yield return Wait(1.2f);
            _playerControl = true;
            _district.CompoundDoor.SetActive(false);

            while (_manco.transform.position.z > Layout.StreetNorthZ + 0.2f)
            {
                TickLineStart();
                if (ConsumeSkip()) _manco.Teleport(new Vector3(Layout.LaneX, 0f, Layout.StreetNorthZ - 1f), 180f);
                yield return null;
            }
        }

        void TickLineStart()
        {
            if (!_line.Moving && (_manco.transform.position.z < Layout.StreetNorthZ + 0.5f ||
                                  Vector3.Distance(_manco.transform.position, _line.MancoSlotPosition) < 3f))
                _line.Begin();
            _line.Tick(Time.deltaTime);
        }

        // ------------------------------------------------------------------ 4:00 Beat 2 — The porter

        IEnumerator Street()
        {
            bool retry = false;
            while (true)
            {
                Beats.Advance(Beat.Street);
                if (retry) yield return ResetStreet();
                retry = true;

                var kinsman = new KinsmanEncounter(World.Tupa != KinsmanOutcome.None);
                _tupa.gameObject.SetActive(World.Tupa != KinsmanOutcome.Fled);
                _captured = false;
                _hidePickupPending = false;
                foreach (var s in _streetSoldiers) s.Active = true;
                _rig.SetProfile(CameraProfile.Medium);
                bool done = false;

                while (!done)
                {
                    float dt = Time.deltaTime;
                    TickLineStart();
                    _manco.SpeedCap = _manco.Carrying ? _line.PaceFor(_manco) : null;

                    if (kinsman.Phase == KinsmanPhase.Waiting || kinsman.Phase == KinsmanPhase.Window)
                    {
                        _routeInteractToKinsman = kinsman.Phase == KinsmanPhase.Window;
                        var input = _routeInteractToKinsman && _playerControl ? _frame.Interact : InteractInput.None;
                        // "On Manco passing within 2 m": distance along the street to the doorway, while in the street.
                        Vector3 mp = _manco.transform.position;
                        float passing = mp.x < Layout.StreetX1 + 0.2f && mp.x > Layout.StreetX0 - 0.2f ? Mathf.Abs(mp.z - Layout.TupaDoorZ) : 99f;
                        var ev = kinsman.Tick(dt, passing, input);
                        KinsmanPhase = kinsman.Phase;
                        switch (ev)
                        {
                            case KinsmanEvent.SteppedOut:
                                // On Manco passing within 2 m he steps out and starts: "Manc—"
                                _tupa.transform.position = _district.TupaStepOut;
                                _tupa.transform.rotation = Quaternion.LookRotation((_manco.transform.position - _district.TupaStepOut).WithY(0f).normalized);
                                _ui.ClearSubtitles();
                                _ui.Say(SliceScript.TupaCut);
                                _routeInteractToKinsman = true;
                                break;
                            case KinsmanEvent.Strike:
                                _routeInteractToKinsman = false;
                                StartCoroutine(StrikeTupa());
                                break;
                            case KinsmanEvent.Hide:
                                _routeInteractToKinsman = false;
                                StartCoroutine(HideTupa());
                                break;
                            case KinsmanEvent.NameSpoken:
                                // Tupa says the name. Alert. Chase. Reset; Tupa is gone on the retry (he fled).
                                _routeInteractToKinsman = false;
                                _ui.ClearSubtitles();
                                _ui.Say(SliceScript.TupaFull);
                                World.Tupa = KinsmanOutcome.Fled;
                                RaiseAlarm(_tupa.transform.position);
                                StartCoroutine(TupaFlees());
                                break;
                        }
                    }

                    if (_hidePickupPending && _manco.Carrying)
                    {
                        // Back in line with the load, but the torch soldier has looked hard at it: ~60.
                        _hidePickupPending = false;
                        _torchSoldier.Brain.SetSuspicion(Mathf.Max(_torchSoldier.Brain.Suspicion, Tuning.HideBranchSuspicionAfterPickup));
                    }

                    if (_captured) break;
                    if (Layout.StreetExit.Contains(_manco.transform.position)) done = true;
                    if (ConsumeSkip())
                    {
                        _manco.Teleport(new Vector3(-6f, 0f, -74f), 150f);
                        done = true;
                    }
                    if (!done) yield return null;
                }

                if (!done)
                {
                    yield return Captured();
                    continue;
                }
                break;
            }
            _routeInteractToKinsman = false;
            _manco.SpeedCap = null;
            World.CarryingLoad = _manco.Carrying;
            foreach (var s in _streetSoldiers) s.Active = false;
        }

        bool _hidePickupPending;

        IEnumerator StrikeTupa()
        {
            // Manco drops the load and kills Tupa with a single blow; the body falls into the
            // doorway. The torch soldier glances (+10). Manco picks up the load. The line moves on.
            _playerControl = false;
            _manco.SetScripted(true);
            _manco.Body.SetLoad(false);
            Vector3 start = _manco.transform.position;
            Vector3 hit = _tupa.transform.position + (start - _tupa.transform.position).WithY(0f).normalized * 0.6f;
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                _manco.ScriptedPose(Vector3.Lerp(start, hit, t / 0.25f), _tupa.transform.position - _manco.transform.position);
                yield return null;
            }
            _audio.PlayAt("land_small", _tupa.transform.position + Vector3.up, 0.7f, 0.6f, 15f);
            Vector3 fallTo = _district.TupaDoorway;
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                _tupa.Lying = t / 0.5f;
                _tupa.transform.position = Vector3.Lerp(_district.TupaStepOut, fallTo, t / 0.5f);
                yield return null;
            }
            _tupa.Lying = 1f;
            World.Tupa = KinsmanOutcome.Killed;
            _torchSoldier.Brain.AddSuspicion(10f);
            yield return new WaitForSeconds(0.6f);
            _manco.Body.SetLoad(true);
            yield return new WaitForSeconds(0.5f);
            _manco.SetScripted(false);
            _manco.Teleport(_manco.transform.position, 180f);
            _playerControl = true;
        }

        IEnumerator HideTupa()
        {
            // Manco drops the load, pulls Tupa into the doorway, hand over his mouth. The load lies
            // in the street. The torch soldier stares at it (+25), approaches, kicks it (4 s). The
            // player must wait for him to turn, then step out and pick it up with suspicion at ~60.
            _playerControl = false;
            _manco.DropLoad(loud: true);
            _manco.SetScripted(true);
            World.Tupa = KinsmanOutcome.Hidden;
            Vector3 mStart = _manco.transform.position, tStart = _tupa.transform.position;
            Vector3 mEnd = _district.TupaDoorway + new Vector3(-0.15f, 0f, 0.3f), tEnd = _district.TupaDoorway + new Vector3(0.15f, 0f, -0.3f);
            for (float t = 0; t < 0.6f; t += Time.deltaTime)
            {
                float k = t / 0.6f;
                _manco.ScriptedPose(Vector3.Lerp(mStart, mEnd, k), tEnd - mEnd);
                _tupa.transform.position = Vector3.Lerp(tStart, tEnd, k);
                yield return null;
            }
            _manco.Body.HandsOnWall = _tupa.Head.position;
            _manco.SetCrouched(true);

            var s = _torchSoldier;
            var load = _manco.DroppedLoad;
            s.Brain.BeginScripted();
            s.Brain.AddSuspicion(25f);
            if (load != null) s.ScriptLookAt(load.transform.position);
            _manco.SetScripted(false);
            _manco.Teleport(mEnd, 0f);
            _playerControl = true;
            _hidePickupPending = true;
            yield return new WaitForSeconds(1f);
            if (load != null)
            {
                Vector3 l = load.transform.position;
                s.ScriptMoveTo(l + (s.transform.position - l).WithY(0f).normalized * 0.8f, 1.3f);
                float guard = 0f;
                while (!s.ScriptArrived && guard < 8f) { guard += Time.deltaTime; yield return null; }
                for (int k = 0; k < 3 && load.activeSelf; k++)
                {
                    _audio.PlayAt("land_small", l, 0.5f, 1.2f, 18f);
                    load.transform.position += Random.insideUnitSphere.WithY(0f) * 0.12f;
                    yield return new WaitForSeconds(Tuning.SoldierKillLoadKickSeconds / 3f);
                }
            }
            s.ScriptClear();
            s.Brain.Release(); // turns back toward his post
            _manco.Body.HandsOnWall = null;
        }

        IEnumerator TupaFlees()
        {
            _tupa.Lying = 0f;
            for (float t = 0; t < 6f; t += Time.deltaTime)
            {
                _tupa.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                _tupa.transform.position += Vector3.forward * (4.2f * Time.deltaTime);
                if (_tupa.transform.position.z > Layout.StreetNorthZ - 1f) break;
                yield return null;
            }
            _tupa.gameObject.SetActive(false);
        }

        IEnumerator Captured()
        {
            // "The street is the only place the player can fail: capture triggers a short chase
            // and a reset to the compound door. No death screen."
            _playerControl = false;
            _routeInteractToKinsman = false;
            _manco.SetScripted(true);
            _ui.FadeTo(1f, 1.1f);
            yield return new WaitForSeconds(1.3f);
            World.StreetResets++;
        }

        IEnumerator ResetStreet()
        {
            foreach (var s in _streetSoldiers) s.ResetToPost();
            _manco.SetScripted(false);
            _manco.Teleport(Layout.StreetResetSpawn, 180f);
            if (_manco.DroppedLoad != null) _manco.DroppedLoad.SetActive(false);
            _manco.SetCarrying(true);
            _manco.Breath.Reset();
            _line.Place(Layout.StreetNorthZ - 3f);
            if (World.Tupa == KinsmanOutcome.Hidden)
            {
                _tupa.transform.position = _district.TupaDoorway;
                _tupa.Lying = 0f;
            }
            _rig.Yaw = 180f;
            _rig.Pitch = 10f;
            yield return new WaitForSeconds(0.3f);
            _ui.FadeTo(0f, 1f);
            _playerControl = true;
        }

        // ------------------------------------------------------------------ 8:00 Beat 3 — The knots

        IEnumerator Yard()
        {
            // Ninan, the old keeper, is burning cords. He stops. The lesson: one cord, one count.
            Beats.Advance(Beat.Yard);
            _rig.SetProfile(CameraProfile.Medium);
            float feed = 0f;
            while (!Layout.NinanApproach.Contains(_manco.transform.position))
            {
                _line.Tick(Time.deltaTime);
                feed += Time.deltaTime;
                _ninan.HandsOnWall = Layout.Fire + Vector3.up * (0.4f + Mathf.Abs(Mathf.Sin(feed * 0.8f)) * 0.3f);
                if (ConsumeSkip()) _manco.Teleport(Layout.NinanApproach.center.WithY(0f), 180f);
                yield return null;
            }
            _ninan.HandsOnWall = null;
            _playerControl = false;
            _manco.SetScripted(true);
            _manco.ScriptedPose(Layout.NinanSeat + Vector3.forward * 1.5f, Vector3.back);
            if (_manco.Carrying) _manco.SetCarrying(false);
            _rig.SetFixed(Layout.NinanSeat + new Vector3(1.6f, 1.7f, 3.2f), Layout.NinanSeat + Vector3.up * 0.9f, 1f);
            _ninan.HeadDown = 0f;
            if (World.Tupa == KinsmanOutcome.Killed)
            {
                // Ninan looks at Manco's hands and says nothing: a 2 s pause before the first line.
                _ninan.HeadDown = 0.6f;
                yield return new WaitForSeconds(2f);
                _ninan.HeadDown = 0f;
            }
            _ui.Say(SliceScript.NinanReadIt);
            yield return new WaitForSeconds(SliceScript.NinanReadIt.Duration);

            var lesson = new QuipuLesson();
            _quipu.gameObject.SetActive(true);
            _quipu.Build(QuipuLesson.SliceValues);
            _quipu.SetActivePendant(0);
            _quipu.Point(0);
            _rig.SetFixed(_quipu.CameraPosition, _quipu.LookAt, 1.2f);
            _manco.Body.gameObject.SetActive(false); // the close-up is his point of view
            _ui.Say(SliceScript.NinanCount);

            yield return ReadPendant(lesson, 0, SliceScript.NinanCord1Right);
            lesson.NextPendant();
            _quipu.SetActivePendant(1);
            _quipu.Point(1);
            _ui.Say(SliceScript.NinanCord2);
            yield return ReadPendant(lesson, 1, SliceScript.NinanCord2Right);
            lesson.NextPendant();

            // The third pendant has no knots at all.
            _quipu.SetActivePendant(2);
            _quipu.Point(2);
            _ui.Markers.Clear();
            yield return new WaitForSeconds(2.2f);
            _ui.Say(SliceScript.NinanCord3);
            yield return new WaitForSeconds(SliceScript.NinanCord3.Duration + 1f);
            World.CordsRead = 3;

            _quipu.Point(-1);
            _ui.Say(SliceScript.NinanSecondCord);
            World.HasSecondCord = true;
            yield return new WaitForSeconds(SliceScript.NinanSecondCord.Duration);
            _ui.Markers.Clear();
            _quipu.gameObject.SetActive(false);
            _manco.Body.gameObject.SetActive(true);

            // He points to the drain.
            _ninan.HandsOnWall = _ninan.transform.position + (Layout.Drain - _ninan.transform.position).WithY(0f).normalized * 0.7f + Vector3.up * 1.1f;
            _manco.SetScripted(false);
            _manco.Teleport(Layout.NinanSeat + Vector3.forward * 1.5f, 120f);
            _rig.Yaw = 120f;
            _rig.Follow(_manco, snap: false);
            _playerControl = true;
            bool open = false;
            Interactable.Add(_district.DrainCover, 1.9f, () => open = true);
            while (!open)
            {
                if (ConsumeSkip()) open = true;
                yield return null;
            }
            _ninan.HandsOnWall = null;
        }

        IEnumerator ReadPendant(QuipuLesson lesson, int index, ScriptLine rightLine)
        {
            while (true)
            {
                bool confirm = _quipu.Tick(_frame, lesson, _rig.Cam, _ui, Time.deltaTime);
                if (ConsumeSkip())
                {
                    World.QuipuTries[index] = lesson.Tries;
                    World.CordsRead++;
                    yield break;
                }
                if (confirm)
                {
                    var r = lesson.Confirm();
                    World.QuipuTries[index] = r.Tries;
                    if (r.Verdict == QuipuVerdict.Correct || r.Verdict == QuipuVerdict.Revealed)
                    {
                        World.QuipuSolvedByPlayer[index] = r.Verdict == QuipuVerdict.Correct;
                        World.CordsRead++;
                        if (r.Verdict == QuipuVerdict.Revealed) _quipu.Tap(r.WrongPosition);
                        _ui.ClearSubtitles();
                        _ui.Say(rightLine);
                        float t = 0f;
                        while (t < rightLine.Duration + 0.4f) { t += Time.deltaTime; _quipu.Tick(default, lesson, _rig.Cam, _ui, Time.deltaTime); yield return null; }
                        yield break;
                    }
                    _quipu.Tap(r.WrongPosition);
                    _ui.ClearSubtitles();
                    _ui.Say(SliceScript.NinanWrong);
                }
                yield return null;
            }
        }

        // ------------------------------------------------------------------ 11:00 The tunnel

        IEnumerator Tunnel()
        {
            // A short traversal under the street to the Coricancha's outer wall; a soldier's
            // torch above through a grate. (Qenqo is already built: it "preloads during the tunnel".)
            Beats.Advance(Beat.Tunnel);
            _playerControl = false;
            _manco.SetScripted(true);
            var cover = _district.DrainCover.transform;
            Vector3 from = cover.position;
            for (float t = 0; t < 0.8f; t += Time.deltaTime)
            {
                cover.position = from + Vector3.right * (1.7f * t / 0.8f);
                yield return null;
            }
            _audio.PlayAt("scrape", from, 0.6f, 0.6f);
            _ui.FadeTo(1f, 0.5f);
            yield return new WaitForSeconds(0.6f);
            _manco.SetScripted(false);
            _manco.Teleport(Layout.TunnelStart + Vector3.up * 0.05f, 90f);
            _manco.SetCrouched(true);
            _rig.Yaw = 90f;
            _rig.Pitch = 8f;
            _rig.Follow(_manco, snap: true);
            _rig.SetProfile(CameraProfile.Close);
            _audio.SetAmbience(Ambience.Tunnel);
            _audio.Loop(0, "water", 0.22f, 0.85f);
            _grateSoldier.Active = true;
            _ui.FadeTo(0f, 0.8f);
            _playerControl = true;
            while (!(Layout.TunnelExit.Contains(_manco.transform.position) && _manco.transform.position.y > -0.6f))
            {
                if (ConsumeSkip()) _manco.Teleport(new Vector3(53f, 0f, -95f), 90f);
                yield return null;
            }
            _grateSoldier.Active = false;
            _audio.StopLoop(0);
            _audio.SetAmbience(Ambience.District);
        }

        // ------------------------------------------------------------------ 12:30 Beat 4 — The roof

        IEnumerator Roof()
        {
            Beats.Advance(Beat.Roof);
            _rig.SetProfile(CameraProfile.Wide);
            while (!(Layout.CoricanchaRoof.Contains(_manco.transform.position) && _manco.Mode == MancoMode.Ground))
            {
                if (ConsumeSkip()) _manco.Teleport(Layout.CoricanchaZenith + new Vector3(-4f, 0f, 0f), 0f);
                yield return null;
            }

            // The second control prompt: look up.
            _ui.ShowPrompt(ControlPrompt.LookUp);
            float up = 0f;
            while (true)
            {
                bool looking = _rig.Pitch < -Tuning.SkyLookUpPitchDegrees || _frame.Look.y > 3f;
                up = looking ? up + Time.deltaTime : 0f;
                if (up > 0.2f || ConsumeSkip()) break;
                yield return null;
            }
            _ui.HidePrompt();

            _playerControl = false;
            _manco.SetScripted(true);
            _manco.ScriptedPose(_manco.transform.position, Vector3.forward);
            Log.MarkSkyTilt(Beats.Clock);
            _rig.BeginTilt(_manco.Head + Vector3.back * 0.7f + Vector3.up * 0.25f, 0f);
            _audio.SetAmbience(Ambience.Sky);
            _skyMap.gameObject.SetActive(true);
            _skyMap.AttachCord(_rig.Cam);

            var model = new SkyMapModel(World.Tupa);
            var lastHover = SkyTarget.None;
            Vector2? exitCursor = null;
            while (true)
            {
                float dt = Time.deltaTime;
                var ev = model.Tick(dt);
                bool sky = _rig.Mode == CameraMode.Sky;
                if (sky)
                {
                    // Mouse/stick moves a soft cursor; WASD and arrows too (every beat completable by keyboard).
                    Vector2 keys = _frame.Move + new Vector2((_frame.DialRight ? 1 : 0) - (_frame.DialLeft ? 1 : 0), (_frame.DialUp ? 1 : 0) - (_frame.DialDown ? 1 : 0)) * 0.5f;
                    _rig.Look(_frame.Cursor + keys * (45f * dt));
                    if (model.Step == SkyStep.ExitOffered)
                    {
                        // "The gold line to the north-east pulses. The cursor is drawn to it."
                        exitCursor ??= FindCursorFor(_skyMap.ExitDirection);
                        _rig.SkyCursor = Vector2.Lerp(_rig.SkyCursor, exitCursor.Value, 1f - Mathf.Exp(-dt * 0.6f));
                    }
                }
                _skyMap.Sync(model, _rig.Cam, _rig.CursorDirection, _ui, dt);
                var vp = _rig.Cam.WorldToViewportPoint(_rig.Cam.transform.position + _rig.CursorDirection * 100f);
                _ui.SkyCursor = sky && vp.z > 0f ? new Vector2(vp.x, vp.y) : (Vector2?)null;
                _ui.SkyCursorPulse = _skyMap.Hovered != SkyTarget.None ? 1f : 0f;

                if (sky)
                {
                    if (_skyMap.Hovered != lastHover)
                    {
                        ev |= model.Hover(_skyMap.Hovered);
                        lastHover = _skyMap.Hovered;
                    }
                    if (_frame.Interact.Pressed && model.Step == SkyStep.CordGlowing) ev |= model.Interact();
                    else if (_frame.SelectPressed || _frame.ContextPressed || _frame.Interact.Pressed) ev |= model.Select(_skyMap.Hovered);
                }

                if ((ev & SkyEvent.FirstInteraction) != 0) Log.MarkSkyInteraction(Beats.Clock);
                if ((ev & SkyEvent.LlamaHoveredBeforeCord) != 0) Log.MarkLlamaHoveredBeforeCord();
                if ((ev & SkyEvent.CordRead) != 0) { World.SecondCordRead = true; World.ArmyConfidenceSharp = true; }
                if ((ev & SkyEvent.ThreadTaken) != 0) World.ThreadTaken = true;
                if ((ev & SkyEvent.OrderGiven) != 0) { World.OrderSent = true; _audio.Play("pututu_far", 0.6f); }
                if ((ev & SkyEvent.ArmyArrived) != 0) World.ArmyNode = model.ArmyNode;
                while (model.Lines.Count > 0) _ui.Say(model.Lines.Dequeue());
                if ((ev & SkyEvent.Exit) != 0 || model.Step == SkyStep.Done) break;
                if (ConsumeSkip()) break;
                yield return null;
            }

            // Selecting the line tilts the camera back down and Manco is already walking. Cut to Qenqo.
            _ui.SkyCursor = null;
            _ui.Markers.Clear();
            _skyMap.DetachCord();
            _skyMap.gameObject.SetActive(false);
            _audio.SetAmbience(Ambience.District);
            _rig.Follow(_manco, snap: false);
            _rig.Yaw = SkyMapView.ExitAzimuth;
            _rig.Pitch = 15f;
            Vector3 dir = Layout.Dir(SkyMapView.ExitAzimuth);
            _ui.FadeTo(1f, 1.4f);
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            {
                _manco.ScriptedPose(_manco.transform.position + dir * (1.1f * Time.deltaTime), dir);
                yield return null;
            }
        }

        Vector2 FindCursorFor(Vector3 dir)
        {
            Vector2 best = Vector2.zero;
            float bestAngle = float.MaxValue;
            Vector2 keep = _rig.SkyCursor;
            for (float x = -64f; x <= 64f; x += 2f)
                for (float y = -64f; y <= 64f; y += 2f)
                {
                    _rig.SkyCursor = new Vector2(x, y);
                    float a = Vector3.Angle(_rig.CursorDirection, dir);
                    if (a < bestAngle) { bestAngle = a; best = _rig.SkyCursor; }
                }
            _rig.SkyCursor = keep;
            return best;
        }

        // ------------------------------------------------------------------ 16:00 Beat 5 — Qenqo

        IEnumerator Qenqo()
        {
            // Follow the gold line: the walk compressed to 30 s on a fixed path. The carved rock; the
            // Pleiades over it. Prompt-less: at the channel's head, interact is the only input that does anything.
            Beats.Advance(Beat.Qenqo);
            _manco.SetScripted(true);
            _manco.SetCarrying(false);
            _playerControl = true; // look only: Manco is on the rail
            var path = _qenqo.WalkPath;
            float length = 0f;
            for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1], path[i]);
            _ui.FadeTo(0f, 1.5f);
            float t = 0f;
            Vector3 facing = _qenqo.Axis;
            while (t < Tuning.QenqoWalkSeconds)
            {
                t += Time.deltaTime;
                if (ConsumeSkip()) t = Tuning.QenqoWalkSeconds;
                float d = length * Mathf.Clamp01(t / Tuning.QenqoWalkSeconds);
                Vector3 pos = PathAt(path, d, out var tangent);
                if (tangent.WithY(0f).sqrMagnitude > 1e-4f) facing = tangent.WithY(0f).normalized;
                _manco.ScriptedPose(pos, facing);
                Vector3 right = Vector3.Cross(Vector3.up, facing);
                _rig.SetRail(pos - facing * 2.6f + Vector3.up * 1.8f + right * 0.45f, Quaternion.LookRotation((facing - Vector3.up * 0.12f).normalized), Tuning.VisionFreeLookDegrees);
                yield return null;
            }
            _manco.ScriptedPose(_qenqo.ChannelHead, _qenqo.Axis);
            _rig.SetRail(_qenqo.ChannelHead - _qenqo.Axis * 2.4f + Vector3.up * 1.8f + Vector3.Cross(Vector3.up, _qenqo.Axis) * 0.5f,
                         Quaternion.LookRotation((_qenqo.Axis * 6f + Vector3.up * 1.5f).normalized), Tuning.VisionFreeLookDegrees);

            // A faint pulse in the stone invites the touch.
            var pulse = _qenqo.Pulse.GetComponent<MeshRenderer>().sharedMaterial;
            while (!_frame.Interact.Pressed && !_skipRequested)
            {
                pulse.SetFloat("_Intensity", 0.25f + 0.35f * Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.time * 1.1f)), 3f));
                yield return null;
            }
            ConsumeSkip();
            pulse.SetFloat("_Intensity", 0f);
            _vision.ConsumeSkip = ConsumeSkip;
            yield return _vision.Run();
        }

        static Vector3 PathAt(System.Collections.Generic.List<Vector3> path, float distance, out Vector3 tangent)
        {
            for (int i = 1; i < path.Count; i++)
            {
                float seg = Vector3.Distance(path[i - 1], path[i]);
                if (distance <= seg || i == path.Count - 1)
                {
                    tangent = (path[i] - path[i - 1]).normalized;
                    return Vector3.Lerp(path[i - 1], path[i], seg > 0f ? Mathf.Clamp01(distance / seg) : 1f);
                }
                distance -= seg;
            }
            tangent = Vector3.forward;
            return path[path.Count - 1];
        }

        // ------------------------------------------------------------------ 19:00 Title

        IEnumerator Title()
        {
            // PACHACUTI. Then a single line: Cusco, 1532. He was sixteen. End.
            Beats.Advance(Beat.Title);
            _playerControl = false;
            _ui.FadeTo(1f, 1.2f);
            yield return new WaitForSeconds(1.5f);
            _audio.HardSilence();
            _ui.ShowTitle(true);
            yield return new WaitForSeconds(7f);
            Beats.Advance(Beat.Ended);
            World.Beat = Beat.Ended;
            // Playtest logging (§15): serialized to the console — the browser console on WebGL.
            Debug.Log("[PACHACUTI] PLAYTEST " + Log.ToJson(Beats, World));
            Debug.Log("[PACHACUTI] WORLD " + World.ToJson());
            LockCursor(false);
        }
    }
}
