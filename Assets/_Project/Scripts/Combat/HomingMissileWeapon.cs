using UnityEngine;
using ACR.Core;
using ACR.Vehicle;

namespace ACR.Combat
{
    /// <summary>
    /// First fictional weapon: fires a homing missile at the nearest other vehicle within range.
    /// All cooldown/input/lock handling comes from WeaponBase — this class only implements Fire()
    /// and target acquisition, which is the point of the modular framework.
    ///
    /// Serves Pillars: "Readable Chaos" & "Always a Comeback".
    /// </summary>
    public class HomingMissileWeapon : WeaponBase
    {
        [Header("Projectile")]
        [SerializeField] private HomingMissileProjectile projectilePrefab;
        [SerializeField] private int poolPrewarmCount = 5;
        [SerializeField] private Transform firePoint;

        [Header("Missile Tuning")]
        [SerializeField] private float missileSpeed = 30f;
        [SerializeField] private float missileTurnRateDegreesPerSec = 120f;
        [SerializeField] private float missileLifetime = 4f;
        [SerializeField] private float targetSearchRadius = 40f;

        private ObjectPool<HomingMissileProjectile> pool;

        public ObjectPool<HomingMissileProjectile> Pool => pool;
        public int AvailableMissiles => pool != null ? pool.AvailableCount : 0;

        public void Configure(HomingMissileProjectile prefab, Transform point, float cooldown = 2f)
        {
            projectilePrefab = prefab;
            firePoint = point;
            cooldownDuration = cooldown;
            InitializePool();
        }

        protected override void Start()
        {
            base.Start();
            InitializePool();
        }

        private void InitializePool()
        {
            if (pool != null || projectilePrefab == null) return;
            pool = new ObjectPool<HomingMissileProjectile>(projectilePrefab, poolPrewarmCount, transform);
        }

        protected override void Fire(VehicleController firer)
        {
            if (pool == null || firePoint == null) return;

            Transform target = FindNearestTarget(firer);
            HomingMissileProjectile missile = pool.Get();
            missile.Launch(firePoint.position, firePoint.rotation, target, firer,
                            missileSpeed, missileTurnRateDegreesPerSec, missileLifetime, ReturnToPool);
        }

        private void ReturnToPool(HomingMissileProjectile projectile)
        {
            pool.Release(projectile);
        }

        /// <summary>
        /// Target acquisition: finds the nearest other vehicle within targetSearchRadius.
        /// If no other vehicles are in range, returns null and missile flies straight.
        /// </summary>
        private Transform FindNearestTarget(VehicleController firer)
        {
#if UNITY_2023_1_OR_NEWER
            VehicleController[] allVehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
#else
            VehicleController[] allVehicles = FindObjectsOfType<VehicleController>();
#endif
            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (VehicleController vehicle in allVehicles)
            {
                if (vehicle == firer) continue;

                float distance = Vector3.Distance(firer.transform.position, vehicle.transform.position);
                if (distance < nearestDistance && distance <= targetSearchRadius)
                {
                    nearestDistance = distance;
                    nearest = vehicle.transform;
                }
            }

            return nearest;
        }
    }
}
