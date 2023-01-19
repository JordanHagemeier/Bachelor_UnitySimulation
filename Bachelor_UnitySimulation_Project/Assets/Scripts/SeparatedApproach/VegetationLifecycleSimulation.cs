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
    [SerializeField] private float m_DistanceBetweenTrees;              public float distanceBetweenTrees { get { return m_DistanceBetweenTrees; } }
    [SerializeField] private float m_MaxPlantSize;

    [Header("Soil Composition")]
    [SerializeField] private float m_AdditionalSoilTextureWeight;
    [SerializeField] private bool  m_UseSoilTexture;                    public bool useSoilTexture { get { return m_UseSoilTexture; } }
    [SerializeField] private bool  m_UseSoilpH;                         public bool useSoilPH { get { return m_UseSoilpH; } }

    [Header("Other Managers")]
    [SerializeField] private GroundInfoManager m_GroundInfoManager;
    [SerializeField] private SeparatedSimulationManager m_OverallSimManager;

    private const int m_GRID_CELL_DIVISIONS = 64;
    private List<PlantInfoStruct>[,] m_PlantGridCells = new List<PlantInfoStruct>[m_GRID_CELL_DIVISIONS, m_GRID_CELL_DIVISIONS];


 
    public void InitPlantLifecycleSim(int iteration)
    {
        m_OverallSimManager = gameObject.GetComponent<SeparatedSimulationManager>();
        m_GroundInfoManager = gameObject.GetComponent<GroundInfoManager>();
        if(!m_OverallSimManager | !m_GroundInfoManager)
        {
            Debug.Log("Manager not initialized properly.");
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
                m_PlantGridCells[iX, iY] = new List<PlantInfoStruct>(8);
            }
        }
    }

    private void InitializePlantInfos()
    {
        m_Plants = new List<PlantInfoStruct>();
        for (int i = 0; i < m_InitialPlantAmount; i++)
        {

            //get random position on terrain 
            Vector3 randomPos = new Vector3();
            while (!m_GroundInfoManager.IsOnLand(randomPos))
            {
                randomPos = CalculatePreliminaryPosition();
            }
            
            randomPos.y = Singletons.simulationManager.terrain.SampleHeight(randomPos);


            //initialize new plant with that position 
            m_IDCounter++;
            PlantSpeciesInfoScriptableObject randomlychosenPlantSpecies = plantSpecies[Random.Range(0, plantSpecies.Length)];
            PlantInfoStruct newPlant    = new PlantInfoStruct(randomPos, m_IDCounter, randomlychosenPlantSpecies);
            newPlant.age                = Random.Range(0, randomlychosenPlantSpecies.deathAge);
            newPlant.health             = CalculateViability(newPlant);

            SetOwnPlantInfoInQuadCell(newPlant);
            m_Plants.Add(newPlant);
        }
        m_CurrentPlantAmount = m_Plants.Count;
    }



    private Vector3 CalculatePreliminaryPosition()
    {
        Bounds terrainBounds = m_GroundInfoManager.terrain.terrainData.bounds;
        if (!m_GroundInfoManager.terrain)
        {
            Debug.Log("Terrain not set in Ground Manager.");
        }
        Vector3 randomPos = new Vector3(Random.Range(terrainBounds.min.x, terrainBounds.max.x), 0.0f, Random.Range(terrainBounds.min.z, terrainBounds.max.z));
        return randomPos;
    }

    public bool TickPlants()
    {
        bool success = false;
        for (int i = 0; i < m_Plants.Count; i++)
        {
            m_Plants[i].age++;
            if (CheckIfPlantIsAlive(m_Plants[i]))
            {
                m_Plants[i].health = CalculateViability(m_Plants[i]);
                CheckIfPlantCanReproduce(m_Plants[i]);
            }
        }
        m_CurrentPlantAmount = m_Plants.Count;
        success = true;

        //for each plant in plants List:
        //  - calculate viability
        //  - calculate death 
        //  - calculate age
        //  - calculate seeds and offspring
        return success;
    }

    private float CalculateViability(PlantInfoStruct plant)
    {
        const float OCCLUSION_MIN = 0.0f;
        const float OCCLUSION_MAX = 1.0f;
        const float FLOW_MIN = 0.0f;
        const float FLOW_MAX = 1.0f;
        const float ACIDITY_MIN = 0.0f;
        const float ACIDITY_MAX = 14.0f;

        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(plant.type);
        GroundInfoStruct groundInfo = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)plant.position.x, (int)plant.position.z);

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

    private bool CheckIfPlantIsAlive(PlantInfoStruct plant)
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
        if (survives)
        {
            return true;
        }

        RemoveOwnPlantInfoFromQuadCell(plant);
        m_Plants.Remove(plant);

        return false;
    }

    private bool CheckIfPlantCanReproduce(PlantInfoStruct plant)
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

        // 0 - 20   -> 3 seedlings
        // 20 - 40  -> 2 seedlings
        // 40 - 60  -> 1 seedling
        // 60 - 80  -> 0 seedlings
        // 80 - 100

        float randomValue = Random.value;
        if (randomValue > plant.health)
        {
            return;
        }

        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(plant.type);
        int seedlings = Mathf.RoundToInt((randomValue / plant.health) * currentPlantSO.seedDistributionAmount);

        for (int i = 0; i < seedlings; i++)
        {
            Vector2 randomPos       = Random.insideUnitCircle * currentPlantSO.maxDistributionDistance;
            Vector3 calculatedPos   = plant.position + new Vector3(randomPos.x, 0.0f, randomPos.y);
            calculatedPos.y         = m_GroundInfoManager.terrain.SampleHeight(calculatedPos);
            RectInt cells           = GetCellsFromGrid(new Vector2(calculatedPos.x, calculatedPos.z));

            if (!IsCalculatedNewPosWithinTerrainBounds(calculatedPos))
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
            SetOwnPlantInfoInQuadCell(newPlant);
        }

    }

    private RectInt GetCellsFromGrid(Vector2 pos)
    {
        //get own QuadCell
        float positionPercentageX = pos.x / 1024.0f;
        float positionPercentageY = pos.y / 1024.0f;

        float nonFlooredPosX = positionPercentageX * m_GRID_CELL_DIVISIONS;
        float nonFlooredPosY = positionPercentageY * m_GRID_CELL_DIVISIONS;


        int gridCellX = Mathf.FloorToInt(nonFlooredPosX);
        int gridCellY = Mathf.FloorToInt(nonFlooredPosY);
        float percentageWithinCellX = nonFlooredPosX - (float)gridCellX;
        float percentageWithinCellY = nonFlooredPosY - (float)gridCellY;

        float nextCellRangeMinPercentage = m_DistanceBetweenTrees / (float)m_GRID_CELL_DIVISIONS;
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


    //create quad
    //each plant registers itself in the quad at the appropriate cell
    //a searching plant looks in what grid cell it is
    //  - looks at what relative position in the cell it is (is it close to the border or not, look that up via DistanceBetweenPlans value)
    //  - separate x and y relative positions for this, to expand the searched cells in a quadratic form
    //  - now it only has to look at all plants in these neighbouring cells

    private void SetOwnPlantInfoInQuadCell(PlantInfoStruct plant)
    {
        Vector2Int cell = GetOwnQuadCell(new Vector2(plant.position.x, plant.position.z));

        m_PlantGridCells[cell.x, cell.y].Add(plant);
    }

    private Vector2Int GetOwnQuadCell(Vector2 pos)
    {
        float positionPercentageX = pos.x / 1024.0f;
        float positionPercentageY = pos.y / 1024.0f;

        int gridCellX = Mathf.FloorToInt(positionPercentageX * m_GRID_CELL_DIVISIONS);
        int gridCellY = Mathf.FloorToInt(positionPercentageY * m_GRID_CELL_DIVISIONS);

        return new Vector2Int(gridCellX, gridCellY);
    }

    private void RemoveOwnPlantInfoFromQuadCell(PlantInfoStruct plant)
    {
        Vector2Int cell = GetOwnQuadCell(new Vector2(plant.position.x, plant.position.z));
        m_PlantGridCells[cell.x, cell.y].Remove(plant);
    }

    private bool IsPlantWithinDistanceToOtherPlantsQUADCELL(Vector2 pos, RectInt cells)
    {
        bool withinDistance = false;


        //check against plants in these cells
        int counterX = 0;
        int counterY = 0;
        for (int iY = cells.yMin; iY <= cells.yMax; iY++)
        {
            counterX = 0;
            for (int iX = cells.xMin; iX <= cells.xMax; iX++)
            {
                foreach (PlantInfoStruct plant in m_PlantGridCells[iX, iY])
                {
                    float distance = Vector2.SqrMagnitude(new Vector2(pos.x - plant.position.x, pos.y - plant.position.z));
                    if (distance < m_DistanceBetweenTrees * m_DistanceBetweenTrees)
                    {

                        return true;
                    }
                }

                counterX++;
            }
            counterY++;
        }


        return withinDistance;



    }


    private float AffectedViabilityThroughProximity(PlantInfoStruct givenPlant, float calculatedViability)
    {

        int countOfTreesInProximity = 0;
        //check against plants in these cells
        int counterX = 0;
        int counterY = 0;
        for (int iY = givenPlant.affectedCells.yMin; iY <= givenPlant.affectedCells.yMax; iY++)
        {
            counterX = 0;
            for (int iX = givenPlant.affectedCells.xMin; iX <= givenPlant.affectedCells.xMax; iX++)
            {
                foreach (PlantInfoStruct plant in m_PlantGridCells[iX, iY])
                {
                    float distance = Vector2.SqrMagnitude(new Vector2(givenPlant.position.x - plant.position.x, givenPlant.position.z - plant.position.z));
                    if (distance < m_DistanceBetweenTrees * m_DistanceBetweenTrees)
                    {

                        countOfTreesInProximity++;
                    }
                }

                counterX++;
            }
            counterY++;
        }


        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(givenPlant.type);
        float resultingViability = Mathf.Clamp01(calculatedViability - ((.1f * (1.0f - currentPlantSO.persistenceValue)) * countOfTreesInProximity));
        return resultingViability;

    }




    private bool IsCalculatedNewPosWithinTerrainBounds(Vector3 pos)
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
        float temperatureAtHeight = currentTemperature - (0.0065f * (plant.position.y * Singletons.simulationManager.heightScalingFactor));
        if (temperatureAtHeight <= 0.0f)
        {
            return 0.0f;
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
