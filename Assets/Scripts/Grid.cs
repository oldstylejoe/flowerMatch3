using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface ICustomEvents : IEventSystemHandler
{
    void Swap();
    void DropStarted();
    void DropComplete();
    void NoMoves();
}

public class SHelper
{
    public int id = -1;
    public int type = -1;
    public int sizeX = 1;
    public int sizeY = 1;
}

public class pairInt
{
    public int x;
    public int y;

    public pairInt(int ix, int iy) { x = ix; y = iy; }
    public pairInt() { x = 0; y = 0; }
    public pairInt(pairInt a) { x = a.x; y = a.y; }
}

public class Grid : MonoBehaviour, ICustomEvents
{

    //Game size
    public static int w = 8;
    public static int h = 8;

    private int[] m_numAddedAtX;
    private int[] m_lowestDrop;

    public float m_clueDelay = 4.0f;

    public scoreHandler m_scr;
    public GameOverHandler m_gameOver;

    public GameObject m_finger;
    private GameObject m_held;
    private int m_heldID = -1;
    private bool m_grabEnabled = true;
    private int m_hintID = -1;

    public GameObject m_bouquet;

    public List<GameObject> m_flowers = new List<GameObject>();

    private List<GameObject> m_current = new List<GameObject>();

    private List<int> m_movableFlowers = new List<int>();

    //hold templates for checking validity of moves.
    private List<List<pairInt>> check3right;
    private List<List<pairInt>> check3up;

    private float m_flashTime = 0.0f;

    private GameObject m_gameBoard;

    public GameObject scoreAnim;

