using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public interface IDropEvent : IEventSystemHandler
{
    void HandleDrop(float x, float y);
}

public class Gem : MonoBehaviour, IDropEvent {

    public static Dictionary<string, int> hashFlowers;

    public int m_type;

    public bool canMoveRight = false;
    public bool canMoveUp = false;
    public bool canMoveLeft = false;
    public bool canMoveDown = false;

    private bool grabbed = false;
    private int matchCount = 0;

    private GameObject m_gameBoard;

    private Vector2 lastPos = new Vector2(0.0f, 0.0f);
    private Vector2 trialPos = new Vector2(0.0f, 0.0f);
    private Vector2 assignedPos = new Vector2(0.0f, 0.0f);

    void Awake()
    {
        m_gameBoard = GameObject.FindGameObjectWithTag("GameBoard");
        if(hashFlowers == null)
        {
            hashFlowers = new Dictionary<string, int>();
        }

        string name = GetComponent<SpriteRenderer>().sprite.ToString();
        if (!hashFlowers.ContainsKey(name))
        {
            hashFlowers[name] = m_type;// hashFlowers.Count;
        }
        //m_type = hashFlowers[name];
        //GoToAssigned();
    }

    public void HandleDrop(float x, float y)
    {
        //Debug.Log("gh1 " + x.ToString() + " " + y.ToString());
        if( Mathf.Abs(assignedPos.x - x) < 1e-6f && assignedPos.y > y-0.5f)
        {
            trialPos.y -= 1.0f;
        }
    }

    // Use this for initialization
    void Start () {
        GoToAssigned();
    }

    // Update is called once per frame
    void Update () {
	}

    public void Grab() { grabbed = true; }
    public bool IsGrabbed() { return grabbed; }
    public void UnGrab() { grabbed = false; }

    public int GetMatchCount() { return matchCount; }
    public void SetMatchCount(int x) { matchCount = x; }

    public void GoToAssigned()
    {
        GetComponent<TargetJoint2D>().target = assignedPos;
        GetComponent<DistanceJoint2D>().connectedAnchor = assignedPos;
        trialPos = assignedPos;
    }

    public void AcceptMove()
    {
        lastPos = assignedPos;
        assignedPos = trialPos;
        GoToAssigned();
    }

    public void MoveTo(Vector2 pos)
    {
        lastPos = assignedPos;
        assignedPos = pos;
        GoToAssigned();
    }

    public void UndoMove()
    {
        assignedPos = lastPos;
        trialPos = assignedPos;
        GoToAssigned();
    }

    public bool sameType(Gem other)
    {
        return m_type == other.m_type;
    }

    private bool ValidMove()
    {
        Vector2 dir = trialPos - assignedPos;
        if (dir.x > 0.5) { return canMoveRight; }
        if (dir.x < -0.5) { return canMoveLeft; }
        if (dir.y > 0.5) { return canMoveUp; }
        if (dir.y < -0.5) { return canMoveDown; }

        Debug.Log("Should not get here");
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //only care about the manipulated one
        if (!grabbed) { return; }
        if (other.gameObject.GetComponent<Gem>() == null) { return; }

        Vector2 t = other.GetComponent<Gem>().assignedPos;
        if( ((t - assignedPos).sqrMagnitude - 1.0f) > 0.1 ) { return; }
        trialPos = t;

        if(!ValidMove()) {
            UndoMove();
            return;
        }

        AcceptMove();
        other.GetComponent<Gem>().MoveTo(lastPos);

        ExecuteEvents.Execute<ICustomEvents>(m_gameBoard, null, (x, y) => x.Swap());
    }

    void OnTriggerExit2D(Collider2D other)
    {
    }
}
