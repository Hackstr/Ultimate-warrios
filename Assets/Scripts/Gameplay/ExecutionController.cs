using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Systems;
using TacticalDuelist.Core.Utils;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Orchestrates step-by-step visual playback of a round.
    /// Receives StepResult list from MatchManager, then drives HeroView3D,
    /// CameraController, and VFXManager through each step with proper timing.
    /// </summary>
    public class ExecutionController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private HeroView3D _hero1View;
        [SerializeField] private HeroView3D _hero2View;
        [SerializeField] private CameraController _cameraController;
        [SerializeField] private GridView _gridView;

        [Header("Timing (seconds)")]
        [SerializeField] private float _stepDelay = 0.3f;
        [SerializeField] private float _moveDuration = 0.25f;
        [SerializeField] private float _turnDuration = 0.15f;
        [SerializeField] private float _shootDuration = 0.3f;
        [SerializeField] private float _hitDuration = 0.4f;
        [SerializeField] private float _eliminationDuration = 0.8f;
        [SerializeField] private float _endOfRoundDelay = 1.5f;

        [Header("Playback")]
        [SerializeField] private float _playbackSpeed = 1f;

        #endregion

        #region Fields

        private List<StepResult> _pendingResults;
        private bool _isPlaying;
        private Coroutine _playbackCoroutine;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the visual playback of a round finishes (or is skipped).
        /// GameManager listens to decide next phase transition.
        /// </summary>
        public event System.Action OnPlaybackComplete;

        #endregion

        #region Properties

        public bool IsPlaying => _isPlaying;

        #endregion

        #region Public Methods

        /// <summary>
        /// Places both hero views at their grid spawn positions.
        /// Called once at match start before any playback.
        /// </summary>
        public void SetInitialPositions(Vector2Int p1Spawn, Direction p1Facing,
                                         Vector2Int p2Spawn, Direction p2Facing)
        {
            _hero1View?.SetGridPosition(p1Spawn, p1Facing);
            _hero2View?.SetGridPosition(p2Spawn, p2Facing);
            _hero1View?.gameObject.SetActive(true);
            _hero2View?.gameObject.SetActive(true);
        }

        /// <summary>
        /// Starts visual playback of a list of step results.
        /// Called after MatchManager completes round resolution.
        /// </summary>
        public void PlayRound(List<StepResult> results)
        {
            if (_isPlaying) return;

            _pendingResults = results;
            _playbackCoroutine = StartCoroutine(PlayRoundCoroutine());
        }

        /// <summary>
        /// Immediately skip to end state of current playback.
        /// </summary>
        public void SkipToEnd()
        {
            if (!_isPlaying || _pendingResults == null) return;

            if (_playbackCoroutine != null)
                StopCoroutine(_playbackCoroutine);

            var lastResult = _pendingResults[_pendingResults.Count - 1];
            ApplyFinalState(lastResult);

            _isPlaying = false;
            OnPlaybackComplete?.Invoke();
        }

        /// <summary>
        /// Sets playback speed (1 = normal, 2 = double).
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            _playbackSpeed = Mathf.Max(0.25f, speed);
        }

        #endregion

        #region Private — Playback Coroutine

        private IEnumerator PlayRoundCoroutine()
        {
            _isPlaying = true;

            if (_pendingResults.Count > 0)
            {
                var first = _pendingResults[0];
                _hero1View?.SetGridPosition(first.P1StartPos, first.P1StartFacing);
                _hero2View?.SetGridPosition(first.P2StartPos, first.P2StartFacing);
            }

            for (int i = 0; i < _pendingResults.Count; i++)
            {
                var step = _pendingResults[i];
                yield return StartCoroutine(PlayStepCoroutine(step));
                yield return new WaitForSeconds(AdjustedTime(_stepDelay));
            }

            yield return new WaitForSeconds(AdjustedTime(_endOfRoundDelay));

            _isPlaying = false;
            OnPlaybackComplete?.Invoke();
        }

        private IEnumerator PlayStepCoroutine(StepResult step)
        {
            UpdateCameraForStep(step);

            // Phase 1: Movement (both heroes move simultaneously)
            yield return StartCoroutine(PlayMovementPhase(step));

            // Phase 2: Combat (both heroes act simultaneously)
            yield return StartCoroutine(PlayCombatPhase(step));

            // Phase 3: Damage resolution
            yield return StartCoroutine(PlayDamagePhase(step));

            // Pickups
            yield return StartCoroutine(PlayPickups(step));
        }

        #endregion

        #region Private — Phase Playback

        private IEnumerator PlayMovementPhase(StepResult step)
        {
            bool p1Moved = step.P1StartPos != step.P1EndPos;
            bool p2Moved = step.P2StartPos != step.P2EndPos;
            bool p1Turned = step.P1StartFacing != step.P1EndFacing && !p1Moved;
            bool p2Turned = step.P2StartFacing != step.P2EndFacing && !p2Moved;

            Coroutine c1 = null;
            Coroutine c2 = null;

            if (p1Moved && _hero1View != null)
                c1 = StartCoroutine(AnimateHeroMove(_hero1View, step.P1StartPos, step.P1EndPos, step.P1EndFacing));
            else if (p1Turned && _hero1View != null)
                c1 = StartCoroutine(_hero1View.AnimateTurn(step.P1EndFacing, AdjustedTime(_turnDuration)));

            if (p2Moved && _hero2View != null)
                c2 = StartCoroutine(AnimateHeroMove(_hero2View, step.P2StartPos, step.P2EndPos, step.P2EndFacing));
            else if (p2Turned && _hero2View != null)
                c2 = StartCoroutine(_hero2View.AnimateTurn(step.P2EndFacing, AdjustedTime(_turnDuration)));

            if (c1 != null) yield return c1;
            if (c2 != null) yield return c2;
        }

        private IEnumerator AnimateHeroMove(HeroView3D hero, Vector2Int from, Vector2Int to, Direction endFacing)
        {
            yield return hero.AnimateMove(from, to, AdjustedTime(_moveDuration));
            hero.SetGridPosition(to, endFacing);
        }

        private IEnumerator PlayCombatPhase(StepResult step)
        {
            Coroutine c1 = null;
            Coroutine c2 = null;

            if (step.P1Fired && _hero1View != null)
            {
                c1 = StartCoroutine(_hero1View.AnimateShoot(AdjustedTime(_shootDuration)));
                SpawnShootVFX(step.P1EndPos, step.P1EndFacing);
            }

            if (step.P2Fired && _hero2View != null)
            {
                c2 = StartCoroutine(_hero2View.AnimateShoot(AdjustedTime(_shootDuration)));
                SpawnShootVFX(step.P2EndPos, step.P2EndFacing);
            }

            if (c1 != null) yield return c1;
            if (c2 != null) yield return c2;
        }

        private IEnumerator PlayDamagePhase(StepResult step)
        {
            if (step.MutualCancel)
            {
                var midpoint = new Vector2Int(
                    (step.P1EndPos.x + step.P2EndPos.x) / 2,
                    (step.P1EndPos.y + step.P2EndPos.y) / 2
                );
                VFXManager.Instance?.SpawnMutualCancelVFX(midpoint);
                yield return new WaitForSeconds(AdjustedTime(_hitDuration));
                yield break;
            }

            Coroutine c1 = null;
            Coroutine c2 = null;

            // Player 1 takes damage
            if (step.P1ArmorBroken && _hero1View != null)
            {
                VFXManager.Instance?.SpawnArmorBreakVFX(step.P1EndPos);
                c1 = StartCoroutine(_hero1View.AnimateArmorBreak(AdjustedTime(_hitDuration)));
            }
            else if (step.P1Eliminated && _hero1View != null)
            {
                VFXManager.Instance?.SpawnEliminationVFX(step.P1EndPos);
                if (_cameraController != null)
                    _cameraController.PunchZoom(GridHelper.GridToWorld(step.P1EndPos));
                c1 = StartCoroutine(_hero1View.AnimateElimination(AdjustedTime(_eliminationDuration)));
            }
            else if (step.P2Hit && _hero1View != null && !step.P1Eliminated && !step.P1ArmorBroken)
            {
                // P2's shot hit P1 but was handled above via armor/elim
            }

            // Player 2 takes damage
            if (step.P2ArmorBroken && _hero2View != null)
            {
                VFXManager.Instance?.SpawnArmorBreakVFX(step.P2EndPos);
                c2 = StartCoroutine(_hero2View.AnimateArmorBreak(AdjustedTime(_hitDuration)));
            }
            else if (step.P2Eliminated && _hero2View != null)
            {
                VFXManager.Instance?.SpawnEliminationVFX(step.P2EndPos);
                if (_cameraController != null)
                    _cameraController.PunchZoom(GridHelper.GridToWorld(step.P2EndPos));
                c2 = StartCoroutine(_hero2View.AnimateElimination(AdjustedTime(_eliminationDuration)));
            }

            if (c1 != null) yield return c1;
            if (c2 != null) yield return c2;
        }

        private IEnumerator PlayPickups(StepResult step)
        {
            if (step.P1PickedUp != PickupType.None)
                VFXManager.Instance?.SpawnPickupVFX(step.P1EndPos);

            if (step.P2PickedUp != PickupType.None)
                VFXManager.Instance?.SpawnPickupVFX(step.P2EndPos);

            if (step.P1PickedUp != PickupType.None || step.P2PickedUp != PickupType.None)
                yield return new WaitForSeconds(AdjustedTime(0.2f));
        }

        #endregion

        #region Private — Helpers

        private float AdjustedTime(float baseTime)
        {
            return baseTime / _playbackSpeed;
        }

        private void UpdateCameraForStep(StepResult step)
        {
            if (_cameraController == null) return;

            var p1World = GridHelper.GridToWorld(step.P1EndPos);
            var p2World = GridHelper.GridToWorld(step.P2EndPos);
            _cameraController.FrameAction(p1World, p2World);
        }

        private void SpawnShootVFX(Vector2Int from, Direction facing)
        {
            if (VFXManager.Instance == null) return;
            var dir = GridHelper.DirectionToVector(facing);
            var target = from + dir * 3;
            VFXManager.Instance.SpawnShootVFX(from, target);
        }

        private void ApplyFinalState(StepResult lastStep)
        {
            _hero1View?.SetGridPosition(lastStep.P1EndPos, lastStep.P1EndFacing);
            _hero2View?.SetGridPosition(lastStep.P2EndPos, lastStep.P2EndFacing);

            if (lastStep.P1Eliminated && _hero1View != null)
                _hero1View.gameObject.SetActive(false);
            if (lastStep.P2Eliminated && _hero2View != null)
                _hero2View.gameObject.SetActive(false);
        }

        #endregion
    }
}
