using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class CamerasControl : MonoBehaviour
{
    public CinemachineVirtualCamera[] cameras;

    private int i = 0;
    void Start()
    {
        foreach (CinemachineVirtualCamera cam in cameras)
        {
            cam.Priority = 0;
        }
        cameras[0].Priority = 1;
    }

    
    public void nextCam()
    {
        cameras[i].Priority = 0;

        i++;
        if (i >= cameras.Length) i = 0;
        
        cameras[i].Priority = 1;
    }
}
