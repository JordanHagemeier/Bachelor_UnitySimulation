using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundInfoManager : MonoBehaviour
{
    [SerializeField] private Texture2D m_FlowMap;
    [SerializeField] private Texture2D m_OcclusionMap;
    [SerializeField] private Texture2D m_ClayMap;
    [SerializeField] private Texture2D m_SandMap;
    [SerializeField] private Texture2D m_SiltMap;


    [SerializeField] private GroundInfoStruct[] m_GroundInfoStructArray; public GroundInfoStruct[] groundInfoStructArray { get { return m_GroundInfoStructArray; } }
    // Start is called before the first frame update
    void Start()
    {
        m_GroundInfoStructArray = new GroundInfoStruct[10 * 10];
        CopyAllMapsIntoTheStructArray();
    }

    // Update is called once per frame
    void Update()
    {
      
    }

    private void CopyAllMapsIntoTheStructArray()
    {
        float columnLength = 10.0f;
        float rowLength = 10.0f;
        for (int i = 0; i < columnLength; i++)
        {
            for(int j = 0; j < rowLength; j++)
            {
                float percentage = i / rowLength;
                int pixelPosXOnMap = (int) (percentage * m_ClayMap.width);

                percentage = j / columnLength;
                int pixelPosYOnMap = (int)(percentage * m_ClayMap.height);


                int currentInfo = (i * (int)columnLength) + j;
                m_GroundInfoStructArray[currentInfo].clay = m_ClayMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).r * 256.0f;
                m_GroundInfoStructArray[currentInfo].sand = m_SandMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).r * 256.0f;
                m_GroundInfoStructArray[currentInfo].silt = m_SiltMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).r * 256.0f;
                m_GroundInfoStructArray[currentInfo].terrainOcclusion = m_OcclusionMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).r * 256.0f;
                m_GroundInfoStructArray[currentInfo].waterflow = m_FlowMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).r * 256.0f;

                Debug.Log(currentInfo);
            }

 

            
        }
    }

   
}
