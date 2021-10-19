#region

using System;

#endregion

public class Board
{
    #region 02. Actions

    private void AddForPiece(int pieceType, int pieceColour, int boardCoordinate)
    {
        switch (pieceType)
        {
            case Pieces.PieceObj.King:
            {
                whiteBlackKings[pieceColour] = boardCoordinate;
                break;
            }
            case Pieces.PieceObj.Pawn:
            {
                whiteBlackArrPawn[pieceColour].AddPieceAtSquare(boardCoordinate);
                break;
            }
            case Pieces.PieceObj.Knight:
            {
                whiteBlackArrKnights[pieceColour].AddPieceAtSquare(boardCoordinate);
                break;
            }
            case Pieces.PieceObj.Bishop:
            {
                whiteBlackArrBishops[pieceColour].AddPieceAtSquare(boardCoordinate);
                break;
            }
            case Pieces.PieceObj.Rook:
            {
                whiteBlackArrRooks[pieceColour].AddPieceAtSquare(boardCoordinate);
                break;
            }
            case Pieces.PieceObj.Queen:
            {
                whiteBlackArrQueens[pieceColour].AddPieceAtSquare(boardCoordinate);
                break;
            }
        }
    }

    private void AddPieces()
    {
        CreateDataArrays();
        var startingBoard = Utilities.StartBoardFen();
        for (var boardCoordinate = 0; boardCoordinate < 64; boardCoordinate++)
        {
            UnitySystemConsoleRedirector.Redirect();
            //Console.WriteLine(boardCoordinate);
            var piece = startingBoard.squares[boardCoordinate];
            positionArr[boardCoordinate] = piece;

            if (piece != Pieces.PieceObj.None)
            {
                var pieceType = Pieces.PieceObj.PieceType(piece);
                var pieceColour = Pieces.PieceObj.IsColour(piece, Pieces.PieceObj.White) ? 0 : 1;
                AddForPiece(pieceType, pieceColour, boardCoordinate);
                Console.WriteLine(boardCoordinate + " " + pieceType + " " + pieceColour);
            }
        }

        whiteToMove = startingBoard.wTurn;
        colourToMove = whiteToMove ? Pieces.PieceObj.White : Pieces.PieceObj.Black;
        enemyPlayerColour = whiteToMove ? Pieces.PieceObj.Black : Pieces.PieceObj.White;
        currTurnPlayerColour = whiteToMove switch
        {
            true => 0,
            _ => 1
        };
        state = 15;
    }

    private void CreateDataArrays()
    {
        positionArr = new int[64];
        whiteBlackKings = new int[2];
        whiteBlackArrPawn = new[] {new Pieces(Utilities.MAXPawn), new Pieces(Utilities.MAXPawn)};
        whiteBlackArrKnights = new[] {new Pieces(Utilities.MAXPiece), new Pieces(Utilities.MAXPiece)};
        whiteBlackArrBishops = new[] {new Pieces(Utilities.MAXPiece), new Pieces(Utilities.MAXPiece)};
        whiteBlackArrRooks = new[] {new Pieces(Utilities.MAXPiece), new Pieces(Utilities.MAXPiece)};
        whiteBlackArrQueens = new[] {new Pieces(Utilities.MAXQueen), new Pieces(Utilities.MAXQueen)};
        _piecesArrArr = new[]
        {
            whiteBlackArrPawn[0],
            whiteBlackArrKnights[0],
            whiteBlackArrBishops[0],
            whiteBlackArrRooks[0],
            whiteBlackArrQueens[0],
            whiteBlackArrPawn[1],
            whiteBlackArrKnights[1],
            whiteBlackArrBishops[1],
            whiteBlackArrRooks[1],
            whiteBlackArrQueens[1]
        };
    }

    public void CreateStartPosition()
    {
        AddPieces();
    }

