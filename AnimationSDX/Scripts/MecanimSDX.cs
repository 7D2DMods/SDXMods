using System;
using System.Collections.Generic;
using System.Reflection;
using SDX.Payload;
using UnityEngine;

class MecanimSDX : AvatarController
{
    // If set to true, logging will be very verbose for troubleshooting
    private readonly bool blDisplayLog = false;

    // interval between changing the indexes in the LateUpdate
    private float nextCheck = 0.0f;
    public float CheckDelay = 5f;

    // Animator support method to keep our current state
    protected AnimatorStateInfo currentBaseState;
    private AnimatorStateInfo currentOverrideLayer;
    private AnimatorStateInfo currentFullBodyOverlayLayer;
    public float animSyncWaitTime = 0.5f;

    // Our transforms for key elements
    protected Transform bipedTransform;
    protected Transform modelTransform;
    protected Transform head;

    // This controls the animations if we are holding a weapon.
    protected Animator rightHandAnimator;
    private string RightHand = "RightHand";
    private Transform rightHandItemTransform;
    private Transform rightHand;

    // support variable for timing attacks.
    protected int specialAttackTicks;
    protected float timeSpecialAttackPlaying;
    protected float timeAttackAnimationPlaying;
    protected float idleTime;

    protected bool isCrippled;


    // Indexes used to add more variety to state machines
    private int AttackIndexes;
    private int AttackIdleIndexes;
    private int CrouchIndexes;
    private int DeathIndexes;
    private int EatingIndexes;
    private int ElectrocutionIndexes;
    private int HarvestIndexes;
    private int IdleIndexes;
    private int JumpIndexes;
    private int PainIndexes;
    private int RagingIndexes;
    private int RandomIndexes;
    private int RunIndexes;
    private int SleeperIndexes;
    private int SpecialAttackIndexes;
    private int SpecialSecondIndexes;
    private int StunIndexes;
    private int WalkIndexes;

    // bools to check if we are performing an action already
    private bool isInDeathAnim;
    private bool IsElectrocuting = false;
    private bool IsHarvesting = false;
    private bool isEating = false;

    // Maintenance varaibles
    protected bool m_bVisible = false;
    private bool CriticalError = false;

    // Jumping tags and bools
    protected int jumpState;
    protected int fpvJumpState;
    protected new int jumpTag;
    private bool Jumping = false;

    protected int movementStateOverride = -1;


    protected bool headDismembered;
    protected bool leftUpperArmDismembered;
    protected bool leftLowerArmDismembered;
    protected bool rightUpperArmDismembered;
    protected bool rightLowerArmDismembered;
    protected bool leftUpperLegDismembered;
    protected bool leftLowerLegDismembered;
    protected bool rightUpperLegDismembered;
    protected bool rightLowerLegDismembered;
    protected Transform neckGore;
    protected Transform neck;

    protected Transform leftUpperArm;
    protected Transform leftLowerArm;
    protected Transform rightLowerArm;
    protected Transform rightUpperArm;
    protected Transform leftUpperLeg;
    protected Transform leftLowerLeg;
    protected Transform rightLowerLeg;
    protected Transform rightUpperLeg;
    protected Transform leftUpperArmGore;
    protected Transform leftLowerArmGore;
    protected Transform rightUpperArmGore;
    protected Transform rightLowerArmGore;
    protected Transform leftUpperLegGore;
    protected Transform leftLowerLegGore;
    protected Transform rightLowerLegGore;
    protected Transform rightUpperLegGore;
    private bool bBlockLookPosition;
    private float currentLookWeight;
    private float lookWeightTarget;
    private bool isJumpStarted;
    private AnimatorStateInfo currentAdditiveLayer;

    protected int itemUseTicks;
    protected HashSet<int> reloadStates = new HashSet<int>();
    protected HashSet<int> deathStates = new HashSet<int>();
    protected AnimatorStateInfo currentWeaponHoldLayer;

