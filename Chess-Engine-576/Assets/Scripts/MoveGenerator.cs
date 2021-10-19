#region

using System;
using System.Collections.Generic;
using static System.Math;

#endregion

public class MoveGenerator
{
    #region 02. Actions

    private bool CheckPin(int square)
    {
        if (!_pinPosition) return false;
        return ((_pinRayBitmask >> square) & 1) != 0;
    }

    private void FindAttacks()
    {
        _opponentDiagAttacks = 0;
        var start = 0;
        var target = 8;

        // for long move pieces
        var enemyRooks = _board.whiteBlackArrRooks[_colourTag];
        var enemyQueens = _board.whiteBlackArrQueens[_colourTag];
        var enemyBishops = _board.whiteBlackArrBishops[_colourTag];

        for (var i = 0; i < enemyRooks.Count; i++) SetLongMovePiece(enemyRooks[i], 0, 4);

        for (var i = 0; i < enemyQueens.Count; i++) SetLongMovePiece(enemyQueens[i], 0, 8);

        for (var i = 0; i < enemyBishops.Count; i++) SetLongMovePiece(enemyBishops[i], 4, 8);

        if (_board.whiteBlackArrQueens[_colourTag].Count == 0)
        {
            start = _board.whiteBlackArrRooks[_colourTag].Count > 0 ? 0 : 4;
            target = _board.whiteBlackArrBishops[_colourTag].Count > 0 ? 8 : 4;
        }


        for (var direction = start; direction < target; direction++)
        {
            var isDiagonal = direction > 3;

            var distanceToEdge = _squaresFromEdge[_alliedKingPos][direction];
            var offset = _offsets[direction];
            var isAlliedPiece = false;
            ulong rayMask = 0;

            for (var nSquare = 0; nSquare < distanceToEdge; nSquare++)
            {
                var squarePos = _alliedKingPos + offset * (nSquare + 1);
                rayMask |= 1ul << squarePos;
                var piece = _board.positionArr[squarePos];


                if (piece == Pieces.PieceObj.None) continue;
                if (!Pieces.PieceObj.IsColour(piece, _friendlyColour))
                {
                    var pieceType = Pieces.PieceObj.PieceType(piece);

                    if (isDiagonal && Pieces.PieceObj.BishopOrQueen(pieceType) ||
                        !isDiagonal && Pieces.PieceObj.RookOrQueen(pieceType))
                    {
                        if (isAlliedPiece)
                        {
                            _pinPosition = true;
                            _pinRayBitmask |= rayMask;
                        }

                        else
                        {
                            _checkRayBitmask |= rayMask;
                            _inDoubleCheck = _inCheck;
                            _inCheck = true;
                        }
                    }

                    break;
                }

                if (!isAlliedPiece)
                    isAlliedPiece = true;
                else
                    break;
            }

            if (_inDoubleCheck) break;
        }


        var opponentKnights = _board.whiteBlackArrKnights[_colourTag];
        _opponentKnightAttacks = 0;
        var isKnight = false;

        for (var knightIndex = 0; knightIndex < opponentKnights.Count; knightIndex++)
        {
            var startSquare = opponentKnights[knightIndex];
            _opponentKnightAttacks |= _knightBitboard[startSquare];

            if (isKnight || !Utilities.ContainsSquare(_opponentKnightAttacks, _alliedKingPos)) continue;
            isKnight = true;
            _inDoubleCheck = _inCheck;
            _inCheck = true;
            _checkRayBitmask |= 1ul << startSquare;
        }

        var opponentPawns = _board.whiteBlackArrPawn[_colourTag];
        _enemyPawnAttacks = 0;
        var isPawnCheck = false;

        for (var pawnIndex = 0; pawnIndex < opponentPawns.Count; pawnIndex++)
        {
            var pawnSquare = opponentPawns[pawnIndex];
            var pawnAttacks = _pawnBitboard[pawnSquare][_colourTag];
            _enemyPawnAttacks |= pawnAttacks;

            if (isPawnCheck || !Utilities.ContainsSquare(pawnAttacks, _alliedKingPos)) continue;
            isPawnCheck = true;
            _inDoubleCheck = _inCheck;
            _inCheck = true;
            _checkRayBitmask |= 1ul << pawnSquare;
        }

        var enemyKingSquare = _board.whiteBlackKings[_colourTag];

        _opponentAttackMapNoPawns =
            _opponentDiagAttacks | _opponentKnightAttacks | _kingBitboard[enemyKingSquare];
        _opponentAttacks = _opponentAttackMapNoPawns | _enemyPawnAttacks;
    }

