using System.Collections.Generic;
using UnityEngine;
using TacticalDuelist.Core.Models;
using TacticalDuelist.Core.Utils;

namespace TacticalDuelist.Gameplay
{
    /// <summary>
    /// Manages visual effects (VFX) spawning and pooling.
    /// All VFX go through this manager to ensure proper pooling and cleanup.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("VFX Prefabs")]
        [SerializeField] private GameObject _shootVFXPrefab;
        [SerializeField] private GameObject _hitVFXPrefab;
        [SerializeField] private GameObject _armorBreakVFXPrefab;
        [SerializeField] private GameObject _eliminationVFXPrefab;
        [SerializeField] private GameObject _mutualCancelVFXPrefab;
        [SerializeField] private GameObject _pickupVFXPrefab;
        [SerializeField] private GameObject _dangerZoneVFXPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _initialPoolSize = 5;

        #endregion

        #region Fields

        private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
        private Transform _poolParent;

        private static VFXManager _instance;

        #endregion

        #region Properties

        public static VFXManager Instance => _instance;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _poolParent = new GameObject("VFX_Pool").transform;
            _poolParent.SetParent(transform);

            WarmPool(_shootVFXPrefab);
            WarmPool(_hitVFXPrefab);
            WarmPool(_armorBreakVFXPrefab);
            WarmPool(_eliminationVFXPrefab);
            WarmPool(_mutualCancelVFXPrefab);
            WarmPool(_pickupVFXPrefab);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion

        #region Public Methods — Spawn by Type

        public void SpawnShootVFX(Vector2Int from, Vector2Int to, float tileSize = 1f)
        {
            var pos = GridHelper.GridToWorld(from, tileSize) + Vector3.up * 0.5f;
            var lookAt = GridHelper.GridToWorld(to, tileSize) + Vector3.up * 0.5f;

            // Muzzle flash (particle)
            var flash = ParticleVFXFactory.CreateMuzzleFlash(new Color(1f, 0.8f, 0.3f));
            flash.transform.position = pos;
            flash.transform.LookAt(lookAt);
            StartCoroutine(DestroyAfter(flash, 0.3f));

            // Projectile trail (particle) moving toward target
            var projectile = ParticleVFXFactory.CreateProjectileTrail(new Color(1f, 0.85f, 0.2f));
            projectile.transform.position = pos;
            projectile.transform.LookAt(lookAt);
            float distance = Vector3.Distance(pos, lookAt);
            float speed = 15f;
            StartCoroutine(DestroyAfter(projectile, distance / speed + 0.3f));
        }

        public void SpawnHitVFX(Vector2Int pos, float tileSize = 1f)
        {
            var worldPos = GridHelper.GridToWorld(pos, tileSize) + Vector3.up * 0.5f;
            var burst = ParticleVFXFactory.CreateBurst(new Color(1f, 0.6f, 0.2f), 0.3f, 15, 0.5f);
            burst.transform.position = worldPos;
            StartCoroutine(DestroyAfter(burst, 1f));
        }

        public void SpawnArmorBreakVFX(Vector2Int pos, float tileSize = 1f)
        {
            var worldPos = GridHelper.GridToWorld(pos, tileSize) + Vector3.up * 0.5f;
            // Blue sparks for armor + white flash
            var sparks = ParticleVFXFactory.CreateBurst(new Color(0.3f, 0.6f, 1f), 0.5f, 25, 0.7f);
            sparks.transform.position = worldPos;
            StartCoroutine(DestroyAfter(sparks, 1.2f));
        }

        public void SpawnEliminationVFX(Vector2Int pos, float tileSize = 1f)
        {
            var worldPos = GridHelper.GridToWorld(pos, tileSize) + Vector3.up * 0.5f;
            var burst = ParticleVFXFactory.CreateEliminationBurst(new Color(1f, 0.3f, 0.1f));
            burst.transform.position = worldPos;
            StartCoroutine(DestroyAfter(burst, 2f));
        }

