using UnityEngine;
using System.Collections;

public class StemDrawer : MonoBehaviour {

    // Use this for initialization
    LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetWidth(0.1F, 0.04F);
        lineRenderer.SetVertexCount(2);
    }

    public void SetEnds(Vector3 a, Vector3 b)
    {
        lineRenderer.SetPosition(0, a);
        lineRenderer.SetPosition(1, b);
    }

    public void SetColor(Color col)
    {
        lineRenderer.SetColors(col, col);
        lineRenderer.material.color = col;
        lineRenderer.material.SetColor("_EmissionColor", col);
    }

    public void SetParent(Transform t)
    {
        lineRenderer.transform.parent = t;
    }
}
