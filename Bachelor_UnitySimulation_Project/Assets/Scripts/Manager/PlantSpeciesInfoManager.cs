using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantSpeciesInfoManager : MonoBehaviour
{
    public PlantSpeciesInfoScriptableObject m_OwnSpeciesInfo;
    private int m_IndividualPlantAge = 0;
    // Start is called before the first frame update
    void Start()
    {
        SimulationManager.simulationTick += AgePlant;
    }

    public void AgePlant()
    {
        m_IndividualPlantAge++;
        gameObject.transform.localScale = new Vector3(m_IndividualPlantAge, m_IndividualPlantAge, m_IndividualPlantAge);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CheckForMaturity()
    {

    }

    private void CheckForDeath()
    {

    }

    private void CalculateViability()
    {

    }

    private void OnDestroy()
    {
        SimulationManager.simulationTick -= AgePlant;
    }
}