    private MecanimSDX()
    {
        this.entity = base.transform.gameObject.GetComponent<EntityAlive>();
        EntityClass entityClass = EntityClass.list[this.entity.entityClass];

        // this.AttackHash = this.GenerateLists(entityClass, "AttackAnimations");
        int.TryParse(entityClass.Properties.Values["AttackIndexes"], out this.AttackIndexes);
        int.TryParse(entityClass.Properties.Values["SpecialAttackIndexes"], out this.SpecialAttackIndexes);
        int.TryParse(entityClass.Properties.Values["SpecialSecondIndexes"], out this.SpecialSecondIndexes);
        int.TryParse(entityClass.Properties.Values["RagingIndexes"], out this.RagingIndexes);
        int.TryParse(entityClass.Properties.Values["ElectrocutionIndexes"], out this.ElectrocutionIndexes);
        int.TryParse(entityClass.Properties.Values["CrouchIndexes"], out this.CrouchIndexes);
        int.TryParse(entityClass.Properties.Values["StunIndexes"], out this.StunIndexes);
        int.TryParse(entityClass.Properties.Values["SleeperIndexes"], out this.SleeperIndexes);
        int.TryParse(entityClass.Properties.Values["HarvestIndexes"], out this.HarvestIndexes);
        int.TryParse(entityClass.Properties.Values["PainIndexes"], out this.PainIndexes);
        int.TryParse(entityClass.Properties.Values["DeathIndexes"], out this.DeathIndexes);
        int.TryParse(entityClass.Properties.Values["RunIndexes"], out this.RunIndexes);
        int.TryParse(entityClass.Properties.Values["WalkIndexes"], out this.WalkIndexes);
        int.TryParse(entityClass.Properties.Values["IdleIndexes"], out this.IdleIndexes);
        int.TryParse(entityClass.Properties.Values["JumpIndexes"], out this.JumpIndexes);
        int.TryParse(entityClass.Properties.Values["EatingIndexes"], out this.EatingIndexes);
        int.TryParse(entityClass.Properties.Values["RandomIndexes"], out this.RandomIndexes);
        int.TryParse(entityClass.Properties.Values["AttackIdleIndexes"], out this.AttackIdleIndexes);
        if (entityClass.Properties.Values.ContainsKey("RightHandJointName"))
        {
            this.RightHand = entityClass.Properties.Values["RightHandJointName"];
        }
        this.jumpTag = Animator.StringToHash("Jump");

        if (entityClass.Properties.Values.ContainsKey("RightHandJointName"))
        {
            this.RightHand = entityClass.Properties.Values["RightHandJointName"];

        }
    }


    public override void SwitchModelAndView(string _modelName, bool _bFPV, bool _bMale)
    {
        Log("Running Switch and Model View");
        this.SetBool("IsDead", this.entity.IsDead());
        this.SetBool("IsAlive", this.entity.IsAlive());

        // dummy assign body parts
        this.assignBodyParts();

        Log(" Root Motion: " + this.entity.RootMotion);
        if(this.entity.RootMotion  )
        {
            Log(" Root Motion is enabled.");
            AvatarRootMotion avatarRootMotion = this.bipedTransform.GetComponent<AvatarRootMotion>();
            if(avatarRootMotion == null)
            {
                Log(" AvatarRootMotion() not found. Adding one.");
                avatarRootMotion = this.bipedTransform.gameObject.AddComponent<AvatarRootMotion>();

            }
            Log(" Initializing Root Motion");
            avatarRootMotion.Init(this, this.anim);
        }
        // Check if this entity has a weapon or not
        if(this.rightHandItemTransform != null)
        {
            Log("Setting Right hand position");
            this.rightHandItemTransform.parent = this.rightHandItemTransform;
            Vector3 position = AnimationGunjointOffsetData.AnimationGunjointOffset[this.entity.inventory.holdingItem.HoldType.Value].position;
            Vector3 rotation = AnimationGunjointOffsetData.AnimationGunjointOffset[this.entity.inventory.holdingItem.HoldType.Value].rotation;
            this.rightHandItemTransform.localPosition = position;
            this.rightHandItemTransform.localEulerAngles = rotation;
            this.SetInRightHand(this.rightHandItemTransform);
        }
        this.SetInt("WalkType", this.entity.GetWalkType());
        this.SetBool("IsDead", this.entity.IsDead());
        this.SetBool("IsAlive", this.entity.IsAlive());

    
    }

    public override bool IsAnimationAttackPlaying()
    {
        return this.timeAttackAnimationPlaying > 0f;
    }

    public override void StartAnimationAttack()
    {
        this.SetRandomIndex("AttackIndex");
        this.SetTrigger("Attack");
        this.SetRandomIndex("AttackIdleIndex");
        this.timeAttackAnimationPlaying = 0.3f;
    }

