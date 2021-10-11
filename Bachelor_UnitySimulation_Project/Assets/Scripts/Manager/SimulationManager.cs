using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [SerializeField] private Terrain            m_Terrain;                  public Terrain      terrain             { get { return m_Terrain; } }
    [SerializeField] private Texture2D          m_OcclusionMap;             public Texture2D    occlusionMap        { get { return m_OcclusionMap; } }
    [SerializeField] private Texture2D          m_FlowMap;                  public Texture2D    flowMap             { get { return m_FlowMap; } }
    [SerializeField] private int                m_TerrainDivisionAmount;    public int          terrainDivisionAmount  { get { return m_TerrainDivisionAmount; } }
    [SerializeField] private float              m_GroundTemperature;        public float        groundTemperature { get { return m_GroundTemperature; } }
    [SerializeField] private float              m_HeightScalingFactor;      public float heightScalingFactor { get { return m_HeightScalingFactor; } }
    //TODO Mach die Chunks beim Seed Manager rein! Chunks, in denen eine bestimmte Anzahl an Seeds gespawned wird und mit denen du dann Listen füllen kannst, um nicht immer 
    //beim check auf "ist das im Radius von mir" gegen alle x tausend Seeds/Plants testen zu müssen!

    [Header("Simulation Time")]
    [SerializeField] private float m_Timer;
    [SerializeField] private float m_TimeScale;
    private float m_TimeTillNextSimTick; public float timeTillNextSimTick { get { return m_TimeTillNextSimTick; } }
    public float m_TimeTillNextSimTickPercentage; 

    public delegate void SimulationTick();
    public static SimulationTick simulationTick;


    // Start is called before the first frame update
    void Start()
    {
       
    }

    
    // Update is called once per frame
    void Update()
    {
        m_Timer += Time.deltaTime;
        m_TimeTillNextSimTick = m_TimeScale - m_Timer;
        m_TimeTillNextSimTickPercentage = m_Timer / m_TimeScale;
        if(m_TimeTillNextSimTick <= 0.0f)
        {
            m_Timer = 0.0f;
            m_TimeTillNextSimTick = m_TimeScale;
            simulationTick();
        }
    }
}
