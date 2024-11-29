using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NoiseMapGenerator : MonoBehaviour
{
    [Header("Size of Grid")]
    public int xSize = 256;
    public int zSize = 256;

    [Header("Controls Scale of Noise")]
    private float scale = 20.0f;

    [Header("Slider for Regeneration Wait")]
    [SerializeField] private Slider waitSlider;

    [Header("Slider for Noise Scale")]
    [SerializeField] private Slider scaleSlider;

    [Header("Image to Show Noise")]
    [SerializeField] private RawImage rawImage; // UI element to show the noise

    [Header("Seed for Noise")]
    [SerializeField] private int seed; // seed for randomization
    [SerializeField] private bool randomSeed = true; // use a random seed or not
    [SerializeField] private Toggle randomSeedToggle;
    [SerializeField] private InputField seedInput;

    private Mesh mesh; // Mesh to display the noise
    private MeshRenderer rend; // Renderer to display the noise
    private Vector3[] vertices; // Vertices of the mesh
    private int[] triangles; // Triangles of the mesh, indices that form the mesh

    // Octave parameters
    [Header("Octave Parameters")]
    public int octaves = 4; // Number of layers of noise
    [Range(0, 1)] public float persistence = 0.5f; // How much each octave contributes
    public float lacunarity = 2.0f; // How much the frequency increases for each octave

    private float xCenter;
    private float zCenter;

    private void Start()
    {
        // Create a new mesh and set the mesh filter
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        SetupSliders(); // Setup the sliders
        SetupRegenerate(); // Generate the noise map

        randomSeedToggle.isOn = randomSeed;
        randomSeedToggle.onValueChanged.AddListener((value) => randomSeed = value);
    }

    private void Update()
    {
        UpdateMesh(); // Update the mesh
    }

    private void SetupSliders()
    {
        ScaleSliderSetup();
        WaitSliderSetup();
    }

    // Setup the wait slider
    private void WaitSliderSetup()
    {
        waitSlider.maxValue = 2f;
        waitSlider.minValue = 0.1f;
        waitSlider.value = 0.6f;
    }

    // Setup the scale slider
    private void ScaleSliderSetup()
    {
        scaleSlider.maxValue = 100f;
        scaleSlider.minValue = 1f;
        scaleSlider.value = scale;
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
        {
            if(seedInput.text != "")
                seed = int.Parse(seedInput.text); // use the seed from the input field
            else
            {
                seed = Random.Range(0, 10000); // generate a random seed
                seedInput.placeholder.GetComponent<Text>().text = seed.ToString();
            }
        }

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
        Texture2D texture = new Texture2D(xSize, zSize);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        // calculate center of the texture
        xCenter = xSize / 2;
        float zCenter = zSize / 2;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                // normalize the coordinates based on the scale
                float xCoord = (x - xCenter) / scaleSlider.value;
                float zCoord = (z - zCenter) / scaleSlider.value;
                float noise = GenerateOctaveNoise(xCoord, zCoord);

                float normalizedNoise = (noise + 1f) / 2f; // Normalize to [0, 1] (0-255 for color)

                texture.SetPixel(x, z, new Color(normalizedNoise, normalizedNoise, normalizedNoise));
            }
        }

        texture.Apply();
        return texture;
    }

    // Generate Perlin noise with multiple octaves
    private float GenerateOctaveNoise(float x, float z)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f; // Used to normalize the result

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = x * frequency;  // Adjust x coordinate based on frequency
            float sampleZ = z * frequency;  // Adjust z coordinate based on frequency

            // Use Perlin noise for each octave using seed for random
            float perlinValue = Mathf.PerlinNoise(sampleX + seed, sampleZ + seed);
            total += perlinValue * amplitude;

            // Update frequency and amplitude
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        // Normalize to [-1, 1]
        return (total / maxValue) * 2f - 1f;
    }

    private IEnumerator CreateMesh()
    {
        // Create a grid of vertices
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        // Populate the vertices and UVs
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                // Apply zSize based on Perlin noise with octaves
                float sampleX = (x - xCenter) / scaleSlider.value;
                float sampleZ = (z - zCenter) / scaleSlider.value;
                float heightValue = GenerateOctaveNoise(sampleX, sampleZ);

                // Apply heightValue to the Y position of the vertex
                vertices[i] = new Vector3(x, heightValue * 10f, z); // Scale zSize for better visibility
                i++;
            }
        }


        // Create triangles (quad grid)
        triangles = new int[xSize * zSize * 6];
        int triIndex = 0;
        int vertIndex = 0;

        for (int y = 0; y < zSize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                // Two triangles per quad
                triangles[triIndex + 0] = vertIndex + 0;
                triangles[triIndex + 1] = vertIndex + xSize + 1;
                triangles[triIndex + 2] = vertIndex + 1;
                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + xSize + 1;
                triangles[triIndex + 5] = vertIndex + xSize + 2;

                vertIndex++;
                triIndex += 6;
            }

            vertIndex++;

            float createTime = waitSlider.value / 100f;

            yield return new WaitForSeconds(createTime);
        }
    }
}
