using System.Collections;
using UnityEngine;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Utils;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Controls a single hero's 3D representation: position, rotation, animations.
    /// Attached to the hero prefab root. Driven by ExecutionController during playback.
    /// </summary>
    public class HeroView3D : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private Renderer _mainRenderer;

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _turnSpeed = 720f;

        [Header("Visual")]
        [SerializeField] private GameObject _directionArrow;
        [SerializeField] private GameObject _armorIndicator;

        #endregion

        #region Fields

        private int _playerIndex;
        private float _tileSize = 1f;
        private bool _isAnimating;
        private static readonly int AnimMove = Animator.StringToHash("IsMoving");
        private static readonly int AnimShoot = Animator.StringToHash("Shoot");
        private static readonly int AnimHit = Animator.StringToHash("Hit");
        private static readonly int AnimDeath = Animator.StringToHash("Death");
        private static readonly int AnimVictory = Animator.StringToHash("Victory");
        private static readonly int AnimDefeat = Animator.StringToHash("Defeat");

        #endregion

        #region Properties

        public int PlayerIndex => _playerIndex;
        public bool IsAnimating => _isAnimating;

        #endregion

        #region Public Methods — Setup

        /// <summary>
        /// Initialize the hero view with player index and hero color.
        /// </summary>
        public void Initialize(int playerIndex, Color heroColor, float tileSize = 1f)
        {
            _playerIndex = playerIndex;
            _tileSize = tileSize;

            if (_mainRenderer != null)
                _mainRenderer.material.color = heroColor;
        }

        /// <summary>
        /// Instantly sets grid position and facing (no animation). Used at match start.
        /// </summary>
        public void SetGridPosition(Vector2Int gridPos, Direction facing)
        {
            transform.position = GridHelper.GridToWorld(gridPos, _tileSize);
            transform.rotation = GridHelper.DirectionToRotation(facing);
        }

        #endregion

        #region Public Methods — Animated Actions

        /// <summary>
        /// Smoothly moves from current position to target over duration.
        /// </summary>
        public IEnumerator AnimateMove(Vector2Int from, Vector2Int to, float duration)
        {
            _isAnimating = true;
            var startPos = GridHelper.GridToWorld(from, _tileSize);
            var endPos = GridHelper.GridToWorld(to, _tileSize);

            SetAnimBool(AnimMove, true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            transform.position = endPos;
            SetAnimBool(AnimMove, false);
            _isAnimating = false;
        }

        /// <summary>
        /// Smoothly rotates to face the given direction.
        /// </summary>
        public IEnumerator AnimateTurn(Direction newFacing, float duration)
        {
            _isAnimating = true;
            var startRot = transform.rotation;
            var endRot = GridHelper.DirectionToRotation(newFacing);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            transform.rotation = endRot;
            _isAnimating = false;
        }

        /// <summary>
        /// Plays shoot animation and returns the animation duration.
        /// </summary>
        public IEnumerator AnimateShoot(float duration)
        {
            _isAnimating = true;
            SetAnimTrigger(AnimShoot);
            yield return new WaitForSeconds(duration);
            _isAnimating = false;
        }

        /// <summary>
        /// Plays hit reaction animation.
        /// </summary>
        public IEnumerator AnimateHit(float duration)
        {
            _isAnimating = true;
            SetAnimTrigger(AnimHit);
            yield return new WaitForSeconds(duration);
            _isAnimating = false;
        }

        /// <summary>
        /// Plays armor break visual: hide armor indicator, trigger hit.
        /// </summary>
        public IEnumerator AnimateArmorBreak(float duration)
        {
            _isAnimating = true;
            if (_armorIndicator != null)
                _armorIndicator.SetActive(false);
            SetAnimTrigger(AnimHit);
            yield return new WaitForSeconds(duration);
            _isAnimating = false;
        }

        /// <summary>
        /// Plays elimination/death animation.
        /// </summary>
        public IEnumerator AnimateElimination(float duration)
        {
            _isAnimating = true;
            SetAnimTrigger(AnimDeath);
            yield return new WaitForSeconds(duration);
            gameObject.SetActive(false);
            _isAnimating = false;
        }

        public void PlayVictoryPose()
        {
            SetAnimTrigger(AnimVictory);
        }

        public void PlayDefeatPose()
        {
            SetAnimTrigger(AnimDefeat);
        }

        /// <summary>
        /// Shows/hides the armor indicator visual.
        /// </summary>
        public void SetArmorVisible(bool visible)
        {
            if (_armorIndicator != null)
                _armorIndicator.SetActive(visible);
        }

        #endregion

        #region Private Methods

        private void SetAnimTrigger(int hash)
        {
            if (_animator != null)
                _animator.SetTrigger(hash);
        }

        private void SetAnimBool(int hash, bool value)
        {
            if (_animator != null)
                _animator.SetBool(hash, value);
        }

        #endregion
    }
}
