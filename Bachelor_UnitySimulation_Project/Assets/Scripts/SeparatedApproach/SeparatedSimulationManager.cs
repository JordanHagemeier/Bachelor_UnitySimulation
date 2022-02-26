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
    [SerializeField] private int m_EvaluationAtIterations; 

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



    // Start is called before the first frame update
    void Start()
    {
        
        Random.InitState(m_SimulationSeed);
        StartUpMetaInformation();
        m_CurrentSimCounter = m_StartingIteration;
        ResetSimulation(m_CurrentSimCounter);
      
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

            if(m_CurrentIterationCounter % m_EvaluationAtIterations == 0)
            {
                if (m_RenderAllPlants)
                {
                    RenderAllTrees();
                }
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
        bool isAlive = true;
        if (plant.age >= plantSpeciesTable.GetSOByType(plant.type).deathAge)
        {
            //die
            //might have to unregister from the seed manager, but I will look into that later
            //PlantDies();
            isAlive = false;
            RemoveOwnPlantInfoFromQuadCell(plant);
            m_Plants.Remove(plant);

            //TODO implement soil replenishment after death based on data from the plant 
            return isAlive;
            
        }

        return isAlive;
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
        //a simple test of concept with occlusion, flow and temperature
        PlantSpeciesInfoScriptableObject currentPlantSO     = plantSpeciesTable.GetSOByType(plant.type);
        float overallViability;

        //float occlusionFactor     = CalculateFactorWithGroundValue(ground[(int)plant.position.x, (int)plant.position.z].terrainOcclusion, currentPlantSO.optimalOcclusion);
        //float flowFactor          = CalculateFactorWithGroundValue(ground[(int)plant.position.x, (int)plant.position.z].waterflow, currentPlantSO.optimalflow);
        int factorCounter = 0;
        Vector2 terrainBounds = new Vector2(m_TerrainBounds.max.x, m_TerrainBounds.max.z);
        float groundOcclusionFactor = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)plant.position.x, (int)plant.position.z, terrainBounds).terrainOcclusion;
        float occlusionFactor       = CalculateFactorWithGroundValue(groundOcclusionFactor, currentPlantSO.optimalOcclusion);
        factorCounter++;
       
        float groundFlowFactor      = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)plant.position.x, (int)plant.position.z, terrainBounds).waterflow;
        float flowFactor            = CalculateFactorWithGroundValue(groundFlowFactor, currentPlantSO.optimalflow);
        factorCounter++;


        //TODO change this one from local ground to ground array and implement changes to ground values in it
        float soilTextureValue = 0.0f;
        if (m_UseSoilTexture)
        {
             soilTextureValue = CalculateSoilTextureValue(new Vector2((int)plant.position.x, (int)plant.position.z), currentPlantSO);
            factorCounter++;
        }

        float soilCompositionValue = 0.0f;
        if (m_UseSoilpH)
        {
            soilCompositionValue = CalculateSoilCompositionValue(new Vector2((int)plant.position.x, (int)plant.position.z), currentPlantSO);
            factorCounter++;
        }

        float temperatureBasedOnHeight = CalculateTemperatureFactorAtAltitude(Singletons.simulationManager.groundTemperature, currentPlantSO.optimalTemperature, plant);
        factorCounter++;
        if (temperatureBasedOnHeight <= 0.0f)
        {
            overallViability = 0.0f;
        }
        else
        {

            overallViability = ((occlusionFactor * currentPlantSO.occlusionFactorWeight) 
                + (flowFactor * currentPlantSO.flowFactorWeight) 
                + (temperatureBasedOnHeight * currentPlantSO.temperatureWeight 
                + (soilTextureValue * (currentPlantSO.soilTextureWeight + m_AdditionalSoilTextureWeight))
                + soilCompositionValue * currentPlantSO.soilCompositionWeight)) /(float)factorCounter;
        }

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
        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(plant.type);
        for (int i = 0; i < currentPlantSO.seedDistributionAmount; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * currentPlantSO.maxDistributionDistance;

            Vector3 calculatedPos = plant.position + new Vector3(randomPos.x, 0.0f, randomPos.y);
            calculatedPos.y = m_Terrain.SampleHeight(calculatedPos);
            if (IsCalculatedNewPosWithinTerrainBounds(calculatedPos) && IsOnLand(calculatedPos) && !IsPlantWithinDistanceToOtherPlantsQUADCELL(new Vector2(calculatedPos.x, calculatedPos.z)))
            {
               
                m_IDCounter++;
                PlantInfoStruct newPlant = new PlantInfoStruct(calculatedPos, m_IDCounter, currentPlantSO);
                m_Plants.Add(newPlant);
                SetOwnPlantInfoInQuadCell(newPlant);
                
            }

        }
       
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

    private bool IsPlantWithinDistanceToOtherPlantsQUADCELL(Vector2 pos)
    {
        bool withinDistance = false;


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

        List<PlantInfoStruct>[,] plantCellsToCheck = new List<PlantInfoStruct>[(maxX - minX)+1, (maxY - minY)+1];

        //check against plants in these cells
        int counterX = 0;
        int counterY = 0;
        for (int iY = minY; iY <= maxY; iY++)
        {
            counterX = 0;
            for (int iX = minX; iX <= maxX; iX++)
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
        //get own QuadCell
        float positionPercentageX = givenPlant.position.x / 1024.0f;
        float positionPercentageY = givenPlant.position.z / 1024.0f;

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


        int countOfTreesInProximity = 0;
        //check against plants in these cells
        int counterX = 0;
        int counterY = 0;
        for (int iY = minY; iY <= maxY; iY++)
        {
            counterX = 0;
            for (int iX = minX; iX <= maxX; iX++)
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

   
    private float CalculateFactorWithGroundValue(float groundValue, float wantedValue)
    {
        groundValue = Mathf.Clamp(groundValue, 0.0f, wantedValue * 2.0f);
        //return Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (groundValue / wantedValue))));
        float result =  (Mathf.Abs(wantedValue - Mathf.Abs(wantedValue - groundValue))) / wantedValue;
        return result;
    }

    private float CalculateAcidtyFactor(float groundValue, float wantedValue)
    {
        float result = (Mathf.Abs(wantedValue - Mathf.Abs(wantedValue - groundValue))) / wantedValue;
        return result;
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
        float clayFactor = CalculateFactorWithGroundValue(clayValue, preferredClayValue);

        float sandValue = GetValueFromSoilCompositionMap(m_SandMap, pos);
        float preferredSandValue = plantSO.preferredSandValue;
        float sandFactor = CalculateFactorWithGroundValue(sandValue, preferredSandValue);

        float siltValue = GetValueFromSoilCompositionMap(m_SiltMap, pos);
        float preferredSiltValue = plantSO.preferredSiltValue;
        float siltFactor = CalculateFactorWithGroundValue(siltValue, preferredSiltValue);

        return values;
    }

    private float CalculateSoilTextureValue(Vector2 pos, PlantSpeciesInfoScriptableObject plantSO)
    {
        float soilViabilityValue    = 0.0f;
        Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z); 
        
        //ground info array version 
        float clayValueFromGround               = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y, terrainBounds).clay;
        float preferredClayValueFromGround      = plantSO.preferredClayValue;
        float clayFactorFromGround              = CalculateFactorWithGroundValue(clayValueFromGround, preferredClayValueFromGround);

        float sandValueFromGround               = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y, terrainBounds).sand;
        float preferredSandValueFromGround      = plantSO.preferredSandValue;
        float sandFactorFromGround              = CalculateFactorWithGroundValue(sandValueFromGround, preferredSandValueFromGround);

        float siltValueFromGround               = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y, terrainBounds).silt;
        float preferredSiltValueFromGround      = plantSO.preferredSiltValue;
        float siltFactorFromGround              = CalculateFactorWithGroundValue(siltValueFromGround, preferredSiltValueFromGround);
        //update each map on what was taken 
        //WriteValueToTexture(m_ClayMapCopied, plantSO.takenClayValue, clayValue, pos);

        soilViabilityValue = (clayFactorFromGround + sandFactorFromGround + siltFactorFromGround) % 3.0f;
        return soilViabilityValue;
    }

    private float CalculateSoilCompositionValue(Vector2 pos, PlantSpeciesInfoScriptableObject plantSO)
    {
        float soilCompositionValue = 0.0f;

        //use acidity map information stored in the ground infos and acidity values from the plant type to get this bread
        float acidityValueFromGround = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain(pos.x, pos.y, new Vector2(m_TerrainBounds.max.x, m_TerrainBounds.max.z)).ph;
        float preferredAcidityValue = plantSO.preferredAcidityValue;
        float acidityFactor = CalculateAcidtyFactor(acidityValueFromGround, preferredAcidityValue);

        soilCompositionValue = acidityFactor;
        //this map is actually the only one that can be updated
        return soilCompositionValue;
    }
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
        
     
        const float MINIMUM_INFLUENCE_TO_COUNT_AS_DOMINANT = 1.0f;

        // Find dominant species for every position on the map

        // I) For each plant, mark the fields in its influence
        float[,,] plantDominance = new float[m_MAP_SIZE, m_MAP_SIZE, (int)PlantType.Count];
        foreach (PlantInfoStruct plant in m_Plants)
        {
            Vector2 plantPosition2D = new Vector2(plant.position.x, plant.position.z);
            float dominanceValue = CalculatePlantDominanceValue(plant);
            float influenceRadius = CalculatePlantInfluenceRadius(plant);
            if (influenceRadius <= 0.0f)
            {
                Debug.LogError("Plant " + plant.id + " has no influence at all. How did that happen?");
                continue;
            }

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
                        continue; //< TODO: Consider if we want this
                    }

                    float distance = Vector2.Distance(plantPosition2D, new Vector2(iX, iY));
                    if (distance > influenceRadius)
                    {
                        continue;
                    }

                    float distancePercentage = 1.0f - (distance / influenceRadius);
                    plantDominance[iX, iY, (int)plant.type] += distancePercentage * dominanceValue;
                }
            }
        }

        // 2) Build a map of which plant is the most influential at every point
        PlantType?[,] dominantPlantType = new PlantType?[m_MAP_SIZE, m_MAP_SIZE];
        Color[] dominantPlantColors = new Color[m_MAP_SIZE * m_MAP_SIZE];

        for (int iY = 0; iY < m_MAP_SIZE; iY++)
        {
            for (int iX = 0; iX < m_MAP_SIZE; iX++)
            {
                float highestDominanceValue = 0.0f;
                for (int iP = 0; iP < (int)PlantType.Count; iP++)
                {
                    if (plantDominance[iX, iY, iP] > highestDominanceValue)
                    {
                        highestDominanceValue = plantDominance[iX, iY, iP];
                        if (highestDominanceValue >= MINIMUM_INFLUENCE_TO_COUNT_AS_DOMINANT)
                        {
                            dominantPlantType[iX, iY] = (PlantType)iP;
                            dominantPlantColors[iY * m_MAP_SIZE + iX] = plantSpeciesTable.GetSOByType((PlantType)iP).typeColor;
                        }
                    }
                }

                if (highestDominanceValue <= MINIMUM_INFLUENCE_TO_COUNT_AS_DOMINANT)
                {
                    dominantPlantColors[iY * m_MAP_SIZE + iX] = Color.gray;
                }
            }
        }

        // 3) Save map
        Texture2D dominanceMap = new Texture2D(m_MAP_SIZE, m_MAP_SIZE);
        dominanceMap.SetPixels(0, 0, m_MAP_SIZE, m_MAP_SIZE, dominantPlantColors);
        byte[] dominanceMapBytes = dominanceMap.EncodeToPNG();
        string circumstancesPath = "initPA_" + m_InitialPlantAmount + "_dist_" + m_DistanceBetweenTrees +"/";
        string dirPath = Application.dataPath + "/../Generated/" + circumstancesPath;
        string timeString = System.DateTime.Now.Day + "-" + System.DateTime.Now.Month + "_" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute;
        string filePath = dirPath + "dominanceMap_" + timeString + "_simCount" + m_CurrentSimCounter + "_iteration" + m_CurrentIterationCounter + ".png";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(filePath, dominanceMapBytes);
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
        // TODO: Proper calculation

        const float BASE_INFLUENCE = 2.5f;
        const float RADIUS_INCREASE_PER_YEAR = 0.2f;

        return (BASE_INFLUENCE + RADIUS_INCREASE_PER_YEAR * plant.age) * plant.health;
    }

    private float CalculatePlantDominanceValue(PlantInfoStruct plant)
    {
        // TODO: Proper calculation
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
