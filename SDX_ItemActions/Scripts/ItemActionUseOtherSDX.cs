using System.Collections.Generic;
using UnityEngine;
class ItemActionUseOtherSDX : ItemActionUseOther
{
    List<string> lstTargetCvar = new List<string>();

    public override void ReadFrom(DynamicProperties _props)
    {
        base.ReadFrom(_props);

        if (_props.Values.ContainsKey("TargetCVARs"))
        {
            string strTemp = _props.Values["TargetCVARs"].ToString();
            string[] array = strTemp.Split(new char[]
            {
                ','
            });
            for (int i = 0; i < array.Length; i++)
            {
                if (lstTargetCvar.Contains(array[i].ToString()))
                    continue;
                lstTargetCvar.Add(array[i].ToString());
            }
        }
    }

    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        FeedInventoryData feedInventoryData = (FeedInventoryData)_actionData;
        EntityAlive entityAlive = null;
        EntityAlive holdingEntity = feedInventoryData.invData.holdingEntity;
        feedInventoryData.ray = holdingEntity.GetLookRay();
        if (Voxel.Raycast(feedInventoryData.invData.world, feedInventoryData.ray, 4f, -538750981, 128, this.SphereRadius))
            entityAlive = (ItemActionUseOther.GetEntityFromHit(Voxel.voxelRayHitInfo) as EntityAlive);

        if (entityAlive != null)
        {
            foreach (string strCVar in this.lstTargetCvar)
            {
                Debug.Log("' Checking for CVAR: " + strCVar);
                if (entityAlive.Buffs.HasCustomVar(strCVar))
                {
                    if (entityAlive.Buffs.GetCustomVar(strCVar) > 100)
                    {
                        base.ExecuteAction(_actionData, _bReleased);
                        if (feedInventoryData.bFeedingStarted)
                            entityAlive.Buffs.SetCustomVar(strCVar, 0f, true);
                    }
                    return;
                }
            }
        }
        else
        {
            Debug.Log("No Target Entity");
        }
    }
}

