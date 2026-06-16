using UnityEngine;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// 3D perspective camera with isometric-like angle (Brawl Stars style).
    /// Follows action during execution, frames the grid during planning.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Camera Reference")]
        [SerializeField] private Camera _camera;

        [Header("Isometric Setup")]
        [Tooltip("Vertical angle from horizontal (higher = more top-down)")]
        [Range(30f, 70f)]
        [SerializeField] private float _pitchAngle = 55f;

        [Tooltip("Horizontal rotation around Y axis (0 = grid-aligned for portrait)")]
        [Range(0f, 360f)]
        [SerializeField] private float _yawAngle = 0f;

        [Tooltip("FOV — wider fits more grid in portrait mode")]
        [Range(20f, 70f)]
        [SerializeField] private float _fieldOfView = 50f;

        [Header("Follow")]
        [SerializeField] private float _followSpeed = 5f;
        [SerializeField] private float _zoomSpeed = 3f;
        [SerializeField] private float _defaultDistance = 20f;
        [SerializeField] private float _executionZoomMultiplier = 0.85f;

        [Header("Bounds")]
        [SerializeField] private float _minDistance = 8f;
        [SerializeField] private float _maxDistance = 35f;

        [Header("Framing")]
        [Tooltip("Extra padding multiplier for grid framing (>1 = more margin)")]
        [SerializeField] private float _framePadding = 1.15f;

        #endregion

        #region Fields

        private Vector3 _targetLookAt;
        private float _targetDistance;
        private Vector3 _currentLookAt;
        private float _currentDistance;

        // Screen shake
        private float _shakeTrauma;
        private const float ShakeDecay = 3f;
        private const float ShakeMaxOffset = 0.25f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            if (_camera != null)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);
            }
        }

        private void Start()
        {
            if (_camera != null)
                _camera.fieldOfView = _fieldOfView;
        }

        private void LateUpdate()
        {
            _currentLookAt = Vector3.Lerp(_currentLookAt, _targetLookAt, Time.deltaTime * _followSpeed);
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, Time.deltaTime * _zoomSpeed);

            // Apply screen shake offset
            var shakeOffset = Vector3.zero;
            if (_shakeTrauma > 0f)
            {
                float shake = _shakeTrauma * _shakeTrauma; // quadratic falloff
                float time = Time.unscaledTime * 25f;
                shakeOffset.x = (Mathf.PerlinNoise(time, 0f) * 2f - 1f) * ShakeMaxOffset * shake;
                shakeOffset.y = (Mathf.PerlinNoise(0f, time) * 2f - 1f) * ShakeMaxOffset * shake;
                _shakeTrauma = Mathf.Max(0f, _shakeTrauma - ShakeDecay * Time.unscaledDeltaTime);
            }

            UpdateCameraTransform(shakeOffset);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets up camera to frame the entire grid. Call at match start.
        /// Accounts for portrait aspect ratio — uses the narrower (horizontal) FOV
        /// as the limiting dimension so the grid fits on screen.
        /// </summary>
        public void FrameGrid(Vector3 gridCenter, float gridExtent)
        {
            _targetLookAt = gridCenter;
            _currentLookAt = gridCenter;

            float aspect = _camera != null ? _camera.aspect : (9f / 16f);
            float vFovHalfRad = _fieldOfView * 0.5f * Mathf.Deg2Rad;
            float hFovHalfRad = Mathf.Atan(Mathf.Tan(vFovHalfRad) * aspect);

            float effectiveHalfFov = (aspect < 1f) ? hFovHalfRad : vFovHalfRad;
            float requiredDistance = (gridExtent * _framePadding) / Mathf.Tan(effectiveHalfFov);
            _targetDistance = Mathf.Clamp(requiredDistance, _minDistance, _maxDistance);
            _currentDistance = _targetDistance;

            UpdateCameraTransform();
        }

        /// <summary>
        /// Smoothly moves camera to look at a world position.
        /// </summary>
        public void LookAt(Vector3 worldPos)
        {
            _targetLookAt = worldPos;
        }

        /// <summary>
        /// Focuses between two hero positions during execution.
        /// </summary>
        public void FrameAction(Vector3 pos1, Vector3 pos2)
        {
            _targetLookAt = (pos1 + pos2) * 0.5f;
            float heroDistance = Vector3.Distance(pos1, pos2);
            float baseDistance = Mathf.Max(_defaultDistance, heroDistance * 1.5f);
            _targetDistance = Mathf.Clamp(baseDistance * _executionZoomMultiplier, _minDistance, _maxDistance);
        }

        /// <summary>
        /// Resets to default grid overview.
        /// </summary>
        public void ResetToOverview(Vector3 gridCenter, float gridExtent)
        {
            FrameGrid(gridCenter, gridExtent);
        }

        /// <summary>
        /// Brief zoom toward a target position (for hit/kill moments).
        /// </summary>
        public void PunchZoom(Vector3 target, float zoomAmount = 0.7f)
        {
            _targetLookAt = target;
            _targetDistance *= zoomAmount;
            _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
        }

        /// <summary>
        /// Adds screen shake trauma (0-1). Decays over time with quadratic falloff.
        /// Use ~0.3 for hits, ~0.5 for armor break, ~0.7 for eliminations.
        /// </summary>
        public void Shake(float trauma = 0.5f)
        {
            _shakeTrauma = Mathf.Clamp01(_shakeTrauma + trauma);
        }

        #endregion

        #region Private Methods

        private void UpdateCameraTransform(Vector3 shakeOffset = default)
        {
            var rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
            var lookAt = _currentLookAt + shakeOffset;
            var offset = rotation * (Vector3.back * _currentDistance);
            transform.position = lookAt + offset;
            transform.rotation = rotation;
        }

        #endregion
    }
}
