using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSpeciesInfoManager : MonoBehaviour
{
    public PlantSpeciesInfoScriptableObject m_OwnSpeciesInfo;
    [SerializeField] private int m_IndividualPlantAge = 0;
    [SerializeField] private float m_HealthyModelScaleFactor;
    [SerializeField] private float m_UnhealthyModelScaleFactor;
    [SerializeField] private float m_CurrentScaleFactor;
    [SerializeField] private float m_OverallScaleFactor = 0.2f; 
    [SerializeField] private float m_CutOffFactorOcclusion;
    [SerializeField] private float m_TemperatureAtThisAltitude;
    [SerializeField] private float m_TestValue;
    private bool m_CanSpawn = false;


    public PlantSpeciesInfoManager()
    {

    }

    public PlantSpeciesInfoManager(PlantSpeciesInfoScriptableObject ownInfo)
    {
        m_OwnSpeciesInfo = ownInfo;
    }




    // Start is called before the first frame update
    private void OnEnable()
    {
        m_IndividualPlantAge = 0;
        m_CurrentScaleFactor = m_HealthyModelScaleFactor; //only because we only have one factor so far
        SimulationManager.simulationTick += AgePlant;
        CalculateViability();
        CheckForDeath();
        Singletons.simulationManager.m_ActivePlants++;
    }
    

    public void AgePlant()
    {
        m_IndividualPlantAge++;
        Vector3 newlyCalculatedScale =  new Vector3(m_IndividualPlantAge, m_IndividualPlantAge, m_IndividualPlantAge) * m_CurrentScaleFactor * m_OverallScaleFactor;
        if (float.IsNaN(newlyCalculatedScale.x))
        {
            Debug.LogError("Plant cannot be aged with incorrect NaN scale.");
            return;
        }
        m_CanSpawn = true;
        //gameObject.transform.localScale = newlyCalculatedScale;
        
    }

    // Update is called once per frame
    void Update()
    {
        
   
        CheckForDeath();
        CheckForMaturity();
        CalculateViability();
        LookUpTestMapAndPrintValue();
    }

    private void CheckForMaturity()
    {
        if(m_IndividualPlantAge >= m_OwnSpeciesInfo.maturityAge && m_CanSpawn)
        {
            m_CanSpawn = false;
            //start producing seeds around you
            Singletons.seedManager.ScatterSeedsAroundPoint(this);
        }
    }

    private void CheckForDeath()
    {
        if(m_IndividualPlantAge >= m_OwnSpeciesInfo.deathAge)
        {
            //die
            //might have to unregister from the seed manager, but I will look into that later
            PlantDies();
            
        }

        if(m_CurrentScaleFactor <= 0.0f)
        {
            PlantDies();
        }
    }

    private void PlantDies()
    {
        gameObject.SetActive(false);
        
    }

    private void OnDisable()
    {
        SimulationManager.simulationTick -= AgePlant;
        Singletons.simulationManager.m_ActivePlants--;
    }

    private float CalculateViability()
    {
        //a simple test of concept with occlusion, flow and temperature

        float overallViability;
        float occlusionFactor           = CalculateFactorWithMap(Singletons.simulationManager.occlusionMap, m_OwnSpeciesInfo.optimalOcclusion);
        float flowFactor                = CalculateFactorWithMap(Singletons.simulationManager.flowMap, m_OwnSpeciesInfo.optimalflow);
        float temperatureBasedOnHeight  = CalculateTemperatureFactorAtAltitude(Singletons.simulationManager.groundTemperature, m_OwnSpeciesInfo.optimalTemperature);
        if (temperatureBasedOnHeight <= 0.0f)
        {
            overallViability = 0.0f;
        }
        else
        {

            overallViability = (occlusionFactor * m_OwnSpeciesInfo.occlusionFactorWeight) + (flowFactor * m_OwnSpeciesInfo.flowFactorWeight) + (temperatureBasedOnHeight * m_OwnSpeciesInfo.temperatureWeight);
        }

        if (float.IsNaN(overallViability))
        {
            Debug.LogError("Overall Viability is NaN");
            return -1.0f;
        }
        m_CurrentScaleFactor = overallViability;
        return overallViability;

        //float differenceInOcclusion = m_OwnSpeciesInfo.optimalOcclusion - (float)colorChannels.r;
        //if(Mathf.Abs(differenceInOcclusion) >= m_CutOffFactorOcclusion)
        //{
        //    m_CurrentScaleFactor = m_UnhealthyModelScaleFactor;
        //} 
    }

    private float CalculateTemperatureFactorAtAltitude(float currentTemperature, float wantedTemperature)
    {
        float temperatureAtHeight = currentTemperature - (0.0065f * (gameObject.transform.position.y * Singletons.simulationManager.heightScalingFactor));
        m_TemperatureAtThisAltitude = temperatureAtHeight;
        if(temperatureAtHeight <= 0.0f)
        {
            return 0.0f;
        }
        return Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (temperatureAtHeight / wantedTemperature))));
    }

    private float CalculateFactorWithMap(Texture2D map, float wantedValue)
    {
        Color mapColorsAtPosition = map.GetPixel((int)gameObject.transform.position.x, 1 - (int)gameObject.transform.position.z);
        return Mathf.Clamp01((1.0f - Mathf.Abs(1.0f - (mapColorsAtPosition.r / wantedValue))));
    }


    private void LookUpTestMapAndPrintValue()
    {
        int mapDimensions = Singletons.simulationManager.testMap.width;
        int correspondingXPos = (int)((gameObject.transform.position.x / Singletons.simulationManager.terrain.terrainData.size.x) * mapDimensions);
        int correspondingZPos = (int)((gameObject.transform.position.z / Singletons.simulationManager.terrain.terrainData.size.x) * mapDimensions);
        Color mapColorsAtPosition = Singletons.simulationManager.testMap.GetPixel(correspondingXPos, 1 - correspondingZPos);
        m_TestValue = mapColorsAtPosition.r;
    }
    

    private void OnDestroy()
    {
        SimulationManager.simulationTick -= AgePlant;
    }
}
