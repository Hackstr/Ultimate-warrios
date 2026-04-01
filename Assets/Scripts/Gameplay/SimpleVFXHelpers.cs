using UnityEngine;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Moves the object forward (local Z) at a constant speed.
    /// Used for bullet tracer VFX.
    /// </summary>
    public class SimpleMover : MonoBehaviour
    {
        public float speed = 15f;

        private void Update()
        {
            transform.Translate(Vector3.forward * (speed * Time.deltaTime), Space.Self);
        }
    }

    /// <summary>
    /// Scales the object up from 0 to target over its lifetime, then fades.
    /// Used for hit/elimination VFX.
    /// </summary>
    public class SimpleScaleUp : MonoBehaviour
    {
        private Vector3 _targetScale;
        private float _elapsed;
        private const float Duration = 0.4f;

        private void OnEnable()
        {
            _targetScale = transform.localScale;
            transform.localScale = Vector3.zero;
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Duration);
            // Ease out
            float eased = 1f - (1f - t) * (1f - t);
            transform.localScale = _targetScale * eased;
        }
    }
}
