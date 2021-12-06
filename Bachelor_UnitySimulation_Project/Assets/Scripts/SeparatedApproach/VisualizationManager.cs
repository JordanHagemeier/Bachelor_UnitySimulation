using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizationManager : MonoBehaviour
{
    private Dictionary<int, GameObject> IDToPlantsGOMap;
    SeparatedSimulationManager m_SimManager;
    [SerializeField] private Mesh m_InstancedMesh;
    [SerializeField] private Material m_InstancedMaterial;
    private bool m_PlantsHaveBeenUpdated = false;
    [SerializeField] private PlantInfoStruct[] m_CopiedPlants; public PlantInfoStruct[] copiedPlants { set { m_CopiedPlants = value; m_PlantsHaveBeenUpdated = true; } }
    Matrix4x4[] positionMatricesArray;
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
            positionMatricesArray = new Matrix4x4[m_CopiedPlants.Length];
            for (int i = 0; i < m_CopiedPlants.Length; i++)
            {
                positionMatricesArray[i] = Matrix4x4.Translate(m_CopiedPlants[i].position);
                positionMatricesArray[i] = positionMatricesArray[i] * Matrix4x4.Scale(new Vector3(m_CopiedPlants[i].health, m_CopiedPlants[i].health, m_CopiedPlants[i].health) * m_ScaleFactor);

            }
        }
        if(positionMatricesArray != null)
        {
            if(positionMatricesArray.Length < 1024 && positionMatricesArray.Length > 0)
            {
                Graphics.DrawMeshInstanced(m_InstancedMesh, 0, m_InstancedMaterial, positionMatricesArray);

            }    
            if(positionMatricesArray.Length >= 1024)
            {
                //Matrix4x4[] otherPositions = new Matrix4x4[1023];
                //System.Array.Copy(positionMatricesArray, 1023, otherPositions, 0, positionMatricesArray.Length - 1023);
                //Graphics.DrawMeshInstanced(m_InstancedMesh, 0, m_InstancedMaterial, otherPositions);
                CreateAndDrawInstancedMeshes((int)(positionMatricesArray.Length - 1023) / 1023  );
            }
        }
       
    }

    private void CreateAndDrawInstancedMeshes(int iterations)
    {

        //create a bunch of matrices arrays with the information about how many can fit into the length of the  copied position matrices array
        List<Matrix4x4[]> listOfMatrices = new List<Matrix4x4[]>();
        for(int i = 0; i < iterations; i++)
        {
            Matrix4x4[] matrices = new Matrix4x4[1023];
            System.Array.Copy(positionMatricesArray, 1023 * (i + 1), matrices, 0, 1023);
            listOfMatrices.Add(matrices);
        }

        // an additional array for the remaining (below 1023) positions
        int rest = positionMatricesArray.Length % (1023 * (iterations + 1));
        Matrix4x4[] restMatrices = new Matrix4x4[rest];
        listOfMatrices.Add(restMatrices);

        for(int k = 0; k < listOfMatrices.Count; k++)
        {
            Graphics.DrawMeshInstanced(m_InstancedMesh, 0, m_InstancedMaterial, listOfMatrices[k]);
        }
        
    }
}
