# Track System Specification (Phase 5)

## Overview
The track system defines the physical and logical structure of racing circuits in *Animal Combat Racing*. It decouples spatial layout (checkpoints, grid slots, road meshes) from race governance (laps, countdown, placements), adhering strictly to single-responsibility architecture.

## Design Pillars Served
- **Pillar 1: Readable Chaos**
  Clear, unambiguous checkpoint triggers and wide track lanes (~14m minimum) ensure drivers and AI have ample maneuvering room during intense combat and overtaking manoeuvres.
- **Pillar 2: Always a Comeback**
  Staggered starting grid layouts (2x2 F1 format) give trailing grid slots slipstream opportunities immediately off the line.

## Architecture & Components

### 1. `TrackCheckpoint.cs`
Mounted on each checkpoint trigger volume (`ACR.Race` assembly).
- **Trigger Volume**: Uses a non-physical `Collider (isTrigger = true)` spanning the width and height of the track segment.
- **Static Decoupled Event**:
  ```csharp
  public static event Action<TrackCheckpoint, VehicleController> OnVehiclePassedCheckpoint;
  ```
  Fires purely observational facts: "vehicle $V$ crossed checkpoint $C$".
- **Zero Race Knowledge**: Does not track laps, timers, splits, or rankings. Phase 7's `LapSystem` will subscribe to this event to manage lap progress and anti-cheat validation.
- **Visual Gizmos**: Color-coded in the Unity scene view (green for finish line, cyan for intermediate splits) with a directional forward arrow.

### 2. `TrackManager.cs`
Scene container holding circuit topology and configuration (`ACR.Race` assembly).
- **Properties**:
  - `TrackCheckpoint[] checkpoints`: Contiguous sequence starting at index 0.
  - `Transform[] startGridPositions`: Staggered starting grid nodes (Slot 0 = pole position).
  - `int lapCount`: Number of laps required to complete the race (default 3).
- **Validation Engine (`[ContextMenu("Validate Track")]`)**:
  Performs structural sanity checks at edit and load time:
  1. Verifies checkpoints array is populated.
  2. Ensures checkpoint indices are strictly contiguous ($0, 1, 2, \dots, N-1$).
  3. Enforces that exactly **one** checkpoint is flagged as `isFinishLine` (index 0).
  4. Confirms starting grid positions are populated and non-empty.

## Responsibility Split: Checkpoint Crossing Direction
As established in Phase 5 architectural decisions:
- `TrackCheckpoint` is a simple physical trigger sensor that reports crossings without validating direction or order.
- Phase 7 (`LapSystem` / `RaceManager`) is solely responsible for verifying correct traversal sequence, lap progression, and reverse-driving warnings.

## Downstream Integration Points
- **Phase 6 (Countdown & Race Start)**: Positions player and AI opponents onto `TrackManager.GetStartPosition(gridSlot)` and freezes vehicle physics until the green light.
- **Phase 7 (Lap & Placement System)**: Subscribes to `TrackCheckpoint.OnVehiclePassedCheckpoint` to update racer lap numbers and race leaderboards.
- **Phase 8 (AI Opponent Pathfinding)**: Uses checkpoint nodes to align waypoints and track centerlines.
