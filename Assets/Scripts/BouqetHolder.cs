using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BouqetHolder : MonoBehaviour {

    public GameObject m_flowers;
    public GameObject m_stems;
    public GameObject vase;

    private List<GameObject> m_flowerHeads = new List<GameObject>();
    private List<GameObject> m_flowerStems = new List<GameObject>();

    private List<Vector3> m_pos = new List<Vector3>();

	// Use this for initialization
	void Awake () {
        transform.SetParent(vase.transform);
        transform.localPosition = new Vector3(0, 3, 0);

        //hacked in for now
        m_pos.Add(new Vector3(0f, 0f));
        m_pos.Add(new Vector3(-1.8f, -0.5f));
        m_pos.Add(new Vector3(1.8f, -0.5f));
        m_pos.Add(new Vector3(-0.9f, 1.7f));
        m_pos.Add(new Vector3(0.4f, 1.8f));
        m_pos.Add(new Vector3(-1.6f, 1.1f));
        m_pos.Add(new Vector3(1.6f, 1.3f));
    }

    public void AddFlower(Color col)
    {
        //Vector3 hackoffset = new Vector3(2.5f, 4f, 0f);
        var clone = Instantiate(m_flowers, Vector3.zero, Quaternion.identity);
        ((GameObject)clone).transform.SetParent(gameObject.transform);
        ((GameObject)clone).transform.localPosition = m_pos[m_flowerHeads.Count];
        ((GameObject)clone).GetComponent<FlowerDrawer>().SetColor(col);

        var clone2 = Instantiate(m_stems, Vector3.zero, Quaternion.identity);
        ((GameObject)clone2).transform.SetParent(gameObject.transform);
        ((GameObject)clone2).transform.localPosition = new Vector3(0,0,10);
        ((GameObject)clone2).GetComponent<StemDrawer>().SetColor(new Color(0.1f, 0.8f, 0.1f));
        ((GameObject)clone2).GetComponent<StemDrawer>().SetEnds(new Vector3(0,-3,0), m_pos[m_flowerHeads.Count]);

        m_flowerHeads.Add((GameObject)clone);
        m_flowerStems.Add((GameObject)clone2);
    }

    public IEnumerator Increment(int i)
    {
        yield return StartCoroutine(((GameObject)m_flowerHeads[i]).GetComponent<FlowerDrawer>().Increment());
    }

    /*// Update is called once per frame
	void Update () {
	
	}*/
}
