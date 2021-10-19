#region

using System;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

public class Player
{
    #region 02. Actions

    private void CancelPieceSelection()
    {
        if (_currentState == InputState.None) return;
        _currentState = InputState.None;
        _boardRep.ApplyColours();
        _boardRep.ResetPiecePosition(_selectedPieceSquare);
    }

    private void ChoseMove(Utilities.Move move)
    {
        ONMoveChosen?.Invoke(move);
    }

    private void HandleDragMovement(Vector2 mousePos)
    {
        _boardRep.spriteRenderer[_selectedPieceSquare.filePos, _selectedPieceSquare.rankPos].transform.position =
            new Vector3(mousePos.x, mousePos.y);

        if (Input.GetMouseButtonUp(0)) PlacePiece(mousePos);
    }

    private void HandleInput()
    {
        Vector2 mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);

        if (_currentState == InputState.None)
        {
            PieceSelect(mousePos);
        }
        else
        {
            if (_currentState == InputState.DraggingPiece)
            {
                HandleDragMovement(mousePos);
            }
            else if (_currentState == InputState.PieceSelected)
            {
                if (Input.GetMouseButton(0)) PlacePiece(mousePos);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        if (Input.GetMouseButtonDown(1)) CancelPieceSelection();
    }

    private static bool MouseSquare(Vector2 mouseWorld, out Utilities.SquarePosition selectedSquarePosition)
    {
        var file = (int) (mouseWorld.x + 4);
        var rank = (int) (mouseWorld.y + 4);
        selectedSquarePosition = Utilities.SquarePosition.CreatePositionInstance(file, rank);
        return (file >= 0) switch
        {
            true when file < 8 && rank >= 0 && rank < 8 => true,
            _ => false
        };
    }

    private void PieceSelect(Vector2 mousePos)
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (!MouseSquare(mousePos, out _selectedPieceSquare)) return;
        var index = Utilities.FindPosition(_selectedPieceSquare);
        if (!Pieces.PieceObj.IsColour(_board.positionArr[index], _board.colourToMove)) return;
        _boardRep.HighlightLegalMoves(_board, _selectedPieceSquare);
        _boardRep.SetSquareColour(_selectedPieceSquare, _boardRep.boardColours.white.selectedPieceColor,
            _boardRep.boardColours.black.selectedPieceColor);
        _currentState = InputState.DraggingPiece;
        UnitySystemConsoleRedirector.Redirect();
        Console.WriteLine(index);
    }

    private void PlacePiece(Vector2 mousePos)
    {
        if (MouseSquare(mousePos, out var targetSquare))
        {
            if (targetSquare.Equals(_selectedPieceSquare))
            {
                _boardRep.ResetPiecePosition(_selectedPieceSquare);
                if (_currentState == InputState.DraggingPiece)
                {
                    _currentState = InputState.PieceSelected;
                }
                else
                {
                    _currentState = InputState.None;
                    _boardRep.ApplyColours();
                }
            }
            else
            {
                var targetIndex = targetSquare.rankPos * 8 + targetSquare.filePos;
                if (Pieces.PieceObj.IsColour(_board.positionArr[targetIndex], _board.colourToMove) &&
                    _board.positionArr[targetIndex] != 0)
                {
                    CancelPieceSelection();
                    PieceSelect(mousePos);
                }
                else
                {
                    TryMakeMove(_selectedPieceSquare, targetSquare);
                }
            }
        }
        else
        {
            CancelPieceSelection();
        }
    }

    private void TryMakeMove(Utilities.SquarePosition startSquare, Utilities.SquarePosition targetSquare)
    {
        var startIndex = Utilities.FindPosition(startSquare);
        var targetIndex = Utilities.FindPosition(targetSquare);
        var moveIsLegal = false;
        var chosenMove = new Utilities.Move();

        var moveGenerator = new MoveGenerator();
        var wantsKnightPromotion = Input.GetKey(KeyCode.LeftAlt);

        var legalMoves = moveGenerator.GenerateMoves(_board);
        for (var i = 0; i < legalMoves.Count; i++)
        {
            var legalMove = legalMoves[i];

            if (legalMove.StartSquare == startIndex && legalMove.TargetSquare == targetIndex)
            {
                if (legalMove.Promotion)
                {
                    if (legalMove.MoveMarker == Utilities.Move.Marker.QueenPromotion && wantsKnightPromotion) continue;

                    if (legalMove.MoveMarker != Utilities.Move.Marker.QueenPromotion && !wantsKnightPromotion) continue;
                }

                moveIsLegal = true;
                chosenMove = legalMove;

                break;
            }
        }

        if (moveIsLegal)
        {
            ChoseMove(chosenMove);
            _currentState = InputState.None;
        }
        else
        {
            CancelPieceSelection();
        }
    }

    public void Update()
    {
        HandleInput();
    }

    #endregion

    #region 04. Public variables

    public enum InputState
    {
        None,
        PieceSelected,
        DraggingPiece
    }

    #endregion

    #region 05. Private variables

    private readonly Board _board;

    private readonly BoardRep _boardRep;
    private readonly Camera _cam;

    private InputState _currentState;
    private Utilities.SquarePosition _selectedPieceSquare;

    #endregion

    public Player(Board board)
    {
        _boardRep = Object.FindObjectOfType<BoardRep>();
        _cam = Camera.main;
        _board = board;
    }

    public event Action<Utilities.Move> ONMoveChosen;
}