    void Awake()
    {
        long fTest;
        Clock.GetSystemTimePreciseAsFileTime(out fTest);
        //Debug.Log("start time " + fTest);
        Clock.write("start_time " + fTest);

        m_flashTime = Time.time + 1e6f;
        m_numAddedAtX = new int[w];
        m_lowestDrop = new int[w];
        m_gameBoard = GameObject.FindGameObjectWithTag("GameBoard");
        m_finger.GetComponent<SpringJoint2D>().enabled = false;

        //setup the template for groups of three (hacked in, but just 4 per direction)
        List<pairInt> ins;
        check3right = new List<List<pairInt>>();
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(1, 1));
        ins.Add(new pairInt(1, 2));
        check3right.Add(ins);
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(1, 1));
        ins.Add(new pairInt(1, -1));
        check3right.Add(ins);
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(1, -1));
        ins.Add(new pairInt(1, -2));
        check3right.Add(ins);
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(2, 0));
        ins.Add(new pairInt(3, 0));
        check3right.Add(ins);

        check3up = new List<List<pairInt>>();
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(-2, 1));
        ins.Add(new pairInt(-1, 1));
        check3up.Add(ins);
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(-1, 1));
        ins.Add(new pairInt(1, 1));
        check3up.Add(ins);
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(1, 1));
        ins.Add(new pairInt(2, 1));
        check3up.Add(ins);
        ins = new List<pairInt>(2);
        ins.Add(new pairInt(0, 2));
        ins.Add(new pairInt(0, 3));
        check3up.Add(ins);
    }

    // Use this for initialization
    void Start()
    {
        setupBoard();
        Swap();
        Input.simulateMouseWithTouches = true;

        m_gameOver.GameStart();
    }

    private void setupBouquet()
    {
        foreach(var f in m_flowers) {
            m_bouquet.GetComponent<BouqetHolder>().AddFlower(f.GetComponent<SpriteRenderer>().color);
        }
    }

    //Replace a flower, id is the element in m_current that points to the flower to be removed.
    //Started on Coroutine so multiple can be modified at once.
    private int flowerDropRefCount = 0;
    public IEnumerator ReplaceFlower(int id)
    {
        ++flowerDropRefCount;

        GameObject f = m_current[id];
        float x = f.GetComponent<TargetJoint2D>().target.x;

        //make the new one
        Vector2 pos = new Vector2(x, h + m_numAddedAtX[Mathf.RoundToInt(x)]);
        m_numAddedAtX[Mathf.RoundToInt(x)] += 1;
        AddFlower(pos);

        //delay to see the animation
        StartCoroutine(f.GetComponent<jiggle>().DestroyAction());
        f.GetComponent<ParticleSystem>().Emit(100);
        yield return new WaitForSeconds(f.GetComponent<jiggle>().destroyTime + 0.1f);

        float y = f.GetComponent<TargetJoint2D>().target.y;

        //remove the old one
        m_current.RemoveAt(id);
        Destroy(f);
        foreach (var ff in m_current)
        {
            ExecuteEvents.Execute<IDropEvent>(ff, null, (a, b) => a.HandleDrop(x, y));
        }
        //Debug.Log("gh2 " + x.ToString() + " " + y.ToString());

        --flowerDropRefCount;
    }

    //catch the swap event
    public void Swap()
    {
        DropFlower();

        StartCoroutine(DestroyTriples());
    }

    public void DropStarted()
    {
        //m_gameOver.GameOver();  //testing only
        m_grabEnabled = false;
    }
    public void DropComplete()
    {
        markValidMoves();

        m_flashTime = Time.time + m_clueDelay;
        m_hintID = -1;

        if (m_movableFlowers.Count < 1)
        {
            ExecuteEvents.Execute<ICustomEvents>(gameObject, null, (a, b) => a.NoMoves());
        }
        else
        {
            m_grabEnabled = true;
        }
    }

    public void NoMoves()
    {
        m_gameOver.GameOver();
        //Debug.Log("No more moves");
    }

    public void RestartButton()
    {
        //Application.LoadLevel(Application.loadedLevel);
        SceneManager.LoadScene("playScene2");
    }

    private bool m_destroyingTriples = false;
    private IEnumerator DestroyTriples()
    {
        if (!m_destroyingTriples)
        {
            m_destroyingTriples = false;

            //only one at a time
            ExecuteEvents.Execute<ICustomEvents>(gameObject, null, (a, b) => a.DropStarted());

            int rmCount = -1;
            int multiplier = 1;
            do
            {
                flowerDropRefCount = 0;
                for (int i = 0; i < w; ++i) { m_numAddedAtX[i] = 0; }
                markClusters();

                List<int> idToRemove = new List<int>();
                Dictionary<int, int> hit = new Dictionary<int, int>();
                for (int i = 0; i < w; ++i)
                {
                    for (int j = 0; j < h; ++j)
                    {
                        if (m_grid[i, j].sizeX > 2 || m_grid[i, j].sizeY > 2)
                        {
                            idToRemove.Add(m_grid[i, j].id);
                            hit[m_grid[i, j].type] = 0;
                            hit[m_grid[i, j].type] = Mathf.Max(hit[m_grid[i, j].type], m_grid[i, j].sizeX);
                            hit[m_grid[i, j].type] = Mathf.Max(hit[m_grid[i, j].type], m_grid[i, j].sizeY);
                        }
                    }
                }

                rmCount = idToRemove.Count;
                var rm = idToRemove.OrderByDescending(x => x);
                foreach (int i in rm)
                {
                    m_scr.IncrementScore(multiplier * m_current[i].GetComponent<Gem>().GetMatchCount());
                    var pos = m_current[i].transform.position;
                    pos.z = -1.0f;
                    var clone = Instantiate(scoreAnim, pos, m_current[i].transform.rotation);
                    //var clone = Instantiate(scoreAnim, new Vector3(0,8), m_current[i].transform.rotation);
                    ((GameObject)clone).GetComponent<TextMesh>().text = (multiplier * m_current[i].GetComponent<Gem>().GetMatchCount()).ToString();
                    var tVel = Random.insideUnitCircle;
                    tVel.y = Mathf.Abs(tVel.y);
                    ((GameObject)clone).GetComponent<Rigidbody2D>().velocity = tVel;
                    StartCoroutine(ReplaceFlower(i));
                }
                yield return new WaitUntil(() => flowerDropRefCount == 0);

                foreach (var f in m_current)
                {
                    f.GetComponent<Gem>().AcceptMove();
                }

                yield return new WaitForSeconds(0.2f);

                var flowers = m_bouquet.GetComponent<BouqetHolder>();
                foreach(var fl in hit)
                {
                    for (int i = 0; i < fl.Value-2; ++i) {
                        yield return StartCoroutine(flowers.Increment(fl.Key));
                    }
                }
                //yield return StartCoroutine(  );

                multiplier *= 2;

            } while (rmCount > 0);

            ExecuteEvents.Execute<ICustomEvents>(gameObject, null, (a, b) => a.DropComplete());

            m_destroyingTriples = false;
        }

        yield return null;

    }

    public void DropFlower()
    {
        //safe even if nothing is being held
        if (m_finger.GetComponent<SpringJoint2D>().enabled)
        {
            m_finger.GetComponent<SpringJoint2D>().enabled = false;
            m_held.GetComponent<Gem>().UnGrab();
        }
    }

    public void GrabFlower(GameObject f)
    {
        f.GetComponent<Gem>().Grab();
        StartCoroutine(f.GetComponent<jiggle>().SelectAction());
        m_finger.GetComponent<SpringJoint2D>().connectedBody = f.GetComponent<Rigidbody2D>();
        m_finger.GetComponent<SpringJoint2D>().enabled = true;
        m_held = f;

        //reset the added row numbers if we managed a grab
        for (int i = 0; i < m_numAddedAtX.Count(); ++i) { m_numAddedAtX[i] = 0; }

        Vector2 pos = f.GetComponent<TargetJoint2D>().target;
        //Debug.Log("grabbed at " + pos.x.ToString() + " " + pos.y.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        if (m_grabEnabled && Input.GetMouseButtonDown(0))
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Clock.markEvent("fingerDown_" + pos.x + "_" + pos.y);
            //attach the finger to the object
            int count = 0;
            foreach (var f in m_current)
            {
                if (f.GetComponent<BoxCollider2D>().OverlapPoint(pos))
                {
                    GrabFlower(f);
                    m_heldID = count;
                    break;
                }
                ++count;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Clock.markEvent("fingerLifted_" + pos.x + "_" + pos.y);
            DropFlower();
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Clock.markEvent("fingerMoved_" + pos.x + "_" + pos.y);
            m_finger.transform.position = pos;
        }

        //jiggle one for a clue after a while
        if (Time.time > m_flashTime && m_movableFlowers.Count > 0)
        {
            if (m_hintID < 0)
            {
                m_hintID = Random.Range(0, m_movableFlowers.Count);
            }
            Vector2 pos = m_current[m_movableFlowers[m_hintID]].GetComponent<TargetJoint2D>().anchor;
            Clock.markEvent("hint_" + m_current[m_movableFlowers[m_hintID]].name + "_" + pos.x + "_" + pos.y);
            StartCoroutine(m_current[m_movableFlowers[m_hintID]].GetComponent<jiggle>().SelectAction());
            m_flashTime += m_clueDelay;
        }

    }

    private void AddFlower(Vector2 pos)
    {
        Object clone = Instantiate(m_flowers[Random.Range(0, m_flowers.Count)], pos, Quaternion.identity);
        ((GameObject)clone).transform.parent = m_gameBoard.transform;
        //Object clone = Instantiate(m_flowers[0], pos, Quaternion.identity);
        ((GameObject)clone).GetComponent<Gem>().MoveTo(pos);
        m_current.Add((GameObject)clone);

        Clock.markEvent("newFlower_" + ((GameObject)clone).name + "_" + pos.x + "_" + pos.y);
    }

    private void setupBoard()
    {
        for (int i = 0; i < w; ++i)
        {
            for (int j = 0; j < h; ++j)
            {
                Vector3 pos = new Vector3(i, j, 0);
                AddFlower(pos);
            }
        }

        markClusters();
        while (AtLeastOneTriple())
        {
            ReassignTriples();
            markClusters();
        }

        setupBouquet();
    }



    /* ****************************************logic for finding ones to drop starts here***************************** */

    //create a 2d array of the types
    private SHelper[,] createHelper()
    {
        SHelper[,] ret = new SHelper[w, h];

        for (int i = 0; i < m_current.Count; ++i)
        {
            var f = m_current[i];
            Vector2 pos = f.GetComponent<TargetJoint2D>().target;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            ret[x, y] = new SHelper();
            ret[x, y].id = i;
            ret[x, y].type = f.GetComponent<Gem>().m_type;
        }

        return ret;
    }

    //at the end the size of all the clusters is marked
    private SHelper[,] m_grid;
    private void markClusters()
    {
        //this keeps track of the lowest one that gets dropped (everything above this has to be dropped down)
        for (int i = 0; i < h; ++i) { m_lowestDrop[i] = h; }

        //this is only to help speed things up
        m_grid = createHelper();

        for (int i = 0; i < w; ++i)
        {
            for (int j = 0; j < h; ++j)
            {
                //rightward
                for (int k = i + 1; k < w; ++k)
                {
                    if (m_grid[i, j].type == m_grid[k, j].type)
                    {
                        m_grid[i, j].sizeX += 1;
                        m_grid[k, j].sizeX += 1;
                    }
                    else
                    {
                        break;
                    }
                }
                //upward
                for (int k = j + 1; k < h; ++k)
                {
                    if (m_grid[i, j].type == m_grid[i, k].type)
                    {
                        m_grid[i, j].sizeY += 1;
                        m_grid[i, k].sizeY += 1;
                    }
                    else
                    {
                        break;
                    }
                }

                //record on the actual object (not really used?)
                m_current[m_grid[i, j].id].GetComponent<Gem>().SetMatchCount(m_grid[i, j].sizeX + m_grid[i, j].sizeY - 1);

                if (m_grid[i, j].sizeX > 2 || m_grid[i, j].sizeY > 2)
                {
                    m_lowestDrop[i] = (j < m_lowestDrop[i]) ? j : m_lowestDrop[i];
                }
            }
        }
    }

    private void markValidMoves()
    {
        //this is only to help speed things up
        m_grid = createHelper();

        //for displaying a hint
        m_movableFlowers = new List<int>();

        for (int i = 0; i < w - 1; ++i)
        {
            for (int j = 0; j < h - 1; ++j)
            {
                m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveRight = false;
                m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveUp = false;
                m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveLeft = false;
                m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveDown = false;
            }
        }
        for (int i = 0; i < w; ++i)
        {
            for (int j = 0; j < h; ++j)
            {
                foreach (var t in check3right)
                {
                    //could do an extra for loop, but it's called match THREE for a reason
                    try
                    {
                        if (m_grid[i, j].type == m_grid[i + t[0].x, j + t[0].y].type && m_grid[i, j].type == m_grid[i + t[1].x, j + t[1].y].type)
                        {
                            m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveRight = true;
                            m_current[m_grid[i + 1, j].id].GetComponent<Gem>().canMoveLeft = true;
                            m_movableFlowers.Add(m_grid[i, j].id);
                        }
                    }
                    catch (System.IndexOutOfRangeException) { }
                    try
                    {
                        if (m_grid[i, j].type == m_grid[i - t[0].x, j + t[0].y].type && m_grid[i, j].type == m_grid[i - t[1].x, j + t[1].y].type)
                        {
                            m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveLeft = true;
                            m_current[m_grid[i - 1, j].id].GetComponent<Gem>().canMoveRight = true;
                            m_movableFlowers.Add(m_grid[i, j].id);
                        }
                    }
                    catch (System.IndexOutOfRangeException) { }
                }
                foreach (var t in check3up)
                {
                    try
                    {
                        if (m_grid[i, j].type == m_grid[i + t[0].x, j + t[0].y].type && m_grid[i, j].type == m_grid[i + t[1].x, j + t[1].y].type)
                        {
                            m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveUp = true;
                            m_current[m_grid[i, j + 1].id].GetComponent<Gem>().canMoveDown = true;
                            m_movableFlowers.Add(m_grid[i, j].id);
                        }
                    }
                    catch (System.IndexOutOfRangeException) { }

                    try
                    {
                        if (m_grid[i, j].type == m_grid[i + t[0].x, j - t[0].y].type && m_grid[i, j].type == m_grid[i + t[1].x, j - t[1].y].type)
                        {
                            m_current[m_grid[i, j].id].GetComponent<Gem>().canMoveDown = true;
                            m_current[m_grid[i, j - 1].id].GetComponent<Gem>().canMoveUp = true;
                            m_movableFlowers.Add(m_grid[i, j].id);
                        }
                    }
                    catch (System.IndexOutOfRangeException) { }
                }
            }
        }
    }

    private bool AtLeastOneTriple()
    {
        for (int i = 0; i < w; ++i)
        {
            for (int j = 0; j < h; ++j)
            {
                if (m_grid[i, j].sizeX > 2 || m_grid[i, j].sizeY > 2)
                {
                    return true;
                }
            }
        }
        return false;
    }

    //reset the ones marked as triples. Meant to be called at initial setup (no animation or scoring).
    private void ReassignTriples()
    {

        List<int> toRemove = new List<int>();

        for (int i = 0; i < w; ++i)
        {
            for (int j = 0; j < h; ++j)
            {
                if (m_grid[i, j].sizeX > 2 || m_grid[i, j].sizeY > 2)
                {
                    toRemove.Add(m_grid[i, j].id);
                    AddFlower(new Vector3(i, j, 0));
                }
            }
        }
        var rm = toRemove.OrderByDescending(x => x);
        foreach (int i in rm)
        {
            var q = m_current[i];
            m_current.RemoveAt(i);
            Destroy(q);
        }
    }
}

