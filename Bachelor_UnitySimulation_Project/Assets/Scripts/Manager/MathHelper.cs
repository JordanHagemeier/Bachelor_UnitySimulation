using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathHelper : MonoBehaviour
{
    public static float Remap(float value, float rangeAMin, float rangeAMax, float rangeBMin, float rangeBMax)
    {
        //1.
        //first figure out at what percentage the value is, relative to range A
        //for example: Range A= [1 - 7], value = 3
        //gotta subtract the 1 from the 7 to get the range to be [0 - 6] (the step is rangeAMax - rangeAMin)
        //gotta also subtract the 1 from the 3 to get it relative to the range again ( the step is value - rangeAMin)
        //now divide to get the percentage 

        float inverseLerpResult = (value - rangeAMin) / (rangeAMax - rangeAMin);

        //2.
        //now onto doing the lerp
        // gotta map this percentage to the new range 
        //example: [3,9]
        // also gotta subtract the rangeMin from rangeMax to get the range to start at 0 ( rangeBMax - rangeBMin) [9-3] = [0,6]
        //now  multiply the rangeBMax with the inverseLerpresult (percentage) and add the minBRange to it ((value * rangeBMax) + rangeBMin)

        float lerpResult = ((rangeBMax - rangeBMin) * inverseLerpResult) + rangeBMin;

        return lerpResult;
    }
}
