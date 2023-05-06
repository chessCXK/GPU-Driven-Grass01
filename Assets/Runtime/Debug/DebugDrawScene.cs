using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDrawScene : MonoBehaviour
{
#if UNITY_EDITOR
    public VegetationPreDCData preDCCeils;

    public Bounds b;

    public RenderTexture rt;
    void OnDrawGizmos()
    {
        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
        if(m_gManager.VData == null || m_gManager.VData.preDCCeils == null)
        {
            return;
        }
        VegetationCeilGather ceilGather = m_gManager.VData.preDCCeils.ceilGather;
        VegetationCeil c = ceilGather.GetCeil(Camera.main.transform.position);
        if(c != null)
        {
            Gizmos.DrawWireCube(c.bound.center, c.bound.size);
        }

        if (UnityEditor.Selection.activeGameObject != this.gameObject)
        {
            return;
        }
        
        Gizmos.color = Color.red;
        foreach (var row in preDCCeils.ceilGather.vegetationCeilRow)
        {
            foreach(var column in row.ceilColumn)
            {
                
                Gizmos.DrawWireCube(column.bound.center, column.bound.size);
            }
        }
        
    }
#endif
}
