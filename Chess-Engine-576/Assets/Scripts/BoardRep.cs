#region

using System.Linq;
using UnityEngine;

#endregion

public class BoardRep : MonoBehaviour
{
    public Sprites sprites;
    public BoardColours boardColours;
    private MoveGenerator _moveGenerator;
    private MeshRenderer[,] _squareRenderer;
    public SpriteRenderer[,] spriteRenderer;

    private void Awake()
    {
        _moveGenerator = new MoveGenerator();
        MakeBoard();
    }

    public void HighlightLegalMoves(Board board, Utilities.SquarePosition start)
    {
        var moves = _moveGenerator.GenerateMoves(board);

        foreach (var square in from move in moves
            where move.StartSquare == Utilities.FindPosition(start)
            select Utilities.MakeSquarePosition(move.TargetSquare))
            if (square.WhiteSquare())
                _squareRenderer[square.filePos, square.rankPos].material.color = boardColours.white.legalMoveColor;
            else
                _squareRenderer[square.filePos, square.rankPos].material.color = boardColours.black.legalMoveColor;
    }

    public void ResetPiecePosition(Utilities.SquarePosition pieceSquarePosition)
    {
        var pos = new Vector3(-3.5f + pieceSquarePosition.filePos, -3.5f + pieceSquarePosition.rankPos);
        spriteRenderer[pieceSquarePosition.filePos, pieceSquarePosition.rankPos].transform.position = pos;
    }

    public void UpdatePosition(Board board)
    {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++)
        {
            var position = Utilities.SquarePosition.CreatePositionInstance(file, rank);
            var piece = board.positionArr[Utilities.FindPosition(position)];
            spriteRenderer[file, rank].sprite = sprites.GetPieceSprite(piece);
            spriteRenderer[file, rank].transform.position = new Vector3(-3.5f + file, -3.5f + rank);
        }
    }

    public void OnMoveMade(Board board, Utilities.Move move)
    {
        UpdatePosition(board);
        ApplyColours();
    }

    private void MakeBoard()
    {
        var shader = Shader.Find("Unlit/Color");
        _squareRenderer = new MeshRenderer[8, 8];
        spriteRenderer = new SpriteRenderer[8, 8];
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++)
        {
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            square.parent = transform;
            square.name = Utilities.FileNames[file] + "" + (rank + 1);
            square.position = new Vector3(-3.5f + file, -3.5f + rank);
            var squareMaterial = new Material(shader);

            _squareRenderer[file, rank] = square.gameObject.GetComponent<MeshRenderer>();
            _squareRenderer[file, rank].material = squareMaterial;

            var pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
            var transform2 = pieceRenderer.transform;
            transform2.parent = square;
            Transform transform1;
            (transform1 = transform2).position = new Vector3(-3.5f + file, -3.5f + rank);
            transform1.localScale = Vector3.one * 100 / (2000 / 6f);
            spriteRenderer[file, rank] = pieceRenderer;
        }

        ApplyColours();
    }

    public void ApplyColours()
    {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++)
        {
            var square = Utilities.SquarePosition.CreatePositionInstance(file, rank);
            if (square.WhiteSquare())
                _squareRenderer[file, rank].material.color = boardColours.white.normalColour;
            else
                _squareRenderer[file, rank].material.color = boardColours.black.normalColour;
        }
    }

    public void SetSquareColour(Utilities.SquarePosition square, Color white, Color black)
    {
        if (square.WhiteSquare())
            _squareRenderer[square.filePos, square.rankPos].material.color = white;
        else
            _squareRenderer[square.filePos, square.rankPos].material.color = black;
    }
}