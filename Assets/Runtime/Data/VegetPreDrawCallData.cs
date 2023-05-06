using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VegetationPreDCData
{
    [SerializeField]
    public VegetationCeilGather ceilGather;

    public VegetationPreDCData(int row, int column, int ceilSize, Bounds maxBounds)
    {
        ceilGather = new VegetationCeilGather(row, column, ceilSize, maxBounds);
    }
}

[Serializable]
public class VegetationCeilGather
{
    public VegetationCeilColumn[] vegetationCeilRow;

    public Vector2 startPos;

    public Vector2 endPos;

    public int ceilSize;

    public VegetationCeilGather(int row, int column, int ceilSize, Bounds maxBounds)
    {
        vegetationCeilRow = new VegetationCeilColumn[row];

        for (int i = 0; i < row; i++)
        {
            vegetationCeilRow[i] = new VegetationCeilColumn(column);
        }

        this.ceilSize = ceilSize;
        var start = maxBounds.center - maxBounds.extents;
        var end = maxBounds.center + maxBounds.extents;
        startPos = new Vector2(start.x, start.z);
        endPos = new Vector2(end.x, end.z);
    }

    public VegetationCeil GetCeil(Vector3 targetPos)
    {
        if(targetPos.x < startPos.x || targetPos.z < startPos.y || targetPos.x > endPos.x || targetPos.z > endPos.y)
        {
            //超出范围
            return null;
        }
        int rowIndex = (int)((targetPos.x - startPos.x) / ceilSize);
        VegetationCeilColumn row = vegetationCeilRow[rowIndex];
        if(row == null)
        {
            return null;
        }

        int columnIndex = (int)((targetPos.z - startPos.y) / ceilSize);
        VegetationCeil ceil = row[columnIndex];
        return ceil;
    }
    public VegetationCeilColumn this[int index]
    {
        get
        {
            return vegetationCeilRow[index];
        }
    }

}

[Serializable]
public class VegetationCeilColumn
{
    public VegetationCeil[] ceilColumn;

    public VegetationCeilColumn(int column)
    {
        ceilColumn = new VegetationCeil[column];
    }
    
    public VegetationCeil this[int index]
    {
        get
        {
            return ceilColumn[index];
        }
        set
        {
            ceilColumn[index] = value;
        }
    }

}

[Serializable]
public class VegetationCeil
{
    public Bounds bound;

    public List<Vector2Int> dcIndexList;//x：植被index(allVegetation)，y:哪一级LOD
    public VegetationCeil(Bounds bound)
    {
        this.bound = bound;
        dcIndexList = new List<Vector2Int>();
    }

}
