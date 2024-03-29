using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;


public enum PlantType
{
    //TestPlantA,
    //TestPlantB,
    //Abies,
    Alnus,
    Betula,
    Carpinus,
    Castanea,
    //Conifers,
    Eucalyptus,
    Fagus,
    Fraxinus,
    Larix,
    //Broadleaved,
    //PinusMisc,
    //QuercusMisc,
    Picea,
    PinusPinaster,
    PinusSylestris,
    Populus,
    //PseudotsugaMenziesii,
    QuercusRoburEtPetraea,
    Robinia,
    //Debug,
    Count
}

public class PlantSpeciesTable
{
    PlantSpeciesInfoScriptableObject crashObject;
    public Dictionary<PlantType, PlantSpeciesInfoScriptableObject> typeToSO = new Dictionary<PlantType, PlantSpeciesInfoScriptableObject>(); 
    public void AddToDictionary(PlantType type, PlantSpeciesInfoScriptableObject SO)
    {
        typeToSO.Add(type, SO);
    }
    public bool IsEmpty()
    {
        return typeToSO.Count == 0;
    }
    public PlantSpeciesInfoScriptableObject GetSOByType(PlantType type)
    {
        if (typeToSO.ContainsKey(type))
        {
            return typeToSO[type];

        }
        Debug.Log("Table does not contain key.");
        return crashObject;
    }
    public bool PlantWeightsAreSet(PlantType type)
    {
        PlantSpeciesInfoScriptableObject plantInfo = GetSOByType(type);
        if (plantInfo.temperatureWeight == 0.0f || plantInfo.occlusionFactorWeight == 0.0f || plantInfo.flowFactorWeight == 0.0f || plantInfo.soilTextureWeight == 0.0f || plantInfo.soilAcidityWeight == 0.0f)
        {
            return false;
        }
        return true;
    }
}

[System.Serializable]
public class PlantInfoStruct 
{
   
    public Vector3 position;
    [XmlAttribute("Age")]
    public int age;
    [XmlAttribute("Health")]
    public float health; // from 0 to 1


    public int id;
    public PlantType type;
    public RectInt affectedCells;
    

    public PlantInfoStruct(Vector3 POS, int ID, PlantSpeciesInfoScriptableObject plantSpecies)
    {
        position    = POS;
        age         = 0;
        health      = 0.0f;
        id          = ID;
        type        = plantSpecies.plantType;
        
    }

    public PlantInfoStruct()
    {
        
    }
    
}
