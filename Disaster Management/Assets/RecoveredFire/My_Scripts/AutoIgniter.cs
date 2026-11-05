using UnityEngine;

namespace FireSimulation
{
    [RequireComponent(typeof(FireSimulationManager))]
    public class AutoIgniter : MonoBehaviour
    {
        [Header("Ignition Settings")]
        [Tooltip("Drag empty GameObjects here to mark where fires should start.")]
        public Transform[] ignitionMarkers;

        [Tooltip("Seconds to wait before starting the fire")]
        public float delay = 1.0f;

        private void Start()
        {
            Invoke(nameof(TriggerFires), delay);
        }

        private void TriggerFires()
        {
            FireSimulationManager manager = GetComponent<FireSimulationManager>();
            Vector3 origin = manager.GetOrigin();

            foreach (Transform marker in ignitionMarkers)
            {
                if (marker == null) continue;

                // Convert the marker's physical world position into grid coordinates
                Vector3 localPos = marker.position - origin;
                int gridX = Mathf.FloorToInt(localPos.x / manager.cellSize);
                int gridZ = Mathf.FloorToInt(localPos.z / manager.cellSize);

                manager.IgniteCell(gridX, gridZ);
                Debug.Log($"Auto-Ignited from marker {marker.name} at Grid [{gridX}, {gridZ}]");
            }
        }
    }
}