    public List<Utilities.Move> GenerateMoves(Board board)
    {
        _board = board;
        Init();
        FindAttacks();
        KingMoves();
        if (_inDoubleCheck) return _moves; // no need to calculate further moves at this point as king must move
        LongMoves();
        KnightMoves();
        PawnMoves();
        return _moves;
    }

    private void GenLongMoves(int startSquare, int startDirIndex, int endDirIndex)
    {
        var isPinned = CheckPin(startSquare);


        if (_inCheck && isPinned) return;

        for (var directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
        {
            var currentDirOffset = _offsets[directionIndex];


            if (isPinned && !RayMovement(currentDirOffset, _alliedKingPos, startSquare)) continue;

            for (var n = 0; n < _squaresFromEdge[startSquare][directionIndex]; n++)
            {
                var targetSquare = startSquare + currentDirOffset * (n + 1);
                var targetSquarePiece = _board.positionArr[targetSquare];


                if (Pieces.PieceObj.IsColour(targetSquarePiece, _friendlyColour)) break;

                var isCapture = targetSquarePiece != Pieces.PieceObj.None;

                var movePreventsCheck = SquareIsInCheckRay(targetSquare);
                if (movePreventsCheck || !_inCheck)
                    _moves.Add(Utilities.Move.CreateMoveInstance(startSquare, targetSquare));

                if (isCapture || movePreventsCheck) break;
            }
        }
    }

    public bool InCheck()
    {
        return _inCheck;
    }

    private bool InCheckAfterEnPassant(int startSquare, int targetSquare, int epCapturedPawnSquare)
    {
        _board.positionArr[targetSquare] = _board.positionArr[startSquare];
        _board.positionArr[startSquare] = Pieces.PieceObj.None;
        _board.positionArr[epCapturedPawnSquare] = Pieces.PieceObj.None;
        var inCheckAfterEpCapture = PostEpCaptureSquare(epCapturedPawnSquare);
        _board.positionArr[targetSquare] = Pieces.PieceObj.None;
        _board.positionArr[startSquare] = Pieces.PieceObj.Pawn | _friendlyColour;
        _board.positionArr[epCapturedPawnSquare] = Pieces.PieceObj.Pawn | _opponentColour;
        return inCheckAfterEpCapture;
    }

    private void Init()
    {
        _moves = new List<Utilities.Move>(64);
        _inCheck = false;
        _inDoubleCheck = false;
        _pinPosition = false;
        _checkRayBitmask = 0;
        _pinRayBitmask = 0;
        _friendlyColour = _board.colourToMove;
        _opponentColour = _board.enemyPlayerColour;
        _alliedKingPos = _board.whiteBlackKings[_board.currTurnPlayerColour];
        _friendlyColourIndex = _board.whiteToMove ? 0 : 1;
        _colourTag = 1 - _friendlyColourIndex;
    }

    private void KingMoves()
    {
        for (var i = 0; i < _kingMoves[_alliedKingPos].Length; i++)
        {
            int targetSquare = _kingMoves[_alliedKingPos][i];
            var pieceOnTargetSquare = _board.positionArr[targetSquare];


            if (Pieces.PieceObj.IsColour(pieceOnTargetSquare, _friendlyColour)) continue;

            var isCapture = Pieces.PieceObj.IsColour(pieceOnTargetSquare, _opponentColour);
            if (!isCapture)


                if (SquareIsInCheckRay(targetSquare))
                    continue;


            if (SquareIsAttacked(targetSquare)) continue;
            _moves.Add(Utilities.Move.CreateMoveInstance(_alliedKingPos, targetSquare));


            if (_inCheck || isCapture) continue;
            if ((targetSquare == 5 || targetSquare == 61) && CastleShortLegality)
            {
                var castleShort = targetSquare + 1;
                if (_board.positionArr[castleShort] != Pieces.PieceObj.None) continue;
                if (!SquareIsAttacked(castleShort))
                    _moves.Add(Utilities.Move.CreateMoveInstanceMarker(_alliedKingPos,
                        castleShort,
                        Utilities.Move.Marker.Castling));
            }

            else if ((targetSquare == 3 || targetSquare == 59) && CastleLongLegality)
            {
                var castleLong = targetSquare - 1;
                if (_board.positionArr[castleLong] != Pieces.PieceObj.None ||
                    _board.positionArr[castleLong - 1] != Pieces.PieceObj.None) continue;
                if (!SquareIsAttacked(castleLong))
                    _moves.Add(Utilities.Move.CreateMoveInstanceMarker(_alliedKingPos,
                        castleLong,
                        Utilities.Move.Marker.Castling));
            }
        }
    }

    private void KnightMoves()
    {
        var alliedKnights = _board.whiteBlackArrKnights[_friendlyColourIndex];

        for (var i = 0; i < alliedKnights.Count; i++)
        {
            var startSquare = alliedKnights[i];

            if (CheckPin(startSquare)) continue;

            for (var movePos = 0; movePos < _knightMoves[startSquare].Length; movePos++)
            {
                int targetSquare = _knightMoves[startSquare][movePos];
                var targetSquarePiece = _board.positionArr[targetSquare];
                Pieces.PieceObj.IsColour(targetSquarePiece, _opponentColour);
                if (Pieces.PieceObj.IsColour(targetSquarePiece, _friendlyColour) ||
                    _inCheck && !SquareIsInCheckRay(targetSquare))
                    continue;

                _moves.Add(Utilities.Move.CreateMoveInstance(startSquare, targetSquare));
            }
        }
    }

    private void LongMoves()
    {
        var rooks = _board.whiteBlackArrRooks[_friendlyColourIndex];
        for (var i = 0; i < rooks.Count; i++) GenLongMoves(rooks[i], 0, 4);

        var bishops = _board.whiteBlackArrBishops[_friendlyColourIndex];
        for (var i = 0; i < bishops.Count; i++) GenLongMoves(bishops[i], 4, 8);

        var queens = _board.whiteBlackArrQueens[_friendlyColourIndex];
        for (var i = 0; i < queens.Count; i++) GenLongMoves(queens[i], 0, 8);
    }

    private void MakePromotionMoves(int fromSquare, int toSquare)
    {
        _moves.Add(Utilities.Move.CreateMoveInstanceMarker(fromSquare, toSquare, Utilities.Move.Marker.QueenPromotion));
    }

    private void PawnMoves()
    {
        var myPawns = _board.whiteBlackArrPawn[_friendlyColourIndex];
        var pawnOffset = _friendlyColour == Pieces.PieceObj.White ? 8 : -8;
        var startRank = _board.whiteToMove ? 1 : 6;
        var finalRankBeforePromotion = _board.whiteToMove ? 6 : 1;

        var enPassantFile = ((int) (_board.state >> 4) & 15) - 1;
        var enPassantSquare = -1;
        if (enPassantFile != -1) enPassantSquare = 8 * (_board.whiteToMove ? 5 : 2) + enPassantFile;

        for (var i = 0; i < myPawns.Count; i++)
        {
            var startSquare = myPawns[i];
            var rank = Utilities.RankPos(startSquare);
            var oneStepFromPromotion = rank == finalRankBeforePromotion;

            if (true)
            {
                var squareOneForward = startSquare + pawnOffset;


                if (_board.positionArr[squareOneForward] == Pieces.PieceObj.None)

                    if (!CheckPin(startSquare) || RayMovement(pawnOffset, startSquare, _alliedKingPos))
                    {
                        if (!_inCheck || SquareIsInCheckRay(squareOneForward))
                        {
                            if (oneStepFromPromotion)
                                MakePromotionMoves(startSquare, squareOneForward);
                            else
                                _moves.Add(Utilities.Move.CreateMoveInstance(startSquare, squareOneForward));
                        }


                        if (rank == startRank)
                        {
                            var squareTwoForward = squareOneForward + pawnOffset;
                            if (_board.positionArr[squareTwoForward] == Pieces.PieceObj.None)

                                if (!_inCheck || SquareIsInCheckRay(squareTwoForward))
                                    _moves.Add(Utilities.Move.CreateMoveInstanceMarker(startSquare, squareTwoForward,
                                        4));
                        }
                    }
            }


            for (var j = 0; j < 2; j++)

                if (_squaresFromEdge[startSquare][_pawnAttackDirections[_friendlyColourIndex][j]] > 0)
                {
                    var pawnCaptureDir = _offsets[_pawnAttackDirections[_friendlyColourIndex][j]];
                    var targetSquare = startSquare + pawnCaptureDir;
                    var targetPiece = _board.positionArr[targetSquare];


                    if (CheckPin(startSquare) &&
                        !RayMovement(pawnCaptureDir, _alliedKingPos, startSquare)) continue;


                    if (Pieces.PieceObj.IsColour(targetPiece, _opponentColour))
                    {
                        if (_inCheck && !SquareIsInCheckRay(targetSquare)) continue;

                        if (oneStepFromPromotion)
                            MakePromotionMoves(startSquare, targetSquare);
                        else
                            _moves.Add(Utilities.Move.CreateMoveInstance(startSquare, targetSquare));
                    }


                    if (targetSquare != enPassantSquare) continue;
                    var epCapturedPawnSquare = targetSquare + (_board.whiteToMove ? -8 : 8);
                    if (!InCheckAfterEnPassant(startSquare, targetSquare, epCapturedPawnSquare))
                        _moves.Add(Utilities.Move.CreateMoveInstanceMarker(startSquare, targetSquare,
                            Utilities.Move.Marker.Ep));
                }
        }
    }

    private bool PostEpCaptureSquare(int epCaptureSquare)
    {
        if (Utilities.ContainsSquare(_opponentAttackMapNoPawns, _alliedKingPos)) return true;

        var dirIndex = epCaptureSquare < _alliedKingPos ? 2 : 3;
        for (var i = 0;
            _alliedKingPos >= 0 && _squaresFromEdge.Length > _alliedKingPos &&
            i < _squaresFromEdge[_alliedKingPos][dirIndex];
            i++)
        {
            var squareIndex = _alliedKingPos + _offsets[dirIndex] * (i + 1);
            var piece = _board.positionArr[squareIndex];
            if (piece == Pieces.PieceObj.None) continue;
            if (Pieces.PieceObj.IsColour(piece, _friendlyColour)) break;


            if (Pieces.PieceObj.RookOrQueen(piece)) return true;


            break;
        }


        for (var i = 0; i < 2; i++)

            if (_squaresFromEdge[_alliedKingPos][_pawnAttackDirections[_friendlyColourIndex][i]] > 0)
            {
                var piece = _board.positionArr[
                    _alliedKingPos + _offsets[_pawnAttackDirections[_friendlyColourIndex][i]]];
                if (piece == (Pieces.PieceObj.Pawn | _opponentColour))
                    return true;
            }

        return false;
    }

    private bool RayMovement(int rayDir, int startSquare, int targetSquare)
    {
        var moveDir = _directionLookup[targetSquare - startSquare + 63];
        return rayDir == moveDir || -rayDir == moveDir;
    }

    private void SetLongMovePiece(int startSquare, int startDirIndex, int endDirIndex)
    {
        for (var directionIndex = startDirIndex; directionIndex < endDirIndex; directionIndex++)
        {
            var currentDirOffset = _offsets[directionIndex];
            for (var n = 0; n < _squaresFromEdge[startSquare][directionIndex]; n++)
            {
                var targetSquare = startSquare + currentDirOffset * (n + 1);
                var targetSquarePiece = _board.positionArr[targetSquare];
                _opponentDiagAttacks |= 1ul << targetSquare;
                if (targetSquare == _alliedKingPos) continue;
                if (targetSquarePiece != Pieces.PieceObj.None)
                    break;
            }
        }
    }

    private bool SquareIsAttacked(int square)
    {
        if (Utilities.ContainsSquare(_opponentAttacks, square)) return true;
        return false;
    }

    private bool SquareIsInCheckRay(int square)
    {
        return _inCheck switch
        {
            true when ((_checkRayBitmask >> square) & 1) != 0 => true,
            _ => false
        };
    }

    #endregion

    #region 04. Public variables

    private static int[] _directionLookup;
    private ulong _enemyPawnAttacks;
    private static ulong[] _kingBitboard;
    private static byte[][] _kingMoves;
    private static ulong[] _knightBitboard;
    private static byte[][] _knightMoves;
    private static int[] _offsets = {8, -8, -1, 1, 7, -7, 9, -9};
    private ulong _opponentAttacks;

    private static byte[][] _pawnAttackDirections =
    {
        new byte[] {4, 6},
        new byte[] {7, 5}
    };

    private static ulong[][] _pawnBitboard;
    private static int[][] _squaresFromEdge;

    #endregion

    #region 05. Private variables

    private int _alliedKingPos;

    private Board _board;
    private ulong _checkRayBitmask;
    private int _colourTag;
    private int _friendlyColour;
    private int _friendlyColourIndex;

    private bool _inCheck;
    private bool _inDoubleCheck;


    private List<Utilities.Move> _moves;
    private ulong _opponentAttackMapNoPawns;
    private int _opponentColour;
    private ulong _opponentDiagAttacks;
    private ulong _opponentKnightAttacks;
    private bool _pinPosition;
    private ulong _pinRayBitmask;

    private bool CastleLongLegality
    {
        get
        {
            var mask = _board.whiteToMove ? 2 : 8;
            return (_board.state & mask) != 0;
        }
    }

    private bool CastleShortLegality
    {
        get
        {
            var mask = _board.whiteToMove ? 1 : 4;
            return (_board.state & mask) != 0;
        }
    }

    #endregion


    static MoveGenerator()
    {
        _squaresFromEdge = new int[64][];
        _kingMoves = new byte[64][];
        var pawnAttacksWhite = new int[64][];
        var pawnAttacksBlack = new int[64][];
        _knightMoves = new byte[64][];
        var bishopMoves = new ulong[64];
        var rookMoves = new ulong[64];
        var queenMoves = new ulong[64];
        _kingBitboard = new ulong[64];
        _pawnBitboard = new ulong[64][];
        int[] allKnightJumps = {15, 17, -17, -15, 10, -6, 6, -10};
        _knightBitboard = new ulong[64];

        for (var i = 0; i < 64; i++)
        {
            var y = i / 8;
            var x = i - y * 8;
            var north = 7 - y;
            var east = 7 - x;
            _squaresFromEdge[i] = new int[8];
            UnitySystemConsoleRedirector.Redirect();
            Console.WriteLine(i);
            _squaresFromEdge[i][0] = north;
            _squaresFromEdge[i][1] = y;
            _squaresFromEdge[i][2] = x;
            _squaresFromEdge[i][3] = east;
            _squaresFromEdge[i][4] = Min(north, x);
            _squaresFromEdge[i][5] = Min(y, east);
            _squaresFromEdge[i][6] = Min(north, east);
            _squaresFromEdge[i][7] = Min(y, x);

            var knightPositions = new List<byte>();
            ulong knightBitboardMarker = 0;
            foreach (var knightJumpDelta in allKnightJumps)
            {
                var knightJumpSquare = i + knightJumpDelta;
                if (knightJumpSquare < 0 || knightJumpSquare >= 64) continue;
                var knightSquareY = knightJumpSquare / 8;
                var knightSquareX = knightJumpSquare - knightSquareY * 8;

                var maxCoordMoveDst = Max(Abs(x - knightSquareX), Abs(y - knightSquareY));
                if (maxCoordMoveDst != 2) continue;
                knightPositions.Add((byte) knightJumpSquare);
                knightBitboardMarker |= 1ul << knightJumpSquare;
            }

            _knightMoves[i] = knightPositions.ToArray();
            MoveGenerator._knightBitboard[i] = knightBitboardMarker;


            var legalKingMoves = new List<byte>();
            foreach (var kingMoveDelta in _offsets)
            {
                var kingMoveSquare = i + kingMoveDelta;
                if (kingMoveSquare < 0 || kingMoveSquare >= 64) continue;
                var kingSquareY = kingMoveSquare / 8;
                var kingSquareX = kingMoveSquare - kingSquareY * 8;

                var maxCoordMoveDst = Max(Abs(x - kingSquareX), Abs(y - kingSquareY));
                if (maxCoordMoveDst != 1) continue;
                legalKingMoves.Add((byte) kingMoveSquare);
                _kingBitboard[i] |= 1ul << kingMoveSquare;
            }

            _kingMoves[i] = legalKingMoves.ToArray();


            var wPawnCapture = new List<int>();
            var bPawnCapture = new List<int>();
            _pawnBitboard[i] = new ulong[2];
            switch (x > 0)
            {
                case true:
                {
                    if (y < 7)
                    {
                        wPawnCapture.Add(i + 7);
                        _pawnBitboard[i][0] |= 1ul << (i + 7);
                    }

                    if (y > 0)
                    {
                        bPawnCapture.Add(i - 9);
                        _pawnBitboard[i][1] |= 1ul << (i - 9);
                    }

                    break;
                }
            }

            switch (x < 7)
            {
                case true:
                {
                    if (y < 7)
                    {
                        wPawnCapture.Add(i + 9);
                        _pawnBitboard[i][0] |= 1ul << (i + 9);
                    }

                    if (y > 0)
                    {
                        bPawnCapture.Add(i - 7);
                        _pawnBitboard[i][1] |= 1ul << (i - 7);
                    }

                    break;
                }
            }

            pawnAttacksWhite[i] = wPawnCapture.ToArray();
            pawnAttacksBlack[i] = bPawnCapture.ToArray();


            for (var directionIndex = 0; directionIndex < 4; directionIndex++)
            {
                var currentDirOffset = _offsets[directionIndex];
                for (var n = 0; n < _squaresFromEdge[i][directionIndex]; n++)
                {
                    var targetSquare = i + currentDirOffset * (n + 1);
                    rookMoves[i] |= 1ul << targetSquare;
                }
            }


            for (var directionIndex = 4; directionIndex < 8; directionIndex++)
            {
                var currentDirOffset = _offsets[directionIndex];
                for (var n = 0; n < _squaresFromEdge[i][directionIndex]; n++)
                {
                    var targetSquare = i + currentDirOffset * (n + 1);
                    bishopMoves[i] |= 1ul << targetSquare;
                }
            }

            queenMoves[i] = rookMoves[i] | bishopMoves[i];
        }

        _directionLookup = new int[127];
        for (var i = 0; i < 127; i++)
        {
            var offset = i - 63;
            var absOffset = Abs(offset);
            var absDir = 1;
            if (absOffset % 9 == 0)
                absDir = 9;
            else if (absOffset % 8 == 0)
                absDir = 8;
            else if (absOffset % 7 == 0) absDir = 7;

            _directionLookup[i] = absDir * Sign(offset);
        }
    }
}