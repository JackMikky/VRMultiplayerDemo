public enum CitizenState
{
    Watching,
    Wandering,
    Staying,
    Interacted,
    Roaming,
    Leaving,
    Panic
}

public enum AssassinState
{
    Staying = 1,
    Approaching = 2,
    Rushing = 3,
    NavLinking = 4,
    Interacted = 5,
    Attacking = 7,
    SeekingWeapon = 8,
    Wandering = 9,
    BombPlanting = 10,
    Leaving = 11
}

public enum PoliceState
{
    Patrolling,
    RandomPatrolling,
    KeepWatching,
    Chasing,
    Staying,
    Interacted
}