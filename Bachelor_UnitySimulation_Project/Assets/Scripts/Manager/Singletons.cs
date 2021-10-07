using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singletons : MonoBehaviour
{
    public SeedManager m_SeedManager; public static SeedManager seedManager { get { return instance.m_SeedManager; } }
    public SimulationManager m_SimulationManager; public static SimulationManager simulationManager { get { return instance.m_SimulationManager; } }
    

    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static bool s_IsShuttingDown = false;

    public static bool IsShuttingDown()
    {
        return s_IsShuttingDown;
    }

    private void OnApplicationQuit()
    {
        s_IsShuttingDown = true;
    }

    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    private static Singletons s_Instance;



    public static Singletons instance
    {

        get
        {
            if (!s_Instance)
            {
                s_Instance = GameObject.FindObjectOfType<Singletons>();
            }
            return s_Instance;
        }
    }



}

