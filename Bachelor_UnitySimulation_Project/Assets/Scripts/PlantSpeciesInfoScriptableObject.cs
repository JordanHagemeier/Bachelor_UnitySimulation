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

    // Satisfaction configuration
    [SerializeField] private float m_OptimalOcclusion;          public float optimalOcclusion { get { return m_OptimalOcclusion; } }
    [SerializeField] private AnimationCurve m_OcclusionSatisfaction; public AnimationCurve occlusionSatisfaction { get { return m_OcclusionSatisfaction; } }
    [Range(0.0f, 1.0f)] [SerializeField] private float m_OcclusionFactorWeight; public float occlusionFactorWeight { get { return m_OcclusionFactorWeight; } }

    [SerializeField] private float m_OptimalFlow;               public float optimalflow { get { return m_OptimalFlow; } }
    [SerializeField] private AnimationCurve m_FlowSatisfaction; public AnimationCurve flowSatisfaction { get { return m_FlowSatisfaction; } }
    [Range(0.0f, 1.0f)] [SerializeField] private float m_FlowFactorWeight; public float flowFactorWeight { get { return m_FlowFactorWeight; } }

    [SerializeField] private float m_OptimalTemperature;        public float optimalTemperature { get { return m_OptimalTemperature; } }
    [SerializeField] private AnimationCurve m_TemperatureSatisfaction; public AnimationCurve temperatureSatisfaction { get { return m_TemperatureSatisfaction; } }
    [Range(0.0f, 1.0f)] [SerializeField] private float m_TemperatureWeight; public float temperatureWeight { get { return m_TemperatureWeight; } }

    // weight of all soil texture parameters after being averaged based on their weights
    [SerializeField] private float m_SoilTextureWeight; public float soilTextureWeight { get { return m_SoilTextureWeight; } }
    
    [SerializeField] private float m_PreferredClayValue; public float preferredClayValue { get { return m_PreferredClayValue; } }
    [SerializeField] private AnimationCurve m_SoilTextureSatisfaction_Clay; public AnimationCurve soilTextureSatisfaction_Clay { get { return m_SoilTextureSatisfaction_Clay; } }
    [Range(0.0f, 1.0f)] [SerializeField] private float m_SoilTextureWeight_Clay; public float soilTextureWeight_Clay { get { return m_SoilTextureWeight_Clay; } }
   
    [SerializeField] private float m_PreferredSiltValue; public float preferredSiltValue { get { return m_PreferredSiltValue; } }
    [SerializeField] private AnimationCurve m_SoilTextureSatisfaction_Silt; public AnimationCurve soilTextureSatisfaction_Silt { get { return m_SoilTextureSatisfaction_Silt; } }
    [Range(0.0f, 1.0f)] [SerializeField] private float m_SoilTextureWeight_Silt; public float soilTextureWeight_Silt { get { return m_SoilTextureWeight_Silt; } }
    
    [SerializeField] private float m_PreferredSandValue; public float preferredSandValue { get { return m_PreferredSandValue; } }
    [SerializeField] private AnimationCurve m_SoilTextureSatisfaction_Sand; public AnimationCurve soilTextureSatisfaction_Sand { get { return m_SoilTextureSatisfaction_Sand; } }
    [Range(0.0f, 1.0f)] [SerializeField] private float m_SoilTextureWeight_Sand; public float soilTextureWeight_Sand { get { return m_SoilTextureWeight_Sand; } }

    [SerializeField] private float m_PreferredAcidityValue; public float preferredAcidityValue { get { return m_PreferredAcidityValue; } }
    [Range(0.0f, 1.0f)] [SerializeField] private float m_SoilAcidityWeight; public float soilAcidityWeight { get { return m_SoilAcidityWeight; } }
    [SerializeField] private AnimationCurve m_SoilAciditySatisfaction; public AnimationCurve soilAciditySatisfaction { get { return m_SoilAciditySatisfaction; } }

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
