using System;

public class AntiNerdPoll : SkyManager
{
    // returns true if the block cannot be placed.
    public static bool NerdPoleCheck( EntityAlive entity )
    {
        if(entity is EntityPlayerLocal)
        {
            // If you are in God mode, let you do whatever.
            if((entity as EntityPlayerLocal).IsGodMode == true)
                return false;

            if((entity as EntityPlayerLocal).IsFlyMode == true)
                return false;

            if((entity as EntityPlayerLocal).IsInElevator())
                return false;

            if((entity as EntityPlayerLocal).IsInWater() )
                return false;
        
            // If you are on a ladder, let you place blocks.
            if((entity as EntityPlayerLocal).isAttachedToLadder)
                return false;

            // If you aren't on the ground, don't place the block.
            if(!entity.onGround )
                return true;
        }
        return false;
    }

}