    public static T[,] Make2DArray<T>(T[] input, int height, int width)
    {
        var output = new T[height, width];
        for (var i = 0; i < height; i++)
        for (var j = 0; j < width; j++)
            output[i, j] = input[i * width + j];
        return output;
    }


    public void MakeMove(Utilities.Move attemptedMove)
    {
        state = 0;
        var enemyPlayerColour = 1 - currTurnPlayerColour;
        var start = attemptedMove.StartSquare;
        var target = attemptedMove.TargetSquare;
        var capturedPieceType = Pieces.PieceObj.PieceType(positionArr[target]);
        var movePiece = positionArr[start];
        var movePieceType = Pieces.PieceObj.PieceType(movePiece);
        var marker = attemptedMove.MoveMarker;
        var promotion = attemptedMove.Promotion;
        var ep = marker == Utilities.Move.Marker.Ep;

        state |= (ushort) (capturedPieceType << 8);

        if (capturedPieceType != 0 && !ep)
        {
            var dif = (uint) Math.Log(capturedPieceType, 2.0) + 1;
            _piecesArrArr[enemyPlayerColour * 5 + capturedPieceType - dif].RemovePieceAtSquare(target);
        }

        if (movePieceType == Pieces.PieceObj.King)
        {
            whiteBlackKings[currTurnPlayerColour] = target;
        }
        else if (movePieceType != 0)
        {
            var dif = (uint) Math.Log(movePieceType, 2.0) + 1;
            _piecesArrArr[currTurnPlayerColour * 5 + movePieceType - dif].MovePiece(start, target);
        }

        var pieceOnTargetSquare = movePiece;


        if (promotion)
        {
            pieceOnTargetSquare = Pieces.PieceObj.Queen | colourToMove;
            whiteBlackArrPawn[currTurnPlayerColour].RemovePieceAtSquare(target);
        }
        else
        {
            switch (marker)
            {
                case Utilities.Move.Marker.Ep:
                {
                    var epPawnSquare = target + (colourToMove == Pieces.PieceObj.White ? -8 : 8);
                    state |= (ushort) (positionArr[epPawnSquare] << 8);
                    positionArr[epPawnSquare] = 0;
                    whiteBlackArrPawn[enemyPlayerColour].RemovePieceAtSquare(epPawnSquare);
                    break;
                }
                case Utilities.Move.Marker.Castling:
                {
                    var kingside = target == 6 || target == 62;
                    var castlingRookFromIndex = kingside ? target + 1 : target - 2;
                    var castlingRookToIndex = kingside ? target - 1 : target + 1;

                    positionArr[castlingRookFromIndex] = Pieces.PieceObj.None;
                    positionArr[castlingRookToIndex] = Pieces.PieceObj.Rook | colourToMove;

                    whiteBlackArrRooks[currTurnPlayerColour].MovePiece(castlingRookFromIndex, castlingRookToIndex);
                    break;
                }
            }
        }


        positionArr[target] = pieceOnTargetSquare;
        positionArr[start] = 0;


        if (marker == 4)
        {
            var file = Utilities.FilePos(start) + 1;
            state |= (ushort) (file << 4);
        }

        whiteToMove = !whiteToMove;
        if (whiteToMove)
            colourToMove = Pieces.PieceObj.White;
        else
            colourToMove = Pieces.PieceObj.Black;
        currTurnPlayerColour = 1 - currTurnPlayerColour;
    }

    #endregion

    #region 04. Public variables

    public int colourToMove;

    public int currTurnPlayerColour;
    public int enemyPlayerColour;

    public int[] positionArr;

    public uint state;
    public Pieces[] whiteBlackArrBishops;
    public Pieces[] whiteBlackArrKnights;
    public Pieces[] whiteBlackArrPawn;
    public Pieces[] whiteBlackArrQueens;
    public Pieces[] whiteBlackArrRooks;
    public int[] whiteBlackKings;
    public bool whiteToMove;

    #endregion

    #region 05. Private variables

    private Pieces[] _piecesArrArr;

    #endregion
}