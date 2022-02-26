using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using System.IO;

public class SeparatedSimulationManager : MonoBehaviour
{
    [Header ("Simulation Meta Data")]
    [SerializeField] private int m_SimulationSeed;
    [SerializeField] private int m_StartingIteration;
    [SerializeField] private int m_SimulationAmount;
    [SerializeField] private int m_CurrentSimCounter = 0;
    [SerializeField] private int m_IterationsPerSimulation;
    [SerializeField] private int m_CurrentIterationCounter = 0;
    [SerializeField] private float m_TimeTillOneYearIsOver;
    [SerializeField] private float timer;
    //[SerializeField] private int m_EvaluationAtIterations;
    [SerializeField] private List<int> m_EvaluateAtIterationID;

    [Header ("Plant Information")]
    [SerializeField] private List<PlantInfoStruct> m_Plants;
    [SerializeField] private int m_AmountOfPlants;
    [SerializeField] private float m_MaxPlantSize;



    [SerializeField] private int m_InitialPlantAmount;
    [SerializeField] PlantSpeciesInfoScriptableObject[] plantSpecies;
    [SerializeField] Texture2D m_FlowMap;
    [SerializeField] Texture2D m_OcclusionMap;
    [SerializeField] private Terrain m_Terrain;

    [SerializeField] private Texture2D m_UsableGround;
    [SerializeField] private float m_SeaLevelCutOff;
    [SerializeField] private float m_DistanceBetweenTrees;

    [Header("Soil Composition")]
    [SerializeField] private float m_AdditionalSoilTextureWeight;
    [SerializeField] private bool m_UseSoilTexture;
    [SerializeField] private bool m_UseSoilpH;
    [SerializeField] private Texture2D m_ClayMap;
    [SerializeField] private Texture2D m_SandMap;
    [SerializeField] private Texture2D m_SiltMap;

    [SerializeField] private bool m_RenderAllPlants;

    private int m_IDCounter = 0;

    [SerializeField] private Texture2D m_WritingTextureTest;
    [SerializeField] private Texture2D m_WritingTextureTestCopy;

    
    [HideInInspector] public PlantInfoStruct[] m_CopiedPlants;
    private VisualizationManager m_VisManager;

    [SerializeField] private GroundInfoManager m_GroundInfoManager;
    CurrentPlantsSerializer plantSerializer = new CurrentPlantsSerializer();
    [HideInInspector] public PlantSpeciesTable plantSpeciesTable = new PlantSpeciesTable();

    [SerializeField] private float m_PlantPrimeAgePercentage; 

    Bounds m_TerrainBounds;

    private const int m_GRID_CELL_DIVISIONS = 64;
    private const int m_MAP_SIZE = 1024;
    private List<PlantInfoStruct>[,] m_PlantGridCells = new List<PlantInfoStruct>[m_GRID_CELL_DIVISIONS, m_GRID_CELL_DIVISIONS];

    private string simulationString = "SIM";

    // Start is called before the first frame update
    void Start()
    {
        
        Random.InitState(m_SimulationSeed);
        StartUpMetaInformation();
        m_CurrentSimCounter = m_StartingIteration;
        ResetSimulation(m_CurrentSimCounter);

        simulationString = System.DateTime.Now.Day + "-" + System.DateTime.Now.Month + "_" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute + (m_UseSoilTexture ? "_SoilTexture" : "_NoSoilTexture") + (m_UseSoilpH ? "_SoilPH" : "_NoSoilPH");

    }

    private bool ResetSimulation(int iteration)
    {
        bool success = true;
        if(m_CurrentSimCounter > m_SimulationAmount)
        {
            Debug.Break();
        }
        //Give new seed 
        //Reset Data from previous run
        //  - new plants, spawned at new locations due to new seed
        //  - restore old ground array 
        //Start new Sim
        Random.InitState(m_SimulationSeed + (42 * iteration));
        InitializePlantInfos();
        m_GroundInfoManager.ResetGroundInfoArray();
        System.Array.Clear(m_PlantGridCells, 0, m_PlantGridCells.Length);
        CreatePlantPositionsQuad();
        return success;
    }

    private void StartUpMetaInformation()
    {
        m_TerrainBounds = m_Terrain.terrainData.bounds;
        CreatePlantPositionsQuad();
        if (m_FlowMap == null)
        {
            m_FlowMap = Singletons.simulationManager.flowMap;
        }
        if (m_OcclusionMap == null)
        {
            m_OcclusionMap = Singletons.simulationManager.occlusionMap;
        }
        m_Terrain = Singletons.simulationManager.terrain;

        m_VisManager = gameObject.GetComponent<VisualizationManager>();


        //create a dictionary of plant type enums to plant type scriptable objects, to enable deserialization of plant info structs
        for (int k = 0; k < plantSpecies.Length; k++)
        {
            plantSpeciesTable.AddToDictionary(plantSpecies[k].plantType, plantSpecies[k]);
        }
    }

  