    // Main Update method
    protected virtual void Update()
    {
        if (this.timeAttackAnimationPlaying > 0f)
        {
            this.timeAttackAnimationPlaying -= Time.deltaTime;
        }

        // No need to proceed if the model isn't initialized.
        if (this.bipedTransform == null || !this.bipedTransform.gameObject.activeInHierarchy)
        {
            return;
        }

        if (!(this.anim == null) && this.anim.avatar.isValid && this.anim.enabled)
        {
            // Logic to handle our movements
            float num = 0f;
            float num2 = 0f;
            if (!this.entity.IsFlyMode.Value)
            {
                num = this.entity.speedForward;
                num2 = this.entity.speedStrafe;
            }
            float num3 = num2;
            if (num3 >= 1234f)
            {
                num3 = 0f;
            }
            this.SetFloat("Forward", num);
            this.SetFloat("Strafe", num3);
            if (!this.entity.IsDead())
            {
                if (this.movementStateOverride != -1)
                {
                    this.SetInt("MovementState", this.movementStateOverride);
                    this.movementStateOverride = -1;
                }
                else if (num2 >= 1234f)
                {
                    this.SetInt("MovementState", 4);

                }
                else
                {
                    float num4 = num * num + num3 * num3;
                    int intMovementState = (num4 <= this.entity.moveSpeedAggro * this.entity.moveSpeedAggro) ? ((num4 <= this.entity.moveSpeed * this.entity.moveSpeed) ? ((num4 <= 0.001f) ? 0 : 1) : 2) : 3;
                    this.SetInt("MovementState", intMovementState);
                }
            }

            if (Mathf.Abs(num) <= 0.01f && Mathf.Abs(num2) <= 0.01f)
            {
                this.SetBool("IsMoving", false);
            }
            else
            {
                this.idleTime = 0f;
                this.SetBool("IsMoving", true);
            }

            if (nextCheck == 0.0f || nextCheck < Time.time)
            {
                nextCheck = Time.time + CheckDelay;
                SetRandomIndex("RandomIndex");

                SetRandomIndex("WalkIndex");
                SetRandomIndex("RunIndex");
                SetRandomIndex("IdleIndex");
            }

            this.SetFloat("IdleTime", this.idleTime);
            this.idleTime += Time.deltaTime;
            this.SetFloat("RotationPitch", this.entity.rotation.x);

            // if the entity is in water, flag it, so we'll do the swiming conditions before the movement.
            this.SetBool("IsInWater", this.entity.IsInWater());
            //Log("Entity is in Water: " + this.entity.IsInWater());

            // This logic handles distributing the attack animations to clients and servers, and keeps them in sync
            this.animSyncWaitTime -= Time.deltaTime;
            if (this.animSyncWaitTime <= 0f)
            {
                this.animSyncWaitTime = 0.05f;

                if (this.ChangedAnimationParameters.Count > 0)
                {
                   Log("Changed Animation Paramters: " + ChangedAnimationParameters.Count);
                    foreach (var each in ChangedAnimationParameters)
                    {
                        Log(each.Value.ToString());
                    }
                    if (!this.entity.isEntityRemote)
                    {
                        Log("Preparing package");
                        List<global::AnimParamData> list = new List<global::AnimParamData>(this.ChangedAnimationParameters.Count);
                        this.ChangedAnimationParameters.CopyValuesTo(list);
                        if (global::Steam.Network.IsServer)
                        {
                            global::ConnectionManager instance = global::SingletonMonoBehaviour<global::ConnectionManager>.Instance;
                            global::NetPackageEntityAnimationData package = new global::NetPackageEntityAnimationData(this.entity.entityId, list);
                            int entityId = this.entity.entityId;
                            instance.SendPackage(package, false, -1, entityId, -1, -1);
                        }
                        else
                        {
                            global::SingletonMonoBehaviour<global::ConnectionManager>.Instance.SendToServer(new global::NetPackageEntityAnimationData(this.entity.entityId, list), false);
                        }
                    }

                    Log("Clearing Parameter List");
                    this.ChangedAnimationParameters.Clear();
                }
                else
                    Log("No Parameters");
            }
            return;
        }
    }

  
    public override bool IsAnimationSpecialAttackPlaying()
    {
        return this.IsAnimationAttackPlaying();
    }

