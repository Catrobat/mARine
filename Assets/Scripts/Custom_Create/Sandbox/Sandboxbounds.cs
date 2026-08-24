using UnityEngine;


public static class SandboxBounds
{
    private static Vector3 center = Vector3.zero;
    private static float halfWidth = 2.5f;  
    private static float halfDepth = 2.5f;   
    private static float margin = 0.2f;      

    private static float minY = 0f;
    private static float maxY = 3f;
    private static float verticalMargin = 0.1f;

    private static bool isInitialized = false;


    public static void Initialize(Vector3 rootPosition, SandboxSettings settings, int totalLayers = 3)
    {
        center = rootPosition;

        if (settings != null)
        {
            halfWidth = settings.sandboxWidth / 2f;
            halfDepth = settings.sandboxDepth / 2f;

            float topY = settings.GetLayerHeight(0, totalLayers);
            float bottomY = settings.GetLayerHeight(totalLayers - 1, totalLayers);

            // Convert to world space relative to root
            minY = rootPosition.y + bottomY;
            maxY = rootPosition.y + topY;
        }

        isInitialized = true;
        Debug.Log($"[SandboxBounds] Initialized. Center={center}, " +
                  $"halfWidth={halfWidth}, halfDepth={halfDepth}, Y range=[{minY}, {maxY}]");
    }

   
    public static void SetMargin(float newMargin)
    {
        margin = newMargin;
    }

   
    public static Vector3 Clamp(Vector3 worldPosition)
    {
        if (!isInitialized) return worldPosition;

        float minX = center.x - halfWidth + margin;
        float maxX = center.x + halfWidth - margin;
        float minZ = center.z - halfDepth + margin;
        float maxZ = center.z + halfDepth - margin;

        worldPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.z = Mathf.Clamp(worldPosition.z, minZ, maxZ);
        worldPosition.y = Mathf.Clamp(worldPosition.y, minY + verticalMargin, maxY - verticalMargin);

        return worldPosition;
    }

    public static Vector3 ClampHorizontal(Vector3 worldPosition)
    {
        if (!isInitialized) return worldPosition;

        float minX = center.x - halfWidth + margin;
        float maxX = center.x + halfWidth - margin;
        float minZ = center.z - halfDepth + margin;
        float maxZ = center.z + halfDepth - margin;

        worldPosition.x = Mathf.Clamp(worldPosition.x, minX, maxX);
        worldPosition.z = Mathf.Clamp(worldPosition.z, minZ, maxZ);

        return worldPosition;
    }

  
    public static float ClampY(float y)
    {
        if (!isInitialized) return y;
        return Mathf.Clamp(y, minY + verticalMargin, maxY - verticalMargin);
    }

    public static float MinY => minY;
    public static float MaxY => maxY;
    public static Vector3 Center => center;
    public static float HalfWidth => halfWidth;
    public static float HalfDepth => halfDepth;

 
    public static bool IsAtBoundary(Vector3 worldPosition, float tolerance = 0.05f)
    {
        if (!isInitialized) return false;

        float minX = center.x - halfWidth + margin;
        float maxX = center.x + halfWidth - margin;
        float minZ = center.z - halfDepth + margin;
        float maxZ = center.z + halfDepth - margin;

        return worldPosition.x <= minX + tolerance || worldPosition.x >= maxX - tolerance ||
               worldPosition.z <= minZ + tolerance || worldPosition.z >= maxZ - tolerance;
    }

  
    public static Vector3 GetRedirectedDirection(Vector3 currentPosition, Vector3 desiredDirection)
    {
        if (!isInitialized) return desiredDirection;

        Vector3 nextPos = currentPosition + desiredDirection.normalized * 0.5f;
        Vector3 clamped = Clamp(nextPos);

        // If clamping changed the position significantly, the direction was
        // heading out of bounds  redirect back toward the sandbox's center point
        if (Vector3.Distance(nextPos, clamped) > 0.01f)
        {
            Vector3 verticalCenter = new Vector3(center.x, (minY + maxY) / 2f, center.z);
            Vector3 towardCenter = verticalCenter - currentPosition;
            return towardCenter.normalized;
        }

        return desiredDirection;
    }

    public static bool IsInitialized => isInitialized;
}