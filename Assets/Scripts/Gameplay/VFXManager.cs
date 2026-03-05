using System.Collections.Generic;
using UnityEngine;
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
            if (_shootVFXPrefab == null) return;
            var pos = GridHelper.GridToWorld(from, tileSize) + Vector3.up * 0.5f;
            var lookAt = GridHelper.GridToWorld(to, tileSize) + Vector3.up * 0.5f;
            var vfx = GetFromPool(_shootVFXPrefab);
            vfx.transform.position = pos;
            vfx.transform.LookAt(lookAt);
            AutoReturn(vfx, _shootVFXPrefab, 1f);
        }

        public void SpawnHitVFX(Vector2Int pos, float tileSize = 1f)
        {
            SpawnAt(_hitVFXPrefab, pos, tileSize, 1.5f);
        }

        public void SpawnArmorBreakVFX(Vector2Int pos, float tileSize = 1f)
        {
            SpawnAt(_armorBreakVFXPrefab, pos, tileSize, 1.5f);
        }

        public void SpawnEliminationVFX(Vector2Int pos, float tileSize = 1f)
        {
            SpawnAt(_eliminationVFXPrefab, pos, tileSize, 2f);
        }

        public void SpawnMutualCancelVFX(Vector2Int midpoint, float tileSize = 1f)
        {
            SpawnAt(_mutualCancelVFXPrefab, midpoint, tileSize, 1.5f);
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
