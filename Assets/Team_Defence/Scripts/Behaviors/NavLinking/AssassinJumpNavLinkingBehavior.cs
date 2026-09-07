using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(fileName = "NewAssassinJumpNavLink", menuName = BehaviorConstants.NavLinkBehaviorMenuName + "AssassinJumpNavLink")]
public class AssassinJumpNavLinkBehavior : ScriptableNavLinkBehavior
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpDuration = 0.8f;

    [SerializeField] private float jumpHeight = 1.2f;

    private Coroutine traversalCoroutine;

    public override void Enter(NPCBase npc)
    {
        npc.ResetMovementAnimationFlags();
        if (npc.Anim != null)
        {
            npc.Anim.SetTrigger(AnimationConstants.JumpingNavLinkAnim);
        }

        traversalCoroutine = npc.StartCoroutine(TraverseNavLinkCoroutine(npc));
    }

    public override void UpdateBehavior(NPCBase npc)
    {
    }

    public override void Exit(NPCBase npc)
    {
        if (traversalCoroutine != null)
        {
            npc.StopCoroutine(traversalCoroutine);
            traversalCoroutine = null;
        }
    }

    private IEnumerator TraverseNavLinkCoroutine(NPCBase npc)
    {
        if (npc.Agent == null) yield break;

        OffMeshLinkData data = npc.Agent.currentOffMeshLinkData;
        Vector3 startPos = npc.transform.position;
        Vector3 endPos = data.endPos;

        Vector3 lookTarget = new Vector3(endPos.x, npc.transform.position.y, endPos.z);
        npc.transform.LookAt(lookTarget);

        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / jumpDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, normalizedTime);

            currentPos.y += Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;

            npc.transform.position = currentPos;
            yield return null;
        }

        npc.transform.position = endPos;
        npc.Agent.CompleteOffMeshLink();

        Debug.Log("[Behavior] The assassin successfully executed a leaping pursuit using the SO asset.");

        if (npc is AssassinNPC assassin)
        {
            assassin.TargetDistanceCheckAndTransition();
        }
    }
}