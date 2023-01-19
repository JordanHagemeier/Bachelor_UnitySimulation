using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GroundInfoManager : MonoBehaviour
{
    [Header("Soil Maps")]
    [SerializeField] private Texture2D m_FlowMap;
    [SerializeField] private Texture2D m_OcclusionMap;
    [SerializeField] private Texture2D m_ClayMap;
    [SerializeField] private Texture2D m_SandMap;
    [SerializeField] private Texture2D m_SiltMap;
    [SerializeField] private Texture2D m_SoilAcidityMap;

    [Header("Max Values for each Map")]
    [SerializeField] private float m_MaxValueClay;   //solely for debugging purposes
    [SerializeField] private float m_MaxValueSand;   //solely for debugging purposes
    [SerializeField] private float m_MaxValueSilt;   //solely for debugging purposes
    [SerializeField] private float m_MaxPH;          //solely for debugging purposes

    [Header("Terrain")]
    [SerializeField] private Terrain m_Terrain; public Terrain terrain { get { return m_Terrain; } }
                     private Bounds m_TerrainBounds;
    [SerializeField] private Texture2D m_UsableGround;
    [SerializeField] private float m_SeaLevelCutOff;

    [Header("Everything else")]
    [SerializeField] private GameObject m_DebuggingObject;
    [SerializeField] private Material m_DebuggingMaterial;

    [SerializeField] private float m_mapWidth;
    [SerializeField] private float m_mapHeight;
    

    [SerializeField] private GroundInfoStruct[] m_GroundInfoStructArray; public GroundInfoStruct[] groundInfoStructArray { get { return m_GroundInfoStructArray; } }
    private GroundInfoStruct[] m_GroundInfoStructArrayCOPY;
    private bool m_ResetGroundInfoArray = false;
    // Start is called before the first frame update
    void Awake()
    {
        m_GroundInfoStructArray         = new GroundInfoStruct[(int)m_mapWidth * (int)m_mapHeight];
        m_GroundInfoStructArrayCOPY     = new GroundInfoStruct[m_GroundInfoStructArray.Length];
        SetupTerrainBounds();
        SetUpComparisonValuesForMapData();
        CopyAllMapsIntoTheStructArray();
        m_GroundInfoStructArray.CopyTo(m_GroundInfoStructArrayCOPY, 0);

    }

    //private void Start()
    //{
    //    RenderSoilValueToMap();
    //}
  
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

    private void SetupTerrainBounds()
    {
        m_Terrain = Singletons.simulationManager.terrain;
        if (!m_Terrain)
        {
            Debug.Log("Terrain not found.");
        }
        m_TerrainBounds = m_Terrain.terrainData.bounds;
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

    public GroundInfoStruct GetGroundInfoAtPositionOnTerrain(float x, float y)
    {
        //GroundInfoStruct groundInfo;

        //the map is 1024 x 1024
        //get the position of the plant x and y 
        //translation of that position onto a two dimensional position within our width and height for the ground 
        // get index in one dimension
        //lookup ground info in array 

        float xPercentage = x / m_TerrainBounds.max.x;
        float yPercentage = y / m_TerrainBounds.max.z;
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
                float percentageX = i / m_mapWidth;
                int pixelPosXOnMap = (int) (percentageX * m_ClayMap.width);

                float percentageY = j / m_mapHeight;
                int pixelPosYOnMap = (int)(percentageY * m_ClayMap.height);


                int currentInfo = (i * (int)m_mapHeight) + j;
                m_GroundInfoStructArray[currentInfo].posX = pixelPosXOnMap;
                m_GroundInfoStructArray[currentInfo].posY = pixelPosYOnMap;

                //all of these maps have the same size
 
                m_GroundInfoStructArray[currentInfo].clay = (m_ClayMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).r * 256.0f);
                m_GroundInfoStructArray[currentInfo].sand = (m_SandMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).g * 256.0f);
                m_GroundInfoStructArray[currentInfo].silt = (m_SiltMap.GetPixel(pixelPosXOnMap, pixelPosYOnMap).b * 256.0f);
                
             
               
                int pixelPosXOnMapSmall = (int)(percentageX * m_OcclusionMap.width);

               
                int pixelPosYOnMapSmall = (int)(percentageY * m_OcclusionMap.height);

                m_GroundInfoStructArray[currentInfo].terrainOcclusion   = m_OcclusionMap.GetPixel(pixelPosXOnMapSmall, pixelPosYOnMapSmall).r;
                m_GroundInfoStructArray[currentInfo].waterflow          = m_FlowMap.GetPixel(pixelPosXOnMapSmall,pixelPosYOnMapSmall).r;

               
                int pixelPosXOnMapPH = (int)(percentageX * m_SoilAcidityMap.width);
                int pixelPosYOnMapPH = (int)(percentageY * m_SoilAcidityMap.height);
                m_GroundInfoStructArray[currentInfo].ph                 = (m_SoilAcidityMap.GetPixel(pixelPosXOnMapPH, pixelPosYOnMapPH).r * 256.0f);

                m_GroundInfoStructArray[currentInfo].onLand = false;


                float perc = i / m_mapWidth;
                int pixelposX = (int)(perc * m_UsableGround.width);

                perc = j / m_mapHeight;
                int pixelposY = (int)(perc * m_UsableGround.height);
                float landValue = m_UsableGround.GetPixel(pixelposX, pixelposY).r;
                //m_GroundInfoStructArray[currentInfo].DEBUGLandValue = landValue;
                if (landValue > m_SeaLevelCutOff)
                {
                    m_GroundInfoStructArray[currentInfo].onLand = true;
                }
            }
            
        }
        //SetupDebuggingObjects((int)m_mapHeight, (int)m_mapWidth);


    }

    public bool IsOnLand(Vector3 pos)
    {
        return GetGroundInfoAtPositionOnTerrain(pos.x, pos.z).onLand;
    }

    public bool IsOnLand(int x, int y)
    {
        float pixelX = (x / m_mapWidth) * m_UsableGround.width;

        float pixelY = (y / m_mapHeight) * m_UsableGround.height;
        float landValue = m_UsableGround.GetPixel((int)pixelX, (int)pixelY).r;
        //m_GroundInfoStructArray[currentInfo].DEBUGLandValue = landValue;
        if (landValue > m_SeaLevelCutOff)
        {
            return true;
        }
        return false;
    }

    private void RenderSoilValueToMap()
    {
        Color[] allGroundColors = new Color[(int)m_mapWidth * (int)m_mapHeight];
        for (int iY = 0; iY < m_mapHeight; iY++)
        {
            for (int iX = 0; iX < m_mapWidth; iX++)
            {
                if ( IsOnLand(iX,  iY))
                {
                    int index = GetIndex1D(iX, iY);
                    allGroundColors[iY * (int)m_mapWidth + iX] = new Color(m_GroundInfoStructArray[index].sand / 256.0f, m_GroundInfoStructArray[index].sand / 256.0f, m_GroundInfoStructArray[index].sand / 256.0f);
                    
                }
                else
                {
                    allGroundColors[iY * (int)m_mapWidth + iX] = Color.grey;
                }
            }
        }

        
 
        //2) fill map with array content
        Texture2D allGroundValuesMap = new Texture2D((int)m_mapWidth, (int)m_mapHeight);
        allGroundValuesMap.SetPixels(0, 0, (int)m_mapWidth, (int)m_mapHeight, allGroundColors);

        byte[] groundInfoBytes = allGroundValuesMap.EncodeToPNG();
        string dirPath = Application.dataPath + "/../Generated/";
        string timeString = System.DateTime.Now.Day + "-" + System.DateTime.Now.Month + "_" + System.DateTime.Now.Hour + "-" + System.DateTime.Now.Minute;
        string filePath = dirPath + "groundSandValues" + "_time" + timeString + ".png";

        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(filePath, groundInfoBytes);
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
            
            // row is [0; height -1]
            // column is [0; width -1]
            float xPercentage = 1.0f / (width - 1) * column; // col / (width -1)
            float yPercentage = 1.0f / (height - 1) * row;

            currentDebuggingObject.GetComponent<Renderer>().material.color = new Color(m_GroundInfoStructArray[i].ph, m_GroundInfoStructArray[i].ph, m_GroundInfoStructArray[i].ph);
       
        }
    }

    private void SetUpComparisonValuesForMapData()
    {
        m_MaxValueClay = ReadMaxValueFromPixel(m_ClayMap);
        m_MaxValueSand = ReadMaxValueFromPixel(m_SandMap);
        m_MaxValueSilt = ReadMaxValueFromPixel(m_SiltMap);
        m_MaxPH        = ReadMaxValueFromPixel(m_SoilAcidityMap);
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

        //Debug.Log("px, highest256: " + maxValue * 256.0f);
        return maxValue * 256.0f;
        
    }

}
