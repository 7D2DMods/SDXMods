﻿using System;
using System.Collections.Generic;
using UnityEngine;

class EAISetAsTargetNearestEnemySDX : EAISetAsTargetIfHurt
{
    private List<Entity> NearbyEntities = new List<Entity>();
    private List<Entity> NearbyEnemies = new List<Entity>();


    private bool blDisplayLog = false;
    public void DisplayLog(String strMessage)
    {
        if (blDisplayLog)
            Debug.Log(this.GetType() + " : " + this.theEntity.EntityName + ": " + this.theEntity.entityId + ": " + strMessage);
    }
 
    public override bool CanExecute()
    {
        return CheckSurroundingEntities();
    }


    public bool CheckFactionForEnemy(EntityAlive Entity)
    {
        FactionManager.Relationship myRelationship = FactionManager.Instance.GetRelationshipTier(this.theEntity, Entity);
        if (myRelationship == FactionManager.Relationship.Hate)
        {
            DisplayLog(" I hate this entity: " + Entity.ToString());
            return true;
        }
        else
            DisplayLog(" My relationship with this " + Entity.ToString() + " is: " + myRelationship.ToString());
        return false;
    }

    public bool CheckSurroundingEntities()
    {
        this.NearbyEntities.Clear();
        NearbyEnemies.Clear();

        EntityAlive leader = null;
        if (this.theEntity.Buffs.HasCustomVar("Leader"))
        {
            DisplayLog(" leader Detected.");
            int EntityID = (int)this.theEntity.Buffs.GetCustomVar("Leader");
            leader = this.theEntity.world.GetEntity(EntityID) as EntityAlive;

        }

        float originalView = this.theEntity.GetMaxViewAngle();
        this.theEntity.SetMaxViewAngle(180f);

        // Search in the bounds are to try to find the most appealing entity to follow.
        Bounds bb = new Bounds(this.theEntity.position, new Vector3(this.theEntity.GetSeeDistance(), 20f, this.theEntity.GetSeeDistance()));
        this.theEntity.world.GetEntitiesInBounds(typeof(EntityAlive), bb, this.NearbyEntities);
        DisplayLog(" Nearby Entities: " + this.NearbyEntities.Count);
        for (int i = this.NearbyEntities.Count - 1; i >= 0; i--)
        {
            EntityAlive x = (EntityAlive)this.NearbyEntities[i];
            if (x != this.theEntity && x.IsAlive())
            {
                if (x == leader )
                    continue;

                if (!this.theEntity.CanSee(x.position))
                {
                    DisplayLog(" I know an entity is there, but I can't see it: " + x.EntityName);
                    continue;
                }

                DisplayLog("Nearby Entity: " + x.EntityName);
                if (CheckFactionForEnemy(x))
                    NearbyEnemies.Add(x);
            }
        }

        this.theEntity.SetMaxViewAngle(originalView);
        return NearestEnemy();
        
    }

    public bool NearestEnemy()
    {
        if (this.NearbyEnemies.Count == 0)
            return false;

        // Finds the closet block we matched with.
        EntityAlive closeEnemy = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = this.theEntity.position;
        foreach (EntityAlive enemy in NearbyEnemies)
        {
            float dist = Vector3.Distance(enemy.position, currentPos);
            DisplayLog(" Entity: " + enemy.EntityName + "'s distance is: " + dist);
            if (dist < minDist)
            {
                closeEnemy = enemy;
                minDist = dist;
            }
        }

        if (closeEnemy != null)
        {
            DisplayLog(" Closes Enemy: " + closeEnemy.ToString() );
            this.theEntity.SetRevengeTarget(closeEnemy);
            return true;
        }
        return false;
    }
}