    protected void LateUpdate()
    {
        if(!this.bipedTransform.gameObject.activeInHierarchy)
        {
            return;
        }
        if(!this.anim.enabled)
        {
            return;
        }
        this.bBlockLookPosition = false;
        //if(this.anim.layerCount > 2)
        //{

        //    if(!this.anim.IsInTransition(2) && this.anim.GetInteger("HitBodyPart") != 0 && this.IsAnimationHitRunning())
        //    {
        //        this.SetInt("HitBodyPart", 0);
        //        this.SetBool("isCritical", false);
        //        this.bBlockLookPosition = false;
        //    }
        //}
        this.currentLookWeight = Mathf.Lerp(this.currentLookWeight, this.lookWeightTarget, Time.deltaTime);
        if(this.bBlockLookPosition || !this.isAllowLookPosition())
        {
            this.currentLookWeight = 0f;
        }

        // base Class
        try
        {
            if(this.entity == null || this.bipedTransform == null || !this.bipedTransform.gameObject.activeInHierarchy)
            {
                return;
            }
            if(this.anim == null || !this.anim.enabled)
            {
                return;
            }
            this.updateLayerStateInfo();
            this.updateSpineRotation();
            if(this.entity.inventory.holdingItem.Actions[0] != null)
            {
                this.entity.inventory.holdingItem.Actions[0].UpdateNozzleParticlesPosAndRot(this.entity.inventory.holdingItemData.actionData[0]);
            }
            if(this.entity.inventory.holdingItem.Actions[1] != null)
            {
                this.entity.inventory.holdingItem.Actions[1].UpdateNozzleParticlesPosAndRot(this.entity.inventory.holdingItemData.actionData[1]);
            }
            int fullPathHash = this.currentBaseState.fullPathHash;
            bool flag = this.anim.IsInTransition(0);
            if(!flag)
            {
                this.isJumpStarted = false;
                if(fullPathHash == this.jumpState || fullPathHash == this.fpvJumpState)
                {
                    this.SetBool("Jump", false);
                }
                if(this.deathStates.Contains(fullPathHash))
                {
                    this.SetBool("IsDead", false);
                }
                if(this.anim.GetBool("Reload") && this.reloadStates.Contains(this.currentWeaponHoldLayer.fullPathHash))
                {
                    this.SetBool("Reload", false);
                }
            }
            if(this.anim.GetBool("ItemUse") && --this.itemUseTicks <= 0)
            {
                this.SetBool("ItemUse", false);
            }
            if(this.isInDeathAnim)
            {
                if((this.currentBaseState.tagHash == AvatarController.deathTag || this.deathStates.Contains(fullPathHash)) && this.currentBaseState.normalizedTime >= 1f && !flag)
                {
                    this.isInDeathAnim = false;
                    if(this.entity.HasDeathAnim)
                    {
                        this.entity.emodel.DoRagdoll(DamageResponse.New(true), 999999f);
                    }
                }
                if(this.entity.HasDeathAnim && this.entity.RootMotion && this.entity.isCollidedHorizontally)
                {
                    this.isInDeathAnim = false;
                    this.entity.emodel.DoRagdoll(DamageResponse.New(true), 999999f);
                }
            }
        }
        catch(Exception ex)
        {
            Debug.Log(" Exception: " + ex.ToString());
        }
    }

    private void updateSpineRotation()
    {
       
    }

    protected void updateLayerStateInfo()
    {
        for (int x = 0; x < this.anim.layerCount; x++)
        {
            switch (x)
            {
                case 0:
                    this.currentBaseState = this.anim.GetCurrentAnimatorStateInfo(x);
                    break;
                case 1:
                    this.currentOverrideLayer = this.anim.GetCurrentAnimatorStateInfo(x);
                    break;
                case 2:
                    this.currentFullBodyOverlayLayer = this.anim.GetCurrentAnimatorStateInfo(x);
                    break;
                case 3:
                    this.currentAdditiveLayer = this.anim.GetCurrentAnimatorStateInfo(x);
                    break;
            }
        }
    }

    private bool isAllowLookPosition()
    {
        return true;
    }

    public override void StartAnimationSpecialAttack(bool _b)
    {
        if (_b)
        {
            this.Log("Firing Special attack");
            this.SetRandomIndex("SpecialAttackIndex");
            this.SetTrigger("SpecialAttack");
            this.idleTime = 0f;
            this.specialAttackTicks = 3;
            this.timeSpecialAttackPlaying = 0.8f;

        }
    }

    // Logic to handle Special Attack
    public override bool IsAnimationSpecialAttack2Playing()
    {
        return this.IsAnimationAttackPlaying();
    }
    public override void StartAnimationSpecialAttack2()
    {
        this.Log("Firing Second Special attack");
        this.SetRandomIndex("SpecialSecondAttack");
        this.SetTrigger("SpecialSecondAttack");
    }


