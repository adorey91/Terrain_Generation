using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PerlinTerrain : MonoBehaviour
{
    [SerializeField] private int width = 100;
    [SerializeField] private int depth = 100;
    [SerializeField] private float scale = 20;

    private Mesh mesh;
    private Vector3[] vertices;

    private void Start()
    {
        GenerateTerrain();
    }

    private void GenerateTerrain()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        
    }

    private void UpdateTerrainHeight(float[] audioData)
    {

    }
}
