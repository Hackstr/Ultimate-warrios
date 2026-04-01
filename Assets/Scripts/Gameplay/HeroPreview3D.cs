using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Renders a 3D hero model to a RenderTexture and displays it
    /// in a UI Toolkit VisualElement. Used for main menu hero preview.
    /// When real hero models arrive, just assign heroPrefab on HeroConfig.
    /// </summary>
    public class HeroPreview3D : MonoBehaviour
    {
        private Camera _previewCamera;
        private RenderTexture _renderTexture;
        private GameObject _currentHeroInstance;
        private VisualElement _targetElement;
        private Transform _heroRoot;

        private float _rotationAngle;
        private const float RotationSpeed = 20f;

        public void Initialize(VisualElement previewElement)
        {
            _targetElement = previewElement;

            // Create RenderTexture (match preview area aspect)
            _renderTexture = new RenderTexture(512, 640, 16);
            _renderTexture.antiAliasing = 2;

            // Create preview camera
            // Hero root first (for rotation) — placed far from game grid
            var rootGo = new GameObject("HeroPreviewRoot");
            rootGo.transform.SetParent(transform);
            rootGo.transform.position = new Vector3(50f, 0f, 50f);
            _heroRoot = rootGo.transform;

            // Camera looks at hero root from front
            var camGo = new GameObject("HeroPreviewCamera");
            camGo.transform.SetParent(transform);
            camGo.transform.position = new Vector3(50f, 1.0f, 54f); // 4 units in front
            camGo.transform.LookAt(new Vector3(50f, 0.8f, 50f)); // Look at hero chest

            _previewCamera = camGo.AddComponent<Camera>();
            _previewCamera.targetTexture = _renderTexture;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0.16f, 0.16f, 0.25f, 1f); // #2A2A40
            _previewCamera.fieldOfView = 30f;
            _previewCamera.cullingMask = 1 << 31;
            _previewCamera.depth = -10;

            // Light for preview
            var lightGo = new GameObject("HeroPreviewLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.position = new Vector3(51f, 3f, 52f);
            lightGo.transform.rotation = Quaternion.Euler(35f, -150f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.color = new Color(1f, 0.95f, 0.9f);
            light.cullingMask = 1 << 31;

            // Set RenderTexture as background — hide all children, use element itself
            if (_targetElement != null)
            {
                // Hide all child elements (inner container, placeholder text)
                foreach (var child in _targetElement.Children())
                    child.style.display = DisplayStyle.None;

                _targetElement.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_renderTexture));
                _targetElement.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }
        }

        public void ShowHero(HeroConfig heroConfig)
        {
            if (heroConfig == null) return;

            // Destroy previous
            if (_currentHeroInstance != null)
                Destroy(_currentHeroInstance);

            // Use hero prefab if available, otherwise create capsule
            if (heroConfig.heroPrefab != null)
            {
                _currentHeroInstance = Instantiate(heroConfig.heroPrefab, _heroRoot);
                // Auto-fit: scale to ~2 units height if too big or small
                var bounds = GetBounds(_currentHeroInstance);
                if (bounds.size.y > 0.1f)
                {
                    float targetHeight = 1.8f;
                    float scale = targetHeight / bounds.size.y;
                    _currentHeroInstance.transform.localScale *= scale;
                    // Re-center vertically
                    bounds = GetBounds(_currentHeroInstance);
                    float yOffset = -bounds.min.y;
                    _currentHeroInstance.transform.localPosition = new Vector3(0f, yOffset, 0f);
                }
            }
            else
            {
                // Placeholder capsule with hero color
                _currentHeroInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _currentHeroInstance.transform.SetParent(_heroRoot);
                _currentHeroInstance.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

                var capsuleMat = FindLitMaterial(heroConfig.heroColor);
                var rend = _currentHeroInstance.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = capsuleMat;

                // Direction arrow on preview
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.transform.SetParent(_currentHeroInstance.transform);
                arrow.transform.localPosition = new Vector3(0f, 0.6f, 0.55f);
                arrow.transform.localScale = new Vector3(0.15f, 0.15f, 0.5f);
                var arrowRend = arrow.GetComponent<Renderer>();
                if (arrowRend != null) arrowRend.sharedMaterial = capsuleMat;
                var col = arrow.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var capsCol = _currentHeroInstance.GetComponent<Collider>();
                if (capsCol != null) Destroy(capsCol);
            }

            _currentHeroInstance.transform.localPosition = Vector3.zero;
            _currentHeroInstance.transform.localRotation = Quaternion.identity;

            // Setup idle animation
            if (heroConfig.animatorController != null)
            {
                var animator = _currentHeroInstance.GetComponentInChildren<Animator>();
                if (animator == null)
                    animator = _currentHeroInstance.AddComponent<Animator>();

                var existingAvatar = animator.avatar;
                animator.runtimeAnimatorController = heroConfig.animatorController;
                animator.applyRootMotion = false;
                if (animator.avatar == null && existingAvatar != null)
                    animator.avatar = existingAvatar;
                animator.Rebind();
                animator.Update(0f);
            }

            // Set layer 31 recursively for camera isolation
            SetLayerRecursive(_currentHeroInstance, 31);

            _rotationAngle = 0f;
        }

        public void SetVisible(bool visible)
        {
            if (_currentHeroInstance != null)
                _currentHeroInstance.SetActive(visible);
            if (_previewCamera != null)
                _previewCamera.enabled = visible;
        }

        private void Update()
        {
            if (_heroRoot == null) return;
            _rotationAngle += RotationSpeed * Time.deltaTime;
            _heroRoot.localRotation = Quaternion.Euler(0f, _rotationAngle, 0f);
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
            if (_currentHeroInstance != null)
                Destroy(_currentHeroInstance);
        }

        private static Material FindLitMaterial(Color color)
        {
            // Try URP Lit first, then fallbacks
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }
    }
}