    // Logic to handle Raging
    public override void StartAnimationRaging()
    {
        this.SetRandomIndex("RagingIndex");
        this.SetTrigger("Raging");
    }

    // Logic to handle electrocution
    public override bool IsAnimationElectrocutedPlaying()
    {
        return this.IsElectrocuting;
    }
    public override void StartAnimationElectrocuted()
    {
        if (!this.IsAnimationElectrocutedPlaying())
        {
            this.IsElectrocuting = true;
            this.SetRandomIndex("ElectrocutionIndex");
            this.SetTrigger("Electrocution");
        }
    }

    public override bool IsAnimationHarvestingPlaying()
    {
        return this.IsHarvesting;
    }
    public override void StartAnimationHarvesting()
    {
        if (!this.IsAnimationHarvestingPlaying())
        {
            this.IsHarvesting = true;
            this.SetRandomIndex("HarvestIndex");
            this.SetTrigger("Harvest");
        }
    }

    public override void SetAlive()
    {
        this.SetBool("IsAlive", true);
        this.SetBool("IsDead", false);
        this.SetTrigger("Alive");
    }

    public override void SetDrunk(float _numBeers)
    {
        if (_numBeers > 3f)
        {
            this.SetRandomIndex("DrunkIndex");
            this.SetTrigger("Drunk");
        }
    }


    public override void SetCrouching(bool _bEnable)
    {
        if (_bEnable)
        {
            this.SetRandomIndex("CrouchIndex");
        }
        this.SetBool("IsCrouching", _bEnable);
    }