    private void CreatePlantPositionsQuad()
    {
        m_PlantGridCells = new List<PlantInfoStruct>[m_GRID_CELL_DIVISIONS, m_GRID_CELL_DIVISIONS];
        for(int iY = 0; iY < m_GRID_CELL_DIVISIONS; iY++)
        {
            for(int iX = 0; iX < m_GRID_CELL_DIVISIONS; iX++)
            {
                m_PlantGridCells[iX, iY] = new List<PlantInfoStruct>(8);
            }
        }
    }

    private void CopyPlantInfosToVisPlantArray(List<PlantInfoStruct> plantsToBeCopied)
    {
        m_CopiedPlants = new PlantInfoStruct[plantsToBeCopied.Count];
        for (int i = 0; i < plantsToBeCopied.Count; i++)
        {
            m_CopiedPlants[i] = plantsToBeCopied[i];
        }
        m_VisManager.copiedPlants = m_CopiedPlants;
    }

    // Update is called once per frame
    void Update()
    {
       
        timer += Time.deltaTime;
        if (timer > m_TimeTillOneYearIsOver)
        {
            timer = 0.0f;

            if (m_EvaluateAtIterationID.Contains(m_CurrentIterationCounter))
            {
                //if (m_RenderAllPlants)
                //{
                //    RenderAllTrees();
                //}
                EvaluateSimulation();
            }
            if (m_CurrentIterationCounter > m_IterationsPerSimulation)
            {
                EvaluateSimulation();
                m_CurrentIterationCounter = 0;
                m_CurrentSimCounter++;
                ResetSimulation(m_CurrentSimCounter);
                return;
            }

            bool plantsAreUpdated = TickPlants();
            if (plantsAreUpdated == true)
            {

                CopyPlantInfosToVisPlantArray(m_Plants);

                plantSerializer.currentPlantsInSim = new List<PlantInfoStruct>();
                for (int k = 0; k < m_CopiedPlants.Length; k++)
                {
                    plantSerializer.currentPlantsInSim.Add(m_CopiedPlants[k]);
                }

            }

            CheckForInput();
            m_CurrentIterationCounter++;
        }
    }

