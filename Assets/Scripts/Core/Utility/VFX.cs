using System;
using System.Collections;
using System.Collections.Generic;
using Core.Singletons;
using UnityEngine;

namespace Core.Utility
{
    public class VFX
    {
        private readonly VFXPrefabContainer _prefabContainer;
        private readonly Dictionary<int, List<PooledVFXInstance>> _effectPools = new Dictionary<int, List<PooledVFXInstance>>();
        private readonly Dictionary<GameObject, PooledVFXInstance> _allInstances = new Dictionary<GameObject, PooledVFXInstance>();
        private Transform _poolParent;
        private const float OffscreenPosition = 10000f;

        private class PooledVFXInstance
        {
            public GameObject InstanceObject { get; }
            public ParticleSystem ParticleSys { get; }
            public int PrefabIndex { get; }
            public bool IsActive { get; set; }

            public PooledVFXInstance(GameObject instance, ParticleSystem ps, int index)
            {
                InstanceObject = instance;
                ParticleSys = ps;
                PrefabIndex = index;
                IsActive = false;
            }
        }

        public VFX(VFXPrefabContainer prefabContainer)
        {
            _prefabContainer = prefabContainer ?? throw new ArgumentNullException(nameof(prefabContainer));
        }

        public void InitializePool(int initialPoolSize = 5)
        {
            if (_prefabContainer == null || _prefabContainer.Prefabs == null || _prefabContainer.Prefabs.Count == 0)
            {
                Debug.LogError("VFX Prefab Container is not set or has no prefabs!");
                return;
            }

            if (_poolParent == null)
            {
                GameObject poolParentObj = new GameObject("VFX_Pool");
                poolParentObj.transform.position = new Vector3(OffscreenPosition, OffscreenPosition, OffscreenPosition);
                _poolParent = poolParentObj.transform;
            }

            // Pre-validate prefabs
            List<int> validPrefabIndices = new List<int>();
            for (int i = 0; i < _prefabContainer.Prefabs.Count; i++)
            {
                GameObject prefab = _prefabContainer.Prefabs[i];
                if (prefab == null) continue;

                // Check if prefab has a particle system
                ParticleSystem testPS = prefab.GetComponent<ParticleSystem>();
                if (testPS == null) testPS = prefab.GetComponentInChildren<ParticleSystem>();
                if (testPS == null)
                {
                    Debug.LogWarning($"Prefab {prefab.name} has no ParticleSystem component and will be skipped.");
                    continue;
                }

                validPrefabIndices.Add(i);
                if (!_effectPools.ContainsKey(i))
                {
                    _effectPools[i] = new List<PooledVFXInstance>();
                }
            }

            // Create initial pool instances for valid prefabs
            foreach (int prefabIndex in validPrefabIndices)
            {
                for (int j = 0; j < initialPoolSize; j++)
                {
                    CreateAndPoolInstance(prefabIndex);
                }
            }
        }

        private PooledVFXInstance CreateAndPoolInstance(int prefabIndex)
        {
            GameObject prefab = _prefabContainer.Prefabs[prefabIndex];
            if (prefab == null) return null;

            // Create instance
            GameObject instance = UnityEngine.Object.Instantiate(prefab, _poolParent);
            instance.name = $"{prefab.name}_Pooled_{_allInstances.Count}";

            // Get particle system component - we already validated this in InitializePool
            ParticleSystem ps = instance.GetComponent<ParticleSystem>();
            if (ps == null) ps = instance.GetComponentInChildren<ParticleSystem>();

            // Create and store the pooled instance
            PooledVFXInstance pooledInstance = new PooledVFXInstance(instance, ps, prefabIndex);
            _allInstances.Add(instance, pooledInstance);

            instance.transform.position = new Vector3(OffscreenPosition, OffscreenPosition, OffscreenPosition);
            instance.SetActive(false);
            _effectPools[prefabIndex].Add(pooledInstance);

            return pooledInstance;
        }

