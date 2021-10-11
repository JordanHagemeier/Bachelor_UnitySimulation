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

    [SerializeField] private List<GameObject> m_SeedPrefabs;
    [SerializeField] private List<GameObject> m_AllPlants; 
    [SerializeField] private List<SeedChunkList> m_SeedChunkLists; 
    [SerializeField] private int m_InitialOverallSeedAmount;
    public int m_SeedAmountPerTerrainChunk;
    [SerializeField] private Terrain m_Terrain;
    private int m_TerrainDivisionAmount;
    [SerializeField] private List<GameObject> m_PooledEmptyGOs;

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
        m_AllPlants                 = new List<GameObject>();

        for(int i = 0; i < m_PooledEmptyGOs.Count; i++)
        {
            m_PooledEmptyGOs[i] = Instantiate(m_SeedPrefabs[0]);
            m_PooledEmptyGOs[i].SetActive(false);
        }
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
                for(int j = 0; j < m_SeedPrefabs.Count; j++)
                {
                    if (m_SeedPrefabs[j].GetComponent<PlantSpeciesInfoManager>() == null)
                    {
                        Debug.LogError("Seed Prefab has no Species Info Manager");
                        return;
                    }
                }
                

                for (int l = 0; l <= m_SeedAmountPerTerrainChunk; l++)
                {
                    for(int m = 0; m < m_SeedPrefabs.Count; m++)
                    {
                        m_AllPlants.Add(InstantiatePrefab(m_SeedPrefabs[m], terrainBoundsXMin, terrainBoundsXMax, terrainBoundsZMin, terrainBoundsZMax, seeds));
                    }
                    
                }

                //m_SeedChunkLists.Add(seeds);
            }
        }
    }

    private GameObject FindFirstInactivePooledGO()
    {
        for (int i = 0; i < m_PooledEmptyGOs.Count; i++)
        {
            if (m_PooledEmptyGOs[i].activeSelf == false)
            {
                return m_PooledEmptyGOs[i];
            }
        }
        return null;
    }
    private GameObject InstantiatePrefab(GameObject prefab, float terrainBoundsXMin, float terrainBoundsXMax, float terrainBoundsZMin, float terrainBoundsZMax, SeedChunkList seeds)
    {
        GameObject newSeed = GetPooledGameObjectWithPrefabDataSet(prefab);

        Vector3 randomSpawnPoint = new Vector3(Random.Range(terrainBoundsXMin, terrainBoundsXMax),
                                                0.0f,
                                                Random.Range(terrainBoundsZMin, terrainBoundsZMax));
        randomSpawnPoint.y = m_Terrain.SampleHeight(randomSpawnPoint);
        newSeed.transform.position = randomSpawnPoint;
        return newSeed;
        //seeds.m_Seeds.Add(newSeed);
    }


    private GameObject GetPooledGameObjectWithPrefabDataSet(GameObject prefab)
    {
        GameObject newSeed = FindFirstInactivePooledGO();

        if (newSeed == null)
        {
            Debug.LogWarning("All pooled gameobjects are in use");
            return null;
        }

        newSeed.SetActive(true);
        if (newSeed.GetComponent<PlantSpeciesInfoManager>() == null)
        {
            newSeed.AddComponent<PlantSpeciesInfoManager>();
        }
        newSeed.GetComponent<PlantSpeciesInfoManager>().m_OwnSpeciesInfo    = prefab.GetComponent<PlantSpeciesInfoManager>().m_OwnSpeciesInfo;
        newSeed.GetComponent<MeshFilter>().mesh                             = prefab.GetComponent<MeshFilter>().mesh;
        newSeed.GetComponent<MeshRenderer>().material                       = prefab.GetComponent<MeshRenderer>().material;
        newSeed.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
        return newSeed;
    }
    


    public void ScatterSeedsAroundPoint(PlantSpeciesInfoManager maturePlant)
    {
        //would have to check for collision usually, but I will add this later
        //take position, take seed radius, take amount of seeds 
        //get random position in radius around position
        
        //SeedChunkList seeds = new SeedChunkList();
        for(int i = 0; i < maturePlant.m_OwnSpeciesInfo.seedDistributionAmount; i++)
        {
            Vector2 randomPos = Random.insideUnitCircle * maturePlant.m_OwnSpeciesInfo.maxDistributionDistance;
            
            Vector3 calculatedPos = maturePlant.gameObject.transform.position + new Vector3(randomPos.x, 0.0f, randomPos.y);
            calculatedPos.y = m_Terrain.SampleHeight(calculatedPos);
            if(IsCalculatedNewPosWithinTerrainBounds(calculatedPos))
            {
                GameObject newSeed = GetPooledGameObjectWithPrefabDataSet(maturePlant.gameObject);
                if(newSeed == null)
                {
                    Debug.Log("No more inactive Gos left");
                    return;
                }
                newSeed.transform.position = calculatedPos;
                m_AllPlants.Add(newSeed);
                //seeds.m_Seeds.Add(newSeed);
            }
            
        }
        //m_SeedChunkLists.Add(seeds);
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


    private void ComparePlants()
    {

    }
}
