using UnityEngine;
using System.Collections;

public class FlowerDrawer : MonoBehaviour {

    public int lengthOfLineRenderer = 100;


    public float k = 1.0f;
    private float n = 1.0f;
    private float k_over_n;

    public float renderTime;

    LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetWidth(0.06F, 0.06F);
        lineRenderer.SetVertexCount(lengthOfLineRenderer);
    }

    // Use this for initialization
    void Start () {
        k_over_n = k / n;
        DoDraw();
    }

    public void DoDraw()
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        for (int i = 0; i < lengthOfLineRenderer; ++i)
        {
            float t = n*2.0f * Mathf.PI * i / (lengthOfLineRenderer - 1.0f);
            Vector3 pos = new Vector3(Mathf.Cos(k_over_n * t) * Mathf.Sin(t), Mathf.Cos(k_over_n * t) * Mathf.Cos(t), 0);
            lineRenderer.SetPosition(i, pos);
        }

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

    //move to the next flower
    public IEnumerator Increment() {
        n += 1.0f;
        if ( n - k > -1.0e-6f)
        {
            n = 1.0f;
            k += 1.0f;
        }
        float start = k_over_n;
        float target = k / n;

        float starttime = Time.time;

        while (Time.time < starttime + renderTime)
        {
            k_over_n = (target - start) * (Time.time - starttime) / renderTime + start;
            DoDraw();
            yield return null;
        }

        k_over_n = target;
        DoDraw();
        yield return null;
    }

}