        private PooledVFXInstance GetFromPool(int prefabIndex)
        {
            if (!_effectPools.TryGetValue(prefabIndex, out List<PooledVFXInstance> pool))
            {
                return null;
            }

            for (int i = pool.Count - 1; i >= 0; i--)
            {
                if (!pool[i].IsActive)
                {
                    PooledVFXInstance instanceToUse = pool[i];
                    instanceToUse.IsActive = true;
                    instanceToUse.InstanceObject.SetActive(true);
                    return instanceToUse;
                }
            }

            // All instances are active, create a new one
            return CreateAndPoolInstance(prefabIndex);
        }

        private void ReturnToPool(PooledVFXInstance instanceInfo)
        {
            if (instanceInfo == null || instanceInfo.InstanceObject == null) return;
            instanceInfo.InstanceObject.SetActive(false);
            instanceInfo.InstanceObject.transform.SetParent(_poolParent);
            instanceInfo.InstanceObject.transform.position = new Vector3(OffscreenPosition, OffscreenPosition, OffscreenPosition);
            instanceInfo.InstanceObject.transform.localScale = Vector3.one;
            instanceInfo.IsActive = false;
        }

        public void PlayEffectAt(Transform targetTransform, float scale = 1.0f, float duration = -1f)
        {
            if (targetTransform == null || _prefabContainer == null || _prefabContainer.Prefabs.Count == 0)
            {
                return;
            }

            // Get a random valid prefab index
            List<int> validIndices = new List<int>(_effectPools.Keys);
            if (validIndices.Count == 0) return;
            
            int prefabIndex = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
            PooledVFXInstance vfxInstance = GetFromPool(prefabIndex);
            if (vfxInstance == null || vfxInstance.InstanceObject == null) return;

            GameObject vfxObject = vfxInstance.InstanceObject;
            ParticleSystem ps = vfxInstance.ParticleSys;
            
            //Debug.Log($"VFX debug: '{vfxObject.name}' Duration={ps.main.duration}s");

            
            vfxObject.transform.position = targetTransform.position;
            Vector3 pos = vfxObject.transform.position;
            pos.z -= 15f; // Z offset
            vfxObject.transform.position = pos;
            vfxObject.transform.localScale = Vector3.one * scale;

            // Calculate duration BEFORE playing
            float actualDuration;
            var main = ps.main;
            actualDuration = duration < 0 ? main.duration + main.startLifetime.constantMax + 0.1f : duration;

            // Now play the particle system
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();

            // Schedule return to pool
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCoroutine(ReturnToPoolAfterDelay(vfxInstance, actualDuration));
            }
        }

        private IEnumerator ReturnToPoolAfterDelay(PooledVFXInstance instanceInfo, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(instanceInfo);
        }

        public void TestCycleAllEffects(Transform testLocation, float scale = 1.0f)
        {
            if (_prefabContainer == null || _prefabContainer.Prefabs.Count == 0) return;
            if (GameManager.Instance == null) return;
            
            GameManager.Instance.StartCoroutine(CycleEffectsCoroutine(testLocation, scale));
        }

        private IEnumerator CycleEffectsCoroutine(Transform testLocation, float scale)
        {
            List<int> validIndices = new List<int>(_effectPools.Keys);
            
            foreach (int prefabIndex in validIndices)
            {
                PooledVFXInstance vfxInstance = GetFromPool(prefabIndex);
                if (vfxInstance == null) continue;

                GameObject vfxObject = vfxInstance.InstanceObject;
                ParticleSystem ps = vfxInstance.ParticleSys;

                vfxObject.transform.position = testLocation.position;
                Vector3 pos = vfxObject.transform.position;
                pos.z -= 1f;
                vfxObject.transform.position = pos;
                vfxObject.transform.localScale = Vector3.one * scale;

                // Calculate duration
                var main = ps.main;
                float duration = main.duration + main.startLifetime.constantMax + 0.5f;
                
                // Play effect
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
                
                yield return new WaitForSeconds(duration);
                ReturnToPool(vfxInstance);
                yield return null;
            }
        }
    }
}
