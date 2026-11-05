using UnityEngine;

namespace FireSimulation
{
    [System.Serializable]
    public struct SimParameters
    {
        public float ambientTemp;
        public float ignitionTemp;
        public float maxFireTemp;
        public float burnRateMultiplier;
        public float fuelConsumptionRate;
        public float thermalDiffusivity;
        public float smokeDiffusivity;
        public float fireHeatOutput;
        public float coolingCoeff;

        public static SimParameters GetDefault()
        {
            return new SimParameters
            {
                ambientTemp = 20f,
                ignitionTemp = 200f,
                maxFireTemp = 1000f,
                burnRateMultiplier = 1.5f,
                fuelConsumptionRate = 0.015f,
                thermalDiffusivity = 6.0f,
                smokeDiffusivity = 4.0f,
                fireHeatOutput = 400f,
                coolingCoeff = 0.02f
            };
        }
    }

    public struct CellTopology
    {
        public float permNorth;
        public float permSouth;
        public float permEast;
        public float permWest;
        public float initialFuel;
        public bool hasSolidObstacle;
    }

    public struct CellState
    {
        public float temperature;
        public float fuel;
        public float fireIntensity;
        public float smokeUpper;
        public float smokeLower;

        public static CellState Lerp(CellState a, CellState b, float t)
        {
            return new CellState
            {
                temperature = Mathf.Lerp(a.temperature, b.temperature, t),
                fuel = Mathf.Lerp(a.fuel, b.fuel, t),
                fireIntensity = Mathf.Lerp(a.fireIntensity, b.fireIntensity, t),
                smokeUpper = Mathf.Lerp(a.smokeUpper, b.smokeUpper, t),
                smokeLower = Mathf.Lerp(a.smokeLower, b.smokeLower, t)
            };
        }
    }

    public struct HazardSample
    {
        public float temperature;
        public float fireIntensity;
        public float smokeUpper;
        public float smokeLower;
    }
}