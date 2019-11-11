using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeCreator : MonoBehaviour
{

    [HideInInspector]
    public List<Shape> shapes = new List<Shape>();

    public float handleRadius = 0.5f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

}

[System.Serializable]
public class Shape
{
    public List<Vector3> points = new List<Vector3>();
}
