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

}
