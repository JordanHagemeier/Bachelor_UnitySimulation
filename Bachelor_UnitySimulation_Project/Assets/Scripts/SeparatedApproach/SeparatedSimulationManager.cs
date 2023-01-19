using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using System.IO;

public class SeparatedSimulationManager : MonoBehaviour
{
    [Header ("Simulation Meta Data")]
    [SerializeField] private int m_SimulationSeed; public int simulationSeed { get { return m_SimulationSeed; } set { m_SimulationSeed = value; } }
    [SerializeField] private int m_StartingIteration;
    [SerializeField] private int m_SimulationAmount;
    [SerializeField] private int m_CurrentSimCounter = 0;
    [SerializeField] private int m_IterationsPerSimulation;
    [SerializeField] private int m_CurrentIterationCounter = 0;
    [SerializeField] private float m_TimeTillOneYearIsOver;
    [SerializeField] private float timer;
    //[SerializeField] private int m_EvaluationAtIterations;
    [SerializeField] private List<int> m_EvaluateAtIterationID;

    [Header("Plant Information")]
    private VegetationLifecycleSimulation m_VegetationLifecycleSim;

    [Header ("Terrain Information")]

    [SerializeField] private Terrain m_Terrain;
    private Bounds m_TerrainBounds;



    [SerializeField] private bool m_RenderAllPlants;


    [SerializeField] private Texture2D m_WritingTextureTest;
    [SerializeField] private Texture2D m_WritingTextureTestCopy;

    
    [HideInInspector] public PlantInfoStruct[] m_CopiedPlants;
    private VisualizationManager m_VisManager;

    [SerializeField] private GroundInfoManager m_GroundInfoManager;
                             CurrentPlantsSerializer m_PlantSerializer = new CurrentPlantsSerializer();
    [HideInInspector] public PlantSpeciesTable plantSpeciesTable = new PlantSpeciesTable();

    [SerializeField] private float m_PlantPrimeAgePercentage; 


    private const int m_GRID_CELL_DIVISIONS = 64;
    private const int m_MAP_SIZE = 1024;
    

    private string simulationString = "SIM";

    // Start is called before the first frame update
    void Start()
    {
        if(m_VegetationLifecycleSim == null)
        {
            m_VegetationLifecycleSim = gameObject.GetComponent<VegetationLifecycleSimulation>();
        }
        Random.InitState(m_SimulationSeed);
        StartUpMetaInformation();

        m_CurrentSimCounter = m_StartingIteration;
        ResetSimulation(m_CurrentSimCounter);

        simulationString = System.DateTime.Now.Day + "-" + System.DateTime.Now.Month + "_" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute + (m_VegetationLifecycleSim.useSoilTexture ? "_SoilTexture" : "_NoSoilTexture") + (m_VegetationLifecycleSim.useSoilPH ? "_SoilPH" : "_NoSoilPH");
        
    }

    private bool ResetSimulation(int iteration)
    {
        bool success = true;
        if(m_CurrentSimCounter > m_SimulationAmount)
        {
            Debug.Log("Simulation has finished.");
            Debug.Break();
        }
        //Give new seed 
        //Reset Data from previous run
        //  - new plants, spawned at new locations due to new seed
        //  - restore old ground array 
        //Start new Sim
        Random.InitState(m_SimulationSeed + (42 * iteration));
        m_VegetationLifecycleSim.InitPlantLifecycleSim(iteration);
        m_GroundInfoManager.ResetGroundInfoArray();
        return success;
    }

