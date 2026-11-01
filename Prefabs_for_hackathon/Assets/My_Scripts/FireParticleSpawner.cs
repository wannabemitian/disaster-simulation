using UnityEngine;
using System.Collections.Generic;

namespace FireSimulation
{
    [RequireComponent(typeof(FireSimulationManager))]
    public class FireParticleSpawner : MonoBehaviour
    {
        [Header("VFX Prefabs")]
        public ParticleSystem firePrefab;
        public ParticleSystem smokePrefab;

        private FireSimulationManager manager;
        private ParticleSystem[] activeFires;
        private ParticleSystem[] activeSmokes;

        private Queue<ParticleSystem> firePool = new Queue<ParticleSystem>();
        private Queue<ParticleSystem> smokePool = new Queue<ParticleSystem>();

        private void Start()
        {
            manager = GetComponent<FireSimulationManager>();
            int totalCells = manager.gridWidth * manager.gridHeight;
            activeFires = new ParticleSystem[totalCells];
            activeSmokes = new ParticleSystem[totalCells];

            // Pre-warm pools
            for (int i = 0; i < 50; i++)
            {
                firePool.Enqueue(CreateNewParticle(firePrefab, "PooledFire"));
                smokePool.Enqueue(CreateNewParticle(smokePrefab, "PooledSmoke"));
            }
        }

        private ParticleSystem CreateNewParticle(ParticleSystem prefab, string name)
        {
            ParticleSystem ps = Instantiate(prefab, transform);
            ps.gameObject.name = name;
            ps.Stop();
            return ps;
        }

        private void Update()
        {
            CellState[] grid = manager.GetReadBuffer();
            if (grid == null) return;

            Vector3 origin = manager.GetOrigin();
            float cellSize = manager.cellSize;

            for (int z = 0; z < manager.gridHeight; z++)
            {
                for (int x = 0; x < manager.gridWidth; x++)
                {
                    int i = z * manager.gridWidth + x;
                    CellState state = grid[i];
                    Vector3 cellPos = origin + new Vector3(x * cellSize, 0f, z * cellSize);

                    // --- FIRE VFX ---
                    if (state.fireIntensity > 0.1f)
                    {
                        if (activeFires[i] == null)
                        {
                            activeFires[i] = firePool.Count > 0 ? firePool.Dequeue() : CreateNewParticle(firePrefab, "Fire");
                            activeFires[i].transform.position = cellPos;
                            activeFires[i].Play(true);
                        }
                        foreach (var ps in activeFires[i].GetComponentsInChildren<ParticleSystem>())
                        {
                            var emission = ps.emission;
                            emission.rateOverTime = state.fireIntensity * 20f;
                        }
                    }
                    else if (activeFires[i] != null)
                    {
                        activeFires[i].Stop(true);
                        firePool.Enqueue(activeFires[i]);
                        activeFires[i] = null;
                    }

                    // --- SMOKE VFX ---
                    // --- SMOKE VFX ---
                    float totalSmoke = state.smokeUpper + state.smokeLower;
                    if (totalSmoke > 0.5f)
                    {
                        if (activeSmokes[i] == null)
                        {
                            activeSmokes[i] = smokePool.Count > 0 ? smokePool.Dequeue() : CreateNewParticle(smokePrefab, "Smoke");
                            activeSmokes[i].transform.position = cellPos + Vector3.up * 1.5f;
                            activeSmokes[i].Play(true);
                        }
                        foreach (var ps in activeSmokes[i].GetComponentsInChildren<ParticleSystem>())
                        {
                            var emission = ps.emission;

                            // CRITICAL FIX: Multiply by 0.2f instead of 5f to drastically reduce particle count, 
                            // and hard-clamp the absolute maximum to 8 particles per second per cell.
                            emission.rateOverTime = Mathf.Clamp(totalSmoke * 0.2f, 0f, 8f);
                        }
                    }
                    else if (activeSmokes[i] != null)
                    {
                        activeSmokes[i].Stop(true);
                        smokePool.Enqueue(activeSmokes[i]);
                        activeSmokes[i] = null;
                    }
                }
            }
        }
    }
}