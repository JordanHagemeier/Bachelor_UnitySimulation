using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CopiedPlantInfo
{
    public Matrix4x4 renderData;
    public Material material;
    public Color color;
}

public class VisualizationManager : MonoBehaviour
{
    private Dictionary<int, GameObject> IDToPlantsGOMap;
    SeparatedSimulationManager m_SimManager;
    [SerializeField] private Mesh m_InstancedMesh;
    //[SerializeField] private Material m_InstancedMaterial;
    //[SerializeField] private Material m_TypeAInstancedMaterial;
    //[SerializeField] private Material m_TypeBInstancedMaterial;

    private bool m_PlantsHaveBeenUpdated = false;
    private PlantInfoStruct[] m_CopiedPlants; public PlantInfoStruct[] copiedPlants { set { m_CopiedPlants = value; m_PlantsHaveBeenUpdated = true; } }
    [SerializeField] private bool m_ShowVisualization = true;

    PlantSpeciesTable m_PlantSpeciesTable; 
    List<List<CopiedPlantInfo>> m_ListOfMatrixArrays;
    [SerializeField] private float m_ScaleFactor;

    // Start is called before the first frame update
    void Start()
    {
        m_ListOfMatrixArrays = new List<List<CopiedPlantInfo>>();
        for(int i = 0; i < (int)PlantType.Count; i++)
        {
            
            List<CopiedPlantInfo> plantDataList = new List<CopiedPlantInfo>();
            m_ListOfMatrixArrays.Add(plantDataList);
        }
        m_SimManager = gameObject.GetComponent<SeparatedSimulationManager>();
        m_PlantSpeciesTable = m_SimManager.plantSpeciesTable;
    }


    private void VisualizePlants()
    {
        
    }

    private bool SortCopiedPlantsIntoMatrixLists()
    {
        m_ListOfMatrixArrays = new List<List<CopiedPlantInfo>>();
        for (int i = 0; i < (int)PlantType.Count; i++)
        {
            List<CopiedPlantInfo> plantDataList = new List<CopiedPlantInfo>();
            m_ListOfMatrixArrays.Add(plantDataList);
        }
        //I don't need to know the exact enums, I just have to sort them
        for (int i = 0; i < m_CopiedPlants.Length; i++)
        {
            CopiedPlantInfo info    = new CopiedPlantInfo();
            float agePercentage     = m_CopiedPlants[i].age / m_PlantSpeciesTable.GetSOByType(m_CopiedPlants[i].type).maturityAge;
            float growthByAge       = agePercentage * m_CopiedPlants[i].health;
            float growthWithSizeLimit = Mathf.Lerp(0.0f, m_PlantSpeciesTable.GetSOByType(m_CopiedPlants[i].type).maxSize, growthByAge);
            info.renderData         = Matrix4x4.Translate(m_CopiedPlants[i].position);
            info.renderData         = info.renderData * Matrix4x4.Scale(new Vector3(m_CopiedPlants[i].health, m_CopiedPlants[i].health, m_CopiedPlants[i].health) * m_CopiedPlants[i].age * growthWithSizeLimit * m_ScaleFactor);
            info.material           = m_PlantSpeciesTable.GetSOByType(m_CopiedPlants[i].type).ownMaterial;
            info.color              = m_PlantSpeciesTable.GetSOByType(m_CopiedPlants[i].type).typeColor;

            m_ListOfMatrixArrays[(int)m_CopiedPlants[i].type].Add(info);
        }
        return true;
    }

    private void SendPlantListsToRenderByType()
    {
        for(int i = 0; i < m_ListOfMatrixArrays.Count; i++)
        {
            if(m_ListOfMatrixArrays[i].Count > 0)
            {
                List<CopiedPlantInfo> currentList   = m_ListOfMatrixArrays[i];

                currentList[0].material.color = currentList[0].color;
                Matrix4x4[] matrices                = new Matrix4x4[currentList.Count];
                for(int j = 0; j < matrices.Length; j++)
                {
                    matrices[j] = currentList[j].renderData;
                }
                CreateAndDrawInstancedMeshes((int) matrices.Length/1023, matrices, currentList[0].material);
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (m_PlantsHaveBeenUpdated && m_ShowVisualization)
        {
            m_PlantsHaveBeenUpdated = false;
           
            SortCopiedPlantsIntoMatrixLists();
            
        }
        SendPlantListsToRenderByType();
       
    }

    private void CreateAndDrawInstancedMeshes(int iterations, Matrix4x4[] positions, Material instanceMaterial)
    {

        //create a bunch of matrices arrays with the information about how many can fit into the length of the  copied position matrices array
        List<Matrix4x4[]> listOfMatrices = new List<Matrix4x4[]>();
        for(int i = 0; i < iterations; i++)
        {
            Matrix4x4[] matrices = new Matrix4x4[1023];
            System.Array.Copy(positions, 1023 * i, matrices, 0, 1023);
            listOfMatrices.Add(matrices);
        }

        // an additional array for the remaining (below 1023) positions
        int rest = 0;
        if (iterations == 0)
        {
            rest = positions.Length % 1023;

        }
        else
        {
            rest = positions.Length % (1023 * iterations);
        }
        Matrix4x4[] restMatrices = new Matrix4x4[rest];
        System.Array.Copy(positions, 1023 * iterations, restMatrices, 0, rest);
        listOfMatrices.Add(restMatrices);

        for(int k = 0; k < listOfMatrices.Count; k++)
        {
            Graphics.DrawMeshInstanced(m_InstancedMesh, 0, instanceMaterial, listOfMatrices[k]);
        }
        
    }
}
