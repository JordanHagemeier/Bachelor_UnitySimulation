using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationLifecycleSimulation : MonoBehaviour
{
    [Header("Plant Information")]
    [SerializeField]  private List<PlantInfoStruct> m_Plants;   public List<PlantInfoStruct> plants { get { return m_Plants; } set { m_Plants = value; } }
    [HideInInspector] public PlantSpeciesTable plantSpeciesTable = new PlantSpeciesTable();
    [SerializeField]  public PlantSpeciesInfoScriptableObject[] plantSpecies;
                      private int m_IDCounter = 0;
    [SerializeField]  private int m_CurrentPlantAmount; //solely for information purposes


    [SerializeField] private int m_InitialPlantAmount;                  public int initialPlantAmount { get { return m_InitialPlantAmount; } set { m_InitialPlantAmount = value; } }
    [SerializeField] private float m_DistanceBetweenPlants;              public float distanceBetweenTrees { get { return m_DistanceBetweenPlants; } }
    [SerializeField] private float m_MaxPlantSize;

    [Header("Soil Composition")]
    [SerializeField] private float m_AdditionalSoilTextureWeight;
    [SerializeField] private bool  m_UseSoilTexture;                    public bool useSoilTexture { get { return m_UseSoilTexture; } }
    [SerializeField] private bool  m_UseSoilpH;                         public bool useSoilPH { get { return m_UseSoilpH; } }

    [Header("Other Managers")]
    [SerializeField] private GroundInfoManager m_GroundInfoManager;
    [SerializeField] private SeparatedSimulationManager m_OverallSimManager;

    private const int m_GRID_CELL_DIVISIONS = 64;
    private const int m_GRID_CELL_ENTRIES_RESERVATION = 8;
    private List<PlantInfoStruct>[,] m_PlantGridCells = new List<PlantInfoStruct>[m_GRID_CELL_DIVISIONS, m_GRID_CELL_DIVISIONS];

    [SerializeField] private Vector2 m_MapResolution; public Vector2 mapResolution { get { return m_MapResolution; } set { m_MapResolution = value; } }
 
    public void InitPlantLifecycleSim(int iteration)
    {
        m_OverallSimManager = gameObject.GetComponent<SeparatedSimulationManager>();
        m_GroundInfoManager = gameObject.GetComponent<GroundInfoManager>();
        if(!m_OverallSimManager | !m_GroundInfoManager)
        {
            Debug.Log("Manager not initialized properly.");
            return;
        }

        Random.InitState(m_OverallSimManager.simulationSeed + (42 * iteration));

        if (plantSpeciesTable.IsEmpty())
        {
            for (int k = 0; k < plantSpecies.Length; k++)
            {
                plantSpeciesTable.AddToDictionary(plantSpecies[k].plantType, plantSpecies[k]);
            }
        }
        
        ResetPlantLifecycleSim();
    }

    public void ResetPlantLifecycleSim()
    {
        CreatePlantPositionsQuad();
        InitializePlantInfos();
    }


    private void CreatePlantPositionsQuad()
    {
        System.Array.Clear(m_PlantGridCells, 0, m_PlantGridCells.Length);
        m_PlantGridCells = new List<PlantInfoStruct>[m_GRID_CELL_DIVISIONS, m_GRID_CELL_DIVISIONS];
        for (int iY = 0; iY < m_GRID_CELL_DIVISIONS; iY++)
        {
            for (int iX = 0; iX < m_GRID_CELL_DIVISIONS; iX++)
            {
                m_PlantGridCells[iX, iY] = new List<PlantInfoStruct>(m_GRID_CELL_ENTRIES_RESERVATION);
            }
        }
    }

    private void InitializePlantInfos()
    {
        m_Plants = new List<PlantInfoStruct>();
        for (int i = 0; i < m_InitialPlantAmount; i++)
        {
            int maxTryCount = 200;
            int tryCount = 0;
            //get random position on terrain 
            Vector3 randomPos = new Vector3();
            while (!m_GroundInfoManager.IsOnLand(randomPos) && tryCount <= maxTryCount)
            {
           
                randomPos = CalculatePreliminaryPosition();
                tryCount++;

                if(tryCount > maxTryCount)
                {
                    Debug.Log("Plant can't find valid spawning position.");
                    return;
                }
            }
            
            randomPos.y = Singletons.simulationManager.terrain.SampleHeight(randomPos);


            //initialize new plant with that position 
            m_IDCounter++;
            PlantSpeciesInfoScriptableObject randomPlantSpecies = plantSpecies[Random.Range(0, plantSpecies.Length)];
            PlantInfoStruct newPlant    = new PlantInfoStruct(randomPos, m_IDCounter, randomPlantSpecies);
            newPlant.age                = Random.Range(0, randomPlantSpecies.deathAge);
            newPlant.health             = CalculateViability(newPlant);

            AddPlantToCell(newPlant);
            m_Plants.Add(newPlant);
        }
        m_CurrentPlantAmount = m_Plants.Count;
    }



    private Vector3 CalculatePreliminaryPosition()
    {
        if (!m_GroundInfoManager)
        {
            Debug.Log("Ground Info Manager not found.");
            return Vector3.zero;
        }
        Bounds terrainBounds = m_GroundInfoManager.terrain.terrainData.bounds;
        if (!m_GroundInfoManager.terrain)
        {
            Debug.Log("Terrain not set in Ground Manager.");
            return Vector3.zero;
        }
        Vector3 randomPos = new Vector3(Random.Range(terrainBounds.min.x, terrainBounds.max.x), 0.0f, Random.Range(terrainBounds.min.z, terrainBounds.max.z));
        return randomPos;
    }

    public bool TickPlants()
    {
       
        for (int i = 0; i < m_Plants.Count; i++)
        {
            m_Plants[i].age++;

            bool plantSurvives = TickPlantSurvival(m_Plants[i]);
            if (plantSurvives)
            {
                m_Plants[i].health = CalculateViability(m_Plants[i]);
                TickPlantReproduction(m_Plants[i]);
            }
           
        }
        m_CurrentPlantAmount = m_Plants.Count;
       

        return true;
    }

    private float CalculateViability(PlantInfoStruct plant)
    {
        //these values relate to the ranges of real input data
        const float OCCLUSION_MIN = 0.0f;
        const float OCCLUSION_MAX = 1.0f;
        const float FLOW_MIN = 0.0f;
        const float FLOW_MAX = 1.0f;
        const float ACIDITY_MIN = 0.0f;
        const float ACIDITY_MAX = 14.0f;

        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(plant.type);
        GroundInfoStruct groundInfo = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)plant.position.x, (int)plant.position.z);

        //Check if any weights aren't given aka would result in a NaN error
        if (!plantSpeciesTable.PlantWeightsAreSet(currentPlantSO.plantType))
        {
            Debug.LogError("Plant Weights aren't set for plant type: " + currentPlantSO.plantType.ToString());
        }

            // Calculate satisfaction based on following factors
        float summedSatisfaction = 0.0f;
        float summedWeights = 0.0f;

        // 1) Temperature

        float temperatureBasedOnHeight = CalculateTemperatureFactorAtAltitude(Singletons.simulationManager.groundTemperature, currentPlantSO.optimalTemperature, plant);
        summedSatisfaction += currentPlantSO.temperatureWeight * temperatureBasedOnHeight;
        summedWeights += currentPlantSO.temperatureWeight;

        if (temperatureBasedOnHeight <= 0.0f)
        {
            // Temperature is the one exception, where a low satisfaction makes the plant completly unviable
            return 0.0f;
        }

        // 2) Occlusion (Light/Shadow)

        summedSatisfaction += currentPlantSO.occlusionFactorWeight * CalculateEnvironmentSatisfaction(groundInfo.terrainOcclusion, currentPlantSO.occlusionSatisfaction, OCCLUSION_MIN, OCCLUSION_MAX);
        summedWeights += currentPlantSO.occlusionFactorWeight;

        // 3) Flow (Rivers/Sea)

        summedSatisfaction += currentPlantSO.flowFactorWeight * CalculateEnvironmentSatisfaction(groundInfo.waterflow, currentPlantSO.flowSatisfaction, FLOW_MIN, FLOW_MAX);
        summedWeights += currentPlantSO.flowFactorWeight;

        // 4) Soil Texture

        if (m_UseSoilTexture)
        {
            summedSatisfaction += currentPlantSO.soilTextureWeight * CalculateSoilTextureSatisfaction(new Vector2((int)plant.position.x, (int)plant.position.z), currentPlantSO);
            summedWeights += currentPlantSO.soilTextureWeight;
        }

        // 5) Soil pH

        if (m_UseSoilpH)
        {
            
            summedSatisfaction += currentPlantSO.soilAcidityWeight * CalculateEnvironmentSatisfaction(groundInfo.ph, currentPlantSO.soilAciditySatisfaction, ACIDITY_MIN, ACIDITY_MAX);
            summedWeights += currentPlantSO.soilAcidityWeight;


        }

        Debug.Assert(summedWeights > 0.0f, "No weights given for plant " + currentPlantSO.name);
        float overallViability = summedSatisfaction / summedWeights;


        if (float.IsNaN(overallViability))
        {
            Debug.LogError("Overall Viability is NaN");
            return -1.0f;
        }


        overallViability = AffectedViabilityThroughProximity(plant, overallViability);

        return overallViability;
    }

    private bool TickPlantSurvival(PlantInfoStruct plant)
    {
        const float e = 2.71828f;

        // Survival chance based on age uses a sigmoid function to let plants die around their deathAge in a smoothed fashion
        // f(x) = 1 - (1 / [1 + e ^(deathAge - x))
        float deathAge          = plantSpeciesTable.GetSOByType(plant.type).deathAge;
        float survivalChanceAge = 1.0f - (1.0f / (1.0f + Mathf.Pow(e, deathAge - plant.age)));

        // Survival chance based on viability uses a sigmoid function to let some but not all of the unviable plants die
        // f(x) = 1 - (1 / [1 + e ^ (s * (v + x))]    s = sigmoid scale (-12), v = viable viability
        const float VIABLE_VIABILITY        = 0.3f; //< plants below this viability will likely die, plants above will likely live
        const float VIABILITY_SIGMOID_SCALE = -12.0f;
        float survivalChanceViability       = 1.0f - (1.0f / (1.0f + Mathf.Pow(e, VIABILITY_SIGMOID_SCALE * (VIABLE_VIABILITY - plant.health))));

        float randomValue = Random.value;
        bool survives = randomValue <= survivalChanceAge * survivalChanceViability;
        if (!survives)
        {
            KillPlant(plant);
            return false;
        }

        return true;
    }

    private void KillPlant(PlantInfoStruct plant)
    {
        RemoveOwnPlantInfoFromQuadCell(plant);
        m_Plants.Remove(plant);
    }

    private bool TickPlantReproduction(PlantInfoStruct plant)
    {
        if (plant.age >= plantSpeciesTable.GetSOByType(plant.type).maturityAge)
        {
            ScatterSeeds(plant);
            return true;
        }

        return false;
    }

   

    private void ScatterSeeds(PlantInfoStruct plant)
    {
        // health: 60%, max seedlings: 3

        // 0 - 20   -> 0 seedlings
        // 20 - 40  -> 1 seedlings
        // 40 - 60  -> 2 seedling
        // 60 - 80  -> 3 seedlings
        // 80 - 100


        float randomValue = Random.value;
        if (randomValue > plant.health || randomValue <= 0.0f | plant.health <= 0.0f)
        {
            return;
        }


        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(plant.type);
        int potentialSeedlings = Mathf.RoundToInt((randomValue / plant.health) * currentPlantSO.seedDistributionAmount);

        for (int i = 0; i < potentialSeedlings; i++)
        {
            //Find valid seedling position
            Vector2 randomPos       = Random.insideUnitCircle * currentPlantSO.maxDistributionDistance;
            Vector3 calculatedPos   = plant.position + new Vector3(randomPos.x, 0.0f, randomPos.y);
            calculatedPos.y         = m_GroundInfoManager.terrain.SampleHeight(calculatedPos);
            RectInt cells           = GetCellRectForPosition(new Vector2(calculatedPos.x, calculatedPos.z));

            if (!IsInTerrainBounds(calculatedPos))
            {
                continue;
            }

            if (!m_GroundInfoManager.IsOnLand(calculatedPos))
            {
                continue;
            }

            if (IsPlantWithinDistanceToOtherPlantsQUADCELL(new Vector2(calculatedPos.x, calculatedPos.z), cells))
            {
                continue;
            }

            // Reproduce!
            m_IDCounter++;
            PlantInfoStruct newPlant = new PlantInfoStruct(calculatedPos, m_IDCounter, currentPlantSO);
            m_Plants.Add(newPlant);
            AddPlantToCell(newPlant);
        }

    }

    private RectInt GetCellRectForPosition(Vector2 pos)
    {
        //get own QuadCell
        float positionPercentageX = pos.x / m_MapResolution.x;
        float positionPercentageY = pos.y / m_MapResolution.y;

        float nonFlooredPosX = positionPercentageX * m_GRID_CELL_DIVISIONS;
        float nonFlooredPosY = positionPercentageY * m_GRID_CELL_DIVISIONS;

        //use quadcell and position within that cell to figure out if the position is close to the quad cell border
        int gridCellX = Mathf.FloorToInt(nonFlooredPosX);
        int gridCellY = Mathf.FloorToInt(nonFlooredPosY);
        float percentageWithinCellX = nonFlooredPosX - (float)gridCellX;
        float percentageWithinCellY = nonFlooredPosY - (float)gridCellY;

        float nextCellRangeMinPercentage = m_DistanceBetweenPlants / (float)m_GRID_CELL_DIVISIONS;
        float nextCellRangeMaxPercentage = 1.0f - nextCellRangeMinPercentage;


        int minX = gridCellX;
        int maxX = gridCellX;
        int minY = gridCellY;
        int maxY = gridCellY;

        //calculate if own pos is within percentage to get neighbouring cells
        if (percentageWithinCellX < nextCellRangeMinPercentage && gridCellX > 0)
        {
            minX--;
        }
        else if (percentageWithinCellX > nextCellRangeMaxPercentage && gridCellX < m_GRID_CELL_DIVISIONS - 2)
        {
            maxX++;
        }

        if (percentageWithinCellY < nextCellRangeMinPercentage && gridCellY > 0)
        {
            minY--;
        }
        else if (percentageWithinCellY > nextCellRangeMaxPercentage && gridCellY < m_GRID_CELL_DIVISIONS - 2)
        {
            maxY++;
        }

        return new RectInt(minX, minY, maxX - minX, maxY - minY);
    }


    private void AddPlantToCell(PlantInfoStruct plant)
    {
        
        Vector2Int cell = GetCellIndex(new Vector2(plant.position.x, plant.position.z));
        if(cell.x < 0 | cell.y < 0 | cell.x > m_GRID_CELL_DIVISIONS | cell.y > m_GRID_CELL_DIVISIONS)
        {
            Debug.LogError("Cell outside of grid boundaries!");
            return;
        }
        m_PlantGridCells[cell.x, cell.y].Add(plant);
    }

    private Vector2Int GetCellIndex(Vector2 pos)
    {
        float positionPercentageX = pos.x / m_MapResolution.x;
        float positionPercentageY = pos.y / m_MapResolution.y;

        int gridCellX = Mathf.FloorToInt(positionPercentageX * m_GRID_CELL_DIVISIONS);
        int gridCellY = Mathf.FloorToInt(positionPercentageY * m_GRID_CELL_DIVISIONS);

        return new Vector2Int(gridCellX, gridCellY);
    }

    private void RemoveOwnPlantInfoFromQuadCell(PlantInfoStruct plant)
    {
        Vector2Int cell = GetCellIndex(new Vector2(plant.position.x, plant.position.z));
        if (cell.x < 0 | cell.y < 0 | cell.x > m_GRID_CELL_DIVISIONS | cell.y > m_GRID_CELL_DIVISIONS)
        {
            Debug.LogError("Cell outside of grid boundaries!");
            return;
        }
        m_PlantGridCells[cell.x, cell.y].Remove(plant);
    }

    private bool IsPlantWithinDistanceToOtherPlantsQUADCELL(Vector2 pos, RectInt cells)
    {
       
        //check against plants in these cells
        for (int iY = cells.yMin; iY <= cells.yMax; iY++)
        {
           
            for (int iX = cells.xMin; iX <= cells.xMax; iX++)
            {
                foreach (PlantInfoStruct plant in m_PlantGridCells[iX, iY])
                {
                    float distanceSquared = Vector2.SqrMagnitude(new Vector2(pos.x - plant.position.x, pos.y - plant.position.z));
                    if (distanceSquared < m_DistanceBetweenPlants * m_DistanceBetweenPlants)
                    {
                        return true;
                    }
                }

            }
            
        }

        return false;
    }


    private float AffectedViabilityThroughProximity(PlantInfoStruct givenPlant, float calculatedViability)
    {

        //0. the idea is that plants compete with each other for resources like light or water, and therefore affect each other by their presence
        //1. check whether a plant is within a certain distance to other plants based on the cells affected by the plant
        int countOfPlantsInProximity = 0;
        for (int iY = givenPlant.affectedCells.yMin; iY <= givenPlant.affectedCells.yMax; iY++)
        {

            for (int iX = givenPlant.affectedCells.xMin; iX <= givenPlant.affectedCells.xMax; iX++)
            {
                foreach (PlantInfoStruct plant in m_PlantGridCells[iX, iY])
                {
                    float distance = Vector2.SqrMagnitude(new Vector2(givenPlant.position.x - plant.position.x, givenPlant.position.z - plant.position.z));
                    if (distance < m_DistanceBetweenPlants * m_DistanceBetweenPlants)
                    {

                        //2. counts how many plants are surrounding the given plant
                        countOfPlantsInProximity++;
                    }
                }

            }
        }

        //3. adjusts the given plant's viability based on the amount of other plants it is surrounded by, takes into account how resilient the given plant is
        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(givenPlant.type);
        float resultingViability = Mathf.Clamp01(calculatedViability - ((.1f * (1.0f - currentPlantSO.persistenceValue)) * countOfPlantsInProximity));
        return resultingViability;

    }




    private bool IsInTerrainBounds(Vector3 pos)
    {
        Bounds terrainBounds = m_GroundInfoManager.terrain.terrainData.bounds;
        if (pos.x < terrainBounds.max.x && pos.x > terrainBounds.min.x && pos.z > terrainBounds.min.z && pos.z < terrainBounds.max.z)
        {
            return true;
        }
        return false;
    }

    private float CalculateTemperatureFactorAtAltitude(float currentTemperature, float wantedTemperature, PlantInfoStruct plant)
    {
        float temperatureDropPerMeter = 0.0065f; //in celsius, based on data
        float elevation = plant.position.y * Singletons.simulationManager.heightScalingFactor;
        float temperatureAtHeight = currentTemperature - (temperatureDropPerMeter * elevation);
        if (temperatureAtHeight <= 0.0f)
        {
            return 0.0f;
        }

        if(wantedTemperature <= 0.0f)
        {
            Debug.LogError("Wanted Temperature for plant " + plant.type.ToString() + " not set correctly.");
            return -1.0f;
        }
        float originalResult = Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (temperatureAtHeight / wantedTemperature))));
        return originalResult;
    }


    private float CalculateEnvironmentSatisfaction(float currentValue, AnimationCurve satisfactionCurve, float minValue, float maxValue)
    {
        float currentPercentage = Mathf.InverseLerp(minValue, maxValue, currentValue);
        return satisfactionCurve.Evaluate(currentPercentage);
    }

    private float CalculateSoilTextureSatisfaction(Vector2 pos, PlantSpeciesInfoScriptableObject plantSO)
    {
        const float MAP_MIN_VALUE = 0.0f;
        const float MAP_MAX_VALUE = 100.0f;

        // Calculates summed average of the three soil texture satisfactions based on their weights

        GroundInfoStruct groundInfo = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y);

        float summedSatisfaction = 0.0f;
        float summedWeights = 0.0f;

        summedSatisfaction += plantSO.soilTextureWeight_Clay * CalculateEnvironmentSatisfaction(groundInfo.clay, plantSO.soilTextureSatisfaction_Clay, MAP_MIN_VALUE, MAP_MAX_VALUE);
        summedSatisfaction += plantSO.soilTextureWeight_Sand * CalculateEnvironmentSatisfaction(groundInfo.sand, plantSO.soilTextureSatisfaction_Sand, MAP_MIN_VALUE, MAP_MAX_VALUE);
        summedSatisfaction += plantSO.soilTextureWeight_Silt * CalculateEnvironmentSatisfaction(groundInfo.silt, plantSO.soilTextureSatisfaction_Silt, MAP_MIN_VALUE, MAP_MAX_VALUE);

        summedWeights += plantSO.soilTextureWeight_Clay;
        summedWeights += plantSO.soilTextureWeight_Sand;
        summedWeights += plantSO.soilTextureWeight_Silt;

        Debug.Assert(summedWeights > 0.0f, "No soil weights given for plant " + plantSO.name);
        return summedSatisfaction / summedWeights;
    }


}
