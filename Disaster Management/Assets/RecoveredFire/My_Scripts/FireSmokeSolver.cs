using UnityEngine;

namespace FireSimulation
{
    public static class FireSmokeSolver
    {
        public static void StepSimulation(
            CellState[] read,
            CellState[] write,
            CellTopology[] topology,
            int width,
            int height,
            float cellSize,
            float tickInterval,
            SimParameters p)
        {
            // CFL Condition: max stable timestep for diffusion
            float maxDiff = Mathf.Max(p.thermalDiffusivity, p.smokeDiffusivity);
            float maxStableDt = (cellSize * cellSize) / (4.01f * maxDiff);

            int subSteps = Mathf.CeilToInt(tickInterval / maxStableDt);
            float dt = tickInterval / subSteps;
            float invDx2 = 1.0f / (cellSize * cellSize);

            // Copy initial state to write buffer for in-place substepping
            System.Array.Copy(read, write, read.Length);

            for (int step = 0; step < subSteps; step++)
            {
                // We use 'write' as both read and write during substeps for simplicity in this flat-array model, 
                // but conceptually we are integrating forward safely.
                CellState[] currentRead = (step == 0) ? read : write;

                for (int z = 0; z < height; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int i = z * width + x;
                        CellTopology topo = topology[i];
                        if (topo.hasSolidObstacle) continue;

                        CellState state = currentRead[i];
                        float tempLaplacian = 0f;
                        float smokeLaplacian = 0f;

                        // North
                        if (z < height - 1)
                        {
                            int n = i + width;
                            tempLaplacian += topo.permNorth * (currentRead[n].temperature - state.temperature);
                            smokeLaplacian += topo.permNorth * (currentRead[n].smokeUpper - state.smokeUpper);
                        }
                        // South
                        if (z > 0)
                        {
                            int s = i - width;
                            tempLaplacian += topo.permSouth * (currentRead[s].temperature - state.temperature);
                            smokeLaplacian += topo.permSouth * (currentRead[s].smokeUpper - state.smokeUpper);
                        }
                        // East
                        if (x < width - 1)
                        {
                            int e = i + 1;
                            tempLaplacian += topo.permEast * (currentRead[e].temperature - state.temperature);
                            smokeLaplacian += topo.permEast * (currentRead[e].smokeUpper - state.smokeUpper);
                        }
                        // West
                        if (x > 0)
                        {
                            int w = i - 1;
                            tempLaplacian += topo.permWest * (currentRead[w].temperature - state.temperature);
                            smokeLaplacian += topo.permWest * (currentRead[w].smokeUpper - state.smokeUpper);
                        }

                        // Combustion & Heat
                        if (state.temperature >= p.ignitionTemp && state.fuel > 0f)
                        {
                            float burnAmount = Mathf.Min(state.fuel, p.fuelConsumptionRate * p.burnRateMultiplier * dt);
                            state.fuel -= burnAmount;
                            state.fireIntensity = burnAmount / (p.fuelConsumptionRate * dt + 0.0001f);
                            state.temperature += burnAmount * p.fireHeatOutput;
                            state.smokeUpper += state.fireIntensity * 8.0f * dt;
                        }
                        else
                        {
                            state.fireIntensity = Mathf.MoveTowards(state.fireIntensity, 0f, dt * 2f);
                        }

                        // Diffusion & Cooling
                        state.temperature += p.thermalDiffusivity * tempLaplacian * invDx2 * dt;
                        state.temperature -= p.coolingCoeff * (state.temperature - p.ambientTemp) * dt;
                        state.temperature = Mathf.Clamp(state.temperature, p.ambientTemp, p.maxFireTemp);

                        // Smoke Transport (Upper diffuses, cools, and settles to Lower)
                        state.smokeUpper += p.smokeDiffusivity * smokeLaplacian * invDx2 * dt;

                        float coolingFactor = Mathf.Clamp01((p.maxFireTemp - state.temperature) / p.maxFireTemp);
                        float settlingAmount = state.smokeUpper * coolingFactor * 0.5f * dt;
                        state.smokeUpper -= settlingAmount;
                        state.smokeLower += settlingAmount;

                        state.smokeUpper = Mathf.Clamp(state.smokeUpper, 0f, 100f);
                        state.smokeLower = Mathf.Clamp(state.smokeLower, 0f, 100f);

                        write[i] = state;
                    }
                }
            }
        }
    }
}