using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Board : MonoBehaviour
{
    public int width;
    public int height;

    public int BorderSize;

    public GameObject tilePrefab;
    public GameObject[] gamePiecePrefabs;

    private Tile[,] m_allTiles;
    private GamePiece[,] m_allGamePieces;

    Tile m_clickedTile;
    Tile m_targetTile;

    public float swapTime = 0.5f;

    void Start()
    {
        m_allTiles = new Tile[this.width, this.height];
        m_allGamePieces = new GamePiece[this.width, this.height];
        this.SetupTiles();
        this.SetupCamera();
        this.FillRandom();
        this.HighlightMatches();
    }


    void Update()
    {

    }

    void SetupTiles()
    {
        for (int i=0; i<this.width; i++)
        {
            for(int j=0; j<this.height; j++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(i, j, 0), Quaternion.identity) as GameObject;
                tile.name = $"Tile ({i},{j})";
                m_allTiles[i, j] = tile.GetComponent<Tile>();
                tile.transform.parent = this.transform;
                m_allTiles[i, j].Init(i, j, this);
            }
        }
    }

    void SetupCamera()
    {
        Camera.main.transform.position = new Vector3((float) (width - 1)/2f, (float) (height - 1)/2f, -10f);

        float aspectRatio = (float) Screen.width / (float) Screen.height;
        float verticalSize = (float) height / 2f + (float)BorderSize;
        float horizontalSize = ((float)width / 2f + (float)BorderSize) / aspectRatio;
        Camera.main.orthographicSize = (verticalSize > horizontalSize) ? verticalSize : horizontalSize;
    }

    GameObject GetRandomGamePiece()
    {
        int randomIndex = Random.Range(0, gamePiecePrefabs.Length);

        if (gamePiecePrefabs[randomIndex] == null)
        {
            Debug.LogWarning($"BOARD: {randomIndex} does not contain a valid game piece prefab!");
        }

        return gamePiecePrefabs[randomIndex];
    }

    public void PlaceGamePiece(GamePiece gamePiece, int x, int y)
    {
        gamePiece.transform.position = new Vector3(x, y, 0);
        gamePiece.transform.rotation = Quaternion.identity;
        
        if (this.IsWithinBounds(x,y))
        {
            m_allGamePieces[x, y] = gamePiece;
        }
        
        gamePiece.SetCoord(x, y);
    }

    bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);        
    }

    void FillRandom()
    {
        for (int i=0; i<width; i++)
        {
            for (int j=0; j<height; j++)
            {
                GameObject randomPiece = Instantiate(this.GetRandomGamePiece(), Vector3.zero, Quaternion.identity) as GameObject;
                randomPiece.GetComponent<GamePiece>().Init(this);
                this.PlaceGamePiece(randomPiece.GetComponent<GamePiece>(), i, j);
                randomPiece.transform.parent = transform;
            }
        }
    }

    public void ClickTile(Tile tile)
    {
        if (m_clickedTile == null)
        {
            m_clickedTile = tile;
            Debug.Log($"Clicked tile: + {tile.name}");
        }
    }

    public void DragToTile(Tile tile)
    {
        if (m_clickedTile != null && IsNextTo(tile, m_clickedTile))
        {
            m_targetTile = tile;
        }
    }

    public void ReleaseTile()
    {
        if (m_clickedTile != null && m_targetTile != null)
        {
            SwitchTiles(m_clickedTile, m_targetTile);
        }

        m_clickedTile = null;
        m_targetTile = null;
    }

    void SwitchTiles(Tile clickedTile, Tile targetTile)
    {
        GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
        GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];

        clickedPiece.Move(targetPiece.xIndex, targetPiece.yIndex, swapTime);
        targetPiece.Move(clickedPiece.xIndex, clickedPiece.yIndex, swapTime);        
    }

    bool IsNextTo(Tile start, Tile end)
    {
        return (Mathf.Abs(start.xIndex - end.xIndex) == 1 && start.yIndex == end.yIndex)
            || (Mathf.Abs(start.yIndex - end.yIndex) == 1 && start.xIndex == end.xIndex);        
    }

    List<GamePiece> FindMatches(int startX, int startY, Vector2 searchDirection, int minLength=3)
    {
        List<GamePiece> matches = new List<GamePiece>();
        GamePiece startPiece = null;

        if (IsWithinBounds(startX, startY))
        {
            startPiece = m_allGamePieces[startX, startY];
        }

        if (startPiece != null)
        {
            matches.Add(startPiece);
        }
        else
        {
            return null;
        }

        int nextX;
        int nextY;

        int maxValue = (width > height) ? width : height;

        for (int i = 1; i < maxValue - 1; i++)
        {
            nextX = startX + Mathf.Clamp((int)searchDirection.x, -1, 1) * i;
            nextY = startY + Mathf.Clamp((int)searchDirection.y, -1, 1) * i;
            
            if (!IsWithinBounds(nextX, nextY))
            {
                break;
            }

            GamePiece nextPiece = m_allGamePieces[nextX, nextY];
            if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece))
            {
                matches.Add(nextPiece);
            }
            else
            {
                break;
            }           
        }

        if (matches.Count >= minLength)
        {
            return matches;
        }

        return null;
    }
    List<GamePiece> FindVerticalMatches(int startX, int startY, int minLenth = 3)
    {
        List<GamePiece> upwardMatches = this.FindMatches(startX, startY, new Vector2(0, 1), 2);
        List<GamePiece> downwardMatches = this.FindMatches(startX, startY, new Vector2(0, -1), 2);
        
        if (upwardMatches == null)
        {
            upwardMatches = new List<GamePiece>();
        }
        if (downwardMatches == null)
        {
            downwardMatches = new List<GamePiece>();
        }

        List<GamePiece> combinedMatches = upwardMatches.Union(downwardMatches).ToList();
        return combinedMatches.Count >= minLenth ? combinedMatches : null;
    }
    List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLenth = 3)
    {
        List<GamePiece> rightMatches = this.FindMatches(startX, startY, new Vector2(1, 0), 2);
        List<GamePiece> leftMatches = this.FindMatches(startX, startY, new Vector2(-1, 0), 2);

        if (rightMatches == null)
        {
            rightMatches = new List<GamePiece>();
        }
        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }

        List<GamePiece> combinedMatches = rightMatches.Union(leftMatches).ToList();
        return combinedMatches.Count >= minLenth ? combinedMatches : null;
    }

    void HighlightMatches()
    {
        for (int i=0; i < width; i++)
        {
            for (int j=0; j<height; j++)
            {
                SpriteRenderer spriteRenderer = m_allTiles[i, j].GetComponent<SpriteRenderer>();
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);

                var horizontalMatches = this.FindHorizontalMatches(i, j, 3);
                var verticalMatches = this.FindVerticalMatches(i, j, 3);

                if (horizontalMatches == null)
                {
                    horizontalMatches = new List<GamePiece>();
                }

                if (verticalMatches == null)
                {
                    verticalMatches = new List<GamePiece>();
                }

                var combinedMatches = horizontalMatches.Union(verticalMatches).ToList();

                if (combinedMatches.Count > 0)
                {
                    foreach (GamePiece piece in combinedMatches)
                    {
                        spriteRenderer = m_allTiles[piece.xIndex, piece.yIndex].GetComponent<SpriteRenderer>();
                        spriteRenderer.color = piece.GetComponent<SpriteRenderer>().color;
                    }
                }
            }
        }
    }
}
