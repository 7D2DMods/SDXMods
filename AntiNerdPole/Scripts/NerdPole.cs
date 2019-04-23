using System;

public class AntiNerdPoll : SkyManager
{
    // returns true if the nerd poll check fails.
    public static bool NerdPoleCheck( EntityAlive entity )
    {
        if(entity is EntityPlayerLocal)
        {
            if(!entity.onGround && !(entity as EntityPlayerLocal).isAttachedToLadder)
                return true;
        }
        return false;
    }

}