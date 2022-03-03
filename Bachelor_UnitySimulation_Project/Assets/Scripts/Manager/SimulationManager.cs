using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [SerializeField] private Terrain            m_Terrain;                  public Terrain      terrain             { get { return m_Terrain; } }
    [SerializeField] private Texture2D          m_OcclusionMap;             public Texture2D    occlusionMap        { get { return m_OcclusionMap; } }
    [SerializeField] private Texture2D          m_FlowMap;                  public Texture2D    flowMap             { get { return m_FlowMap; } }
    [SerializeField] private Texture2D          m_TestMap;                  public Texture2D    testMap             { get { return m_TestMap; } }
    [SerializeField] private Material           m_TerrainMaterial;          public Material     terrainMaterial     { get { return m_TerrainMaterial; } }
    [SerializeField] private int                m_TerrainDivisionAmount;    public int          terrainDivisionAmount  { get { return m_TerrainDivisionAmount; } }
    [SerializeField] private float              m_GroundTemperature;        public float        groundTemperature { get { return m_GroundTemperature; } }
    [SerializeField] private float              m_HeightScalingFactor;      public float heightScalingFactor { get { return m_HeightScalingFactor; } }

    

    [Header("Simulation Time")]
    [SerializeField] private float m_Timer;
    [SerializeField] private float m_TimeScale;
    private float m_TimeTillNextSimTick; public float timeTillNextSimTick { get { return m_TimeTillNextSimTick; } }
    public float m_TimeTillNextSimTickPercentage; 

    public delegate void SimulationTick();
    public static SimulationTick simulationTick;

    public int m_ActivePlants = 0;


    // Start is called before the first frame update
    void Start()
    {
        m_TestMap = (Texture2D)m_TerrainMaterial.mainTexture;
    }

    
    // Update is called once per frame
    void Update()
    {
       
    }

   
}
