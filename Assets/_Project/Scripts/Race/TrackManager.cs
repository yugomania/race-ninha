using System.Linq;
using UnityEngine;

namespace ACR.Race
{
    /// <summary>
    /// Holds a track's structural data: ordered checkpoints and starting grid positions.
    /// Phase 5 scope: structure and validation only. No countdown (Phase 6), no lap counting
    /// or race completion (Phase 7), no AI racing line (Phase 8) — those systems will read
    /// from this TrackManager rather than duplicating track data themselves.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        [Header("Checkpoints (auto-populated by Validate(), or assign manually)")]
        [SerializeField] private TrackCheckpoint[] checkpoints;

        [Header("Starting Grid")]
        [Tooltip("Grid slot 0 = pole position. Order matters.")]
        [SerializeField] private Transform[] startGridPositions;

        [Header("Track Config")]
        [SerializeField] private int lapCount = 3;

        public TrackCheckpoint[] Checkpoints => checkpoints;
        public int CheckpointCount => checkpoints != null ? checkpoints.Length : 0;
        public int LapCount => lapCount;
        public Transform[] StartGridPositions => startGridPositions;

        public void SetCheckpoints(TrackCheckpoint[] newCheckpoints)
        {
            checkpoints = newCheckpoints;
        }

        public void SetStartGridPositions(Transform[] newGridPositions)
        {
            startGridPositions = newGridPositions;
        }

        public Vector3 GetStartPosition(int gridSlot)
        {
            if (startGridPositions == null || gridSlot < 0 || gridSlot >= startGridPositions.Length)
            {
                Debug.LogWarning($"{name}: invalid grid slot {gridSlot}, defaulting to TrackManager position.");
                return transform.position;
            }
            return startGridPositions[gridSlot].position;
        }

        public Quaternion GetStartRotation(int gridSlot)
        {
            if (startGridPositions == null || gridSlot < 0 || gridSlot >= startGridPositions.Length)
            {
                return transform.rotation;
            }
            return startGridPositions[gridSlot].rotation;
        }

        /// <summary>
        /// Editor/startup sanity check: checkpoints must be contiguous starting at 0,
        /// with exactly one marked as the finish line. Catches track-setup mistakes early
        /// instead of surfacing as confusing bugs during Phase 7 lap-counting.
        /// </summary>
        [ContextMenu("Validate Track")]
        public bool Validate()
        {
            if (checkpoints == null || checkpoints.Length == 0)
            {
                checkpoints = GetComponentsInChildren<TrackCheckpoint>();
            }

            if (checkpoints == null || checkpoints.Length == 0)
            {
                Debug.LogError($"{name}: no checkpoints assigned.", this);
                return false;
            }

            var ordered = checkpoints.OrderBy(c => c.checkpointIndex).ToArray();
            for (int i = 0; i < ordered.Length; i++)
            {
                if (ordered[i].checkpointIndex != i)
                {
                    Debug.LogError($"{name}: checkpoint indices are not contiguous starting at 0 " +
                                    $"(expected {i}, found {ordered[i].checkpointIndex} on {ordered[i].name}).", ordered[i]);
                    return false;
                }
            }

            int finishLineCount = checkpoints.Count(c => c.isFinishLine);
            if (finishLineCount != 1)
            {
                Debug.LogError($"{name}: expected exactly 1 finish-line checkpoint, found {finishLineCount}.", this);
                return false;
            }

            if (startGridPositions == null || startGridPositions.Length == 0)
            {
                Debug.LogError($"{name}: no starting grid positions assigned.", this);
                return false;
            }

            Debug.Log($"{name}: track validated OK — {checkpoints.Length} checkpoints, " +
                      $"{startGridPositions.Length} grid slots, {lapCount} laps.");
            return true;
        }
    }
}
