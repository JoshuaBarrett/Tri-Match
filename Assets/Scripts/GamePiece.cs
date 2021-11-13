using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePiece : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    Board m_board;

    private bool m_isMoving = false;

    public MatchValue matchValue;
    public enum MatchValue
    {
        Yellow,
        Blue,
        Magenta,
        Indigo,
        Green,
        Teal,
        Red,
        Cyan,
        Wild
    }

    void Start()
    {
        
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            this.Move((int)this.transform.position.x + 1, (int)this.transform.position.y, 0.5f);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            this.Move((int)this.transform.position.x - 1, (int)this.transform.position.y, 0.5f);
        }
    }

    public void Init(Board board)
    {
        this.m_board = board;
    }

    public void Move(int destX, int destY, float timeToMove)
    {
        if (!this.m_isMoving)
        {
            StartCoroutine(this.MoveRoutine(new Vector3(destX, destY, 0), timeToMove));
        }
    }

    IEnumerator MoveRoutine(Vector3 destination, float timeToMove)
    {
        Vector3 startPosition = this.transform.position;
        
        bool reachedDestination = false;

        float elapsedTime = 0f;
        this.m_isMoving = true;
        while (!reachedDestination)
        {
            if (Vector3.Distance(this.transform.position, destination) < 0.01f)
            {
                reachedDestination = true;
                m_board.PlaceGamePiece(this, (int)destination.x, (int)destination.y);
                break;                
            }

            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp(elapsedTime / timeToMove, 0, 1);
            t = t * t * (3 - 2 * t);
            this.transform.position = Vector3.Lerp(startPosition, destination, t);

            yield return null;
        }
        this.m_isMoving = false;        
    }

    public void SetCoord(int x, int y)
    {
        this.xIndex = x;
        this.yIndex = y;
    }
}
