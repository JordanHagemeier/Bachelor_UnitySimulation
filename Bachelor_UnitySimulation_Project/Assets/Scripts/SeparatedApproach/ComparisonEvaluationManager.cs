using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComparisonEvaluationManager : MonoBehaviour
{
    //needs: 
    //  - actual european dominant species map
    //  - calculated map from sim
    //  - a map of color to value

    [SerializeField] private Texture2D m_EuropeanDominantSpeciesMap;
    [SerializeField] private Texture2D m_SimulatedSpeciesMap;
    [SerializeField] private List<PlantTypeToValueAndColorSO> m_PlantTypesMapping = new List<PlantTypeToValueAndColorSO>();
    [SerializeField] private SeparatedSimulationManager simManager;

    private Dictionary<Color, float> m_MapColorToPlantValue = new Dictionary<Color, float>();
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LateSetColors());
    }

    IEnumerator LateSetColors()
    {
        yield return new WaitForSeconds(0.5f);
        SetColorOfMappings();
    }
    private void SetColorOfMappings()
    {
        for(int i = 0; i < m_PlantTypesMapping.Count; i++)
        {
            m_PlantTypesMapping[i].color = simManager.plantSpeciesTable.GetSOByType(m_PlantTypesMapping[i].plantType).typeColor;
            m_MapColorToPlantValue.Add(m_PlantTypesMapping[i].color, m_PlantTypesMapping[i].value);
        }

    }

    

    private void GenerateOverlapMap()
    {
        //generate new Texture
        //check for each pixel of simulated map
        //  - if grey, discard
        //  - if any other color, lookup in map what value on the original map it should be
        //  - compare with same pixel on european map
        //if same: set pixel on new texture to white
        //if different: set pixel on new texture to black
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
