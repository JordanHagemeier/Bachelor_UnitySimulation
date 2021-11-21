using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FindMaxValue : MonoBehaviour
{
    public string testFile;
    public Texture2D texture; 
    // Start is called before the first frame update
    void Start()
    {
        ReadMaxValueFromPixel();
        //ReadMaxValueFromFile();
    }

    void ReadMaxValueFromPixel()
    {
        float maxValue32 = 0.0f;
        var pixels32 = texture.GetPixels32();
        //decimal to binary 
        //binary to float/double

        //var pixels = texture.GetRawTextureData<double>();
        foreach (var pixel in pixels32)
        {
           
            if (pixel.r > maxValue32)
            {
                maxValue32 = pixel.r;
            }
        }
        Debug.Log("px32, highest: " + maxValue32);
        Debug.Log("px32, highest256: " + maxValue32 * 256.0f);


        //-------------------------------------------------------------
        float maxValue = 0.0f;
        var pixels = texture.GetPixels();
        foreach(var pixel in pixels)
        {
            if (pixel.r > maxValue)
            {
                maxValue = pixel.r;
            }
        }

        Debug.Log("px, highest: " + maxValue);
        Debug.Log("px, highest256: " + maxValue * 256.0f);
    }

    void ReadMaxValueFromFile()
    {
        string path = testFile;
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);

        int lineID = 0;
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            float value = float.Parse(line);
            if (value <= 0.1f)
            {
                continue;
            }

            Debug.Log(reader.Peek() + "| " + line);
            
            if (lineID > 30)
            {
                break;
            }

            lineID++;
        }



        //string[] output = File.ReadAllLines(path);
        //for(int i = 0; i < output.Length; i++)
        //{
        //    Debug.Log(output[i]);
        //}
       
        reader.Close();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
