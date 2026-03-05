using UnityEngine;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Centralized animation state management for a hero model.
    /// Wraps Animator access with cached hashes and state queries.
    /// Designed for future Animator Controller with states:
    /// Idle, Move, Shoot, Hit, Death, Victory, Defeat, Special.
    /// </summary>
    public class AnimationController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Animator _animator;

        [Header("Animation Speeds")]
        [SerializeField] private float _moveAnimSpeed = 1f;
        [SerializeField] private float _shootAnimSpeed = 1f;

        #endregion

        #region Cached Hashes

        private static readonly int ParamIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int ParamShoot = Animator.StringToHash("Shoot");
        private static readonly int ParamHit = Animator.StringToHash("Hit");
        private static readonly int ParamDeath = Animator.StringToHash("Death");
        private static readonly int ParamVictory = Animator.StringToHash("Victory");
        private static readonly int ParamDefeat = Animator.StringToHash("Defeat");
        private static readonly int ParamSpecial = Animator.StringToHash("Special");
        private static readonly int ParamSpeed = Animator.StringToHash("AnimSpeed");

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        #endregion

        #region Public Methods

        public void SetMoving(bool isMoving)
        {
            if (_animator == null) return;
            _animator.SetBool(ParamIsMoving, isMoving);
            _animator.SetFloat(ParamSpeed, _moveAnimSpeed);
        }

        public void TriggerShoot()
        {
            if (_animator == null) return;
            _animator.SetFloat(ParamSpeed, _shootAnimSpeed);
            _animator.SetTrigger(ParamShoot);
        }

        public void TriggerHit()
        {
            _animator?.SetTrigger(ParamHit);
        }

        public void TriggerDeath()
        {
            _animator?.SetTrigger(ParamDeath);
        }

        public void TriggerVictory()
        {
            _animator?.SetTrigger(ParamVictory);
        }

        public void TriggerDefeat()
        {
            _animator?.SetTrigger(ParamDefeat);
        }

        public void TriggerSpecial()
        {
            _animator?.SetTrigger(ParamSpecial);
        }

        /// <summary>
        /// Returns true if the animator is currently in a transition or playing a non-idle state.
        /// </summary>
        public bool IsPlayingAction()
        {
            if (_animator == null) return false;
            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            return _animator.IsInTransition(0) || stateInfo.normalizedTime < 1f;
        }

        /// <summary>
        /// Resets all triggers to prevent queued animations from firing.
        /// </summary>
        public void ResetTriggers()
        {
            if (_animator == null) return;
            _animator.ResetTrigger(ParamShoot);
            _animator.ResetTrigger(ParamHit);
            _animator.ResetTrigger(ParamDeath);
            _animator.ResetTrigger(ParamVictory);
            _animator.ResetTrigger(ParamDefeat);
            _animator.ResetTrigger(ParamSpecial);
        }

        /// <summary>
        /// Force-sets the animator speed multiplier (e.g., 2x for fast-forward replay).
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            if (_animator != null)
                _animator.speed = speed;
        }

        #endregion
    }
}
