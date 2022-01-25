using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using System.IO;



[XmlRoot("Current Plant Data")]
public class CurrentPlantsSerializer
{
    public CurrentPlantsSerializer() { }
    [XmlArray("current Plants"), XmlArrayItem("Plants")]
    public List<PlantInfoStruct> currentPlantsInSim = new List<PlantInfoStruct>();

    public void Save(string path)
    {
        var serializer = new XmlSerializer(typeof(CurrentPlantsSerializer));
        using (var stream = new FileStream(path, FileMode.Create))
        {
            serializer.Serialize(stream, this);
        }
    }

    public static CurrentPlantsSerializer Load(string path)
    {
        var serializer = new XmlSerializer(typeof(CurrentPlantsSerializer));
        using (var stream = new FileStream(path, FileMode.Open))
        {
            return serializer.Deserialize(stream) as CurrentPlantsSerializer;
        }
    }
}

