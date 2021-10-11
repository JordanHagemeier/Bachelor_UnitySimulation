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
    [SerializeField] private float m_CutOffFactorOcclusion;
    [SerializeField] private float m_TemperatureAtThisAltitude;
    

    // Start is called before the first frame update
    void Start()
    {
        m_CurrentScaleFactor = m_HealthyModelScaleFactor; //only because we only have one factor so far
        SimulationManager.simulationTick += AgePlant;
        CalculateViability();
        CheckForDeath();
    }

    public void AgePlant()
    {
        m_IndividualPlantAge++;
        Vector3 newlyCalculatedScale =  new Vector3(m_IndividualPlantAge, m_IndividualPlantAge, m_IndividualPlantAge) * m_CurrentScaleFactor;
        if (float.IsNaN(newlyCalculatedScale.x))
        {
            Debug.LogError("Plant cannot be aged with incorrect NaN scale.");
            return;
        }
       
        gameObject.transform.localScale = newlyCalculatedScale;
    }

    // Update is called once per frame
    void Update()
    {
        
   
        CheckForDeath();
        CheckForMaturity();
        CalculateViability();
    }

    private void CheckForMaturity()
    {
        if(m_IndividualPlantAge >= m_OwnSpeciesInfo.maturityAge)
        {
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
        SimulationManager.simulationTick -= AgePlant;
    }

    private void CalculateViability()
    {
        //a simple test of concept with occlusion, flow and temperature
       

        float occlusionFactor           = CalculateFactorWithMap(Singletons.simulationManager.occlusionMap, m_OwnSpeciesInfo.optimalOcclusion);
        float flowFactor                = CalculateFactorWithMap(Singletons.simulationManager.flowMap, m_OwnSpeciesInfo.optimalflow);
        float temperatureBasedOnHeight  = CalculateTemperatureFactorAtAltitude(Singletons.simulationManager.groundTemperature, m_OwnSpeciesInfo.optimalTemperature);
        float overallViability = (occlusionFactor * m_OwnSpeciesInfo.occlusionFactorWeight) + (flowFactor * m_OwnSpeciesInfo.flowFactorWeight) + (temperatureBasedOnHeight * m_OwnSpeciesInfo.temperatureWeight);


        if (float.IsNaN(overallViability))
        {
            Debug.LogError("Overall Viability is NaN");
            return;
        }
        m_CurrentScaleFactor = overallViability;


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

    

    private void OnDestroy()
    {
        SimulationManager.simulationTick -= AgePlant;
    }
}
