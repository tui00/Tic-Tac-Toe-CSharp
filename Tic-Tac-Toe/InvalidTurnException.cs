namespace TicTacToe;

[Serializable]
public class InvalidTurnException : Exception
{
    public static string GetMsg(int cell, int player, ulong player1, ulong player2, ulong state)
    {
        return $"Invalid {player} move: {cell}. {player1} vs {player2}. {state}";
    }

    public InvalidTurnException() : base(GetMsg(-1, -1, unchecked ((ulong)-1), unchecked ((ulong)-1), unchecked ((ulong)-1))) { }
    public InvalidTurnException(int cell, int player, ulong player1, ulong player2, ulong state) : base(GetMsg(cell, player, player1, player2, state)) { }
    public InvalidTurnException(int cell, int player, ulong player1, ulong player2, ulong state, Exception inner) : base(GetMsg(cell, player, player1, player2, state), inner) { }
    [Obsolete("Obsolete .ctor")]
    protected InvalidTurnException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
