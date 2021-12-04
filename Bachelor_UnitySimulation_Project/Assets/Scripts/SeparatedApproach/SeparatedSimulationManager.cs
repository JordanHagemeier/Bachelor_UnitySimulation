using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeparatedSimulationManager : MonoBehaviour
{
    
    [SerializeField] private List<PlantInfoStruct> plants;
    [SerializeField] private GroundInfoStruct[,] ground;


    [SerializeField] private int m_InitialPlantAmount;
    [SerializeField] PlantSpeciesInfoScriptableObject[] plantSpecies;
    [SerializeField] Texture2D m_FlowMap;
    [SerializeField] Texture2D m_OcclusionMap;
    [SerializeField] private Terrain m_Terrain;

    [SerializeField] private float m_TimeTillOneYearIsOver;
    private float timer;

    private int m_IDCounter = 0;
    


    // Start is called before the first frame update
    void Start()
    {
        if(m_FlowMap == null)
        {
            m_FlowMap = Singletons.simulationManager.flowMap;
        }
        if(m_OcclusionMap == null)
        {
            m_OcclusionMap = Singletons.simulationManager.occlusionMap;
        }
        m_Terrain = Singletons.simulationManager.terrain;
        //initialize first couple of trees in the list 
        InitializePlantInfos();
        //fill ground info structs with data about their pixel position
        InitializeGroundInfos();
    }

    // Update is called once per frame
    void Update()
    {
        TickPlants();

        //"Tick Ground" needs further evaluation if it is even necessary or if I have enough data and research for correct soil behavior
        TickGround();
    }

    private void InitializePlantInfos()
    {
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
            PlantInfoStruct newPlant = new PlantInfoStruct(randomPos, m_IDCounter, plantSpecies[0]);
            plants.Add(newPlant);
        }
    }
    
    private void InitializeGroundInfos()
    {
        ground = new GroundInfoStruct[1024, 1024];
        for(int i = 0; i < ground.GetLength(0); i++)
        {
            for(int j = 0; j < ground.Length / ground.GetLength(0); j++)
            {
                GroundInfoStruct newGroundPixel = new GroundInfoStruct();
                newGroundPixel.terrainOcclusion = CalculateGroundValueWithMap(m_OcclusionMap, new Vector2(i, j));
                newGroundPixel.waterflow        = CalculateGroundValueWithMap(m_FlowMap, new Vector2(i, j));
                ground[i, j] = newGroundPixel;

               // Debug.Log(i + ", " + j);
            }
        }
    }
    private bool TickPlants()
    {
        bool success = false;
        timer += Time.deltaTime;
        if (timer > m_TimeTillOneYearIsOver)
        {
            timer = 0.0f;


            for (int i = 0; i < plants.Count; i++)
            {
                plants[i].age++;
                if (CheckIfPlantIsAlive(plants[i]))
                {
                    plants[i].health = CalculateViability(plants[i]);
                    CheckIfPlantCanReproduce(plants[i]);

                }
            }
        }
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
        if (plant.age >= plant.plantSpeciesInfo.deathAge)
        {
            //die
            //might have to unregister from the seed manager, but I will look into that later
            //PlantDies();
            isAlive = false;
            plants.Remove(plant);
            return isAlive;
            
        }

        return isAlive;
    }

    private bool CheckIfPlantCanReproduce(PlantInfoStruct plant)
    {
        if(plant.age >= plant.plantSpeciesInfo.maturityAge)
        {
            ScatterSeeds(plant);
            return true;
        }

        return false;
    }

    private float CalculateViability(PlantInfoStruct plant)
    {
        //a simple test of concept with occlusion, flow and temperature

        float overallViability;
        float occlusionFactor = CalculateFactorWithGroundValue(ground[(int)plant.position.x, (int)plant.position.z].terrainOcclusion, plant.plantSpeciesInfo.optimalOcclusion);
        float flowFactor = CalculateFactorWithGroundValue(ground[(int)plant.position.x, (int)plant.position.z].waterflow, plant.plantSpeciesInfo.optimalflow);
        //float occlusionFactor_OLD = CalculateFactorWithMap(Singletons.simulationManager.occlusionMap, plant.plantSpeciesInfo.optimalOcclusion, plant);
        //float flowFactor_OLD = CalculateFactorWithMap(Singletons.simulationManager.flowMap, plant.plantSpeciesInfo.optimalflow, plant);

        
        float temperatureBasedOnHeight = CalculateTemperatureFactorAtAltitude(Singletons.simulationManager.groundTemperature, plant.plantSpeciesInfo.optimalTemperature, plant);
        if (temperatureBasedOnHeight <= 0.0f)
        {
            overallViability = 0.0f;
        }
        else
        {

            overallViability = (occlusionFactor * plant.plantSpeciesInfo.occlusionFactorWeight) + (flowFactor * plant.plantSpeciesInfo.flowFactorWeight) + (temperatureBasedOnHeight * plant.plantSpeciesInfo.temperatureWeight);
        }

        if (float.IsNaN(overallViability))
        {
            Debug.LogError("Overall Viability is NaN");
            return -1.0f;
        }
       
        return overallViability;

       
    }


    private void ScatterSeeds(PlantInfoStruct plant)
    {
        for (int i = 0; i < plant.plantSpeciesInfo.seedDistributionAmount; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * plant.plantSpeciesInfo.maxDistributionDistance;

            Vector3 calculatedPos = plant.position + new Vector3(randomPos.x, 0.0f, randomPos.y);
            calculatedPos.y = m_Terrain.SampleHeight(calculatedPos);
            if (IsCalculatedNewPosWithinTerrainBounds(calculatedPos))
            {
                m_IDCounter++;
                PlantInfoStruct newPlant = new PlantInfoStruct(calculatedPos, m_IDCounter, plant.plantSpeciesInfo);
                plants.Add(newPlant);
                
            }

        }
       
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

    private float CalculateFactorWithMap(Texture2D map, float wantedValue, PlantInfoStruct plant)
    {
        Color mapColorsAtPosition = map.GetPixel((int)plant.position.x, 1 - (int)plant.position.z);
        return Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (mapColorsAtPosition.r / wantedValue))));
    }

    private float CalculateFactorWithGroundValue(float groundValue, float wantedValue)
    {
        return Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (groundValue / wantedValue))));
    }

    private float CalculateGroundValueWithMap(Texture2D map, Vector2 pos)
    {
        return map.GetPixel((int)pos.x, 1 - (int)pos.y).r;
    }


    public bool TickGround()
    {
        bool success = false;

        return success;
    }
}
