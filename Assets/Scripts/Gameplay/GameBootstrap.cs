using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using TacticalDuelist.Core.Config;
using TacticalDuelist.UI.Toolkit;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Runtime bootstrapper. Creates gameplay objects (GridView, ExecutionController,
    /// Camera, HeroView3D) and wires them together with GameManager.
    ///
    /// UI is handled by UIManager (UI Toolkit) which is set up as a separate
    /// GameObject with UIDocument + serialized VisualTreeAsset references.
    /// GameBootstrap only needs to find or create the UIManager and pass it
    /// to GameManager.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Game Data (wired by BootstrapSceneCreator)")]
        [SerializeField] private HeroConfig[] _heroes;
        [SerializeField] private MapConfig _defaultMap;
        [SerializeField] private MapConfig[] _allMaps;

        [Header("Grid Materials (assign via Tactical Duelist > Setup Project)")]
        [SerializeField] private Material _matFloor;
        [SerializeField] private Material _matFloorAlt;
        [SerializeField] private Material _matWall;
        [SerializeField] private Material _matDangerZone;
        [SerializeField] private Material _matHighlightMove;
        [SerializeField] private Material _matHighlightShoot;
        [SerializeField] private Material _matSpawnP1;
        [SerializeField] private Material _matSpawnP2;

        [Header("Hero Materials")]
        [SerializeField] private Material _matHeroP1;
        [SerializeField] private Material _matHeroP2;

        [Header("UI Manager (UI Toolkit)")]
        [SerializeField] private UIManager _uiManagerPrefab;

        #endregion

        private const BindingFlags FIELD_FLAGS =
            BindingFlags.NonPublic | BindingFlags.Instance;

        // Stored between Awake and Start
        private UIManager _uiManagerRef;
        private ExecutionController _execCtrlRef;
        private GridView _gridViewRef;
        private CameraController _cameraCtrlRef;
        private HeroPreview3D _heroPreview;
        private HeroPreview3D _heroSelectPreview;

        private void Awake()
        {
            Debug.Log("[GameBootstrap] Starting runtime construction...");

            // Ensure PlatformBootstrap runs first
            EnsurePlatformBootstrap();

            // ── UI Manager ──
            var uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager == null && _uiManagerPrefab != null)
            {
                uiManager = Instantiate(_uiManagerPrefab);
                uiManager.name = "UIManager";
            }
            if (uiManager == null)
            {
                Debug.LogError("[GameBootstrap] No UIManager found! Run 'Tactical Duelist > Setup UI Toolkit' first.");
                return;
            }

            // ── Audio Manager ──
            if (AudioManager.Instance == null)
            {
                var audioGo = new GameObject("AudioManager");
                audioGo.AddComponent<AudioManager>();
            }

            // ── VFX Manager ──
            if (VFXManager.Instance == null)
            {
                var vfxGo = new GameObject("VFXManager");
                var vfxMgr = vfxGo.AddComponent<VFXManager>();
                CreateAndWireVFX(vfxMgr);
            }

            // ── Gameplay Objects ──
            var gridView = new GameObject("GridView").AddComponent<GridView>();
            WireGridViewMaterials(gridView);

            var execCtrl = new GameObject("ExecutionController").AddComponent<ExecutionController>();

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
                camGo.tag = "MainCamera";
                cam = camGo.GetComponent<Camera>();
            }
            var cameraCtrl = cam.gameObject.GetComponent<CameraController>();
            if (cameraCtrl == null)
                cameraCtrl = cam.gameObject.AddComponent<CameraController>();

            // ── Hero 3D Placeholders ──
            HeroView3D hero1View = null;
            HeroView3D hero2View = null;
            try
            {
                hero1View = CreatePlaceholderHero("Hero_P1", _matHeroP1);
                hero2View = CreatePlaceholderHero("Hero_P2", _matHeroP2);
                hero1View.gameObject.SetActive(false);
                hero2View.gameObject.SetActive(false);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GameBootstrap] 3D heroes failed (non-fatal): {ex.Message}");
            }

            if (hero1View != null) Wire(execCtrl, "_hero1View", hero1View);
            if (hero2View != null) Wire(execCtrl, "_hero2View", hero2View);
            Wire(execCtrl, "_cameraController", cameraCtrl);
            Wire(execCtrl, "_gridView", gridView);

            // Store refs for Start() — UIManager.Awake() hasn't run yet
            _uiManagerRef = uiManager;
            _execCtrlRef = execCtrl;
            _gridViewRef = gridView;
            _cameraCtrlRef = cameraCtrl;

            Debug.Log("[GameBootstrap] Awake complete. Waiting for Start() to wire UI...");
        }

        private void Start()
        {
            if (_uiManagerRef == null) return;

            // ── Wire hero data to UI (UIManager.Awake has run by now) ──
            var heroList = _heroes != null ? new System.Collections.Generic.List<HeroConfig>(_heroes) : null;
            if (_uiManagerRef.HeroSelect != null && heroList != null)
                _uiManagerRef.HeroSelect.SetHeroes(heroList);
            if (_uiManagerRef.HeroesCollection != null && heroList != null)
                _uiManagerRef.HeroesCollection.SetHeroes(heroList);

            // ── Hero Preview 3D (main menu + hero select) ──
            var uiDoc = _uiManagerRef.GetComponent<UIDocument>();
            var root = uiDoc?.rootVisualElement;

            // Main menu preview
            var menuRoot = root?.Q("main-menu-root");
            var mainMenuPreviewEl = menuRoot?.Q("hero-preview");
            if (mainMenuPreviewEl != null)
            {
                var previewGo = new GameObject("HeroPreview3D");
                _heroPreview = previewGo.AddComponent<HeroPreview3D>();
                _heroPreview.Initialize(mainMenuPreviewEl);
                if (heroList != null && heroList.Count > 0)
                    _heroPreview.ShowHero(heroList[0]);
            }

            // Hero select preview
            var selectRoot = root?.Q("hero-select-root");
            var selectPreviewEl = selectRoot?.Q("hero-preview");
            if (selectPreviewEl != null)
            {
                var selectPreviewGo = new GameObject("HeroSelectPreview3D");
                _heroSelectPreview = selectPreviewGo.AddComponent<HeroPreview3D>();
                _heroSelectPreview.Initialize(selectPreviewEl);
                if (heroList != null && heroList.Count > 0)
                    _heroSelectPreview.ShowHero(heroList[0]);

                // Wire hero change event
                if (_uiManagerRef.HeroSelect != null)
                    _uiManagerRef.HeroSelect.OnHeroChanged += _heroSelectPreview.ShowHero;

                _heroSelectPreview.SetVisible(false); // Start hidden
            }

            // ── Game Manager ──
            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.SetupRuntime(_uiManagerRef, _execCtrlRef, _gridViewRef, _cameraCtrlRef, _defaultMap, _allMaps);
            if (_heroPreview != null) gm.SetHeroPreview(_heroPreview);
            if (_heroSelectPreview != null) gm.SetHeroSelectPreview(_heroSelectPreview);

            Debug.Log("[GameBootstrap] Runtime construction complete.");
        }

        #region Helpers

        private void EnsurePlatformBootstrap()
        {
            if (FindAnyObjectByType<Platform.PlatformBootstrap>() == null)
            {
                var go = new GameObject("PlatformBootstrap");
                go.AddComponent<Platform.PlatformBootstrap>();
            }
        }

        private void WireGridViewMaterials(GridView gridView)
        {
            Wire(gridView, "_floorMaterial", _matFloor);
            Wire(gridView, "_floorAltMaterial", _matFloorAlt);
            Wire(gridView, "_wallMaterial", _matWall);
            Wire(gridView, "_dangerZoneMaterial", _matDangerZone);
            Wire(gridView, "_highlightMoveMaterial", _matHighlightMove);
            Wire(gridView, "_highlightShootMaterial", _matHighlightShoot);
            Wire(gridView, "_spawnP1Material", _matSpawnP1);
            Wire(gridView, "_spawnP2Material", _matSpawnP2);
        }

        private HeroView3D CreatePlaceholderHero(string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            if (mat != null)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = mat;
            }

            // Direction arrow — small elongated cube pointing forward
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "DirectionArrow";
            arrow.transform.SetParent(go.transform, false);
            arrow.transform.localPosition = new Vector3(0f, 0.6f, 0.55f);
            arrow.transform.localScale = new Vector3(0.15f, 0.15f, 0.5f);
            if (mat != null)
            {
                var arrowRend = arrow.GetComponent<Renderer>();
                if (arrowRend != null) arrowRend.sharedMaterial = mat;
            }
            // Remove collider on arrow
            var arrowCol = arrow.GetComponent<Collider>();
            if (arrowCol != null) Destroy(arrowCol);

            var view = go.AddComponent<HeroView3D>();
            Wire(view, "_directionArrow", arrow);
            return view;
        }

        private void CreateAndWireVFX(VFXManager vfxMgr)
        {
            // Shoot VFX — small stretched sphere (bullet tracer)
            var shootPrefab = CreateVFXPrefab("VFX_Shoot", Color.yellow, PrimitiveType.Sphere,
                new Vector3(0.12f, 0.12f, 0.6f));
            // Add a trail for visual flair
            var trail = shootPrefab.AddComponent<TrailRenderer>();
            trail.startWidth = 0.08f;
            trail.endWidth = 0f;
            trail.time = 0.15f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.yellow;
            trail.endColor = new Color(1f, 1f, 0f, 0f);
            // Animate bullet moving forward
            var bulletMover = shootPrefab.AddComponent<SimpleMover>();
            bulletMover.speed = 15f;
            shootPrefab.SetActive(false);

            // Hit VFX — expanding red sphere
            var hitPrefab = CreateVFXPrefab("VFX_Hit", Color.red, PrimitiveType.Sphere,
                new Vector3(0.3f, 0.3f, 0.3f));
            hitPrefab.AddComponent<SimpleScaleUp>();
            hitPrefab.SetActive(false);

            // Elimination VFX — larger red burst
            var elimPrefab = CreateVFXPrefab("VFX_Elimination", new Color(1f, 0.2f, 0f), PrimitiveType.Sphere,
                new Vector3(0.5f, 0.5f, 0.5f));
            elimPrefab.AddComponent<SimpleScaleUp>();
            hitPrefab.SetActive(false);

            // Mutual Cancel VFX — white/yellow clash burst
            var cancelPrefab = CreateVFXPrefab("VFX_MutualCancel", new Color(1f, 0.9f, 0.3f), PrimitiveType.Sphere,
                new Vector3(0.6f, 0.6f, 0.6f));
            cancelPrefab.AddComponent<SimpleScaleUp>();
            cancelPrefab.SetActive(false);

            // Armor Break VFX — blue shatter
            var armorPrefab = CreateVFXPrefab("VFX_ArmorBreak", new Color(0.25f, 0.45f, 0.95f), PrimitiveType.Sphere,
                new Vector3(0.4f, 0.4f, 0.4f));
            armorPrefab.AddComponent<SimpleScaleUp>();
            armorPrefab.SetActive(false);

            Wire(vfxMgr, "_shootVFXPrefab", shootPrefab);
            Wire(vfxMgr, "_hitVFXPrefab", hitPrefab);
            Wire(vfxMgr, "_eliminationVFXPrefab", elimPrefab);
            Wire(vfxMgr, "_mutualCancelVFXPrefab", cancelPrefab);
            Wire(vfxMgr, "_armorBreakVFXPrefab", armorPrefab);
        }

        private static GameObject CreateVFXPrefab(string name, Color color, PrimitiveType type, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Sprites/Default");
                var mat = new Material(shader);
                mat.color = color;
                rend.sharedMaterial = mat;
            }
            return go;
        }

        private static void Wire<T>(object target, string fieldName, T value) where T : class
        {
            if (value == null) return;
            var field = target.GetType().GetField(fieldName, FIELD_FLAGS);
            if (field != null)
                field.SetValue(target, value);
            else
                Debug.LogWarning($"[GameBootstrap] Field '{fieldName}' not found on {target.GetType().Name}");
        }

        #endregion
    }
}
