using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "PlantSpeciesInfo", order = 1)]
public class PlantSpeciesInfoScriptableObject : ScriptableObject
{
    [Header("Reproduction")]
    [SerializeField] private PlantType m_PlantType;             public PlantType plantType { get { return m_PlantType; } }
    [SerializeField] private GameObject m_OwnSpeciesPrefab;     public GameObject ownSpeciesPrefab { get { return m_OwnSpeciesPrefab; } }
    [SerializeField] private Material m_OwnMaterial;            public Material ownMaterial { get { return m_OwnMaterial; } }
    [SerializeField] private Color m_TypeColor;                 public Color typeColor { get { return m_TypeColor; } }
    [SerializeField] private int m_MaturityAge;                 public int maturityAge { get { return m_MaturityAge; } }
    
    [SerializeField] private int m_DeathAge;                    public int deathAge { get { return m_DeathAge; } }
    [SerializeField] private float m_MaxSize;                   public float maxSize { get { return m_MaxSize; } }
    [SerializeField] private int m_SeedDistributionAmount;      public int seedDistributionAmount { get { return m_SeedDistributionAmount; } }
    [SerializeField] private float m_MaxDistributionDistance;   public float maxDistributionDistance { get { return m_MaxDistributionDistance; } }


    [SerializeField] private float m_OptimalOcclusion;          public float optimalOcclusion { get { return m_OptimalOcclusion; } }
    [Range(0.0f, 1.0f)]
    [SerializeField] private float m_OcclusionFactorWeight;     public float occlusionFactorWeight { get { return m_OcclusionFactorWeight; } }
    [SerializeField] private float m_OptimalFlow;               public float optimalflow { get { return m_OptimalFlow; } }
    [Range(0.0f, 1.0f)]
    [SerializeField] private float m_FlowFactorWeight;          public float flowFactorWeight { get { return m_FlowFactorWeight; } }
    [SerializeField] private float m_OptimalTemperature;        public float optimalTemperature { get { return m_OptimalTemperature; } }
    [Range(0.0f, 1.0f)]
    [SerializeField] private float m_TemperatureWeight; public float temperatureWeight { get { return m_TemperatureWeight; } }

    [SerializeField] private float m_SoilTextureWeight; public float soilTextureWeight { get { return m_SoilTextureWeight; } }
    [SerializeField] private float m_PreferredClayValue; public float preferredClayValue { get { return m_PreferredClayValue; } }
    [SerializeField] private float m_PreferredSiltValue; public float preferredSiltValue { get { return m_PreferredSiltValue; } }
    [SerializeField] private float m_PreferredSandValue; public float preferredSandValue { get { return m_PreferredSandValue; } }

    [SerializeField] private float m_SoilCompositionWeight; public float soilCompositionWeight { get { return m_SoilCompositionWeight; } }
    [SerializeField] private float m_PreferredAcidityValue; public float preferredAcidityValue { get { return m_PreferredAcidityValue; } }

    //[SerializeField] private float m_TakenClayValue; public float takenClayValue { get { return m_TakenClayValue; } }
    //[SerializeField] private float m_TakenSandValue; public float takenSandValue { get { return m_TakenSandValue; } }
    //[SerializeField] private float m_TakenSiltValue; public float takenSiltValue { get { return m_TakenSiltValue; } }

    //[SerializeField] private float m_SoilTakingTestValue;   public float soilTakingTestValue { get { return m_SoilTakingTestValue; } }
    [SerializeField] private float m_PersistenceValue;      public float persistenceValue { get { return m_PersistenceValue; } }
    //Viability 
    //We have altitude, occlusion, flow & soil composition
    //each species should probably have own specifics for each of these values on what it needs and how important it is
    //example: occlusion: needs a lot of sunlight, flow: needs not so much water
    //ambivalence of factors by giving factors a priority? 

}