    public override void SetVisible(bool _b)
    {
        if (this.m_bVisible != _b)
        {
            this.m_bVisible = _b;
            Transform transform = this.bipedTransform;
            if (transform != null)
            {
                Renderer[] componentsInChildren = transform.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].enabled = _b;
                }
            }
        }
    }

    public override void SetRagdollEnabled(bool _b)
    {
    }

    public override void StartAnimationReloading()
    {
    }

    public override void StartAnimationJumping()
    {
        this.SetRandomIndex("JumpIndex");
        this.SetTrigger("Jump");
    }

    public override void StartAnimationFiring()
    {
    }

    public override void StartAnimationHit(EnumBodyPartHit _bodyPart, int _dir, int _hitDamage, bool _criticalHit, int _movementState, float _random)
    {
        this.SetInt("BodyPartHit", (int)_bodyPart);
        this.SetInt("HitDirection", _dir);
        this.SetInt("HitDamage", _hitDamage);
        this.SetBool("CriticalHit", _criticalHit);
        this.SetInt("MovementState", _movementState);
        this.SetInt("Random", Mathf.FloorToInt(_random * 100f));
        this.SetRandomIndex("PainIndex");
        this.SetTrigger("Pain");
    }

    public override bool IsAnimationHitRunning()
    {
        return false;
    }

    public override void StartDeathAnimation(EnumBodyPartHit _bodyPart, int _movementState, float _random)
    {
        this.SetRandomIndex("DeathIndex");
        this.SetBool("IsDead", true);
    }

    public override void SetInRightHand(Transform _transform)
    {
        if (!(this.rightHandItemTransform == null) && !(_transform == null))
        {
            this.Log("Setting Right Hand: " + this.rightHandItemTransform.name.ToString());
            this.idleTime = 0f;
            this.Log("Setting Right Hand Transform");
            this.rightHandItemTransform = _transform;
            if (this.rightHandItemTransform == null)
            {
                this.Log("Right Hand Animator is Null");
            }
            else
            {
                this.Log("Right Hand Animator is NOT NULL ");
                this.rightHandAnimator = this.rightHandItemTransform.GetComponent<Animator>();
                if (this.rightHandItemTransform != null)
                {
                    Utils.SetLayerRecursively(this.rightHandItemTransform.gameObject, 0);
                }
                this.Log("Done with SetInRightHand");
            }
        }
    }

    public override Transform GetRightHandTransform()
    {
        return this.rightHandItemTransform;
    }

    public override Transform GetActiveModelRoot()
    {
        return (!this.modelTransform) ? this.bipedTransform : this.modelTransform;
    }

    public override void CrippleLimb(EnumBodyPartHit _bodyPart, bool restoreState)
    {
        if (_bodyPart.IsLeg() && !this.isCrippled && this.entity.GetWalkType() != 5 && this.entity.GetWalkType() != 4)
        {
            this.isCrippled = true;
            this.SetInt("WalkType", 5);
            this.SetTrigger("LegDamageTrigger");
        }
    }

    public override void BeginStun(EnumEntityStunType stun, EnumBodyPartHit _bodyPart, Utils.EnumHitDirection _hitDirection, bool _criticalHit, float random)
    {
        this.SetRandomIndex("StunIndex");
        this.SetBool("IsStunned", true);

        this.SetInt("StunType", (int)stun);
        this.SetInt("StunBodyPart", (int)_bodyPart);
        this.SetInt("HitDirection", (int)_hitDirection);
        this.SetBool("isCritical", _criticalHit);
        this.SetFloat("HitRandomValue", random);
        this.SetTrigger("BeginStunTrigger");
        this.ResetTrigger("EndStunTrigger");


    }

    public override void EndStun()
    {
        this.SetBool("IsStunned", false);
        this.SetBool("isCritical", false);
        this.SetTrigger("EndStunTrigger");
    }



    public override void TriggerSleeperPose(int pose)
    {
        if (this.anim != null)
        {
            this.anim.SetInteger("SleeperPose", pose);
            this.SetTrigger("SleeperTrigger");
        }
    }

    private void Log(string strLog)
    {
        if (this.blDisplayLog)
        {
            if (this.modelTransform == null)
            {
                Debug.Log(string.Format("Unknown Entity: {0}", strLog));
            }
            else
            {
                Debug.Log(string.Format("{0}: {1}", this.modelTransform.name, strLog));
            }
        }
    }


    private new void Awake()
    {
        this.Log("Method: " + MethodBase.GetCurrentMethod().Name);
        this.Log("Initializing " + this.entity.name);
        try
        {
            this.Log("Checking For Graphics Transform...");
            this.bipedTransform = base.transform.Find("Graphics");
            if (this.bipedTransform == null || this.bipedTransform.Find("Model") == null)
            {
                this.Log(" !! Graphics Transform null!");
                return;
            }

            this.modelTransform = this.bipedTransform.Find("Model").GetChild(0);
            if (this.modelTransform == null)
            {
                this.Log(" !! Model Transform is null!");
                return;
            }
            this.Log("Adding Colliders");
            this.AddTransformRefs(this.modelTransform);

            this.Log("Tagging the Body");
            this.AddTagRecursively(this.modelTransform, "E_BP_Body");

            this.Log("Searching for Animator");
            this.anim = this.modelTransform.GetComponent<Animator>();
            if (this.anim == null)
            {
                this.Log("*** Animator Not Found! Invalid Class");
                this.CriticalError = true;
                throw new Exception("Animator Not Found! Wrong class is being used! Try AnimationSDX instead...");
            }
            this.Log("Animator Found");
            this.anim.enabled = true;
            if (!this.anim.runtimeAnimatorController)
            {
                this.Log(string.Format("{0} : My Animator Controller is null!", this.modelTransform.name));
                this.CriticalError = true;
                throw new Exception("Animator Controller is null!");
            }
            this.Log("My Animator Controller has: " + this.anim.runtimeAnimatorController.animationClips.Length + " Animations");
            foreach (AnimationClip animationClip in this.anim.runtimeAnimatorController.animationClips)
            {
                this.Log("Animation Clip: " + animationClip.name.ToString());
            }

            this.rightHandItemTransform = this.FindTransform(this.bipedTransform, this.bipedTransform, this.RightHand);
            if (this.rightHandItemTransform)
            {
                this.Log("Right Hand Item Transform: " + this.rightHandItemTransform.name.ToString());
            }
            else
            {
                this.Log("Right Hand Item Transform: Could not find Transofmr: " + this.RightHand);
            }
        }
        catch (Exception arg)
        {
            this.Log("Exception thrown in Awake() " + arg);
        }
    }

    private Transform FindTransform(Transform root, Transform t, string objectName)
    {
        Transform result;
        if (t.name.Contains(objectName))
        {
            result = t;
        }
        else
        {
            foreach (object obj in t)
            {
                Transform t2 = (Transform)obj;
                Transform transform = this.FindTransform(root, t2, objectName);
                if (transform != null)
                {
                    return transform;
                }
            }
            result = null;
        }
        return result;
    }


    private void UpdateCurrentState()
    {
        this.currentBaseState = this.anim.GetCurrentAnimatorStateInfo(0);
    }


    // Since we support many different indexes, we use this to generate a random index, and send it to the state machine.
    public void SetRandomIndex(string strParam)
    {
        int index = 0;
        switch (strParam)
        {
            case "AttackIndex":
                index = this.GetRandomIndex(this.AttackIndexes);
                break;
            case "SpecialAttackIndex":
                index = this.GetRandomIndex(this.SpecialAttackIndexes);
                break;
            case "SpecialSecondIndex":
                index = this.GetRandomIndex(this.SpecialSecondIndexes);
                break;
            case "RagingIndex":
                index = this.GetRandomIndex(this.RagingIndexes);
                break;
            case "ElectrocutionIndex":
                index = this.GetRandomIndex(this.ElectrocutionIndexes);
                break;
            case "CrouchIndex":
                index = this.GetRandomIndex(this.CrouchIndexes);
                break;
            case "StunIndex":
                index = this.GetRandomIndex(this.StunIndexes);
                break;
            case "SleeperIndex":
                index = this.GetRandomIndex(this.SleeperIndexes);
                break;
            case "HarvestIndex":
                index = this.GetRandomIndex(this.HarvestIndexes);
                break;
            case "PainIndex":
                index = this.GetRandomIndex(this.PainIndexes);
                break;
            case "DeathIndex":
                index = this.GetRandomIndex(this.DeathIndexes);
                break;
            case "RunIndex":
                index = this.GetRandomIndex(this.RunIndexes);
                break;
            case "WalkIndex":
                index = this.GetRandomIndex(this.WalkIndexes);
                break;
            case "IdleIndex":
                index = this.GetRandomIndex(this.IdleIndexes);
                break;
            case "JumpIndex":
                index = this.GetRandomIndex(this.JumpIndexes);
                break;
            case "RandomIndex":
                index = this.GetRandomIndex(this.RandomIndexes);
                break;
            case "EatingIndex":
                index = this.GetRandomIndex(this.EatingIndexes);
                break;
            case "AttackIdleIndex":
                index = this.GetRandomIndex(this.AttackIdleIndexes);
                break;
        }

        Log(string.Format("Random Generator: {0} Value: {1}", strParam, index));
        SetInt(strParam, index);
    }

    public int GetRandomIndex(int intMax)
    {
        return UnityEngine.Random.Range(0, intMax);
    }

    private EntityClass GetAvailableTriggers()
    {
        return EntityClass.list[this.entity.entityClass];
    }

    private void AddTransformRefs(Transform t)
    {
        if (t.GetComponent<Collider>() != null && t.GetComponent<RootTransformRefEntity>() == null)
        {
            RootTransformRefEntity rootTransformRefEntity = t.gameObject.AddComponent<RootTransformRefEntity>();
            rootTransformRefEntity.RootTransform = base.transform;
        }
        foreach (object obj in t)
        {
            Transform t2 = (Transform)obj;
            this.AddTransformRefs(t2);
        }
    }

    private void AddTagRecursively(Transform trans, string tag)
    {
        if (trans.gameObject.tag.Contains("Untagged"))
        {
            if (trans.name.ToLower().Contains("head"))
            {
                trans.gameObject.tag = "E_BP_Head";
            }
            else
            {
                trans.gameObject.tag = tag;
            }
        }
        foreach (object obj in trans)
        {
            Transform trans2 = (Transform)obj;
            this.AddTagRecursively(trans2, tag);
        }
    }

    public override void RemoveLimb(EnumBodyPartHit _bodyPart, bool restoreState)
    {
        switch (_bodyPart)
        {
            case EnumBodyPartHit.Head:
                if (!this.headDismembered)
                {
                    this.headDismembered = true;
                    this.neck.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.neckGore, "Prefabs/HeadGore", restoreState);
                }
                break;
            case EnumBodyPartHit.LeftUpperArm:
                if (!this.leftUpperArmDismembered)
                {
                    this.leftUpperArmDismembered = true;
                    this.leftUpperArm.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.leftUpperArmGore, "Prefabs/UpperArmGore", restoreState);
                }
                break;
            case EnumBodyPartHit.RightUpperArm:
                if (!this.rightUpperArmDismembered)
                {
                    this.rightUpperArmDismembered = true;
                    this.rightUpperArm.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.rightUpperArmGore, "Prefabs/UpperArmGore", restoreState);
                }
                break;
            case EnumBodyPartHit.LeftUpperLeg:
                if (!this.leftUpperLegDismembered)
                {
                    this.leftUpperLegDismembered = true;
                    this.leftUpperLeg.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.leftUpperLegGore, "Prefabs/UpperLegGore", restoreState);
                }
                break;
            case EnumBodyPartHit.RightUpperLeg:
                if (!this.rightUpperLegDismembered)
                {
                    this.rightUpperLegDismembered = true;
                    this.rightUpperLeg.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.rightUpperLegGore, "Prefabs/UpperLegGore", restoreState);
                }
                break;
            case EnumBodyPartHit.LeftLowerArm:
                if (!this.leftLowerArmDismembered)
                {
                    this.leftLowerArmDismembered = true;
                    this.leftLowerArm.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.leftLowerArmGore, "Prefabs/LowerArmGore", restoreState);
                }
                break;
            case EnumBodyPartHit.RightLowerArm:
                if (!this.rightLowerArmDismembered)
                {
                    this.rightLowerArmDismembered = true;
                    this.rightLowerArm.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.rightLowerArmGore, "Prefabs/LowerArmGore", restoreState);
                }
                break;
            case EnumBodyPartHit.LeftLowerLeg:
                if (!this.leftLowerLegDismembered)
                {
                    this.leftLowerLegDismembered = true;
                    this.leftLowerLeg.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.leftLowerLegGore, "Prefabs/LowerLegGore", restoreState);
                }
                break;
            case EnumBodyPartHit.RightLowerLeg:
                if (!this.rightLowerLegDismembered)
                {
                    this.rightLowerLegDismembered = true;
                    this.rightLowerLeg.localScale = Vector3.zero;
                    this.SpawnLimbGore(this.rightLowerLegGore, "Prefabs/LowerLegGore", restoreState);
                }
                break;
        }
    }

    protected void SpawnLimbGore(Transform parent, string path, bool restoreState)
    {
        if (parent != null)
        {
            GameObject original = ResourceWrapper.Load1P(path) as GameObject;
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, Vector3.zero, Quaternion.identity);
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = parent.localScale;
            GorePrefab component = gameObject.GetComponent<GorePrefab>();
            if (component != null)
            {
                component.restoreState = restoreState;
            }
        }
    }

    protected void assignBodyParts()
    {
        if (this.bipedTransform == null)
        {
            this.Log("assignBodyParts: GraphicsTransform is null!");
        }
        else
        {
            this.Log("Mapping Body Parts");
            this.head = this.bipedTransform.FindInChilds("Head", false);
            this.neck = this.bipedTransform.FindInChilds("Neck", false);
            this.rightHand = this.bipedTransform.FindInChilds(this.entity.GetRightHandTransformName(), false);
            this.leftUpperLeg = this.bipedTransform.FindInChilds("LeftUpLeg", false);
            this.leftLowerLeg = this.bipedTransform.FindInChilds("LeftLeg", false);
            this.rightUpperLeg = this.bipedTransform.FindInChilds("RightUpLeg", false);
            this.rightLowerLeg = this.bipedTransform.FindInChilds("RightLeg", false);
            this.leftUpperArm = this.bipedTransform.FindInChilds("LeftArm", false);
            this.leftLowerArm = this.bipedTransform.FindInChilds("LeftForeArm", false);
            this.rightUpperArm = this.bipedTransform.FindInChilds("RightArm", false);
            this.rightLowerArm = this.bipedTransform.FindInChilds("RightForeArm", false);
            this.neckGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_HeadGore");
            this.leftUpperArmGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_LeftUpperArmGore");
            this.leftLowerArmGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_LeftLowerArmGore");
            this.rightUpperArmGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_RightUpperArmGore");
            this.rightLowerArmGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_RightLowerArmGore");
            this.leftUpperLegGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_LeftUpperLegGore");
            this.leftLowerLegGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_LeftLowerLegGore");
            this.rightUpperLegGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_RightUpperLegGore");
            this.rightLowerLegGore = GameUtils.FindTagInChilds(this.bipedTransform, "L_RightLowerLegGore");


            //this.head = this.FindTransform(this.bipedTransform, this.bipedTransform, "Head");
            //this.rightHand = this.FindTransform(this.bipedTransform, this.bipedTransform, this.RightHand);
        }
    }

    public override bool IsAnimationJumpRunning()
    {
        return this.Jumping || this.jumpTag == this.currentBaseState.tagHash;
    }

    public override void StartEating()
    {
        if (!this.isEating)
        {
            this.SetRandomIndex("EatingIndex");
            this.SetBool("IsEating", true);
            this.SetTrigger("IsEatingTrigger");
            this.isEating = true;
        }
    }
    public override void StopEating()
    {
        if (this.isEating)
        {
            this.SetBool("IsEating", false);
            this.isEating = false;
        }
    }


}