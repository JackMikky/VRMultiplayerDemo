using System.Collections.Generic;
using UnityEngine;

public enum EquipmentCategory
{
    None,
    Melee,
    Gun,
    Bomb,
    Normal,
}

public abstract class ScriptableEquipmentBase : ScriptableObject
{
    public EquipmentCategory Category;
    public string Name;

    public float attackSpeed = 1f;
    public float damage = 10f;
    public float attackDistance = 1.5f;

    [Space(5)]
    [TextArea(3, 10)]
    public string Description;

    [Space(5)]
    [Header("Attack VFX")]
    public List<AudioClip> attackSounds;

    public abstract void UseEquipment(Transform user, Transform target, ItemObject weaponContext);

    public virtual void OnEquipped(ItemObject weaponContext)
    { }

    public virtual void OnUnequipped(ItemObject weaponContext)
    { }

    protected void PlayAttackSound(AudioSource audioSource)
    {
        if (audioSource != null && attackSounds != null && attackSounds.Count > 0)
        {
            var randomIndex = Random.Range(0, attackSounds.Count);
            audioSource.PlayOneShot(attackSounds[randomIndex]);
        }
    }
}