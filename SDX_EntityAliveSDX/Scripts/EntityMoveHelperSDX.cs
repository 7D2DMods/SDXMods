using System;
using GamePath;
using UnityEngine;
class EntityMoveHelperSDX : EntityMoveHelper
{

    public EntityMoveHelperSDX(EntityAlive _entity) : base(_entity)
    {
       // Debug.Log(" Using EntityMoveHelperSDX() ");
    }

    public override void UpdateMoveHelper()
    {
        base.UpdateMoveHelper();
        return;
        if (!this.IsActive)
        {
            return;
        }
        if (--this.expiryTicks <= 0)
        {
            this.Stop();
            return;
        }

     
        Vector3 position = this.entity.position;
        Vector3 vector = this.moveToPos;
        if (this.isTempMove)
        {
            if (!this.IsBlocked)
            {
                this.isTempMove = false;
                this.ResetStuckCheck();
            }
            else
            {
                vector = this.tempMoveToPos;
            }
        }
        float num = vector.x - position.x;
        float num2 = vector.z - position.z;
        float num3 = num * num + num2 * num2;
        float num4 = vector.y - (position.y );

        if ((!this.isDigging && !this.entity.emodel.avatarController.IsAnimationWithMotionRunning()) || this.entity.sleepingOrWakingUp || this.entity.bodyDamage.HasNoArmsAndLegs || !this.entity.bodyDamage.CurrentStun.CanMove() || this.entity.emodel.IsRagdollActive)
        {
            this.entity.SetMoveForward(0f);
            this.ResetStuckCheck();
            return;
        }
        float num5 = this.moveToPos.x - position.x;
        float num6 = this.moveToPos.z - position.z;
        float num7 = num5 * num5 + num6 * num6;
        float num8 = this.moveToPos.y - (position.y );

        float num9 = Mathf.Atan2(num5, num6) * 57.29578f;

        {
            this.moveToDir = Mathf.MoveTowardsAngle(this.moveToDir, num9, 13f);
        }
        float yaw = this.targetYaw;
        if (this.isAutoYaw)
        {


            float y = num5;
            float x = num6;
            if (this.hasNextPos && num7 <= 2.25f)
            {
                float t = Mathf.Sqrt(num7) / 1.5f;
                y = Mathf.Lerp(this.nextMoveToPos.x, this.moveToPos.x, t) - position.x;
                x = Mathf.Lerp(this.nextMoveToPos.z, this.moveToPos.z, t) - position.z;
            }
            if (this.focusTicks > 0)
            {
                this.focusTicks--;
                y = this.focusPos.x - position.x;
                x = this.focusPos.z - position.z;
            }
            yaw = Mathf.Atan2(y, x) * 57.29578f;

        }
        this.entity.SeekYaw(yaw, 0f, 30f);
        float num10 = Mathf.Abs(Mathf.DeltaAngle(num9, this.moveToDir));
        float num11 = 1f;
        if (this.IsUnreachableAbove && !this.entity.IsRunning)
        {
            num11 = 1.3f;
        }
        float num12 = num10 - 15f;
        if (num12 > 0f)
        {
            num11 *= 1f - Utils.FastMin(num12 / 30f, 0.8f);
        }
        if (this.waterPercent > 0.15f)
        {
            num11 *= 1f - this.waterPercent * 0.7f;
        }
        if (num11 > 0.5f)
        {
            if (this.BlockedTime > 0.1f)
            {
                num11 = 0.5f;
            }
            if (this.focusTicks > 0)
            {
                num11 = 0.45f;
            }
        }
        bool isBreakingBlocks = this.entity.IsBreakingBlocks;
        if (isBreakingBlocks)
        {
            num11 = 0.2f;
        }
        if (this.entity.hasBeenAttackedTime > 0)
        {
            num11 = 0.1f;
        }
        this.entity.SetMoveForwardWithModifiers(this.moveSpeed, num11, this.isClimb);
        if (num11 > 0f)
        {
            float x2 = num;
            float z = num2;
            float minMotion = 0.02f * num11;
            float maxMotion = 1f;
            if (!this.isTempMove)
            {
                if (this.sideStepAngle != 0f)
                {
                    float f = (this.moveToDir + this.sideStepAngle) * 0.0174532924f;
                    x2 = Mathf.Sin(f);
                    z = Mathf.Cos(f);
                    minMotion = 0.025f;
                    maxMotion = 0.06f;
                    this.moveToPos = Vector3.MoveTowards(this.moveToPos, position, 0.0100000007f);
                }
                else if (num3 > 0.0625f)
                {
                    float f2 = this.moveToDir * 0.0174532924f;
                    x2 = Mathf.Sin(f2);
                    z = Mathf.Cos(f2);
                }
            }
            this.entity.MakeMotionMoveToward(x2, z, minMotion, maxMotion);
        }

        if (isBreakingBlocks || num10 > 60f || num11 == 0f)
        {
            this.moveToTicks = 0;
        }
        else if (++this.moveToTicks > 5)
        {
            this.moveToTicks = 0;
            float num13 = Mathf.Sqrt(num * num + num4 * num4 + num2 * num2);
            float num14 = this.moveToDistance - num13;
            if (num14 < 0.02f)
            {
                if (num14 < -0.01f)
                {
                    this.moveToDistance = num13;
                }
                if (++this.moveToFailCnt >= 3)
                {
                    this.CheckAreaBlocked();
                    if (this.IsBlocked)
                    {
                        this.DamageScale = 2f;
                        this.obstacleCheckTickDelay = 40;
                        return;
                    }
                  
                }
            }
            else
            {
                this.moveToDistance = num13;
            }
        }
        if (--this.obstacleCheckTickDelay <= 0)
        {
            this.obstacleCheckTickDelay = 4;
            this.IsBlocked = false;
            this.BlockedEntity = null;
            this.blockedPrimaryY = -1;
            this.blockedDistSq = float.MaxValue;
            if (num10 < 10f)
            {
                this.CheckEntityBlocked(position, this.moveToPos);
                this.CheckWorldBlocked();
                this.sideStepAngle = 0f;
                if (!this.IsUnreachableAbove && (this.IsBlocked || this.BlockedEntity))
                {
                    this.sideStepAngle = this.CalcObstacleSideStep();
                    if (this.sideStepAngle != 0f)
                    {
                        this.BlockedEntity = null;
                        this.IsBlocked = false;
                        this.isTempMove = false;
                    }
                }
                if (this.IsBlocked && num8 < -1.5f)
                {
                    float num15 = Mathf.Sqrt(num5 * num5 + num8 * num8 + num6 * num6) + 0.001f;
                    if (num8 / num15 < -0.86f)
                    {
                        this.DigStart(200);
                    }
                }
            }
            this.waterPercent = this.entity.GetWaterLevel() / this.entity.GetHeight();
        }
        if (this.IsBlocked)
        {
            this.BlockedTime += 0.05f;
        }
        else
        {
            this.BlockedTime = 0f;
        }
    }

    public override void ResetStuckCheck()
    {
        Vector3 headPosition = this.entity.getHeadPosition();
        Vector3 vector = this.entity.GetForwardVector();
        vector = Quaternion.Euler(UnityEngine.Random.value * 60f - 30f, UnityEngine.Random.value * 120f - 60f, 0f) * vector;
        this.entity.SetLookPosition(headPosition + vector);

        return;
    }
    public override void CheckEntityBlocked(Vector3 pos, Vector3 endPos)
    {
        base.CheckEntityBlocked(pos, endPos);

        //Debug.Log(" CheckEntityBlocked() Removed");
    }

    public override void Push(EntityAlive blockerEntity)
    {
        base.Push(blockerEntity);
        //Debug.Log(" EntityMoveHelperSDX: Push() ");
    }
}

