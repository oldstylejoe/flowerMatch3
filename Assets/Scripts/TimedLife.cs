using UnityEngine;
using System.Collections;

//Kill this object after a certain time.

public class TimedLife : MonoBehaviour {

    public float lifetime;

	// Use this for initialization
	void Start () {
        //timeDeath = Time.time + lifetime;
        transform.parent = Camera.main.transform;
        StartCoroutine(DoDestroy());
	}

    private IEnumerator DoDestroy()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

}
