using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizationManager : MonoBehaviour
{
    private Dictionary<int, GameObject> IDToPlantsGOMap;
    SeparatedSimulationManager m_SimManager;
    [SerializeField] private Mesh m_InstancedMesh;
    [SerializeField] private Material m_InstancedMaterial;
    [SerializeField] private Material m_TypeAInstancedMaterial;
    [SerializeField] private Material m_TypeBInstancedMaterial;

    private bool m_PlantsHaveBeenUpdated = false;
    private PlantInfoStruct[] m_CopiedPlants; public PlantInfoStruct[] copiedPlants { set { m_CopiedPlants = value; m_PlantsHaveBeenUpdated = true; } }
    Matrix4x4[] positionMatricesArray;
    Matrix4x4[] typeAPlantsMatArray;
    Matrix4x4[] typeBPlantsMatArray;
    [SerializeField] private float m_ScaleFactor;

    // Start is called before the first frame update
    void Start()
    {
        m_SimManager = gameObject.GetComponent<SeparatedSimulationManager>();
    }


    private void VisualizePlants()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        if (m_PlantsHaveBeenUpdated)
        {
            m_PlantsHaveBeenUpdated = false;
            typeAPlantsMatArray = new Matrix4x4[m_CopiedPlants.Length];
            typeBPlantsMatArray = new Matrix4x4[m_CopiedPlants.Length];
            for (int i = 0; i < m_CopiedPlants.Length; i++)
            {
                if(m_CopiedPlants[i].type == PlantType.TestPlantA)
                {
                    typeAPlantsMatArray[i] = Matrix4x4.Translate(m_CopiedPlants[i].position);
                    typeAPlantsMatArray[i] = typeAPlantsMatArray[i] * Matrix4x4.Scale(new Vector3(m_CopiedPlants[i].health, m_CopiedPlants[i].health, m_CopiedPlants[i].health) * m_CopiedPlants[i].age * m_ScaleFactor);
                }

                if(m_CopiedPlants[i].type == PlantType.TestPlantB)
                {
                    typeBPlantsMatArray[i] = Matrix4x4.Translate(m_CopiedPlants[i].position);
                    typeBPlantsMatArray[i] = typeBPlantsMatArray[i] * Matrix4x4.Scale(new Vector3(m_CopiedPlants[i].health, m_CopiedPlants[i].health, m_CopiedPlants[i].health) * m_CopiedPlants[i].age * m_ScaleFactor);
                }
                //positionMatricesArray[i] = Matrix4x4.Translate(m_CopiedPlants[i].position);
                //positionMatricesArray[i] = positionMatricesArray[i] * Matrix4x4.Scale(new Vector3(m_CopiedPlants[i].health, m_CopiedPlants[i].health, m_CopiedPlants[i].health) * m_ScaleFactor);

            }
        }
        if(typeAPlantsMatArray != null | typeBPlantsMatArray != null)
        {
            if(typeAPlantsMatArray.Length < 1024 && typeAPlantsMatArray.Length > 0)
            {
                Graphics.DrawMeshInstanced(m_InstancedMesh, 0, m_TypeAInstancedMaterial, typeAPlantsMatArray);
                

            }

            if (typeBPlantsMatArray.Length < 1024 && typeBPlantsMatArray.Length > 0)
            {
                Graphics.DrawMeshInstanced(m_InstancedMesh, 0, m_TypeBInstancedMaterial, typeBPlantsMatArray);
               

            }
            if (typeAPlantsMatArray.Length >= 1024)
            {
               
                CreateAndDrawInstancedMeshes((int)(typeAPlantsMatArray.Length - 1023) / 1023, typeAPlantsMatArray, m_TypeAInstancedMaterial);
            }
            if (typeBPlantsMatArray.Length >= 1024)
            {

                CreateAndDrawInstancedMeshes((int)(typeBPlantsMatArray.Length - 1023) / 1023, typeBPlantsMatArray, m_TypeBInstancedMaterial);
            }
        }
       
    }

    private void CreateAndDrawInstancedMeshes(int iterations, Matrix4x4[] positions, Material instanceMaterial)
    {

        //create a bunch of matrices arrays with the information about how many can fit into the length of the  copied position matrices array
        List<Matrix4x4[]> listOfMatrices = new List<Matrix4x4[]>();
        for(int i = 0; i < iterations; i++)
        {
            Matrix4x4[] matrices = new Matrix4x4[1023];
            System.Array.Copy(positions, 1023 * (i + 1), matrices, 0, 1023);
            listOfMatrices.Add(matrices);
        }

        // an additional array for the remaining (below 1023) positions
        int rest = positions.Length % (1023 * (iterations + 1));
        Matrix4x4[] restMatrices = new Matrix4x4[rest];
        listOfMatrices.Add(restMatrices);

        for(int k = 0; k < listOfMatrices.Count; k++)
        {
            Graphics.DrawMeshInstanced(m_InstancedMesh, 0, instanceMaterial, listOfMatrices[k]);
        }
        
    }
}
