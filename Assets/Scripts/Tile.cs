using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int xIndex;
    public int yIndex;

    private Board m_board;
        
    public void Init(int x, int y, Board board)
    {
        this.xIndex = x;
        this.yIndex = y;
        this.m_board = board;
    }

    void OnMouseDown()
    {
        m_board.ClickTile(this);
    }

    void OnMouseEnter()
    {
        m_board.DragToTile(this);
    }

    private void OnMouseUp()
    {
        m_board.ReleaseTile();
    }
}
