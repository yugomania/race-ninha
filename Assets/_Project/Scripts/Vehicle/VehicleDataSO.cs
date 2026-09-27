using UnityEngine;

namespace ACR.Vehicle
{
    /// <summary>
    /// Data-driven stat container for a vehicle. One asset per car (e.g. CAR_Lion_01.asset).
    /// VehicleController reads from this at runtime — never hard-code stats in code.
    /// Serves Pillars: "Personality Over Realism" & "One-Thumb Mastery".
    /// </summary>
    [CreateAssetMenu(fileName = "VehicleData", menuName = "AnimalCombatRacing/Vehicle Data", order = 1)]
    public class VehicleDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string vehicleId = "CAR_Unnamed_01";

        [Header("Speed")]
        [Tooltip("Top speed in units/sec (arcade units, not necessarily real km/h).")]
        public float topSpeed = 40f;
        [Tooltip("Forward force applied per fixed update while accelerating.")]
        public float accelerationForce = 25f;
        [Tooltip("Force applied while braking.")]
        public float brakeForce = 40f;
        [Tooltip("Passive deceleration when no input is applied (rolling resistance).")]
        public float coastingDrag = 4f;

        [Header("Steering")]
        [Tooltip("Max steering angle in degrees at low speed.")]
        public float maxSteerAngle = 30f;
        [Tooltip("How quickly steer angle responds to input (higher = snappier).")]
        public float steerSpeed = 5f;
        [Tooltip("0-1. How much of maxSteerAngle is retained at top speed (prevents twitchy high-speed steering).")]
        [Range(0f, 1f)] public float highSpeedSteerRetention = 0.4f;

        [Header("Suspension (raycast-based, locked approach)")]
        [Tooltip("Distance from wheel socket to ground at rest.")]
        public float suspensionRestDistance = 0.5f;
        [Tooltip("Spring stiffness. Higher = stiffer ride, less body roll.")]
        public float springStrength = 60f;
        [Tooltip("Spring damping. Higher = settles faster, less bounce.")]
        public float springDamper = 6f;
        [Tooltip("Radius of each wheel, added to raycast length.")]
        public float wheelRadius = 0.35f;

        [Header("Grip")]
        [Tooltip("0-1. How strongly sideways (lateral) velocity is cancelled at the wheel. 1 = no drift, lower = looser/driftier.")]
        [Range(0f, 1f)] public float gripFactor = 0.85f;

        [Header("Mass & Armor")]
        public float mass = 1200f;
        [Tooltip("Damage reduction multiplier — used later by VehicleHealth, not Phase 2.")]
        public float armor = 1f;

        [Header("Nitro (used from Phase 9 onward — stored here for data-driven consistency)")]
        public float nitroCapacity = 100f;
        public float nitroRechargeRate = 8f;

        [Header("Weapons (used from Phase 10 onward)")]
        public int weaponCapacity = 1;
    }
}