    private void CheckForInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            plantSerializer.Save(Path.Combine(Application.dataPath, "monsters.xml"));
            Debug.Log("Did it!");
        }
        if (Input.GetMouseButtonDown(1))
        {
            var loadedPlants = CurrentPlantsSerializer.Load(Path.Combine(Application.dataPath, "monsters.xml"));
            m_Plants = loadedPlants.currentPlantsInSim;
            CopyPlantInfosToVisPlantArray(m_Plants);

            //plantsAreUpdated = true;
            Debug.Log("Loaded!");
            Debug.Log(loadedPlants.currentPlantsInSim[0].id);
            Debug.Log(loadedPlants.currentPlantsInSim[0].health);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            ReadRandomPlantValueOnMap(m_WritingTextureTestCopy);
        }
    }
   
    private void InitializePlantInfos()
    {
        m_Plants = new List<PlantInfoStruct>();
        for(int i = 0; i < m_InitialPlantAmount; i++)
        {
            
            //Bounds terrainBounds = Singletons.simulationManager.terrain.terrainData.bounds;
            //get random position on terrain 
            Vector3 randomPos = new Vector3();
            while (!IsOnLand(randomPos))
            {
                randomPos = CalculatePreliminaryPosition();
            }
            //Vector3 randomPos   = new Vector3(Random.Range(terrainBounds.min.x, terrainBounds.max.x), 0.0f, Random.Range(terrainBounds.min.z, terrainBounds.max.z));
            randomPos.y         = Singletons.simulationManager.terrain.SampleHeight(randomPos);

            //GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //debugCube.transform.position = randomPos;


            //initialize new plant with that position 
            m_IDCounter++;
            PlantSpeciesInfoScriptableObject randomlychosenPlantSpecies = plantSpecies[Random.Range(0, plantSpecies.Length)];
            PlantInfoStruct newPlant = new PlantInfoStruct(randomPos, m_IDCounter, randomlychosenPlantSpecies);
            newPlant.age = Random.Range(0, randomlychosenPlantSpecies.deathAge);
            newPlant.health = CalculateViability(newPlant);

            SetOwnPlantInfoInQuadCell(newPlant);
            m_Plants.Add(newPlant);
        }

        m_AmountOfPlants = m_Plants.Count;
    }
    
    private Vector3 CalculatePreliminaryPosition()
    {
        //Bounds terrainBounds = Singletons.simulationManager.terrain.terrainData.bounds;
        Vector3 randomPos = new Vector3(Random.Range(m_TerrainBounds.min.x, m_TerrainBounds.max.x),0.0f, Random.Range(m_TerrainBounds.min.z, m_TerrainBounds.max.z));
        return randomPos;
    }

    private bool TickPlants()
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
        m_AmountOfPlants = m_Plants.Count;
        success = true;
        
        //for each plant in plants List:
        //  - calculate viability
        //  - calculate death 
        //  - calculate age
        //  - calculate seeds and offspring
        return success;
    }

    private bool CheckIfPlantIsAlive(PlantInfoStruct plant)
    {
        const float e = 2.71828f;

        // Survival chance based on age uses a sigmoid function to let plants die around their deathAge in a smoothed fashion
        // f(x) = 1 - (1 / [1 + e ^(deathAge - x))
        float deathAge = plantSpeciesTable.GetSOByType(plant.type).deathAge;
        float survivalChanceAge = 1.0f - (1.0f / (1.0f + Mathf.Pow(e, deathAge - plant.age)));

        // Survival chance based on viability uses a sigmoid function to let some but not all of the unviable plants die
        // f(x) = 1 - (1 / [1 + e ^ (s * (v + x))]    s = sigmoid scale (-12), v = viable viability
        const float VIABLE_VIABILITY = 0.3f; //< plants below this viability will likely die, plants above will likely live
        const float VIABILITY_SIGMOID_SCALE = -12.0f;
        float survivalChanceViability = 1.0f - (1.0f / (1.0f + Mathf.Pow(e, VIABILITY_SIGMOID_SCALE * (VIABLE_VIABILITY - plant.health))));

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
        if(plant.age >= plantSpeciesTable.GetSOByType(plant.type).maturityAge)
        {
           // plantSpeciesTable.GetSOByType(plant.type).ageBasedGrowthFactor = Mathf.
            ScatterSeeds(plant);
            return true;
        }

        return false;
    }

    private float CalculateViability(PlantInfoStruct plant)
    {
        const float OCCLUSION_MIN = 0.0f;
        const float OCCLUSION_MAX = 1.0f;
        const float FLOW_MIN = 0.0f;
        const float FLOW_MAX = 1.0f;
        const float ACIDITY_MIN = 0.0f;
        const float ACIDITY_MAX = 14.0f;

        PlantSpeciesInfoScriptableObject currentPlantSO     = plantSpeciesTable.GetSOByType(plant.type);
        Vector2 terrainBounds = new Vector2(m_TerrainBounds.max.x, m_TerrainBounds.max.z);
        GroundInfoStruct groundInfo = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)plant.position.x, (int)plant.position.z, terrainBounds);

        // Calculate satisfaction based on following factors
        float summedSatisfaction = 0.0f;
        float summedWeights = 0.0f;

        // 1) Temperature
        // TODO: For temperature, also use the curve instead of the single prefered value
        float temperatureBasedOnHeight = CalculateTemperatureFactorAtAltitude(Singletons.simulationManager.groundTemperature, currentPlantSO.optimalTemperature, plant);
        summedSatisfaction += currentPlantSO.temperatureWeight * temperatureBasedOnHeight;
        summedWeights += currentPlantSO.temperatureWeight;

        if (temperatureBasedOnHeight <= 0.0f)
        {
            // Temperature is the one exception, where a low satisfaction makes the plant completly unviable
            return 0.0f;
        }

        // 2) Occlusion (Light/Shadow)
        //float occlusionFactor       = CalculateSatisfactionWithGround(groundInfo.terrainOcclusion, currentPlantSO.optimalOcclusion);
        //factorCounter++;
        summedSatisfaction += currentPlantSO.occlusionFactorWeight * CalculateEnvironmentSatisfaction(groundInfo.terrainOcclusion, currentPlantSO.occlusionSatisfaction, OCCLUSION_MIN, OCCLUSION_MAX);
        summedWeights += currentPlantSO.occlusionFactorWeight;

        // 3) Flow (Rivers/Sea)
        //float flowFactor            = CalculateSatisfactionWithGround(groundInfo.waterflow, currentPlantSO.optimalflow);
        //factorCounter++;
        summedSatisfaction += currentPlantSO.flowFactorWeight * CalculateEnvironmentSatisfaction(groundInfo.waterflow, currentPlantSO.flowSatisfaction, FLOW_MIN, FLOW_MAX);
        summedWeights += currentPlantSO.flowFactorWeight;

        // 4) Soil Texture
        //    float soilTextureValue = 0.0f;
        //if (m_UseSoilTexture)
        //{
        //    soilTextureValue = CalculateSoilTextureValue(new Vector2((int)plant.position.x, (int)plant.position.z), currentPlantSO);
        //    factorCounter++;
        //}
        if (m_UseSoilTexture)
        {
            summedSatisfaction += currentPlantSO.soilTextureWeight * CalculateSoilTextureSatisfaction(new Vector2((int)plant.position.x, (int)plant.position.z), currentPlantSO);
            summedWeights += currentPlantSO.soilTextureWeight;
        }

        // 5) Soil pH
        //float soilCompositionValue = 0.0f;
        if (m_UseSoilpH)
        {
            summedSatisfaction += currentPlantSO.soilAcidityWeight * CalculateEnvironmentSatisfaction(groundInfo.ph, currentPlantSO.soilAciditySatisfaction, ACIDITY_MIN, ACIDITY_MAX);
            summedWeights += currentPlantSO.soilAcidityWeight;

            //soilCompositionValue = CalculateSoilCompositionValue(new Vector2((int)plant.position.x, (int)plant.position.z), currentPlantSO);
            //factorCounter++;
        }

        Debug.Assert(summedWeights > 0.0f, "No weights given for plant " + currentPlantSO.name);
        float overallViability = summedSatisfaction / summedWeights;
        //overallViability = ((occlusionFactor * currentPlantSO.occlusionFactorWeight) 
        //    + (flowFactor * currentPlantSO.flowFactorWeight) 
        //    + (temperatureBasedOnHeight * currentPlantSO.temperatureWeight 
        //    + (soilTextureValue * (currentPlantSO.soilTextureWeight))
        //    + soilCompositionValue * currentPlantSO.soilAcidityWeight)) /(float)factorCounter;

        if (float.IsNaN(overallViability))
        {
            Debug.LogError("Overall Viability is NaN");
            return -1.0f;
        }

        //TODO check for other plants and reduce viability value if other plants are in the area
        overallViability = AffectedViabilityThroughProximity(plant, overallViability);

        return overallViability;
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
            Vector2 randomPos = Random.insideUnitCircle * currentPlantSO.maxDistributionDistance;

            Vector3 calculatedPos = plant.position + new Vector3(randomPos.x, 0.0f, randomPos.y);
            calculatedPos.y = m_Terrain.SampleHeight(calculatedPos);
            RectInt cells = GetCellsFromGrid(new Vector2(calculatedPos.x, calculatedPos.z));

            if (!IsCalculatedNewPosWithinTerrainBounds(calculatedPos))
            {
                continue;
            }

            if (!IsOnLand(calculatedPos))
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


        ////get own QuadCell
        //float positionPercentageX = pos.x / 1024.0f;
        //float positionPercentageY = pos.y / 1024.0f;

        //float nonFlooredPosX = positionPercentageX * m_GRID_CELL_DIVISIONS;
        //float nonFlooredPosY = positionPercentageY * m_GRID_CELL_DIVISIONS;


        //int gridCellX = Mathf.FloorToInt(nonFlooredPosX);
        //int gridCellY = Mathf.FloorToInt(nonFlooredPosY);
        //float percentageWithinCellX = nonFlooredPosX - (float)gridCellX;
        //float percentageWithinCellY = nonFlooredPosY - (float)gridCellY;

        //float nextCellRangeMinPercentage = m_DistanceBetweenTrees / (float)m_GRID_CELL_DIVISIONS;
        //float nextCellRangeMaxPercentage = 1.0f - nextCellRangeMinPercentage;


        //int minX = gridCellX;
        //int maxX = gridCellX;
        //int minY = gridCellY;
        //int maxY = gridCellY;

        ////calculate if own pos is within percentage to get neighbouring cells
        //if (percentageWithinCellX < nextCellRangeMinPercentage && gridCellX > 0)
        //{
        //    minX--;
        //}
        //else if (percentageWithinCellX > nextCellRangeMaxPercentage && gridCellX < m_GRID_CELL_DIVISIONS - 2)
        //{
        //    maxX++;
        //}

        //if (percentageWithinCellY < nextCellRangeMinPercentage && gridCellY > 0)
        //{
        //    minY--;
        //}
        //else if (percentageWithinCellY > nextCellRangeMaxPercentage && gridCellY < m_GRID_CELL_DIVISIONS - 2)
        //{
        //    maxY++;
        //}

        //List<PlantInfoStruct>[,] plantCellsToCheck = new List<PlantInfoStruct>[(maxX - minX)+1, (maxY - minY)+1];

        //check against plants in these cells
        int counterX = 0;
        int counterY = 0;
        for (int iY = cells.yMin; iY <= cells.yMax; iY++)
        {
            counterX = 0;
            for (int iX = cells.xMin; iX <= cells.xMax; iX++)
            {
                foreach(PlantInfoStruct plant in m_PlantGridCells[iX, iY])
                {
                    float distance = Vector2.SqrMagnitude(new Vector2(pos.x - plant.position.x, pos.y - plant.position.z));
                    if (distance < m_DistanceBetweenTrees * m_DistanceBetweenTrees)
                    {

                        return true;
                    }
                }
                //plantCellsToCheck[counterX, counterY] = m_PlantGridCells[iX, iY];
                counterX++;
            }
            counterY++;
        }
        
        
        return withinDistance;



    }
    

    private float AffectedViabilityThroughProximity(PlantInfoStruct givenPlant, float calculatedViability)
    {
        // TODO;

        ////get own QuadCell
        //float positionPercentageX = givenPlant.position.x / 1024.0f;
        //float positionPercentageY = givenPlant.position.z / 1024.0f;

        //float nonFlooredPosX = positionPercentageX * m_GRID_CELL_DIVISIONS;
        //float nonFlooredPosY = positionPercentageY * m_GRID_CELL_DIVISIONS;


        //int gridCellX = Mathf.FloorToInt(nonFlooredPosX);
        //int gridCellY = Mathf.FloorToInt(nonFlooredPosY);
        //float percentageWithinCellX = nonFlooredPosX - (float)gridCellX;
        //float percentageWithinCellY = nonFlooredPosY - (float)gridCellY;

        //float nextCellRangeMinPercentage = m_DistanceBetweenTrees / (float)m_GRID_CELL_DIVISIONS;
        //float nextCellRangeMaxPercentage = 1.0f - nextCellRangeMinPercentage;


        //int minX = gridCellX;
        //int maxX = gridCellX;
        //int minY = gridCellY;
        //int maxY = gridCellY;

        ////calculate if own pos is within percentage to get neighbouring cells
        //if (percentageWithinCellX < nextCellRangeMinPercentage && gridCellX > 0)
        //{
        //    minX--;
        //}
        //else if (percentageWithinCellX > nextCellRangeMaxPercentage && gridCellX < m_GRID_CELL_DIVISIONS - 2)
        //{
        //    maxX++;
        //}

        //if (percentageWithinCellY < nextCellRangeMinPercentage && gridCellY > 0)
        //{
        //    minY--;
        //}
        //else if (percentageWithinCellY > nextCellRangeMaxPercentage && gridCellY < m_GRID_CELL_DIVISIONS - 2)
        //{
        //    maxY++;
        //}


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
        float resultingViability = Mathf.Clamp01(calculatedViability - ((.1f *(1.0f - currentPlantSO.persistenceValue)) * countOfTreesInProximity)); 
        return resultingViability;

    }


    private bool IsOnLand(Vector3 pos)
    {
       
        Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z);
        return m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)pos.x, (int)pos.z, terrainBounds).onLand;
        
    }

    private bool IsCalculatedNewPosWithinTerrainBounds(Vector3 pos)
    {
        if (pos.x < m_TerrainBounds.max.x && pos.x > m_TerrainBounds.min.x && pos.z > m_TerrainBounds.min.z && pos.z < m_TerrainBounds.max.z)
        {
            return true;
        }
        return false;
    }

    private float CalculateTemperatureFactorAtAltitude(float currentTemperature, float wantedTemperature, PlantInfoStruct plant)
    {
        float temperatureAtHeight = currentTemperature - (0.0065f * (plant.position.y * Singletons.simulationManager.heightScalingFactor));
        //m_TemperatureAtThisAltitude = temperatureAtHeight;
        if (temperatureAtHeight <= 0.0f)
        {
            return 0.0f;
        }
        
        float originalResult = Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (temperatureAtHeight / wantedTemperature))));
        return originalResult;
    }

   
    private float CalculateSatisfactionWithGround(float groundValue, float wantedValue)
    {
        // 1         .''.
        // |       .'    '.
        // |     .'        '.
        // |   .'            '.
        // 0   0      1w      2w    (w = wantedValue)
        //
        // for 0 <= groundValue <= wantedValue                  | returns [0, 1] with linear interpolation
        // for wantedValue <= groundValue <= 2 * wanted value   | returns [1, 0] with linear interpolation

        float groundValueClamped = Mathf.Clamp(groundValue, 0.0f, wantedValue * 2.0f);
        float result = (Mathf.Abs(wantedValue - Mathf.Abs(wantedValue - groundValueClamped))) / wantedValue;
        return result;
    }

    private float CalculateAcidtyFactor(float groundPh, float desiredPh)
    {
        float result = (Mathf.Abs(desiredPh - Mathf.Abs(desiredPh - groundPh))) / desiredPh;
        return result;
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

        Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z);
        GroundInfoStruct groundInfo = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y, terrainBounds);

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


    public bool TickGround()
    {
        bool success = false;

        return success;
    }

    private float[] CalculateSoilCompositionValuesWithStaticMaps(Vector2 pos, PlantSpeciesInfoScriptableObject plantSO)
    {
        float[] values = new float[3];
        float clayValue = GetValueFromSoilCompositionMap(m_ClayMap, pos);
        float preferredClayValue = plantSO.preferredClayValue;
        float clayFactor = CalculateSatisfactionWithGround(clayValue, preferredClayValue);

        float sandValue = GetValueFromSoilCompositionMap(m_SandMap, pos);
        float preferredSandValue = plantSO.preferredSandValue;
        float sandFactor = CalculateSatisfactionWithGround(sandValue, preferredSandValue);

        float siltValue = GetValueFromSoilCompositionMap(m_SiltMap, pos);
        float preferredSiltValue = plantSO.preferredSiltValue;
        float siltFactor = CalculateSatisfactionWithGround(siltValue, preferredSiltValue);

        return values;
    }

    //private float CalculateSoilTextureValue(Vector2 pos, PlantSpeciesInfoScriptableObject plantSO)
    //{
    //    // Soil texture viability is average of clay, sand and silt satisfaction

    //    Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z);
    //    GroundInfoStruct groundInfo = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y, terrainBounds);

    //    float clayFactorFromGround = CalculateSatisfactionWithGround(groundInfo.clay, plantSO.preferredClayValue);
    //    float sandFactorFromGround = CalculateSatisfactionWithGround(groundInfo.sand, plantSO.preferredSandValue);
    //    float siltFactorFromGround = CalculateSatisfactionWithGround(groundInfo.silt, plantSO.preferredSiltValue);

    //    float soilViabilityValue = (clayFactorFromGround + sandFactorFromGround + siltFactorFromGround) / 3.0f;
    //    return soilViabilityValue;
    //}


    //private float CalculateSoilCompositionValue(Vector2 pos, PlantSpeciesInfoScriptableObject plantSO)
    //{
    //    // Use acidity map information stored in the ground infos and acidity values from the plant type to get this bread

    //    GroundInfoStruct groundInfo = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y, new Vector2(m_TerrainBounds.max.x, m_TerrainBounds.max.z));

    //    return CalculateAcidtyFactor(groundInfo.ph, plantSO.preferredAcidityValue);
    //}

    private float GetValueFromSoilCompositionMap(Texture2D texture,Vector2 pos)
    {
        int posXOnTex = (int)((pos.x / m_Terrain.terrainData.bounds.max.x) * texture.width);
        int posYOnTex = (int)((pos.y / m_Terrain.terrainData.bounds.max.z) * texture.height);
        Color singlePixel = texture.GetPixel(posXOnTex, posYOnTex);
        //var pixels = texture.GetPixels(posXOnTex, posYOnTex, 1,1);
        ////float sumValue = 0.0f;
        //float debugValue = 0.0f;
        //foreach(var pixel in pixels)
        //{
        //    sumValue += pixel.r;
        //    debugValue += pixel.g;
        //}
        //if((debugValue / pixels.Length)*256.0f != (sumValue / pixels.Length)*256.0f)
        //{
        //    Debug.Log("Original Value: " + (sumValue / pixels.Length) * 256.0f + ", Debug Value: " + (debugValue / pixels.Length) * 256.0f);
        //}

        //return (sumValue / pixels.Length) * 256.0f;
        return singlePixel.r * 256.0f;
    }
    
   
    private void ReadRandomPlantValueOnMap(Texture2D texture)
    {
        PlantInfoStruct randomPlant = m_Plants[Random.Range(0, m_Plants.Count)];
        
        float value = GetValueFromSoilCompositionMap(texture, new Vector2(randomPlant.position.x, randomPlant.position.z));
        Debug.Log("Read Value at: " + randomPlant.position.x + ", " + randomPlant.position.z + " and plantType " + randomPlant.type + " is " + value +".");
    }

    private void EvaluateSimulation()
    {
        const float MINIMUM_INFLUENCE_TO_COUNT_AS_DOMINANT = 0.01f;

        // Find dominant species for every position on the map

        // I) For each plant, mark the fields in its influence
        float[,,] plantDominance = new float[m_MAP_SIZE, m_MAP_SIZE, (int)PlantType.Count];
        float[,,] plantViability = new float[m_MAP_SIZE, m_MAP_SIZE, (int)PlantType.Count];
        float[,,] plantViabilityWeights = new float[m_MAP_SIZE, m_MAP_SIZE, (int)PlantType.Count];
        float[] summedUpViability = new float[(int)PlantType.Count];

        foreach (PlantInfoStruct plant in m_Plants)
        {
            Vector2 plantPosition2D = new Vector2(plant.position.x, plant.position.z);
            float influenceRadius = CalculatePlantInfluenceRadius(plant);
            if (influenceRadius <= 0.0f)
            {
                continue;
            }

            // Dominance
            {
                float dominanceValue = CalculatePlantDominanceValue(plant);
                RectInt influencedIndices = GetInfluencedRect(plantPosition2D, influenceRadius);

                for (int iY = influencedIndices.min.y; iY < influencedIndices.max.y; iY++)
                {
                    for (int iX = influencedIndices.min.x; iX < influencedIndices.max.x; iX++)
                    {
                        if (iX < 0 || iX >= m_MAP_SIZE || iY < 0 || iY >= m_MAP_SIZE)
                        {
                            continue;
                        }

                        if (!IsOnLand(new Vector3(iX, 0, iY)))
                        {
                            continue;
                        }

                        float distance = Vector2.Distance(plantPosition2D, new Vector2(iX, iY));
                        if (distance > influenceRadius)
                        {
                            continue;
                        }

                        if (plant.type >= PlantType.Count)
                        {
                            Debug.LogError("Plant " + plant.id + " has invalid plant type: " + plant.type);
                            continue;
                        }

                        float distancePercentage = 1.0f - (distance / influenceRadius);
                        plantDominance[iX, iY, (int)plant.type] += distancePercentage * dominanceValue;
                    }
                }
            }

            {
                // Viability
                float viabilityValue = plant.health;
                summedUpViability[(int) plant.type] += plant.health;

                const float VIABILITY_VISUALIZATION_RADIUS = 5.0f;
                RectInt viabilityIndices = GetInfluencedRect(plantPosition2D, VIABILITY_VISUALIZATION_RADIUS);

                for (int iY = viabilityIndices.min.y; iY < viabilityIndices.max.y; iY++)
                {
                    for (int iX = viabilityIndices.min.x; iX < viabilityIndices.max.x; iX++)
                    {
                        if (iX < 0 || iX >= m_MAP_SIZE || iY < 0 || iY >= m_MAP_SIZE)
                        {
                            continue;
                        }

                        if (!IsOnLand(new Vector3(iX, 0, iY)))
                        {
                            continue;
                        }

                        float distance = Vector2.Distance(plantPosition2D, new Vector2(iX, iY));
                        if (distance > VIABILITY_VISUALIZATION_RADIUS)
                        {
                            continue;
                        }

                        if (plant.type >= PlantType.Count)
                        {
                            Debug.LogError("Plant " + plant.id + " has invalid plant type: " + plant.type);
                            continue;
                        }

                        float distancePercentage = 1.0f - (distance / VIABILITY_VISUALIZATION_RADIUS);
                        plantViability[iX, iY, (int)plant.type] += distancePercentage * viabilityValue;
                        plantViabilityWeights[iX, iY, (int)plant.type] += distancePercentage;
                    }
                }
            }
        }

        // 2) Build a map of which plant is the most influential at every point
        //PlantType?[,] dominantPlantType = new PlantType?[m_MAP_SIZE, m_MAP_SIZE];
        Color[] dominantPlantColors = new Color[m_MAP_SIZE * m_MAP_SIZE];
        Color[][] viabilityPlantColors = new Color[(int)PlantType.Count][];

        for (int iP = 0; iP < (int)PlantType.Count; iP++)
        {
            viabilityPlantColors[iP] = new Color[m_MAP_SIZE * m_MAP_SIZE];
        }

        for (int iY = 0; iY < m_MAP_SIZE; iY++)
        {
            for (int iX = 0; iX < m_MAP_SIZE; iX++)
            {
                // Dominance
                float highestDominanceValue = 0.0f;
                for (int iP = 0; iP < (int)PlantType.Count; iP++)
                {
                    if (plantDominance[iX, iY, iP] > highestDominanceValue)
                    {
                        highestDominanceValue = plantDominance[iX, iY, iP];
                        if (highestDominanceValue >= MINIMUM_INFLUENCE_TO_COUNT_AS_DOMINANT)
                        {
                            //dominantPlantType[iX, iY] = (PlantType)iP;
                            dominantPlantColors[iY * m_MAP_SIZE + iX] = plantSpeciesTable.GetSOByType((PlantType)iP).typeColor;
                        }
                    }
                }

                if (highestDominanceValue <= MINIMUM_INFLUENCE_TO_COUNT_AS_DOMINANT)
                {
                    dominantPlantColors[iY * m_MAP_SIZE + iX] = Color.gray;
                }

                // Viability
                for (int iP = 0; iP < (int)PlantType.Count; iP++)
                {
                    float weights = plantViabilityWeights[iX, iY, iP];
                    if (weights > 0)
                    {
                        float viability = plantViability[iX, iY, iP];
                        float viabilityValue01 = viability / weights;
                        viabilityPlantColors[iP][iY * m_MAP_SIZE + iX] = Color.Lerp(Color.black, Color.white, viabilityValue01);
                    }
                    else
                    {
                        viabilityPlantColors[iP][iY * m_MAP_SIZE + iX] = Color.blue;
                    }
                }
            }
        }

        // 3) For the dominance map, render out a histogram of which species how many pixels
        const int HISTOGRAM_SIZE = 84;
        const int HISTOGRAM_POSITION_X = m_MAP_SIZE - HISTOGRAM_SIZE - 1;
        const int HISTOGRAM_POSITION_Y = 0;
        const float HISTOGRAM_BAR_WIDTH = HISTOGRAM_SIZE / (float)PlantType.Count;

        // I) Find highest summed viability of all plants
        float highestPlantViability = 0.0f;
        for (int iP = 0; iP < (int)PlantType.Count; iP++)
        {
            if (summedUpViability[iP] > highestPlantViability)
            {
                highestPlantViability = summedUpViability[iP];
            }
        }

        for (int iP = 0; iP < (int)PlantType.Count; iP++)
        {
            float barFillPercentage = summedUpViability[iP] / highestPlantViability;

            for (int iY = HISTOGRAM_POSITION_Y; iY < HISTOGRAM_POSITION_Y + HISTOGRAM_SIZE; iY++)
            {
                float pixelPercentage = (iY - HISTOGRAM_POSITION_Y) / (float)(HISTOGRAM_SIZE);
                bool rowFilled = barFillPercentage > pixelPercentage;
                int xStart  = HISTOGRAM_POSITION_X + (int)(iP * HISTOGRAM_BAR_WIDTH);
                int xEnd    = HISTOGRAM_POSITION_X + (int)((iP + 1) * HISTOGRAM_BAR_WIDTH);

                if (rowFilled)
                {
                    Color rowColor = plantSpeciesTable.GetSOByType((PlantType)iP).typeColor;
                    for (int iX = xStart; iX < xEnd; iX++)
                    {
                        dominantPlantColors[iY * m_MAP_SIZE + iX] = rowColor;
                    }
                }
            }
        }

        // 4) Save maps
        string circumstancesPath = "initPA_" + m_InitialPlantAmount + "_dist_" + m_DistanceBetweenTrees + "/";
        string dirPath = Application.dataPath + "/../Generated/" + circumstancesPath + "/" + simulationString + "/";

        // I) Dominance map
        string filePathDominance = dirPath + "dominanceMap_" + "_simCount" + m_CurrentSimCounter + "_iteration" + m_CurrentIterationCounter + ".png";
        Texture2D dominanceMap = new Texture2D(m_MAP_SIZE, m_MAP_SIZE);
        dominanceMap.SetPixels(0, 0, m_MAP_SIZE, m_MAP_SIZE, dominantPlantColors);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(filePathDominance, dominanceMap.EncodeToPNG());

        // II) Occurance maps
        for (int iP = 0; iP < (int)PlantType.Count; iP++)
        {
            string filePathOccurance = dirPath + "occuranceMap_" + iP + plantSpeciesTable.GetSOByType((PlantType)iP).name + "_" + "_simCount" + m_CurrentSimCounter + "_iteration" + m_CurrentIterationCounter + ".png";
            Texture2D occuranceMap = new Texture2D(m_MAP_SIZE, m_MAP_SIZE);
            occuranceMap.SetPixels(0, 0, m_MAP_SIZE, m_MAP_SIZE, viabilityPlantColors[iP]);
            File.WriteAllBytes(filePathOccurance, occuranceMap.EncodeToPNG());
        }
    }


    private void RenderAllTrees()
    {

        //1) write each plant with their color into an array 
        Color[] allPlantColors = new Color[m_MAP_SIZE * m_MAP_SIZE];
        for(int iY = 0; iY < m_MAP_SIZE; iY++)
        {
            for(int iX = 0; iX < m_MAP_SIZE; iX++)
            {
                if(IsOnLand(new Vector3(iX, 0.0f, iY)))
                {
                    allPlantColors[iY * m_MAP_SIZE + iX] = Color.white;
                }
                else
                {
                    allPlantColors[iY * m_MAP_SIZE + iX] = Color.grey;
                }
            }
        }
        foreach(PlantInfoStruct plant in m_Plants)
        {
            Vector2 plantPosition2D = new Vector2(plant.position.x, plant.position.z);
            allPlantColors[(int)plantPosition2D.y * m_MAP_SIZE + (int)plantPosition2D.x] = plantSpeciesTable.GetSOByType(plant.type).typeColor;
        }
        //2) fill map with array content
        Texture2D allPlantPositionsMap = new Texture2D(m_MAP_SIZE, m_MAP_SIZE);
        allPlantPositionsMap.SetPixels(0, 0, m_MAP_SIZE, m_MAP_SIZE, allPlantColors);

        byte[] allPlantsMapBytes    = allPlantPositionsMap.EncodeToPNG();
        string circumstancesPath    = "initPA_" + m_InitialPlantAmount + "_dist_" + m_DistanceBetweenTrees+ "/";
        string dirPath              = Application.dataPath + "/../Generated/" + circumstancesPath;
        string timeString           = System.DateTime.Now.Day + "-" + System.DateTime.Now.Month + "_" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute;
        string filePath             = dirPath + "allPlantsMap_" + m_AmountOfPlants+ "_time" + timeString + "_simCount" + m_CurrentSimCounter + "_iteration" + m_CurrentIterationCounter + ".png";

        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(filePath, allPlantsMapBytes);
    }

    private float CalculatePlantInfluenceRadius(PlantInfoStruct plant)
    {
        const float BASE_INFLUENCE = 2.5f;
        const float RADIUS_INCREASE_PER_YEAR = 0.05f;

        return (BASE_INFLUENCE + RADIUS_INCREASE_PER_YEAR * plant.age) * plant.health;
    }

    private float CalculatePlantDominanceValue(PlantInfoStruct plant)
    {
        float ageInfluence = 0.0f;
        int deathAge = plantSpeciesTable.GetSOByType(plant.type).deathAge;
        float primeAge = deathAge * m_PlantPrimeAgePercentage;
        if (plant.age <= primeAge)
        {
            ageInfluence = (float)plant.age / (float)primeAge;
        }
        else if(plant.age > primeAge)
        {
            ageInfluence = 1.0f - (((float)plant.age - (float)primeAge) / ((float)deathAge - (float)primeAge));
        }
        return (ageInfluence * plant.age) * plant.health;
    }

    private RectInt GetInfluencedRect(Vector2 midPosition, float range)
    {
        // this factor is currently 1 
        const float METER_TO_PIXEL_FACTOR = 1.0f;

        return new RectInt((int)(midPosition.x - METER_TO_PIXEL_FACTOR * range), (int)(midPosition.y - METER_TO_PIXEL_FACTOR * range), 
                           (int)(METER_TO_PIXEL_FACTOR * range * 2.0f), (int)(METER_TO_PIXEL_FACTOR * range * 2.0f));
    }
}
