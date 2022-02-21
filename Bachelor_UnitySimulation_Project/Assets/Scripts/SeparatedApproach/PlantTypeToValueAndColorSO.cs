using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "PlantTypeToValueAndColorMap", order = 1)]
public class PlantTypeToValueAndColorSO : ScriptableObject
{
    public Color m_Color; public Color color { get { return m_Color; } set { m_Color = value; } }
    public PlantType plantType;
    public float value;
}
