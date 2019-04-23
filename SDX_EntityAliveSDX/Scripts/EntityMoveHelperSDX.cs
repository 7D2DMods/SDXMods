using UnityEngine;
class EntityMoveHelperSDX : EntityMoveHelper
{
    public EntityMoveHelperSDX(EntityAlive _entity ): base(_entity)
    {
        Debug.Log(" Using EntityMoveHelperSDX() ");
    }

    public override void CheckEntityBlocked( Vector3 pos, Vector3 endPos)
    {
        Debug.Log(" CheckEntityBlocked() Removed");
    }
}

