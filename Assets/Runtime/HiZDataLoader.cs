using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


[ExecuteInEditMode]
public class HiZDataLoader : MonoBehaviour
{
    public VegetationData data;
    public void OnEnable()
    {
        HiZGlobelManager.Instance.CreateComputeBuffer(data);
    }
    public void OnDisable()
    {
        HiZGlobelManager.Instance.DisposeComputeBuffer();
    }

}
