using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;


public enum PlantType
{
    TestPlantA,
    TestPlantB,
    Count
}

public class PlantSpeciesTable
{
    public Dictionary<PlantType, PlantSpeciesInfoScriptableObject> typeToSO = new Dictionary<PlantType, PlantSpeciesInfoScriptableObject>(); 
    public void AddToDictionary(PlantType type, PlantSpeciesInfoScriptableObject SO)
    {
        typeToSO.Add(type, SO);
    }
    public PlantSpeciesInfoScriptableObject GetSOByType(PlantType type)
    {
        return typeToSO[type];
    }
}

[System.Serializable]
public class PlantInfoStruct 
{
   
    public Vector3 position;
    [XmlAttribute("Age")]
    public int age;
    [XmlAttribute("Health")]
    public float health;

    public int id;
    public PlantType type;
    

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
