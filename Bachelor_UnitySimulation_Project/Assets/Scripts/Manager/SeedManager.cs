using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedManager : MonoBehaviour
{

    [SerializeField] private GameObject m_SeedPrefab;
    [SerializeField] private List<GameObject> m_Seeds;
    [SerializeField] private int m_InitialSeedAmount;
    [SerializeField] private Terrain m_Terrain;

    // Start is called before the first frame update
    void Start()
    {
        m_Terrain = gameObject.GetComponent<SimulationManager>().terrain;
        m_Seeds = new List<GameObject>();
        ScatterInitialSeeds();
    }

    private void ScatterInitialSeeds()
    {
        if(m_SeedPrefab.GetComponent<PlantSpeciesInfoManager>() == null)
        {
            Debug.LogError("Seed Prefab has no Species Info Manager");
            return;
        }
        for (int i = 0; i < m_InitialSeedAmount; i++)
        {
            GameObject newSeed = Instantiate<GameObject>(m_SeedPrefab);
            Vector3 randomSpawnPoint = new Vector3(Random.Range(m_Terrain.terrainData.bounds.min.x, m_Terrain.terrainData.bounds.max.x),
                                                    0.0f,
                                                    Random.Range(m_Terrain.terrainData.bounds.min.z, m_Terrain.terrainData.bounds.max.z));
            randomSpawnPoint.y = m_Terrain.SampleHeight(randomSpawnPoint);
            newSeed.transform.position = randomSpawnPoint;
            m_Seeds.Add(newSeed);
        }
    }



    private void ScatterSeedsAroundPoint(GameObject maturePlant)
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
