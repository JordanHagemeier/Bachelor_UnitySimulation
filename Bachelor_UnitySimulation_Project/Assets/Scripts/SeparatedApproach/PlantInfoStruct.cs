using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlantType
{
    TestPlant,
    Count
}

[System.Serializable]
public class PlantInfoStruct 
{
    
    public Vector3 position;
    public int age;
    public float health;

    public int id;
    public PlantType type;
    public PlantSpeciesInfoScriptableObject plantSpeciesInfo; 

    public PlantInfoStruct(Vector3 POS, int ID, PlantSpeciesInfoScriptableObject plantSpecies)
    {
        position    = POS;
        age         = 0;
        health      = 0.0f;
        id          = ID;
        type        = plantSpecies.plantType;
        plantSpeciesInfo = plantSpecies;
    }
}
