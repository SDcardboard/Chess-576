#region

using System;
using System.Collections.Generic;
using UnityEngine;

#endregion

public class GameManager : MonoBehaviour
{
    public List<Utilities.Move> gameMoves;
    private Player _blackPlayer;
    private BoardRep _boardRep;
    private Result _gameState;
    private Player _playerToMove;
    private Player _whitePlayer;
    private Board Board { get; set; }

    private void Start()
    {
        _boardRep = FindObjectOfType<BoardRep>();
        gameMoves = new List<Utilities.Move>();
        Board = new Board();
        NewGame();
    }

    private void Update()
    {
        if (_gameState == Result.Playing) _playerToMove.Update();
    }

    public event Action ForLoadedPosition;
    public event Action<Utilities.Move> ForMove;

    private void ForGivenMove(Utilities.Move move)
    {
        Board.MakeMove(move);
        gameMoves.Add(move);
        ForMove?.Invoke(move);
        _boardRep.OnMoveMade(Board, move);
        CheckGameState();
    }

    private void NewGame()
    {
        gameMoves.Clear();
        Board.CreateStartPosition();
        ForLoadedPosition?.Invoke();
        _boardRep.UpdatePosition(Board);
        _boardRep.ApplyColours();
        CreatePlayer(ref _whitePlayer);
        CreatePlayer(ref _blackPlayer);
        _gameState = Result.Playing;
        CheckGameState();
    }

    private void CheckGameState()
    {
        _gameState = GetGameState();
        if (_gameState == Result.Playing)
            _playerToMove = Board.whiteToMove ? _whitePlayer : _blackPlayer;
        else
            Debug.Log("end");
    }

    private Result GetGameState()
    {
        var moveGenerator = new MoveGenerator();
        var moves = moveGenerator.GenerateMoves(Board);

        switch (moves.Count)
        {
            case 0 when moveGenerator.InCheck():
                return Board.whiteToMove switch
                {
                    true => Result.WhiteIsMated,
                    _ => Result.BlackIsMated
                };
            case 0:
                return Result.Stalemate;
            default:
                return Result.Playing;
        }
    }

    private void CreatePlayer(ref Player player)
    {
        if (player != null) player.ONMoveChosen -= ForGivenMove;
        player = new Player(Board);
        player.ONMoveChosen += ForGivenMove;
    }

    private enum Result
    {
        Playing,
        WhiteIsMated,
        BlackIsMated,
        Stalemate
    }
}