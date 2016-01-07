using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class jiggle : MonoBehaviour {

    public float selectTime;
    public float selectRotateSpeed;

    public IEnumerator SelectAction()
    {
        float t = Time.time;
        Vector3 axis = new Vector3(0f, 0f, 1f);
        while (Time.time < t + 0.5f * selectTime && transform != null)
        {
            transform.RotateAround(gameObject.transform.position, axis, selectRotateSpeed);
            yield return new WaitForEndOfFrame();
        }
        while (Time.time < t + selectTime && transform != null)
        {
            transform.RotateAround(gameObject.transform.position, axis, -selectRotateSpeed);
            yield return new WaitForEndOfFrame();
        }

        transform.eulerAngles = new Vector3(0f, 0f, 0f);
    }

    public float destroyTime;
    public float destroyScale;

    public IEnumerator DestroyAction()
    {
        float t = Time.time;
        while(Time.time < t + destroyTime && transform != null)
        {
            transform.localScale = destroyScale * transform.localScale;
            yield return new WaitForEndOfFrame();
        }
    }

}
