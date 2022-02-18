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

    [SerializeField] private float m_MaxValueClay;
    [SerializeField] private float m_MaxValueSand;
    [SerializeField] private float m_MaxValueSilt;

    [SerializeField] private Texture2D m_LandMass;
    [SerializeField] private float m_SeaLevelCutOff;

    [SerializeField] private GameObject m_DebuggingObject;
    [SerializeField] private Material m_DebuggingMaterial;

    [SerializeField] private float m_mapWidth;
    [SerializeField] private float m_mapHeight;
    

    [SerializeField] private GroundInfoStruct[] m_GroundInfoStructArray; public GroundInfoStruct[] groundInfoStructArray { get { return m_GroundInfoStructArray; } }
    private GroundInfoStruct[] m_GroundInfoStructArrayCOPY;
    private bool m_ResetGroundInfoArray = false;
    // Start is called before the first frame update
    void Start()
    {
        m_GroundInfoStructArray         = new GroundInfoStruct[(int)m_mapWidth * (int)m_mapHeight];
        m_GroundInfoStructArrayCOPY     = new GroundInfoStruct[m_GroundInfoStructArray.Length];
        SetUpComparisonValuesForMapData();
        CopyAllMapsIntoTheStructArray();
        m_GroundInfoStructArray.CopyTo(m_GroundInfoStructArrayCOPY, 0);

    }

    // Update is called once per frame
    void Update()
    {
        if (m_ResetGroundInfoArray)
        {
            m_ResetGroundInfoArray = false;
            m_GroundInfoStructArrayCOPY.CopyTo(m_GroundInfoStructArray, 0);
        }
    }

    public void ResetGroundInfoArray()
    {
        m_ResetGroundInfoArray = true;
    }

    Vector2Int GetIndex2D(int index1D, int width)
    {
        int row = index1D / width;
        int column = index1D % width;
        return new Vector2Int(row, column);
    }

    public int GetIndex1D(int iX, int iY)
    {
        return (iX * (int)m_mapWidth) + iY;
    }

    public GroundInfoStruct GetGroundInfoAtPositionOnTerrain(float x, float y, Vector2 terrainBounds)
    {
        GroundInfoStruct groundInfo;

        //the map is 1024 x 1024
        //we get the position of the plant x and y 
        //translation of that position onto a two dimensional position within our width and height for the ground 
        // get index in one dimension
        //lookup ground info in array 

        float xPercentage = x / terrainBounds.x;
        float yPercentage = y / terrainBounds.y;
        Vector2Int positionInGroundGrid = new Vector2Int(Mathf.FloorToInt(xPercentage * m_mapWidth), Mathf.FloorToInt(yPercentage * m_mapHeight));
        int arrayPosition = GetIndex1D(positionInGroundGrid.x, positionInGroundGrid.y);

        return m_GroundInfoStructArray[arrayPosition];
    }

    

    private void CopyAllMapsIntoTheStructArray()
    {
        
        for (int i = 0; i < m_mapWidth; i++)
        {
            for(int j = 0; j < m_mapHeight; j++)
            {
                float percentage = i / m_mapWidth;
                int pixelPosXOnMap = (int) (percentage * m_ClayMap.width);

                percentage = j / m_mapHeight;
                int pixelPosYOnMap = (int)(percentage * m_ClayMap.height);


                int currentInfo = (i * (int)m_mapHeight) + j;
                m_GroundInfoStructArray[currentInfo].posX = pixelPosXOnMap;
                m_GroundInfoStructArray[currentInfo].posY = pixelPosYOnMap;

                //all of these maps have the same size
 
                m_GroundInfoStructArray[currentInfo].clay = (m_ClayMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).r * 256.0f) / m_MaxValueClay;
                m_GroundInfoStructArray[currentInfo].sand = (m_SandMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).g * 256.0f) / m_MaxValueSand;
                m_GroundInfoStructArray[currentInfo].silt = (m_SiltMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).b * 256.0f) / m_MaxValueSilt;
                
                
                //these maps should be smaller and need new pixel positions
                //TODO
                float percentageSmall = i / m_mapWidth;
                int pixelPosXOnMapSmall = (int)(percentageSmall * m_OcclusionMap.width);

                percentageSmall = j / m_mapHeight;
                int pixelPosYOnMapSmall = (int)(percentageSmall * m_OcclusionMap.height);

                m_GroundInfoStructArray[currentInfo].terrainOcclusion   = m_OcclusionMap.GetPixel(pixelPosXOnMapSmall, pixelPosYOnMapSmall).r;
                m_GroundInfoStructArray[currentInfo].waterflow          = m_FlowMap.GetPixel(pixelPosXOnMapSmall,pixelPosYOnMapSmall).r;

                m_GroundInfoStructArray[currentInfo].onLand = false;


                float perc = i / m_mapWidth;
                int pixelposX = (int)(perc * m_LandMass.width);

                perc = j / m_mapHeight;
                int pixelposY = (int)(perc * m_LandMass.height);
                float landValue = m_LandMass.GetPixel(pixelposX, pixelposY).r;
                m_GroundInfoStructArray[currentInfo].DEBUGLandValue = landValue;
                if (landValue > m_SeaLevelCutOff)
                {
                    m_GroundInfoStructArray[currentInfo].onLand = true;
                }
                //Debug.Log(currentInfo);
            }
            
        }
        SetupDebuggingObjects((int)m_mapHeight, (int)m_mapWidth);


    }

   

    private void SetupDebuggingObjects(int height, int width)
    {
        for (int i = 0; i < m_GroundInfoStructArray.Length; i++)
        {
            //tilesPerColumn    = amount of rows
            //tilesPerRow       = amount of columns




            int row = i / width;
            int column = i % width;

            //int fakeI = width * row + column;

            GameObject currentDebuggingObject = Instantiate(m_DebuggingObject);
            currentDebuggingObject.transform.position = new Vector3(m_GroundInfoStructArray[i].posX / 7.0f, 20.0f, m_GroundInfoStructArray[i].posY / 7.0f);
            currentDebuggingObject.GetComponent<Renderer>().material = m_DebuggingMaterial;
            //currentDebuggingObject.GetComponent<Renderer>().material.color = Color.blue;
            //currentDebuggingObject.GetComponent<Renderer>().material.color = new Color(((1.0f / width) * row), 0.0f, ((1.0f / height) * column), 1.0f);
            //Debug.Log("r " + ((1.0f / width) * row) + ", b " + ((1.0f / height) * column) + " at " + row + "/" + column);


            // row is [0; height -1]
            // column is [0; width -1]
            float xPercentage = 1.0f / (width - 1) * column; // col / (width -1)
            float yPercentage = 1.0f / (height - 1) * row;

            currentDebuggingObject.GetComponent<Renderer>().material.color = new Color(m_GroundInfoStructArray[i].waterflow, m_GroundInfoStructArray[i].waterflow, m_GroundInfoStructArray[i].waterflow);
            //currentDebuggingObject.GetComponent<Renderer>().material.color = new Color(xPercentage, 0.0f, yPercentage, 1.0f);
            //Debug.Log("r " + xPercentage + ", b " + yPercentage + " at " + row + "/" + column);

        }
    }

    private void SetUpComparisonValuesForMapData()
    {
        m_MaxValueClay = ReadMaxValueFromPixel(m_ClayMap);
        m_MaxValueSand = ReadMaxValueFromPixel(m_SandMap);
        m_MaxValueSilt = ReadMaxValueFromPixel(m_SiltMap);
    }

    private float ReadMaxValueFromPixel(Texture2D texture)
    {
        
        //-------------------------------------------------------------
        float maxValue = 0.0f;
        var pixels = texture.GetPixels();
        foreach (var pixel in pixels)
        {
            if (pixel.r > maxValue)
            {
                maxValue = pixel.r;
            }
        }

        Debug.Log("px, highest256: " + maxValue * 256.0f);
        return maxValue * 256.0f;
        
    }

}
