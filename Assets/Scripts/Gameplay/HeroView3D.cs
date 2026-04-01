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
        #pragma warning disable CS0414
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _turnSpeed = 720f;
        #pragma warning restore CS0414
        [SerializeField] private float _heightOffset = 0.5f;

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
        /// Swap the visual model at runtime. Destroys old children and
        /// instantiates the prefab as a child. Used when hero is selected.
        /// </summary>
        public void SwapModel(GameObject prefab, RuntimeAnimatorController animController = null)
        {
            if (prefab == null) return;

            // Destroy current visual children (capsule + arrow)
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            // Destroy own mesh (if capsule primitive)
            var ownFilter = GetComponent<MeshFilter>();
            if (ownFilter != null) Destroy(ownFilter);
            var ownRenderer = GetComponent<MeshRenderer>();
            if (ownRenderer != null) Destroy(ownRenderer);
            var ownCollider = GetComponent<Collider>();
            if (ownCollider != null) Destroy(ownCollider);

            // Instantiate prefab as child
            var model = Instantiate(prefab, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // Auto-scale to ~1.6 units height (smaller, fits grid better)
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                if (bounds.size.y > 0.1f)
                {
                    float scale = 1.6f / bounds.size.y;
                    model.transform.localScale *= scale;
                }

                // Re-calculate bounds after scaling and center at feet
                bounds = new Bounds(model.transform.position, Vector3.zero);
                renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                    bounds.Encapsulate(r.bounds);

                // Place feet at y=0 local
                model.transform.localPosition = new Vector3(0f, -bounds.min.y + transform.position.y - _heightOffset, 0f);
            }

            // Update references
            _mainRenderer = model.GetComponentInChildren<Renderer>();

            // Find or create Animator — prefer existing one (has correct Avatar)
            _animator = model.GetComponentInChildren<Animator>();

            if (animController != null)
            {
                if (_animator == null)
                {
                    _animator = model.AddComponent<Animator>();
                    Debug.Log("[HeroView3D] Created new Animator (none found on model)");
                }

                // Store existing avatar before assigning controller
                var existingAvatar = _animator.avatar;
                _animator.runtimeAnimatorController = animController;
                _animator.applyRootMotion = false;

                // Restore avatar if it was cleared by controller assignment
                if (_animator.avatar == null && existingAvatar != null)
                    _animator.avatar = existingAvatar;

                // Force rebind to apply new controller
                _animator.Rebind();
                _animator.Update(0f);

                // Ensure avatar is set — try to get from model's existing rig
                if (_animator.avatar == null)
                {
                    // Search all child SkinnedMeshRenderers for a bone structure
                    var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null && smr.rootBone != null)
                    {
                        Debug.Log($"[HeroView3D] Found SkinnedMeshRenderer with root bone: {smr.rootBone.name}");
                    }
                }

                Debug.Log($"[HeroView3D] Swapped model to: {prefab.name} (animator: yes, controller: {animController.name}, avatar: {(_animator.avatar != null ? _animator.avatar.name : "NONE")})");
            }
            else
            {
                Debug.Log($"[HeroView3D] Swapped model to: {prefab.name} (no animation controller)");
            }
        }

        /// <summary>
        /// Instantly sets grid position and facing (no animation). Used at match start.
        /// </summary>
        public void SetGridPosition(Vector2Int gridPos, Direction facing)
        {
            transform.position = GridHelper.GridToWorld(gridPos, _tileSize) + Vector3.up * _heightOffset;
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
            var offset = Vector3.up * _heightOffset;
            var startPos = GridHelper.GridToWorld(from, _tileSize) + offset;
            var endPos = GridHelper.GridToWorld(to, _tileSize) + offset;

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