        public void SpawnMutualCancelVFX(Vector2Int midpoint, float tileSize = 1f)
        {
            var worldPos = GridHelper.GridToWorld(midpoint, tileSize) + Vector3.up * 0.5f;
            var burst = ParticleVFXFactory.CreateBurst(new Color(1f, 1f, 0.5f), 0.6f, 30, 0.8f);
            burst.transform.position = worldPos;
            // Add expanding ring for clash effect
            var ring = ParticleVFXFactory.CreateExpandingRing(new Color(1f, 0.9f, 0.3f, 0.6f), 2f, 0.6f);
            ring.transform.position = worldPos;
            StartCoroutine(DestroyAfter(burst, 1.2f));
            StartCoroutine(DestroyAfter(ring, 1f));
        }

        public void SpawnPickupVFX(Vector2Int pos, float tileSize = 1f)
        {
            SpawnAt(_pickupVFXPrefab, pos, tileSize, 1f);
        }

        public void SpawnDangerZoneVFX(Vector2Int[] tiles, float tileSize = 1f)
        {
            if (_dangerZoneVFXPrefab == null) return;
            foreach (var pos in tiles)
                SpawnAt(_dangerZoneVFXPrefab, pos, tileSize, 2f);
        }

        /// <summary>
        /// Spawns a unique VFX per special ability type.
        /// </summary>
        public void SpawnSpecialVFX(SpecialAbility ability, Vector2Int heroPos, Vector2Int targetPos, float tileSize = 1f)
        {
            switch (ability)
            {
                case SpecialAbility.Ricochet:
                    SpawnRicochetVFX(heroPos, targetPos, tileSize);
                    break;
                case SpecialAbility.Push:
                    SpawnPushVFX(targetPos, tileSize);
                    break;
                case SpecialAbility.Blink:
                    SpawnBlinkVFX(heroPos, targetPos, tileSize);
                    break;
                case SpecialAbility.Scan:
                    SpawnScanVFX(heroPos, tileSize);
                    break;
                case SpecialAbility.PhaseShot:
                    SpawnPhaseShotVFX(heroPos, targetPos, tileSize);
                    break;
                case SpecialAbility.Bomb:
                    SpawnBombVFX(heroPos, tileSize);
                    break;
                case SpecialAbility.Barrier:
                    SpawnBarrierVFX(targetPos, tileSize);
                    break;
                case SpecialAbility.Cloak:
                    SpawnCloakVFX(heroPos, tileSize);
                    break;
                case SpecialAbility.Turret:
                    SpawnTurretVFX(heroPos, targetPos, tileSize);
                    break;
                case SpecialAbility.Charge:
                    SpawnChargeVFX(heroPos, targetPos, tileSize);
                    break;
                case SpecialAbility.Pierce:
                    SpawnPierceVFX(heroPos, targetPos, tileSize);
                    break;
                case SpecialAbility.Decoy:
                    SpawnDecoyVFX(heroPos, targetPos, tileSize);
                    break;
            }
        }

