using UnityEngine;
using System.Collections.Generic;

namespace FireSimulation
{
    public class FireSimulationManager : MonoBehaviour
    {
        [Header("Random Smoke Spawning")]
        public bool enableRandomSmoke = true;
        public float randomSmokeChance = 0.5f; // Chance to spawn smoke per tick
        public float smokeInjectionAmount = 20f;
        public int gridWidth = 50;
        public int gridHeight = 50;
        public float cellSize = 1.0f;
        public LayerMask wallMask;
        public LayerMask fuelMask;
        public float tickInterval = 0.1f;

        public SimParameters simParams = SimParameters.GetDefault();

        private CellTopology[] topology;
        private CellState[] readBuffer;
        private CellState[] writeBuffer;
        private float timer = 0f;

        private Vector3 origin;
        private Queue<int> scheduledIgnitions = new Queue<int>();

        public enum GizmoDrawMode { None, Fuel, Temperature, Fire, SmokeUpper }
        public GizmoDrawMode debugDrawMode = GizmoDrawMode.Temperature;

        private void Awake()
        {
            origin = transform.position;
            int totalCells = gridWidth * gridHeight;
            topology = GridTopologyBaker.BakeTopology(gridWidth, gridHeight, cellSize, origin, wallMask, fuelMask);
            readBuffer = new CellState[totalCells];
            writeBuffer = new CellState[totalCells];

            for (int i = 0; i < totalCells; i++)
            {
                readBuffer[i] = new CellState
                {
                    temperature = simParams.ambientTemp,
                    fuel = topology[i].initialFuel,
                    fireIntensity = 0,
                    smokeUpper = 0,
                    smokeLower = 0
                };
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= tickInterval)
            {
                // --- Add this block ---
                if (enableRandomSmoke && Random.value < randomSmokeChance)
                {
                    int randomX = Random.Range(0, gridWidth);
                    int randomZ = Random.Range(0, gridHeight);
                    int index = randomZ * gridWidth + randomX;

                    if (!topology[index].hasSolidObstacle)
                    {
                        // Inject pure smoke without heat or fire
                        readBuffer[index].smokeUpper = Mathf.Clamp(readBuffer[index].smokeUpper + smokeInjectionAmount, 0f, 100f);
                    }
                }
                // ----------------------

                ProcessIgnitions();
                ProcessIgnitions();
                FireSmokeSolver.StepSimulation(readBuffer, writeBuffer, topology, gridWidth, gridHeight, cellSize, tickInterval, simParams);

                // Swap buffers
                CellState[] temp = readBuffer;
                readBuffer = writeBuffer;
                writeBuffer = temp;

                timer = 0f;
            }
        }

        public void IgniteCell(int x, int z)
        {
            if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            {
                scheduledIgnitions.Enqueue(z * gridWidth + x);
            }
        }

        private void ProcessIgnitions()
        {
            while (scheduledIgnitions.Count > 0)
            {
                int index = scheduledIgnitions.Dequeue();
                if (!topology[index].hasSolidObstacle)
                {
                    readBuffer[index].temperature = simParams.maxFireTemp;
                    readBuffer[index].fireIntensity = 1.5f;
                }
            }
        }

        public HazardSample SampleHazardAtWorldPosition(Vector3 pos)
        {
            Vector3 localPos = pos - origin;
            float fx = localPos.x / cellSize;
            float fz = localPos.z / cellSize;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, gridWidth - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, gridHeight - 1);
            int x1 = Mathf.Clamp(x0 + 1, 0, gridWidth - 1);
            int z1 = Mathf.Clamp(z0 + 1, 0, gridHeight - 1);

            float tx = fx - x0;
            float tz = fz - z0;

            CellState c00 = readBuffer[z0 * gridWidth + x0];
            CellState c10 = readBuffer[z0 * gridWidth + x1];
            CellState c01 = readBuffer[z1 * gridWidth + x0];
            CellState c11 = readBuffer[z1 * gridWidth + x1];

            CellState c0 = CellState.Lerp(c00, c10, tx);
            CellState c1 = CellState.Lerp(c01, c11, tx);
            CellState finalState = CellState.Lerp(c0, c1, tz);

            return new HazardSample
            {
                temperature = finalState.temperature,
                fireIntensity = finalState.fireIntensity,
                smokeUpper = finalState.smokeUpper,
                smokeLower = finalState.smokeLower
            };
        }

        public CellState[] GetReadBuffer() { return readBuffer; }
        public Vector3 GetOrigin() { return origin; }

        private void OnDrawGizmos()
        {
            if (readBuffer == null || debugDrawMode == GizmoDrawMode.None) return;

            Vector3 size = new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f);
            for (int z = 0; z < gridHeight; z++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int i = z * gridWidth + x;
                    if (topology[i].hasSolidObstacle) continue;

                    Vector3 pos = origin + new Vector3(x * cellSize, 0f, z * cellSize);
                    CellState state = readBuffer[i];
                    Color c = Color.clear;

                    switch (debugDrawMode)
                    {
                        case GizmoDrawMode.Fuel:
                            c = Color.Lerp(Color.black, Color.green, state.fuel);
                            break;
                        case GizmoDrawMode.Temperature:
                            c = Color.Lerp(Color.blue, Color.red, (state.temperature - simParams.ambientTemp) / (simParams.maxFireTemp - simParams.ambientTemp));
                            break;
                        case GizmoDrawMode.Fire:
                            c = Color.Lerp(Color.black, new Color(1f, 0.5f, 0f), state.fireIntensity);
                            break;
                        case GizmoDrawMode.SmokeUpper:
                            c = Color.Lerp(Color.clear, Color.gray, state.smokeUpper / 10f);
                            break;
                    }
                    Gizmos.color = c;
                    Gizmos.DrawCube(pos, size);
                }
            }
        }
    }
}