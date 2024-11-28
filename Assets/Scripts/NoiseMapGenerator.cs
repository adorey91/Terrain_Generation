using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NoiseMapGenerator : MonoBehaviour
{
    [Header("Size of Grid")]
    public int width = 256;
    public int height = 256;

    [Header("Controls Scale of Noise")]
    public float scale = 20.0f;

    [Header("Slider for Regeneration Wait")]
    [SerializeField] private Slider waitSlider;

    [Header("Image to Show Noise")]
    [SerializeField] private RawImage rawImage; // UI element to show the noise

    [Header("Seed for Noise")]
    public int seed; // seed for randomization
    public bool randomSeed = true; // use a random seed or not

    private Mesh mesh; // Mesh to display the noise
    private MeshRenderer rend; // Renderer to display the noise
    private Vector3[] vertices; // Vertices of the mesh
    private int[] triangles; // Triangles of the mesh, indices that form the mesh

    // Octave parameters
    [Header("Octave Parameters")]
    public int octaves = 4; // Number of layers of noise
    [Range(0, 1)] public float persistence = 0.5f; // How much each octave contributes
    public float lacunarity = 2.0f; // How much the frequency increases for each octave

    // Perlin Noise - Gradient table
    private static int[] permutationTable = new int[512];

    private void Start()
    {
        // Create a new mesh and set the mesh filter
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        SetupRegenerate(); // Generate the noise map
    }

    private void Update()
    {
        UpdateMesh(); // Update the mesh
    }

    private void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }

    // Setup and generate the noise map and mesh when called
    public void SetupRegenerate()
    {
        StopAllCoroutines(); // stops any existing coroutine

        if (randomSeed)
            seed = Random.Range(0, 10000); // generate a random seed

        InitializePermutationTable();

        // texture based on Perlin noise
        Texture2D texture = GenerateTexture();
        rawImage.texture = texture;

        rend = GetComponent<MeshRenderer>();
        rend.material.mainTexture = texture;

        // start generating the mesh
        StartCoroutine(CreateMesh());
    }

    private Texture2D GenerateTexture()
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // normalize the coordinates based on the scale
                float xCoord = x / scale;
                float yCoord = y / scale;
                float noise = GenerateOctaveNoise(xCoord, yCoord);

                texture.SetPixel(x, y, new Color(noise, noise, noise));
            }
        }

        texture.Apply();
        return texture;
    }

    // Generate Perlin noise with multiple octaves
    private float GenerateOctaveNoise(float x, float y)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f; // Used to normalize the result

        for (int i = 0; i < octaves; i++)
        {
            // Use custom Perlin noise for each octave
            total += PerlinNoise(x * frequency, y * frequency) * amplitude;

            // Update frequency and amplitude
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize the result to a value between 0 and 1
        return total / maxValue;
    }

    // Custom Perlin noise function (2D)
    private float PerlinNoise(float x, float y)
    {
        int X = Mathf.FloorToInt(x) & 255; // Integer part of x, modulo 256
        int Y = Mathf.FloorToInt(y) & 255; // Integer part of y, modulo 256
        int X2 = (X + 1) & 255;            // X+1, wrapped to 0-255
        int Y2 = (Y + 1) & 255;            // Y+1, wrapped to 0-255

        float xf = x - Mathf.Floor(x);    // Fractional part of x
        float yf = y - Mathf.Floor(y);    // Fractional part of y

        // Fade curves for smooth interpolation
        float u = Fade(xf);
        float v = Fade(yf);

        // Hash coordinates
        int aa = permutationTable[X] + Y;
        int ab = permutationTable[X] + Y2;
        int ba = permutationTable[X2] + Y;
        int bb = permutationTable[X2] + Y2;

        // Interpolate
        float gradAA = Grad(permutationTable[aa], xf, yf);
        float gradAB = Grad(permutationTable[ab], xf, yf - 1);
        float gradBA = Grad(permutationTable[ba], xf - 1, yf);
        float gradBB = Grad(permutationTable[bb], xf - 1, yf - 1);

        // Interpolate the results
        float lerpX1 = Lerp(gradAA, gradBA, u);
        float lerpX2 = Lerp(gradAB, gradBB, u);
        return Lerp(lerpX1, lerpX2, v);
    }

    // Fade function (for smooth transitions)
    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    // Linear interpolation function
    private float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    // Gradient function to calculate the dot product
    private float Grad(int hash, float x, float y)
    {
        int h = hash & 15; // Get the last 4 bits
        float u = h < 8 ? x : y; // Select X or Y based on the hash value
        float v = h < 4 ? y : (h == 12 || h == 14 ? x : 0); // Select Y or 0
        return ((h & 1) == 0 ? 1 : -1) * (u + v); // Apply gradient correctly
    }


    // Initialize the permutation table (for randomness)
    private void InitializePermutationTable()
    {
        for (int i = 0; i < 256; i++)
        {
            permutationTable[i] = i;
        }

        // Shuffle the permutation table
        for (int i = 0; i < 256; i++)
        {
            int j = Random.Range(0, 256);
            int temp = permutationTable[i];
            permutationTable[i] = permutationTable[j];
            permutationTable[j] = temp;
        }

        // Copy the permutation table to the second half
        for (int i = 0; i < 256; i++)
        {
            permutationTable[i + 256] = permutationTable[i];
        }
    }

    private IEnumerator CreateMesh()
    {
        // Create a grid of vertices
        vertices = new Vector3[(width + 1) * (height + 1)];

        // Populate the vertices and UVs
        for (int i = 0, y = 0; y <= height; y++)
        {
            for (int x = 0; x <= width; x++)
            {
                // Apply height based on Perlin noise with octaves
                float sampleX = x / scale;
                float sampleY = y / scale;
                float heightValue = GenerateOctaveNoise(sampleX, sampleY);

                // Apply heightValue to the Y position of the vertex
                vertices[i] = new Vector3(x, heightValue * 10f, y); // Scale height for better visibility
                i++;
            }
        }


        // Create triangles (quad grid)
        triangles = new int[width * height * 6];
        int triIndex = 0;
        int vertIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Two triangles per quad
                triangles[triIndex + 0] = vertIndex + 0;
                triangles[triIndex + 1] = vertIndex + width + 1;
                triangles[triIndex + 2] = vertIndex + 1;
                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + width + 1;
                triangles[triIndex + 5] = vertIndex + width + 2;

                vertIndex++;
                triIndex += 6;
            }

            vertIndex++;

            float createTime = waitSlider.value / 100f;

            yield return new WaitForSeconds(createTime);
        }
    }
}
