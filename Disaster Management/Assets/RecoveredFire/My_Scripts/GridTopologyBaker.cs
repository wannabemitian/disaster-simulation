using UnityEngine;

namespace FireSimulation
{
    public static class GridTopologyBaker
    {
        public static CellTopology[] BakeTopology(int width, int height, float cellSize, Vector3 origin, LayerMask wallMask, LayerMask fuelMask)
        {
            CellTopology[] topology = new CellTopology[width * height];
            Vector3 halfExtents = new Vector3(cellSize * 0.45f, cellSize * 0.45f, cellSize * 0.45f);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = z * width + x;
                    Vector3 cellCenter = origin + new Vector3(x * cellSize, 0f, z * cellSize);

                    CellTopology topo = new CellTopology();

                    // Check for solid obstacle taking up the cell
                    topo.hasSolidObstacle = Physics.CheckBox(cellCenter, halfExtents, Quaternion.identity, wallMask);

                    if (topo.hasSolidObstacle)
                    {
                        topo.permNorth = topo.permSouth = topo.permEast = topo.permWest = 0f;
                        topo.initialFuel = 0f;
                    }
                    else
                    {
                        // Raycast edges for permeability (walls between cells)
                        topo.permNorth = CheckEdge(cellCenter, Vector3.forward, cellSize, wallMask);
                        topo.permSouth = CheckEdge(cellCenter, Vector3.back, cellSize, wallMask);
                        topo.permEast = CheckEdge(cellCenter, Vector3.right, cellSize, wallMask);
                        topo.permWest = CheckEdge(cellCenter, Vector3.left, cellSize, wallMask);

                        // Fuel generation
                        bool hasFuelProp = Physics.CheckBox(cellCenter, halfExtents, Quaternion.identity, fuelMask);
                        if (hasFuelProp)
                        {
                            // 0.08f creates large, sweeping organic shapes instead of tight, noisy static
                            float noise = Mathf.PerlinNoise(x * 0.08f, z * 0.08f);
                            // Expands the variance: creates barren spots (0.05) and highly dense fuel spots (1.5)
                            topo.initialFuel = Mathf.Lerp(0.05f, 1.5f, noise);
                        }
                        else
                        {
                            topo.initialFuel = 0.05f; // Trace amounts on empty floor (dust/carpet)
                        }
                    }

                    topology[index] = topo;
                }
            }
            return topology;
        }

        private static float CheckEdge(Vector3 center, Vector3 dir, float cellSize, LayerMask wallMask)
        {
            Vector3 target = center + (dir * cellSize);
            if (Physics.Linecast(center, target, wallMask))
                return 0.0f; // Blocked
            return 1.0f; // Open
        }
    }
}