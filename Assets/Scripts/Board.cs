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
        this.FillBoard();        
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
            Debug.Log("Placing Game piece");
        }
        
        gamePiece.SetCoord(x, y);

    }

    bool IsWithinBounds(int x, int y)
    {
        return (x >= 0 && x < width && y >= 0 && y < height);        
    }

    void FillBoard()
    {
        for (int x=0; x<width; x++)
        {
            for (int y=0; y<height; y++)
            {
                GamePiece gamePiece = this.FillRandomAt(x, y);

                while (this.HasMatchOnFill(x, y))
                {
                    this.ClearPieceAt(x, y);
                    gamePiece = this.FillRandomAt(x, y);
                }
            }
        }
    }

    GamePiece FillRandomAt(int x, int y)
    {
        GameObject randomPiece = Instantiate(this.GetRandomGamePiece(), Vector3.zero, Quaternion.identity) as GameObject;
        randomPiece.transform.parent = transform;
        
        GamePiece gamePiece = randomPiece.GetComponent<GamePiece>();
        gamePiece.Init(this);     
        this.PlaceGamePiece(gamePiece, x, y);
        
        return gamePiece;
    }

    bool HasMatchOnFill(int x, int y, int minLength = 3)
    {
        List<GamePiece> leftMatches = this.FindMatches(x, y, new Vector2(-1, 0), minLength);
        List<GamePiece> downwardMatches = this.FindMatches(x, y, new Vector2(0, -1), minLength);

        if (leftMatches == null)
        {
            leftMatches = new List<GamePiece>();
        }

        if (downwardMatches == null)
        {
            downwardMatches = new List<GamePiece>();
        }

        return (leftMatches.Count > 0 || downwardMatches.Count > 0);
    }

    public void ClickTile(Tile tile)
    {
        if (m_clickedTile == null)
        {
            m_clickedTile = tile;
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
        StartCoroutine(SwitchTilesRoutine(clickedTile, targetTile));
    }

    IEnumerator SwitchTilesRoutine(Tile clickedTile, Tile targetTile)
    {
        GamePiece clickedPiece = m_allGamePieces[clickedTile.xIndex, clickedTile.yIndex];
        GamePiece targetPiece = m_allGamePieces[targetTile.xIndex, targetTile.yIndex];
                
        clickedPiece.Move(targetPiece.xIndex, targetPiece.yIndex, swapTime);
        targetPiece.Move(clickedPiece.xIndex, clickedPiece.yIndex, swapTime);

        yield return new WaitForSeconds(swapTime);

        List<GamePiece> clickedPieceMatches = this.FindMatchesAt(clickedTile.xIndex, clickedTile.yIndex);
        List<GamePiece> targetPieceMatches = this.FindMatchesAt(targetTile.xIndex, targetTile.yIndex);

        if (clickedPieceMatches.Count == 0 && targetPieceMatches.Count == 0)
        {
            clickedPiece.Move(clickedTile.xIndex, clickedTile.yIndex, swapTime/2);
            targetPiece.Move(targetTile.xIndex, targetTile.yIndex, swapTime/2);

            
        }
        else
        {
            yield return new WaitForSeconds(swapTime);
            this.ClearPieceAt(clickedPieceMatches);
            this.ClearPieceAt(targetPieceMatches);

            this.CollapseColumn(clickedPieceMatches);
            this.CollapseColumn(targetPieceMatches);
        }       
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
            if (nextPiece == null)
            {
                break;
            } else if (nextPiece.matchValue == startPiece.matchValue && !matches.Contains(nextPiece))
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
    List<GamePiece> FindHorizontalMatches(int startX, int startY, int minLength = 3)
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
        return combinedMatches.Count >= minLength ? combinedMatches : null;
    }

    void HighlightMatches()
    {
        for (int i=0; i < width; i++)
        {
            for (int j=0; j<height; j++)
            {
                this.HighlightMatchesAt(i, j);
            }
        }
    }

    void HighlightMatchesAt(int x, int y)
    {
        this.HighLightTileOff(x, y);

        var allMatches = this.FindMatchesAt(x, y);

        if (allMatches.Count > 0)
        {
            foreach (GamePiece piece in allMatches)
            {
                this.HighLightTileOn(piece.xIndex, piece.yIndex, piece.GetComponent<SpriteRenderer>().color);
            }
        }
    }

    void HighLightTileOn(int x, int y, Color color)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = color;
    }

    void HighLightTileOff(int x, int y)
    {
        SpriteRenderer spriteRenderer = m_allTiles[x, y].GetComponent<SpriteRenderer>();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0);
    }

    List<GamePiece> FindMatchesAt(int x, int y, int minLength = 3)
    {
        var horizontalMatches = this.FindHorizontalMatches(x, y, minLength);
        var verticalMatches = this.FindVerticalMatches(x, y, minLength);

        if (horizontalMatches == null)
        {
            horizontalMatches = new List<GamePiece>();
        }

        if (verticalMatches == null)
        {
            verticalMatches = new List<GamePiece>();
        }

        var combinedMatches = horizontalMatches.Union(verticalMatches).ToList();
        return combinedMatches;
    }

    void ClearPieceAt(int x, int y)
    {
        GamePiece pieceToClear = m_allGamePieces[x, y];

        if (pieceToClear != null)
        {
            m_allGamePieces[x, y] = null;
            Destroy(pieceToClear.gameObject);            
        }

        HighLightTileOff(x, y);
    }

    void ClearBoard()
    {
        for (int x=0;x<width;x++)
        {
            for (int y=0;y<width;y++)
            {
                this.HighLightTileOff(x, y);
            }
        }
    }

    void ClearPieceAt(List<GamePiece> gamePieces)
    {
        foreach (GamePiece piece in gamePieces)
        {
            this.ClearPieceAt(piece.xIndex, piece.yIndex);
        }
    }

    List<GamePiece> CollapseColumn(int column, float collapseTime = 0.1f)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();

        for (int i=0; i<height-1; i++)
        {
            if (m_allGamePieces[column, i] == null)
            {
                for (int j=i+1; j<this.height; j++)
                {
                    if (m_allGamePieces[column, j] != null)
                    {
                        m_allGamePieces[column, j].Move(column, i, collapseTime);
                        m_allGamePieces[column, i] = m_allGamePieces[column, j];
                        m_allGamePieces[column, j] = null;
                        m_allGamePieces[column, i].SetCoord(column, i);

                        if (!movingPieces.Contains(m_allGamePieces[column, i]))
                        {
                            movingPieces.Add(m_allGamePieces[column, i]);
                        }

                        break;
                    }
                }
            }
        }

        return movingPieces;
    }

    List<int> GetColumns(List<GamePiece> gamePieces)
    {
        return gamePieces.Select(x => x.xIndex).Distinct().ToList();
    }

    List<GamePiece> CollapseColumn(List<GamePiece> gamePieces)
    {
        List<GamePiece> movingPieces = new List<GamePiece>();
        List<int> columnsToCollapse = this.GetColumns(gamePieces);
        foreach (int column in columnsToCollapse)
        {
            movingPieces.Union(this.CollapseColumn(column));
        }

        return movingPieces;
    }

    void ClearAndRefilBoard(List<GamePiece> gamePieces)
    {

    }

    IEnumerator ClearAndRefillBoardRoutine(List<GamePiece> gamePieces)
    {

    }
}