    private void StartUpMetaInformation()
    {
      
        m_Terrain = Singletons.simulationManager.terrain;
        m_TerrainBounds = m_Terrain.terrainData.bounds;

        m_VisManager = gameObject.GetComponent<VisualizationManager>();
        if (!m_VisManager)
        {
            Debug.Log("Manager not found as GameObject Component.");
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

            bool plantsAreUpdated = m_VegetationLifecycleSim.TickPlants();
            if (plantsAreUpdated == true)
            {

                CopyPlantInfosToVisPlantArray(m_VegetationLifecycleSim.plants);

                m_PlantSerializer.currentPlantsInSim = new List<PlantInfoStruct>();
                for (int k = 0; k < m_CopiedPlants.Length; k++)
                {
                    m_PlantSerializer.currentPlantsInSim.Add(m_CopiedPlants[k]);
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
            m_PlantSerializer.Save(Path.Combine(Application.dataPath, "monsters.xml"));
            Debug.Log("Did it!");
        }
        if (Input.GetMouseButtonDown(1))
        {
            var loadedPlants = CurrentPlantsSerializer.Load(Path.Combine(Application.dataPath, "monsters.xml"));
            m_VegetationLifecycleSim.plants = loadedPlants.currentPlantsInSim;
            CopyPlantInfosToVisPlantArray(m_VegetationLifecycleSim.plants);

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

   
    //private bool IsOnLand(Vector3 pos)
    //{

    //    Vector2 terrainBounds = new Vector2(m_Terrain.terrainData.bounds.max.x, m_Terrain.terrainData.bounds.max.z);
    //    return m_GroundInfoManager.GetGroundInfoAtPositionOnTerrain((int)pos.x, (int)pos.z).onLand;

    //}

 
   
    private float GetValueFromSoilCompositionMap(Texture2D texture,Vector2 pos)
    {
        int posXOnTex = (int)((pos.x / m_Terrain.terrainData.bounds.max.x) * texture.width);
        int posYOnTex = (int)((pos.y / m_Terrain.terrainData.bounds.max.z) * texture.height);
        Color singlePixel = texture.GetPixel(posXOnTex, posYOnTex);
       
        return singlePixel.r * 256.0f;
    }
    
   
    private void ReadRandomPlantValueOnMap(Texture2D texture)
    {
        PlantInfoStruct randomPlant = m_VegetationLifecycleSim.plants[Random.Range(0, m_VegetationLifecycleSim.plants.Count)];
        
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

        foreach (PlantInfoStruct plant in m_VegetationLifecycleSim.plants)
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

                        if (!m_GroundInfoManager.IsOnLand(new Vector3(iX, 0, iY)))
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

                        if (!m_GroundInfoManager.IsOnLand(new Vector3(iX, 0, iY)))
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
                            dominantPlantColors[iY * m_MAP_SIZE + iX] = m_VegetationLifecycleSim.plantSpeciesTable.GetSOByType((PlantType)iP).typeColor;
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
                    Color rowColor = m_VegetationLifecycleSim.plantSpeciesTable.GetSOByType((PlantType)iP).typeColor;
                    for (int iX = xStart; iX < xEnd; iX++)
                    {
                        dominantPlantColors[iY * m_MAP_SIZE + iX] = rowColor;
                    }
                }
            }
        }

        // 4) Save maps
        string circumstancesPath = "initPA_" + m_VegetationLifecycleSim.initialPlantAmount + "_dist_" + m_VegetationLifecycleSim.distanceBetweenTrees + "/";
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
            string filePathOccurance = dirPath + "occuranceMap_" + iP + m_VegetationLifecycleSim.plantSpeciesTable.GetSOByType((PlantType)iP).name + "_" + "_simCount" + m_CurrentSimCounter + "_iteration" + m_CurrentIterationCounter + ".png";
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
                if(m_GroundInfoManager.IsOnLand(new Vector3(iX, 0.0f, iY)))
                {
                    allPlantColors[iY * m_MAP_SIZE + iX] = Color.white;
                }
                else
                {
                    allPlantColors[iY * m_MAP_SIZE + iX] = Color.grey;
                }
            }
        }
        foreach(PlantInfoStruct plant in m_VegetationLifecycleSim.plants)
        {
            Vector2 plantPosition2D = new Vector2(plant.position.x, plant.position.z);
            allPlantColors[(int)plantPosition2D.y * m_MAP_SIZE + (int)plantPosition2D.x] = m_VegetationLifecycleSim.plantSpeciesTable.GetSOByType(plant.type).typeColor;
        }
        //2) fill map with array content
        Texture2D allPlantPositionsMap = new Texture2D(m_MAP_SIZE, m_MAP_SIZE);
        allPlantPositionsMap.SetPixels(0, 0, m_MAP_SIZE, m_MAP_SIZE, allPlantColors);

        byte[] allPlantsMapBytes    = allPlantPositionsMap.EncodeToPNG();
        string circumstancesPath    = "initPA_" + m_VegetationLifecycleSim.initialPlantAmount + "_dist_" + m_VegetationLifecycleSim.distanceBetweenTrees + "/";
        string dirPath              = Application.dataPath + "/../Generated/" + circumstancesPath;
        string timeString           = System.DateTime.Now.Day + "-" + System.DateTime.Now.Month + "_" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute;
        string filePath             = dirPath + "allPlantsMap_" + m_VegetationLifecycleSim.plants.Count + "_time" + timeString + "_simCount" + m_CurrentSimCounter + "_iteration" + m_CurrentIterationCounter + ".png";

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
        int deathAge = m_VegetationLifecycleSim.plantSpeciesTable.GetSOByType(plant.type).deathAge;
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
