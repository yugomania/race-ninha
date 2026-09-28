namespace ACR.Vehicle
{
    /// <summary>
    /// Abstraction over "however input is currently being provided" — keyboard (editor/testing),
    /// touch (mobile), or eventually a gamepad. VehicleController only ever talks to this interface,
    /// never to Input.GetAxis directly, so swapping input methods never touches vehicle physics code.
    /// 
    /// Serves Pillar: "One-Thumb Mastery" (enables swappable touch-assists and AI drivers).
    /// </summary>
    public interface IVehicleInputSource
    {
        /// <summary>-1 (full reverse/brake) to 1 (full forward).</summary>
        float Throttle { get; }

        /// <summary>-1 (full left) to 1 (full right).</summary>
        float Steer { get; }

        /// <summary>True while the brake/handbrake input is held.</summary>
        bool Brake { get; }

        /// <summary>True while the nitro input is held/active.</summary>
        bool NitroRequested { get; }

        /// <summary>True while the weapon-fire input is held/pressed.</summary>
        bool WeaponFireRequested { get; }
    }
}
