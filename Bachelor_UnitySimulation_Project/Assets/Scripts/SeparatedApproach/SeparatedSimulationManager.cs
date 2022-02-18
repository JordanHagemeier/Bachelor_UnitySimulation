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
    [SerializeField] private int m_SimulationAmount;
    [SerializeField] private int m_CurrentSimCounter = 0;
    [SerializeField] private int m_IterationsPerSimulation;
    private int m_CurrentIterationCounter = 0;
    [SerializeField] private float m_TimeTillOneYearIsOver;
    [SerializeField] private float timer;

    [Header ("Plant Information")]
    [SerializeField] private List<PlantInfoStruct> m_Plants;
    [SerializeField] private int m_AmountOfPlants;



    [SerializeField] private int m_InitialPlantAmount;
    [SerializeField] PlantSpeciesInfoScriptableObject[] plantSpecies;
    [SerializeField] Texture2D m_FlowMap;
    [SerializeField] Texture2D m_OcclusionMap;
    [SerializeField] private Terrain m_Terrain;

    [SerializeField] private Texture2D m_UsableGround;
    [SerializeField] private float m_SeaLevelCutOff;
    [SerializeField] private float m_DistanceBetweenTrees;

    [SerializeField] private Texture2D m_ClayMap;
    [SerializeField] private Texture2D m_SandMap;
    [SerializeField] private Texture2D m_SiltMap;


    private int m_IDCounter = 0;

    [SerializeField] private Texture2D m_WritingTextureTest;
    [SerializeField] private Texture2D m_WritingTextureTestCopy;

    
    [HideInInspector] public PlantInfoStruct[] m_CopiedPlants;
    private VisualizationManager m_VisManager;

    [SerializeField] private GroundInfoManager m_GroundInfoManager;
    CurrentPlantsSerializer plantSerializer = new CurrentPlantsSerializer();
    [HideInInspector] public PlantSpeciesTable plantSpeciesTable = new PlantSpeciesTable();



    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(m_SimulationSeed);
        StartUpMetaInformation();
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

        return success;
    }

    private void StartUpMetaInformation()
    {
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


            if (m_CurrentIterationCounter > m_IterationsPerSimulation)
            {
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
            
            Bounds terrainBounds = Singletons.simulationManager.terrain.terrainData.bounds;
            //get random position on terrain 
            Vector3 randomPos   = new Vector3(Random.Range(terrainBounds.min.x, terrainBounds.max.x), 0.0f, Random.Range(terrainBounds.min.z, terrainBounds.max.z));
            randomPos.y         = Singletons.simulationManager.terrain.SampleHeight(randomPos);

            //GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //debugCube.transform.position = randomPos;


            //initialize new plant with that position 
            m_IDCounter++;
            PlantInfoStruct newPlant = new PlantInfoStruct(randomPos, m_IDCounter, plantSpecies[Random.Range(0, plantSpecies.Length)]);
            m_Plants.Add(newPlant);
        }

        m_AmountOfPlants = m_Plants.Count;
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

        Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z);
        float groundOcclusionFactor = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)plant.position.x, (int)plant.position.z, terrainBounds).terrainOcclusion;
        float occlusionFactor       = CalculateFactorWithGroundValue(groundOcclusionFactor, currentPlantSO.optimalOcclusion);
       
        float groundFlowFactor      = m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)plant.position.x, (int)plant.position.z, terrainBounds).waterflow;
        float flowFactor            = CalculateFactorWithGroundValue(groundFlowFactor, currentPlantSO.optimalflow);


        //TODO change this one from local ground to ground array and implement changes to ground values in it
        float soilCompositionValue      = CalculateSoilCompositionValue(new Vector2((int)plant.position.x, (int)plant.position.z), currentPlantSO);
        

        float temperatureBasedOnHeight = CalculateTemperatureFactorAtAltitude(Singletons.simulationManager.groundTemperature, currentPlantSO.optimalTemperature, plant);
        if (temperatureBasedOnHeight <= 0.0f)
        {
            overallViability = 0.0f;
        }
        else
        {

            overallViability = (occlusionFactor * currentPlantSO.occlusionFactorWeight) 
                + (flowFactor * currentPlantSO.flowFactorWeight) 
                + (temperatureBasedOnHeight * currentPlantSO.temperatureWeight 
                + (soilCompositionValue * currentPlantSO.soilCompositionWeight));
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
            if (IsCalculatedNewPosWithinTerrainBounds(calculatedPos) && IsOnLand(calculatedPos) && !IsWithinDistanceToOthers(calculatedPos))
            {
                m_IDCounter++;
                PlantInfoStruct newPlant = new PlantInfoStruct(calculatedPos, m_IDCounter, currentPlantSO);
                m_Plants.Add(newPlant);
                
            }

        }
       
    }

    private bool IsWithinDistanceToOthers(Vector3 pos)
    {

        bool withinRad = false;
        foreach(PlantInfoStruct plant in m_Plants)
        {
            
            float distance = Vector2.SqrMagnitude(new Vector2(pos.x - plant.position.x, pos.z - plant.position.z));
            if(distance < m_DistanceBetweenTrees * m_DistanceBetweenTrees)
            {
                withinRad = true;
                return withinRad;
            }
        }
        return withinRad;
    }

    private float AffectedViabilityThroughProximity(PlantInfoStruct givenPlant, float calculatedViability)
    {
        int countOfTreesInProximity = 0;

        foreach(PlantInfoStruct plant in m_Plants)
        {
            float distance = Vector2.SqrMagnitude(new Vector2(givenPlant.position.x - plant.position.x, givenPlant.position.z - plant.position.z));
            if (distance < m_DistanceBetweenTrees * m_DistanceBetweenTrees)
            {
                countOfTreesInProximity++;
            }
        }
        PlantSpeciesInfoScriptableObject currentPlantSO = plantSpeciesTable.GetSOByType(givenPlant.type);
        float resultingViability = calculatedViability - ((1.0f - currentPlantSO.persistenceValue) * countOfTreesInProximity); 
        return resultingViability;

    }


    private bool IsOnLand(Vector3 pos)
    {
       
        Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z);
        return m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)pos.x, (int)pos.z, terrainBounds).onLand;
        
    }

    private bool IsCalculatedNewPosWithinTerrainBounds(Vector3 pos)
    {
        if (pos.x < m_Terrain.terrainData.bounds.max.x && pos.x > m_Terrain.terrainData.bounds.min.x && pos.z > m_Terrain.terrainData.bounds.min.z && pos.z < m_Terrain.terrainData.bounds.max.z)
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
        return Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (temperatureAtHeight / wantedTemperature))));
    }

   
    private float CalculateFactorWithGroundValue(float groundValue, float wantedValue)
    {
        return Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (groundValue / wantedValue))));
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

    private float CalculateSoilCompositionValue(Vector2 pos, PlantSpeciesInfoScriptableObject plantSO)
    {
        float soilViabilityValue    = 0.0f;
        Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z); 
        //get the value from the ground and what the plant type prefers, then look how closely that factor matches up 
        //float clayValue             = GetValueFromSoilCompositionMap(m_ClayMap, pos);
        //float preferredClayValue    = plantSO.preferredClayValue;
        //float clayFactor            = CalculateFactorWithGroundValue(clayValue, preferredClayValue);

        //float sandValue             = GetValueFromSoilCompositionMap(m_SandMap, pos);
        //float preferredSandValue    = plantSO.preferredSandValue;
        //float sandFactor            = CalculateFactorWithGroundValue(sandValue, preferredSandValue);

        //float siltValue             = GetValueFromSoilCompositionMap(m_SiltMap, pos);
        //float preferredSiltValue    = plantSO.preferredSiltValue;
        //float siltFactor            = CalculateFactorWithGroundValue(siltValue, preferredSiltValue);




        //float clayValue = GetValueFromSoilCompositionMap(m_ClayMapCopied, pos);
        //float preferredClayValue = plantSO.preferredClayValue;
        //float clayFactor = CalculateFactorWithGroundValue(clayValue, preferredClayValue);

        //float sandValue = GetValueFromSoilCompositionMap(m_SandMapCopied, pos);
        //float preferredSandValue = plantSO.preferredSandValue;
        //float sandFactor = CalculateFactorWithGroundValue(sandValue, preferredSandValue);

        //float siltValue = GetValueFromSoilCompositionMap(m_SiltMapCopied, pos);
        //float preferredSiltValue = plantSO.preferredSiltValue;
        //float siltFactor = CalculateFactorWithGroundValue(siltValue, preferredSiltValue);



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
}
