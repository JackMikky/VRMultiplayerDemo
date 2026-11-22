using UnityEngine;

public static class AnimationConstants
{
    #region Animation Parameter Names

    public static string IsWalkingString = "IsWalking";

    public static string IsIdlingString = "IsIdling";

    public static string WaveString = "Wave";

    public static string ExposedString = "Exposed";

    public static string IsSearchingString = "IsSearching";

    public static string IsRunningString = "IsRunning";

    public static string IsStayingString = "IsStaying";

    public static string StayingIndexString = "StayingIndex";

    public static string JumpingNavLinkAnimString = "Jumping";

    public static string ApplauseString = "Applause";

    public static string IsTalkingString = "IsTalking";

    #endregion Animation Parameter Names

    #region Animation Hashes

    /// <summary>Animator parameter "Walking".For all NPCs.</summary>
    public static readonly int IsWalking = Animator.StringToHash(IsWalkingString);

    /// <summary> Animator parameter "Idling".For all NPCs.</summary>
    public static readonly int IsIdling = Animator.StringToHash(IsIdlingString);

    /// <summary>
    /// Animator parameter "Wave".For Citizens.
    /// </summary>
    public static readonly int Wave = Animator.StringToHash(WaveString);

    /// <summary>
    /// Animator parameter "Exposed".For Assassins.
    /// </summary>
    public static readonly int Exposed = Animator.StringToHash(ExposedString);

    /// <summary>
    /// Animator parameter "IsSearching". For Assassins, used while crouching to search/pick up a weapon or plant a bomb.
    /// </summary>
    public static readonly int IsSearching = Animator.StringToHash(IsSearchingString);

    /// <summary>
    /// Animator parameter "AnimationConstants.IsRunning".For all NPCs.
    /// </summary>
    public static readonly int IsRunning = Animator.StringToHash(IsRunningString);

    public static readonly int Staying = Animator.StringToHash(IsStayingString);

    public static readonly int StayingIndex = Animator.StringToHash(StayingIndexString);

    /// <summary>
    /// Animator parameter "Jumping".For all NPCs.
    /// </summary>
    public static readonly int JumpingNavLinkAnim = Animator.StringToHash(JumpingNavLinkAnimString);

    /// <summary>
    /// Animator parameter "Applause".For Citizens.
    /// </summary>
    public static readonly int Applause = Animator.StringToHash(ApplauseString);

    /// <summary>
    /// Animator parameter "IsTalking". For VIPs.
    /// </summary>
    public static readonly int IsTalking = Animator.StringToHash(IsTalkingString);

    #endregion Animation Hashes
}