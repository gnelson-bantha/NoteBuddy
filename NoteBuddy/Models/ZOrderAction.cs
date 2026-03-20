namespace NoteBuddy.Models;

/// <summary>
/// Defines the possible z-order actions for corkboard items.
/// </summary>
public enum ZOrderAction
{
    /// <summary>Move the item in front of all other items.</summary>
    BringToFront,

    /// <summary>Move the item one level forward (higher z-index).</summary>
    BringForward,

    /// <summary>Move the item one level backward (lower z-index).</summary>
    SendBackward,

    /// <summary>Move the item behind all other items.</summary>
    SendToBack
}
