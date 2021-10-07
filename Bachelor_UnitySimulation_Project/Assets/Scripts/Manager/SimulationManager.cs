using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [SerializeField] private Terrain            m_Terrain;              public Terrain      terrain             { get { return m_Terrain; } }
    [SerializeField] private Texture2D          m_OcclusionMap;         public Texture2D    occlusionMap        { get { return m_OcclusionMap; } }
    [SerializeField] private Texture2D          m_FlowMap;              public Texture2D    flowMap             { get { return m_FlowMap; } }
    [SerializeField] private int                m_TerrainChunkAmount;   public int          terrainChunkAmount  { get { return m_TerrainChunkAmount; } }

    [Header("Simulation Time")]
    [SerializeField] private float m_Timer;
    [SerializeField] private float m_TimeScale;


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
        if(m_Timer >= m_TimeScale)
        {
            m_Timer = 0.0f;
            simulationTick();
        }
    }
}
