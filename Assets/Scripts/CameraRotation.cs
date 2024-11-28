using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    public NoiseMapGenerator noiseMapGenerator;  // Reference to the NoiseMapGenerator
    public float rotationSpeed = 10f;  // Speed of rotation around the grid
    public float cameraHeight = 20f;  // Height of the camera above the grid (constant)

    private float currentAngle = 0f;  // The current angle of rotation around the grid

    void Update()
    {
        // Get the center of the grid (center of the noise map generator)
        Vector3 gridCenter = GetGridCenter();

        // Get the outer boundary of the grid (half of width and height)
        float gridWidth = noiseMapGenerator.width;
        float gridHeight = noiseMapGenerator.height;

        // Calculate the maximum distance to the edge of the grid (we'll use the furthest point, which is the diagonal)
        float distance = Mathf.Sqrt(Mathf.Pow(gridWidth / 2f, 2) + Mathf.Pow(gridHeight / 2f, 2));

        // Rotate the camera around the grid's center (circular motion)
        currentAngle += rotationSpeed * Time.deltaTime;  // Update angle based on speed

        // Calculate the camera's position based on the current angle around the grid
        float xPos = gridCenter.x + Mathf.Cos(currentAngle * Mathf.Deg2Rad) * distance;  // X position on the circle
        float zPos = gridCenter.z + Mathf.Sin(currentAngle * Mathf.Deg2Rad) * distance;  // Z position on the circle

        // Set the camera's position, with a fixed height above the grid
        transform.position = new Vector3(xPos, gridCenter.y + cameraHeight, zPos);

        // Always look at the center of the grid
        transform.LookAt(gridCenter);
    }

    // This method calculates the center of the grid based on the dimensions of the generated mesh
    private Vector3 GetGridCenter()
    {
        // Get the center position based on the noise map generator's width and height
        float centerX = (float)noiseMapGenerator.width / 2f;
        float centerY = (float)noiseMapGenerator.height / 2f;

        // Return the center position of the grid on the X-Z plane (Y will be handled by cameraHeight)
        return new Vector3(centerX, 0, centerY);
    }
}