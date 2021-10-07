using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "PlantSpeciesInfo", order = 1)]
public class PlantSpeciesInfoScriptableObject : ScriptableObject
{
    [Header("Reproduction")]
    [SerializeField] private int m_MaturityAge; public int maturityAge { get { return m_MaturityAge; } }
    [SerializeField] private int m_SeedDistributionAmount;
    [SerializeField] private float m_MaxDistributionDistance;

    //Viability 
    //We have altitude, occlusion, flow & soil composition
    //each species should probably have own specifics for each of these values on what it needs and how important it is
    //example: occlusion: needs a lot of sunlight, flow: needs not so much water
    //ambivalence of factors by giving factors a priority? 

}
