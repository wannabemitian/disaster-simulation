using UnityEngine;

namespace FireSimulation
{
    [RequireComponent(typeof(FireSimulationManager))]
    public class ClickToIgnite : MonoBehaviour
    {
        private FireSimulationManager manager;

        private void Start()
        {
            manager = GetComponent<FireSimulationManager>();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 localPos = hit.point - manager.GetOrigin();
                    int gridX = Mathf.FloorToInt(localPos.x / manager.cellSize);
                    int gridZ = Mathf.FloorToInt(localPos.z / manager.cellSize);

                    manager.IgniteCell(gridX, gridZ);
                    Debug.Log($"Ignited Cell: [{gridX}, {gridZ}]");
                }
            }
        }
    }
}