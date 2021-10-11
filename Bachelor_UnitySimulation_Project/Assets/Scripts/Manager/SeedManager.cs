using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SeedChunkList
{
    public List<GameObject> m_Seeds;
    public SeedChunkList()
    {
        m_Seeds = new List<GameObject>();
    }
}

public class SeedManager : MonoBehaviour
{

    [SerializeField] private GameObject m_SeedPrefab;
    [SerializeField] private List<SeedChunkList> m_SeedChunkLists; 
    [SerializeField] private int m_InitialOverallSeedAmount;
    private int m_SeedAmountPerTerrainChunk;
    [SerializeField] private Terrain m_Terrain;
    private int m_TerrainDivisionAmount; 

    // Start is called before the first frame update
    void Start()
    {
        m_Terrain                   = gameObject.GetComponent<SimulationManager>().terrain;
        m_TerrainDivisionAmount     = Singletons.simulationManager.terrainDivisionAmount;
        if(m_TerrainDivisionAmount == 0)
        {
            m_SeedAmountPerTerrainChunk = m_InitialOverallSeedAmount;
        }
        else
        {
            m_SeedAmountPerTerrainChunk = m_InitialOverallSeedAmount / ((m_TerrainDivisionAmount + 1) * (m_TerrainDivisionAmount + 1));
        }
        m_SeedChunkLists            = new List<SeedChunkList>();
        ScatterInitialSeedsPerTerrainChunk();
        //ScatterInitialSeeds();
    }

    private void ScatterInitialSeedsPerTerrainChunk()
    {
        //scatter per chunk on specific part of terrain
        //add these seeds to their own list of seeds per chunk

        for(int i = 0; i <= m_TerrainDivisionAmount; i++)
        {
            for(int k = 0; k <= m_TerrainDivisionAmount; k++)
            {
                SeedChunkList seeds = new SeedChunkList();
                float terrainBoundsXMin = m_Terrain.terrainData.bounds.min.x + (m_Terrain.terrainData.size.x / (m_TerrainDivisionAmount + 1.0f)) * i;
                float terrainBoundsXMax = m_Terrain.terrainData.bounds.min.x + (m_Terrain.terrainData.size.x / (m_TerrainDivisionAmount + 1.0f)) * (i + 1.0f);
                float terrainBoundsZMin = m_Terrain.terrainData.bounds.min.z + (m_Terrain.terrainData.size.z / (m_TerrainDivisionAmount + 1.0f)) * k;
                float terrainBoundsZMax = m_Terrain.terrainData.bounds.min.z + (m_Terrain.terrainData.size.z / (m_TerrainDivisionAmount + 1.0f)) * (k + 1.0f);
                if (m_SeedPrefab.GetComponent<PlantSpeciesInfoManager>() == null)
                {
                    Debug.LogError("Seed Prefab has no Species Info Manager");
                    return;
                }

                for (int j = 0; j < m_SeedAmountPerTerrainChunk; j++)
                {
                    GameObject newSeed = Instantiate<GameObject>(m_SeedPrefab);
                    newSeed.SetActive(true);
                    Vector3 randomSpawnPoint = new Vector3(Random.Range(terrainBoundsXMin, terrainBoundsXMax),
                                                            0.0f,
                                                            Random.Range(terrainBoundsZMin, terrainBoundsZMax));
                    randomSpawnPoint.y = m_Terrain.SampleHeight(randomSpawnPoint);
                    newSeed.transform.position = randomSpawnPoint;
                    seeds.m_Seeds.Add(newSeed);
                }

                m_SeedChunkLists.Add(seeds);
            }
        }
    }

    //private void ScatterInitialSeeds(int terrainXPosMultiplier, int terrainZPosMultiplier)
    //{
    //    if(m_SeedPrefab.GetComponent<PlantSpeciesInfoManager>() == null)
    //    {
    //        Debug.LogError("Seed Prefab has no Species Info Manager");
    //        return;
    //    }
    //    for (int i = 0; i < m_SeedAmountPerTerrainChunk; i++)
    //    {
    //        GameObject newSeed = Instantiate<GameObject>(m_SeedPrefab);
    //        Vector3 randomSpawnPoint = new Vector3(Random.Range(m_Terrain.terrainData.bounds.min.x, m_Terrain.terrainData.bounds.max.x),
    //                                                0.0f,
    //                                                Random.Range(m_Terrain.terrainData.bounds.min.z, m_Terrain.terrainData.bounds.max.z));
    //        randomSpawnPoint.y = m_Terrain.SampleHeight(randomSpawnPoint);
    //        newSeed.transform.position = randomSpawnPoint;
    //        m_Seeds.Add(newSeed);
    //    }
    //}



    public void ScatterSeedsAroundPoint(PlantSpeciesInfoManager maturePlant)
    {
        //would have to check for collision usually, but I will add this later
        //take position, take seed radius, take amount of seeds 
        //get random position in radius around position

        for(int i = 0; i < maturePlant.m_OwnSpeciesInfo.seedDistributionAmount; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * maturePlant.m_OwnSpeciesInfo.maxDistributionDistance;
            
            Vector3 calculatedPos = maturePlant.gameObject.transform.position + new Vector3(randomPos.x, 0.0f, randomPos.y);
            calculatedPos.y = m_Terrain.SampleHeight(calculatedPos);
            if(IsCalculatedNewPosWithinTerrainBounds(calculatedPos))
            {
                GameObject newSeed = Instantiate<GameObject>(maturePlant.m_OwnSpeciesInfo.ownSpeciesPrefab);
                newSeed.transform.position = calculatedPos;
               
            }
            
        }
    }

    private bool IsCalculatedNewPosWithinTerrainBounds(Vector3 pos)
    {
        if(pos.x <  m_Terrain.terrainData.bounds.max.x && pos.x > m_Terrain.terrainData.bounds.min.x && pos.z > m_Terrain.terrainData.bounds.min.z && pos.z < m_Terrain.terrainData.bounds.max.z)
        {
            return true;
        }
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