        // ── Ricochet: Gold bouncing trail with spark at bounce point ──
        private void SpawnRicochetVFX(Vector2Int from, Vector2Int to, float ts)
        {
            var go = CreateLineFX("VFX_Ricochet", from, to, ts,
                new Color(1f, 0.85f, 0.2f), 0.07f);
            // Spark at midpoint (bounce)
            var mid = new Vector2Int((from.x + to.x) / 2, (from.y + to.y) / 2);
            var spark = CreateSphere("Spark", mid, ts, new Color(1f, 1f, 0.5f), 0.25f);
            spark.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(spark, 1f));
            StartCoroutine(DestroyAfter(go, 1.2f));
        }

        // ── Push: Expanding cyan shockwave ring at impact ──
        private void SpawnPushVFX(Vector2Int pos, float ts)
        {
            var ring = CreateSphere("VFX_Push", pos, ts, new Color(0.25f, 0.7f, 1f, 0.7f), 0.15f);
            ring.AddComponent<SimpleScaleUp>();
            // Second larger ring for shockwave effect
            var wave = CreateSphere("VFX_Push_Wave", pos, ts, new Color(0.25f, 0.7f, 1f, 0.3f), 0.6f);
            wave.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(ring, 0.8f));
            StartCoroutine(DestroyAfter(wave, 1.2f));
        }

        // ── Blink: Purple portals at start and end ──
        private void SpawnBlinkVFX(Vector2Int from, Vector2Int to, float ts)
        {
            // Entry portal — shrinking
            var entry = CreateSphere("VFX_Blink_Entry", from, ts, new Color(0.6f, 0.2f, 0.9f, 0.8f), 0.5f);
            entry.AddComponent<SimpleScaleUp>();
            // Exit portal — expanding
            var exit = CreateSphere("VFX_Blink_Exit", to, ts, new Color(0.8f, 0.4f, 1f), 0.5f);
            exit.AddComponent<SimpleScaleUp>();
            // Trail between portals
            var trail = CreateLineFX("VFX_Blink_Trail", from, to, ts,
                new Color(0.6f, 0.2f, 0.9f, 0.4f), 0.04f);
            StartCoroutine(DestroyAfter(entry, 1f));
            StartCoroutine(DestroyAfter(exit, 1.2f));
            StartCoroutine(DestroyAfter(trail, 0.8f));
        }

        // ── Scan: Green expanding radar pulse ──
        private void SpawnScanVFX(Vector2Int pos, float ts)
        {
            // 3 concentric expanding rings
            for (int i = 0; i < 3; i++)
            {
                var ring = CreateSphere($"VFX_Scan_{i}", pos, ts,
                    new Color(0.2f, 0.9f, 0.4f, 0.5f - i * 0.15f), 0.3f + i * 0.4f);
                ring.AddComponent<SimpleScaleUp>();
                StartCoroutine(DestroyAfter(ring, 1f + i * 0.3f));
            }
        }

        // ── PhaseShot: Cyan projectile with shimmer through wall ──
        private void SpawnPhaseShotVFX(Vector2Int from, Vector2Int to, float ts)
        {
            var line = CreateLineFX("VFX_PhaseShot", from, to, ts,
                new Color(0.3f, 0.8f, 1f, 0.8f), 0.06f);
            // Shimmer sphere at wall pass-through point
            var mid = new Vector2Int((from.x + to.x) / 2, (from.y + to.y) / 2);
            var shimmer = CreateSphere("VFX_Phase_Shimmer", mid, ts,
                new Color(0.3f, 0.8f, 1f, 0.6f), 0.35f);
            shimmer.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(line, 1f));
            StartCoroutine(DestroyAfter(shimmer, 0.8f));
        }

        // ── Bomb: Orange-red cross explosion ──
        private void SpawnBombVFX(Vector2Int pos, float ts)
        {
            // Center explosion
            var center = CreateSphere("VFX_Bomb_Center", pos, ts,
                new Color(1f, 0.4f, 0.1f), 0.6f);
            center.AddComponent<SimpleScaleUp>();
            // Cross arms (4 directions)
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                var arm = CreateSphere($"VFX_Bomb_Arm", pos + d, ts,
                    new Color(1f, 0.6f, 0.2f, 0.7f), 0.4f);
                arm.AddComponent<SimpleScaleUp>();
                StartCoroutine(DestroyAfter(arm, 1f));
            }
            StartCoroutine(DestroyAfter(center, 1.2f));
        }

        // ── Barrier: Blue wall materialization ──
        private void SpawnBarrierVFX(Vector2Int pos, float ts)
        {
            // Tall blue rectangle (wall shape)
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "VFX_Barrier";
            wall.transform.position = GridHelper.GridToWorld(pos, ts) + Vector3.up * 0.5f;
            wall.transform.localScale = new Vector3(0.9f, 1.2f, 0.15f);
            var col = wall.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var rend = wall.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.5f, 1f, 0.6f);
                rend.sharedMaterial = mat;
            }
            wall.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(wall, 2f));
        }

        // ── Cloak: Fading ghost silhouette ──
        private void SpawnCloakVFX(Vector2Int pos, float ts)
        {
            var ghost = CreateSphere("VFX_Cloak", pos, ts,
                new Color(0.7f, 0.7f, 0.8f, 0.4f), 0.6f);
            // Fade and shrink over time
            ghost.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(ghost, 1.5f));
        }

        // ── Turret: Mechanical spawn + laser beam ──
        private void SpawnTurretVFX(Vector2Int pos, Vector2Int target, float ts)
        {
            // Turret body (cube)
            var turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
            turret.name = "VFX_Turret";
            turret.transform.position = GridHelper.GridToWorld(pos, ts) + Vector3.up * 0.3f;
            turret.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var col = turret.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var rend = turret.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                mat.color = new Color(0.9f, 0.6f, 0.1f);
                rend.sharedMaterial = mat;
            }
            // Laser beam to target
            if (target != pos)
            {
                var beam = CreateLineFX("VFX_Turret_Beam", pos, target, ts,
                    new Color(1f, 0.3f, 0.1f, 0.8f), 0.05f);
                StartCoroutine(DestroyAfter(beam, 1f));
            }
            turret.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(turret, 1.5f));
        }

        // ── Charge: Red dash trail from start to end ──
        private void SpawnChargeVFX(Vector2Int from, Vector2Int to, float ts)
        {
            var trail = CreateLineFX("VFX_Charge", from, to, ts,
                new Color(1f, 0.2f, 0.1f, 0.9f), 0.12f);
            // Impact burst at end
            var impact = CreateSphere("VFX_Charge_Impact", to, ts,
                new Color(1f, 0.3f, 0.1f), 0.45f);
            impact.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(trail, 0.8f));
            StartCoroutine(DestroyAfter(impact, 1f));
        }

        // ── Pierce: White sharp line through everything ──
        private void SpawnPierceVFX(Vector2Int from, Vector2Int to, float ts)
        {
            var line = CreateLineFX("VFX_Pierce", from, to, ts,
                new Color(1f, 1f, 1f, 0.9f), 0.04f);
            // Flash at hero position
            var flash = CreateSphere("VFX_Pierce_Flash", from, ts,
                new Color(1f, 1f, 0.9f), 0.3f);
            flash.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(line, 1.2f));
            StartCoroutine(DestroyAfter(flash, 0.6f));
        }

        // ── Decoy: Pink clone shimmer + swap flash ──
        private void SpawnDecoyVFX(Vector2Int heroPos, Vector2Int decoyPos, float ts)
        {
            // Clone shimmer at decoy position
            var clone = CreateSphere("VFX_Decoy_Clone", decoyPos, ts,
                new Color(0.9f, 0.4f, 0.7f, 0.6f), 0.5f);
            clone.AddComponent<SimpleScaleUp>();
            // Swap flash at hero
            var flash = CreateSphere("VFX_Decoy_Flash", heroPos, ts,
                new Color(0.9f, 0.4f, 0.7f, 0.8f), 0.3f);
            flash.AddComponent<SimpleScaleUp>();
            StartCoroutine(DestroyAfter(clone, 1.5f));
            StartCoroutine(DestroyAfter(flash, 0.8f));
        }

        // ── Shield: Blue energy dome around hero ──
        public void SpawnShieldVFX(Vector2Int pos, float ts)
        {
            // Outer dome (transparent blue sphere)
            var dome = CreateSphere("VFX_Shield_Dome", pos, ts,
                new Color(0.22f, 0.74f, 0.97f, 0.35f), 0.8f);
            dome.AddComponent<SimpleScaleUp>();
            // Inner core (brighter, smaller)
            var core = CreateSphere("VFX_Shield_Core", pos, ts,
                new Color(0.22f, 0.74f, 0.97f, 0.6f), 0.45f);
            core.AddComponent<SimpleScaleUp>();
            // Ring at feet
            var ring = CreateSphere("VFX_Shield_Ring", pos, ts,
                new Color(0.4f, 0.85f, 1f, 0.5f), 0.7f);
            ring.transform.localScale = new Vector3(0.7f, 0.05f, 0.7f);
            StartCoroutine(DestroyAfter(dome, 1.5f));
            StartCoroutine(DestroyAfter(core, 1.2f));
            StartCoroutine(DestroyAfter(ring, 1.5f));
        }

        // ── Helpers for creating VFX primitives ──

        private GameObject CreateSphere(string name, Vector2Int gridPos, float tileSize, Color color, float scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.position = GridHelper.GridToWorld(gridPos, tileSize) + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * scale;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                mat.color = color;
                rend.sharedMaterial = mat;
            }
            return go;
        }

        private GameObject CreateLineFX(string name, Vector2Int from, Vector2Int to, float tileSize, Color color, float width)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, GridHelper.GridToWorld(from, tileSize) + Vector3.up * 0.5f);
            lr.SetPosition(1, GridHelper.GridToWorld(to, tileSize) + Vector3.up * 0.5f);
            lr.startWidth = width;
            lr.endWidth = width * 0.5f;
            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
            mat.color = color;
            lr.material = mat;
            return go;
        }

        private System.Collections.IEnumerator DestroyAfter(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go != null) Destroy(go);
        }

        /// <summary>
        /// Spawns a generic VFX prefab at a grid position and auto-returns it.
        /// </summary>
        public void SpawnGeneric(GameObject prefab, Vector2Int pos, float tileSize = 1f, float lifetime = 1.5f)
        {
            if (prefab == null) return;
            SpawnAt(prefab, pos, tileSize, lifetime);
        }

        #endregion

        #region Private Methods — Pooling

        private void WarmPool(GameObject prefab)
        {
            if (prefab == null) return;
            if (!_pools.ContainsKey(prefab))
                _pools[prefab] = new Queue<GameObject>();

            for (int i = 0; i < _initialPoolSize; i++)
            {
                var obj = Instantiate(prefab, _poolParent);
                obj.SetActive(false);
                _pools[prefab].Enqueue(obj);
            }
        }

        private GameObject GetFromPool(GameObject prefab)
        {
            if (!_pools.ContainsKey(prefab))
                _pools[prefab] = new Queue<GameObject>();

            if (_pools[prefab].Count > 0)
            {
                var obj = _pools[prefab].Dequeue();
                obj.SetActive(true);
                return obj;
            }

            return Instantiate(prefab, _poolParent);
        }

        private void ReturnToPool(GameObject prefab, GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(_poolParent);

            if (!_pools.ContainsKey(prefab))
                _pools[prefab] = new Queue<GameObject>();

            _pools[prefab].Enqueue(obj);
        }

        private void SpawnAt(GameObject prefab, Vector2Int gridPos, float tileSize, float lifetime)
        {
            if (prefab == null) return;
            var worldPos = GridHelper.GridToWorld(gridPos, tileSize) + Vector3.up * 0.5f;
            var vfx = GetFromPool(prefab);
            vfx.transform.position = worldPos;
            vfx.transform.rotation = Quaternion.identity;
            AutoReturn(vfx, prefab, lifetime);
        }

        private void AutoReturn(GameObject obj, GameObject prefab, float delay)
        {
            StartCoroutine(ReturnAfterDelay(obj, prefab, delay));
        }

        private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, GameObject prefab, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
                ReturnToPool(prefab, obj);
        }

        #endregion
    }
}
