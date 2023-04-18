using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{
    ShapeGenerator shapeGenerator;
    Mesh mesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;

    public float normalizeFactor;

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp)
    {
        this.shapeGenerator = shapeGenerator;
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }
    
    public void ConstructMesh()
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        Vector2[] uv = (mesh.uv.Length == vertices.Length) ? mesh.uv : new Vector2[vertices.Length];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (finalResolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnUnitSphere1 = pointOnUnitCube.normalized;
                Vector3 pointOnUnitSphere2 = TangentialNormalize(pointOnUnitCube);
                Vector3 pointOnUnitSphere = Vector3.Lerp(pointOnUnitSphere1, pointOnUnitSphere2, normalizeFactor);

                float unscaledElevation = shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);
                vertices[i] = pointOnUnitSphere * shapeGenerator.GetScaledElevation(unscaledElevation);

                uv[i].y = unscaledElevation;

                if (x != (resolution - 1) && y != (resolution - 1))
                {
                    triangles[triIndex + 0] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh = TrimMesh(mesh, resolution);
        mesh.uv = uv;
    }

    public Vector3 TangentialNormalize(Vector3 pointOnUnitCube)
    {
        float x2 = pointOnUnitCube.x * pointOnUnitCube.x;
        float y2 = pointOnUnitCube.y * pointOnUnitCube.y;
        float z2 = pointOnUnitCube.z * pointOnUnitCube.z;
        Vector3 pointOnUnitSphere;
        pointOnUnitSphere.x = pointOnUnitCube.x * Mathf.Sqrt(1f - y2 / 2f - z2 / 2f + y2 * z2 / 3f);
        pointOnUnitSphere.y = pointOnUnitCube.y * Mathf.Sqrt(1f - x2 / 2f - z2 / 2f + x2 * z2 / 3f);
        pointOnUnitSphere.z = pointOnUnitCube.z * Mathf.Sqrt(1f - x2 / 2f - y2 / 2f + x2 * y2 / 3f);
        return pointOnUnitSphere;
    }

    public Vector3[] CalculateNormals(Mesh mesh)
    {

        Vector3[] vertexNormals = new Vector3[mesh.vertices.Length];
        int triangleCount = mesh.triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = mesh.triangles[normalTriangleIndex + 0];
            int vertexIndexB = mesh.triangles[normalTriangleIndex + 1];
            int vertexIndexC = mesh.triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(mesh.vertices, vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;


        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;

    }

    public Vector3 SurfaceNormalFromIndices(Vector3[] vertices, int indexA, int indexB, int indexC)
    {
        Vector3 pointA = vertices[indexA];
        Vector3 pointB = vertices[indexB];
        Vector3 pointC = vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void UpdateUVs(ColorGenerator colorGenerator)
    {
        Vector2[] uv = mesh.uv;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                uv[i].x = colorGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }
        mesh.uv = uv;
    }

    public static Mesh TrimMesh(Mesh mesh, int resolution)
    {
        Vector3[] vertices = mesh.vertices;
        int xLength = resolution + 1;

        // Calculate the number of vertices to remove from each side
        int verticesToRemove = (xLength + 1) * 2;

        // Calculate the new arrays
        Vector3[] newVertices = new Vector3[vertices.Length - verticesToRemove];
        Vector2[] newUVs = new Vector2[newVertices.Length];
        int[] newTriangles = mesh.triangles;

        // Copy the existing vertices and UVs into the new arrays
        int index = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            int x = i % xLength;
            int y = i / xLength;
            if (x > 0 && x < xLength - 1 && y > 0 && y < resolution)
            {
                newVertices[index] = vertices[i];
                newUVs[index] = mesh.uv[i];
                index++;
            }
        }

        // Calculate the new triangles
        for (int i = 0; i < newTriangles.Length; i += 3)
        {
            int a = newTriangles[i];
            int b = newTriangles[i + 1];
            int c = newTriangles[i + 2];
            if (a >= verticesToRemove) a -= verticesToRemove;
            if (b >= verticesToRemove) b -= verticesToRemove;
            if (c >= verticesToRemove) c -= verticesToRemove;
            newTriangles[i] = a;
            newTriangles[i + 1] = b;
            newTriangles[i + 2] = c;
        }

        // Create a new mesh and assign the new arrays
        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices;
        newMesh.triangles = newTriangles;
        newMesh.uv = newUVs;
        newMesh.normals = mesh.normals;
        newMesh.RecalculateBounds();

        return newMesh;
    }

}
