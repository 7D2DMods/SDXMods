/*
 * Class: MecanimSDX
 * Author:  sphereii, Mortelentus
 * Category: Mecanim Animator
 * Description:
 *
 *      This class provides mecanim animations to be applied to custom entities. It allows us to use Unity Animator State Machine to make
 *      more complex animation sequences.  RootMotion is NOT supported, and must be disabled.
 *
 *      Mecanim allows us to create complex state machines in unity, to allow more variety of animations. For example, we could have 8 different animations for
 *      an attack animation. This class, when properly configured using the AttackIndexes of 8, will randomly pick one of the attacks to display.
 *
 *      Special thanks to Mortelentus, who added in the additional code to allow custom entities to use ranged weapons.
 *
 * Usage:
 *      Add the following class to entities that are meant to use these features.
 *  
 *         <property name="AvatarController" value="MecanimSDX, Mods" />
 *
 *
 * Features:
 *      This class is meant to be used in following the Mecanim Tutorial:https://7d2dsdx.github.io/Tutorials/HowtocreateanAnimatorStateMachin.html
 *
 *      Because Mecanim is so flexible, and we cannot attach scripts to the unity objects, this class has quite a few available XML values to help tweak each entity
 *      without adjusting this class. 
 *
 *      This tag is mandatory and must contain the animator's state names for each possible attack.
 * 				<property name="AttackAnimations" value="Attack0, Attack1" />
 *
 *      The following indexes default to 0, so they do not need to be included if you only have one animation for a particular state. For example, if you only have one Attack,
 *      you would not need to set AttackIndexes value at all. If you have two attacks, you would have to set AttackIndexes to 2.
 *
 *      The following example has two different attacks, 2 different run and walk animations each:
 *				<property name="AttackIndexes" value="2" />
 *				<property name="SpecialAttackIndexes" value="0" />
 * 				<property name="PainIndexes" value="0" />
 *				<property name="DeathIndexes" value="0" />
 *				<property name="RunIndexes" value="2" />
 *				<property name="WalkIndexes" value="2" />
 *				<property name="IdleIndexes" value="0" />
 *				<property name="JumpIndexes" value="0" />
 *
 *      Not all indexes are currently used or called yet, but future revisions will support them, such as Stun, Crouch, Electrocution, Raging, SpecialSecondAttack, harvest, and eating.
 *
 * 				<property name="SpecialSecondAttackIndexes" value="0" />
 *				<property name="RagingIndexes" value="0" />
 *				<property name="ElectrocutionIndexes" value="0" />
 *				<property name="CrouchIndexes" value="0" />
 *				<property name="StunIndexes" value="0" />
 *				<property name="SleeperIndexes" value="0" />
 *				<property name="HarvestIndexes" value="0" />
 *
 *      A special RandomIndexes is supplied, which can be be re-used in the State Machine to add further randomized of attacks.
  */
using System;
using System.Collections.Generic;
using System.Reflection;
using SDX.Payload;
using UnityEngine;

using Random = UnityEngine.Random;

internal class NewMecanimSDX : AvatarZombie01Controller
{
    // Stores our hashes as ints, since that's what the Animator wants
    private readonly HashSet<int> AttackHash;
    
    // Determines what the light threshold of being in the dark is
    private byte LightThreshold = 14;

    // The following are the number of indexes we are getting, populating from the XML files.
    private readonly int AttackIndexes;

    //// Maintain a list of strings of the same animation
    private readonly List<string> AttackStrings = new List<string>();

    // If the displayLog is true, verbosely log out put. Disable in production!
    private readonly bool blDisplayLog = true;

    private readonly int CrouchIndexes;
    private readonly int DeathIndexes;
    private int EatingIndexes;
    private readonly int ElectrocutionIndexes;
    private readonly int HarvestIndexes;
    private readonly int IdleIndexes;
    private readonly int JumpIndexes;
    private readonly int PainIndexes;
    private readonly int RagingIndexes;
    private readonly int RandomIndexes;

    private string RightHand = "RightHand";
    private readonly int RunIndexes;
    private readonly int SleeperIndexes;
    private readonly int SpecialAttackIndexes;
    private readonly int SpecialSecondIndexes;
    private readonly int StunIndexes;
    private readonly int WalkIndexes;
    private readonly int AttackIdleIndexes;

    protected int jumpTag;
    private bool Jumping = false;

    private float currentLookWeight;
    private float lookWeightTarget;

    private float lastPlayerX;
    private float lastPlayerZ;
    private float lastDistance;
    protected int MovementState = Animator.StringToHash("MovementState");

    private NewMecanimSDX()
    {

        entity = transform.gameObject.GetComponent<EntityAlive>();

        var entityClass = EntityClass.list[entity.entityClass];


        AttackHash = GenerateLists(entityClass, "AttackAnimations");


        // The following will read our Index values from the XML to determine the maximum attack animations.
        // The range should be 1-based, meaning a value of 1 will specify the index value 0.
        // <property name="AttackIndexes" value="20", means there are 20 animations, running from 0 to 19
        int.TryParse(entityClass.Properties.Values["AttackIndexes"], out AttackIndexes);
        int.TryParse(entityClass.Properties.Values["SpecialAttackIndexes"], out SpecialAttackIndexes);

        int.TryParse(entityClass.Properties.Values["SpecialSecondIndexes"], out SpecialSecondIndexes);
        int.TryParse(entityClass.Properties.Values["RagingIndexes"], out RagingIndexes);

        int.TryParse(entityClass.Properties.Values["ElectrocutionIndexes"], out ElectrocutionIndexes);
        int.TryParse(entityClass.Properties.Values["CrouchIndexes"], out CrouchIndexes);

        int.TryParse(entityClass.Properties.Values["StunIndexes"], out StunIndexes);
        int.TryParse(entityClass.Properties.Values["SleeperIndexes"], out SleeperIndexes);

        int.TryParse(entityClass.Properties.Values["HarvestIndexes"], out HarvestIndexes);
        int.TryParse(entityClass.Properties.Values["PainIndexes"], out PainIndexes);

        int.TryParse(entityClass.Properties.Values["DeathIndexes"], out DeathIndexes);
        int.TryParse(entityClass.Properties.Values["RunIndexes"], out RunIndexes);

        int.TryParse(entityClass.Properties.Values["WalkIndexes"], out WalkIndexes);
        int.TryParse(entityClass.Properties.Values["IdleIndexes"], out IdleIndexes);

        int.TryParse(entityClass.Properties.Values["JumpIndexes"], out JumpIndexes);
        int.TryParse(entityClass.Properties.Values["EatingIndexes"], out EatingIndexes);

        int.TryParse(entityClass.Properties.Values["RandomIndexes"], out RandomIndexes);
        int.TryParse(entityClass.Properties.Values["AttackIdleIndexes"], out AttackIdleIndexes);

        if (entityClass.Properties.Values.ContainsKey("RightHandJointName"))
            this.RightHand = entityClass.Properties.Values["RightHandJointName"];

        // determines what this entity considers to be "in the dark". Default is 14

        if (entityClass.Properties.Values.ContainsKey("LightThreshold"))
            byte.TryParse(entityClass.Properties.Values["LightThreshold"], out this.LightThreshold);

        jumpTag = Animator.StringToHash("Jump");
        

    }

    protected override void Awake()
    {
        base.Awake();

        var entityClass = EntityClass.list[entity.entityClass];
        if (entityClass.Properties.Values.ContainsKey("RightHandJointName"))
        {
            this.RightHand = entityClass.Properties.Values["RightHandJointName"];
      
        }

    }
    // Triggers the Eating animation state
    public override void StartEating()
    {
        SetRandomIndex("EatingIndex");
        this.SetBool("IsEating", true);
        this.SetTrigger("IsEatingTrigger");
        base.StartEating();
    }

    // Ends the eating animation state
    public override void StopEating()
    {
        this.SetBool("IsEating", false);
        base.StopEating();
    }

    protected override void Update()
    {
        if (this.timeAttackAnimationPlaying > 0f)
        {
            this.timeAttackAnimationPlaying -= Time.deltaTime;
        }
        if (this.timeUseAnimationPlaying > 0f)
        {
            this.timeUseAnimationPlaying -= Time.deltaTime;
        }
        if (this.timeHarestingAnimationPlaying > 0f)
        {
            this.timeHarestingAnimationPlaying -= Time.deltaTime;
            if (this.timeHarestingAnimationPlaying <= 0f && this.anim != null)
            {
                this.SetBool("Harvesting", false);
            }
        }
        if (this.timeSpecialAttack2Playing > 0f)
        {
            this.timeSpecialAttack2Playing -= Time.deltaTime;
        }
        if (this.hitDuration > 0f)
        {
            this.hitDuration -= Time.deltaTime;
            if (this.hitDuration <= 0f)
            {
                this.SetTrigger("HitEndTrigger");
            }
        }
        if (!this.m_bVisible && (this.entity == null || !this.entity.RootMotion || this.entity.isEntityRemote))
        {
            Debug.Log("No Root Motion / Entity is Remote ");
            return;
        }
        if (this.bipedTransform == null || !this.bipedTransform.gameObject.activeInHierarchy)
        {
            Log("BipedTransform is null, or inactive");
            return;
        }
        if (!(this.anim == null) && this.anim.avatar.isValid && this.anim.enabled)
        {
            this.updateLayerStateInfo();
            this.setLayerWeights();
            int value = this.entity.inventory.holdingItem.HoldType.Value;
            this.SetInt("WeaponHoldType", value);


            Log("Last Distance: " + this.lastDistance);
            float playerDistanceX = 0.0f;
            float playerDistanceZ = 0.0f;
            float encroached = this.lastDistance;


            Log("Position X: " + this.entity.position.x);
            Log("Position Z: " + this.entity.position.z);
            
            // Calculates how far away the entity is
            playerDistanceX = Mathf.Abs(this.entity.position.x - this.entity.lastTickPos[0].x) * 6f;
            playerDistanceZ = Mathf.Abs(this.entity.position.z - this.entity.lastTickPos[0].z) * 6f;

            if (!this.entity.isEntityRemote)
            {
                if (Mathf.Abs(playerDistanceX - this.lastPlayerX) > 0.00999999977648258 ||
                    Mathf.Abs(playerDistanceZ - this.lastPlayerZ) > 0.00999999977648258)
                {
                    encroached =
                        Mathf.Sqrt(playerDistanceX * playerDistanceX + playerDistanceZ * playerDistanceZ);
                    this.lastPlayerX = playerDistanceX;
                    this.lastPlayerZ = playerDistanceZ;
                    this.lastDistance = encroached;
                }
            }
            else if (playerDistanceX <= this.lastPlayerX && playerDistanceZ <= this.lastPlayerZ)
            {
                this.lastPlayerX *= 0.9f;
                this.lastPlayerZ *= 0.9f;
                this.lastDistance *= 0.9f;
            }
            else
            {
                encroached =
                    Mathf.Sqrt((playerDistanceX * playerDistanceX + playerDistanceZ * playerDistanceZ));
                this.lastPlayerX = playerDistanceX;
                this.lastPlayerZ = playerDistanceZ;
                this.lastDistance = encroached;
            }

            Debug.Log("encroached: " + encroached);
            if (encroached > 0.05)
            {

                // Running if above 1.0
                if (encroached > 1.0)
                {
                    SetInt(MovementState, 2);
                }
                else
                {
                    SetInt(MovementState, 1);
                }
            }
            else
            {
               SetInt(MovementState, 0);

            }
            Log("Movement State on Animation: " + this.anim.GetInteger("MovementState"));

         
            this.SetFloat("IdleTime", this.idleTime);
            this.idleTime += Time.deltaTime;
            this.SetFloat("RotationPitch", this.entity.rotation.x);
            this.animSyncWaitTime -= Time.deltaTime;
            if (this.animSyncWaitTime <= 0f)
            {
                this.animSyncWaitTime = 0.05f;
                if (this.ChangedAnimationParameters.Count > 0)
                {
                    if (!this.entity.isEntityRemote)
                    {
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
                    this.ChangedAnimationParameters.Clear();
                }
            }
            return;
        }
    }
    // Had to over-ride LateUpdate because it was making a direct call to the second animation layer. we don't support that.
    protected override void LateUpdate()
    {
        if (this.entity == null || this.bipedTransform == null || !this.bipedTransform.gameObject.activeInHierarchy)
        {
            return;
        }
        if (!(this.anim == null) && this.anim.enabled)
        {
            // Allows for the entity to be registered as local, so that the animations can be synced.

            this.updateLayerStateInfo();
            if (this.entity.inventory.holdingItem.Actions[0] != null)
            {
                this.entity.inventory.holdingItem.Actions[0].UpdateNozzleParticlesPosAndRot(this.entity.inventory.holdingItemData.actionData[0]);
            }
            if (this.entity.inventory.holdingItem.Actions[1] != null)
            {
                this.entity.inventory.holdingItem.Actions[1].UpdateNozzleParticlesPosAndRot(this.entity.inventory.holdingItemData.actionData[1]);
            }
            int shortNameHash = this.currentBaseState.shortNameHash;
            bool flag;
            if (!(flag = this.anim.IsInTransition(0)))
            {
                if (shortNameHash == this.jumpState || shortNameHash == this.fpvJumpState)
                {
                    this.SetBool("Jump", false);
                }
                if (this.deathStates.Contains(shortNameHash))
                {
                    this.SetBool("IsDead", false);
                }
                if (this.anim.GetBool("Reload") && this.reloadStates.Contains(this.currentWeaponHoldLayer.fullPathHash))
                {
                    this.SetBool("Reload", false);
                }
            }
            if (this.anim.GetBool("ItemUse") && --this.itemUseTicks <= 0)
            {
                this.SetBool("ItemUse", false);
            }

            //float num = 0f;
            //float num2 = 0f;
            //if (!this.entity.IsFlyMode.Value)
            //{
            //    num = this.entity.speedForward;
            //    num2 = this.entity.speedStrafe;
            //}
            //float num3 = num2;
            //if (num3 >= 1234f)
            //{
            //    num3 = 0f;
            //}
            //float num4 = num * num + num3 * num3;
            //this.SetInt("MovementState", (num4 <= this.entity.moveSpeedAggro * this.entity.moveSpeedAggro) ? ((num4 <= this.entity.moveSpeed * this.entity.moveSpeed) ? ((num4 <= 0.001f) ? 0 : 1) : 2) : 3);
            ////Log("Last Distance: " + this.lastDistance);
            ////float playerDistanceX = 0.0f;
            //float playerDistanceZ = 0.0f;
            //float encroached = this.lastDistance;


            //// Calculates how far away the entity is
            //playerDistanceX = Mathf.Abs(this.entity.position.x - this.entity.lastTickPos[0].x) * 6f;
            //playerDistanceZ = Mathf.Abs(this.entity.position.z - this.entity.lastTickPos[0].z) * 6f;

            //if (!this.entity.isEntityRemote)
            //{
            //    if (Mathf.Abs(playerDistanceX - this.lastPlayerX) > 0.00999999977648258 ||
            //        Mathf.Abs(playerDistanceZ - this.lastPlayerZ) > 0.00999999977648258)
            //    {
            //        encroached =
            //            Mathf.Sqrt(playerDistanceX * playerDistanceX + playerDistanceZ * playerDistanceZ);
            //        this.lastPlayerX = playerDistanceX;
            //        this.lastPlayerZ = playerDistanceZ;
            //        this.lastDistance = encroached;
            //    }
            //}
            //else if (playerDistanceX <= this.lastPlayerX && playerDistanceZ <= this.lastPlayerZ)
            //{
            //    this.lastPlayerX *= 0.9f;
            //    this.lastPlayerZ *= 0.9f;
            //    this.lastDistance *= 0.9f;
            //}
            //else
            //{
            //    encroached = Mathf.Sqrt((playerDistanceX * playerDistanceX + playerDistanceZ * playerDistanceZ));
            //    this.lastPlayerX = playerDistanceX;
            //    this.lastPlayerZ = playerDistanceZ;
            //    this.lastDistance = encroached;
            //}

            //Log("LastPlayerX: " + this.lastPlayerX);
            //Log("LastPlayerZ: " + this.lastPlayerZ);
            //Log("Last Distance:" + this.lastDistance);
            //Log("Encroachment: " + encroached);
            //if (encroached > 0.150000005960464)
            //{

            //    // Running if above 1.0
            //    if (encroached > 1.0)
            //    {
            //        SetInt(MovementState, 2);
            //    }
            //    else
            //    {
            //        SetInt(MovementState, 1);
            //    }
            //}
            //else
            //{
            //    SetInt(MovementState, 0);

            //}
            if (this.isInDeathAnim)
            {
                if ((this.currentBaseState.tagHash == deathTag || this.deathStates.Contains(shortNameHash)) && this.currentBaseState.normalizedTime >= 1f && !flag)
                {
                    this.isInDeathAnim = false;
                    if (this.entity.HasDeathAnim)
                    {
                        this.entity.emodel.DoRagdoll(DamageResponse.New(true), 999999f);
                    }
                }
                if (this.entity.HasDeathAnim && this.entity.RootMotion && this.entity.isCollidedHorizontally)
                {
                    this.isInDeathAnim = false;
                    this.entity.emodel.DoRagdoll(DamageResponse.New(true), 999999f);
                }
            }
            return;
        }

        if (this.anim.GetInteger("HitBodyPart") != 0 && this.IsAnimationHitRunning())
        {
            this.SetInt("HitBodyPart", 0);
            this.SetBool("isCritical", false);
            this.bBlockLookPosition = false;
        }
        this.currentLookWeight = Mathf.Lerp(this.currentLookWeight, this.lookWeightTarget, Time.deltaTime);
        if (this.bBlockLookPosition || !this.isAllowLookPosition())
        {
            this.currentLookWeight = 0f;
        }
    }

    //public override void SetTrigger(int _propertyHash)
    //{
    //    base.SetTrigger(_propertyHash);
    //    this.ChangedAnimationParameters[_propertyHash] = new AnimParamData(_propertyHash, AnimatorControllerParameterType.Trigger, true);
    //    Log(" Entity is Remote? " + this.entity.isEntityRemote);
    //}

    //}
    // starts any special attack that we have
    public override void StartAnimationSpecialAttack(bool _b)
    {
        Debug.Log("Starting Special attack");
        if (_b)
        {
            Log("Firing Special attack");
            SetRandomIndex("SpecialAttackIndex");
            base.StartAnimationSpecialAttack(_b);
        }
    }

    public override void StartAnimationSpecialAttack2()
    {
        Log("Firing Second Special attack");
        SetRandomIndex("SpecialSecondAttack");
        base.StartAnimationSpecialAttack2();
    }

    public override void StartAnimationRaging()
    {
        SetRandomIndex("RagingIndex");
        base.StartAnimationRaging();
    }

    public override void StartAnimationElectrocuted()
    {
        SetRandomIndex("ElectrocutionIndex");
        base.StartAnimationElectrocuted();
    }

    // Starts a Harvest trigger, if there is one
    public override void StartAnimationHarvesting()
    {
        SetRandomIndex("HarvestIndex");
        StartAnimationHarvesting();
    }


    // Determines and triggers whether we fire off the drunk animation
    public override void SetDrunk(float _numBeers)
    {
        SetRandomIndex("DrunkIndex");
        base.SetDrunk(_numBeers);
    }


    // Determines if the entity is crouching or not  
    public override void SetCrouching(bool _bEnable)
    {

            SetRandomIndex("CrouchIndex");
        SetCrouching(_bEnable);
    }

    // starts the jumping animation
    public override void StartAnimationJumping()
    {
        SetRandomIndex("JumpIndex");
        StartAnimationJumping();
    }

    // ANimation for when the entity gets hit
    public override void StartAnimationHit(EnumBodyPartHit _bodyPart, int _dir, int _hitDamage, bool _criticalHit,
        int _movementState, float _random)
    {
        SetRandomIndex("PainIndex");
        base.StartAnimationHit(_bodyPart, _dir, _hitDamage, _criticalHit, _movementState, _random);
    }

    // Starts the death animation
    public override void StartDeathAnimation(EnumBodyPartHit _bodyPart, int _movementState, float _random)
    {
        SetRandomIndex("DeathIndex");
        base.StartDeathAnimation(_bodyPart, _movementState, _random);
    }


    // starts the Stun Animation
    public override void BeginStun(EnumEntityStunType stun, EnumBodyPartHit _bodyPart, Utils.EnumHitDirection _hitDirection, bool _criticalHit,
        float random)
    {
        SetRandomIndex("StunIndex");
        base.BeginStun(stun, _bodyPart, _hitDirection, _criticalHit, random);
    }

    private void Log(string strLog)
    {
        if (blDisplayLog)
        {
            if (this.modelTransform == null)
                Debug.Log(string.Format("Unknown Entity: {0}", strLog));
            else
                Debug.Log(string.Format("{0}: {1}", this.modelTransform.name, strLog));
        }
    }

    private HashSet<int> GenerateLists(EntityClass entityClass, string strAnimationType)
    {
        var hash = new HashSet<int>();
        
        Log("Searching for AnimationType: " + strAnimationType);
        if (entityClass.Properties.Values.ContainsKey(strAnimationType))
        {
            Log("Animation Type found: " + entityClass.Properties.Values[strAnimationType].ToString());
            foreach (var strAnimationState in entityClass.Properties.Values[strAnimationType].Split(','))
            {
                Log("Adding Attack Hash: " + strAnimationState.Trim());
                
                hash.Add(Animator.StringToHash(strAnimationState.Trim()));
                AttackStrings.Add(strAnimationState);
            }
        }
        return hash;
    }

    protected override void updateSpineRotation()
    {
        // No Spines
    }
    // Enables the entity, and checks if we have root motion enabled or not. 
    public override void SwitchModelAndView(string _modelName, bool _bFPV, bool _bMale)
    {
        bipedTransform = transform.Find("Graphics");
        modelTransform = bipedTransform.Find("Model").GetChild(0);

        this.modelName = _modelName;

        //this bit is important for SDXers! It adds the component that links each collider with the Entity class so hits can be registered.
        Log("Adding Colliders");
        AddTransformRefs(modelTransform);

        //if you're using A14 or haven't set specific tags for the collision in Unity un-comment this and it will set them all to being body contacts
        //using this method means things like head shot multiplers won't work but it will enable basic collision
        Log("Tagging the Body");
        AddTagRecursively(modelTransform, "E_BP_Body");

        this.bMale = _bMale;
        this.bFPV = _bFPV;
        this.assignBodyParts();

        this.anim = modelTransform.GetComponent<Animator>();
        if (this.anim != null)
        {
            this.anim.logWarnings = false;
            this.anim.enabled = true;
        }

        this.SetBool("IsMale", _bMale);
        //if (this.entity.RootMotion && this.anim.hasRootMotion)
        //{
        //    Log("Root Motion is detected on both the entity and the animator. Enabling.");
        //    global::AvatarRootMotion avatarRootMotion = this.bipedTransform.GetComponent<global::AvatarRootMotion>();
        //    if (avatarRootMotion == null)
        //    {
        //        avatarRootMotion = this.bipedTransform.gameObject.AddComponent<global::AvatarRootMotion>();
        //    }
        //    avatarRootMotion.Init(this, this.anim);
        //}
        //else
        //{
        //    Log("Root Motion is disabled.");
        //    this.entity.RootMotion = false;
        //}

        this.rightHandItemTransform = FindTransform(this.bipedTransform, this.bipedTransform, RightHand);
        if (this.rightHandItemTransform)
            Log("Right Hand Item Transform: " + this.rightHandItemTransform.name.ToString());
        else
            Log("Right Hand Item Transform: Could not find Transform: " + RightHand);

        if (this.rightHandItemTransform != null)
        {
            this.rightHandItemTransform.parent = this.rightHand;
            Vector3 position = global::AnimationGunjointOffsetData.AnimationGunjointOffset[this.entity.inventory.holdingItem.HoldType.Value].position;
            Vector3 rotation = global::AnimationGunjointOffsetData.AnimationGunjointOffset[this.entity.inventory.holdingItem.HoldType.Value].rotation;
            this.rightHandItemTransform.localPosition = position;
            this.rightHandItemTransform.localEulerAngles = rotation;
            
        }
        else
        {
            Debug.Log("No Right Hand Transform");
        }
        this.SetInt("WalkType", this.entity.GetWalkType());
        this.SetBool("IsDead", this.entity.IsDead());
        this.SetBool("IsFPV", this.bFPV);
        this.SetBool("IsAlive", this.entity.IsAlive());
    }


    // helper method to find particular transforms by name, in our game object
    private Transform FindTransform(Transform root, Transform t, string objectName)
    {
        if (t.name.Contains(objectName)) return t;

        foreach (Transform tran in t)
        {
            var result = FindTransform(root, tran, objectName);
            if (result != null) return result;
        }

        return null;
    }

    protected override void setLayerWeights()
    {
        // we only have one layer.       
    }

    // Token: 0x06001F09 RID: 7945 RVA: 0x000D3EA0 File Offset: 0x000D20A0
    protected override void updateLayerStateInfo()
    {

        this.currentBaseState = this.anim.GetCurrentAnimatorStateInfo(0);
        this.currentOverrideLayer = this.anim.GetCurrentAnimatorStateInfo(0);
        this.currentFullBodyOverlayLayer = this.anim.GetCurrentAnimatorStateInfo(0);
        this.currentWeaponHoldLayer = this.anim.GetCurrentAnimatorStateInfo(0);
        if (this.anim.layerCount > 3)
        {
            this.currentAdditiveLayer = this.anim.GetCurrentAnimatorStateInfo(0);
        }
    }


    /*
     * I'm not convinced this is the best way of handling getting all our index values, however, to maximize the flexibility, I can't see another way of doing it.
     * 
     * I've wrapped all the Index calls to this helper method, as well as the GetRandomIndex value, in the hopes of easier re-factoring in the future
     */
    public void SetRandomIndex(string strParam)
    {
        var intRandom = 0;
        switch (strParam)
        {
            case "AttackIndex":
                intRandom = GetRandomIndex(AttackIndexes);
                break;
            case "SpecialAttackIndex":
                intRandom = GetRandomIndex(SpecialAttackIndexes);
                break;
            case "SpecialSecondIndex":
                intRandom = GetRandomIndex(SpecialSecondIndexes);
                break;
            case "RagingIndex":
                intRandom = GetRandomIndex(RagingIndexes);
                break;
            case "ElectrocutionIndex":
                intRandom = GetRandomIndex(ElectrocutionIndexes);
                break;
            case "CrouchIndex":
                intRandom = GetRandomIndex(CrouchIndexes);
                break;
            case "StunIndex":
                intRandom = GetRandomIndex(StunIndexes);
                break;
            case "SleeperIndex":
                intRandom = GetRandomIndex(SleeperIndexes);
                break;
            case "HarvestIndex":
                intRandom = GetRandomIndex(HarvestIndexes);
                break;
            case "PainIndex":
                intRandom = GetRandomIndex(PainIndexes);
                break;
            case "DeathIndex":
                intRandom = GetRandomIndex(DeathIndexes);
                break;
            case "RunIndex":
                intRandom = GetRandomIndex(RunIndexes);
                break;
            case "WalkIndex":
                intRandom = GetRandomIndex(WalkIndexes);
                break;
            case "IdleIndex":
                intRandom = GetRandomIndex(IdleIndexes);
                break;
            case "JumpIndex":
                intRandom = GetRandomIndex(JumpIndexes);
                break;
            case "RandomIndex":
                intRandom = GetRandomIndex(RandomIndexes);
                break;
            case "EatingIndex":
                intRandom = GetRandomIndex(EatingIndexes);
                break;
            case "AttackIdleIndex":
                intRandom = GetRandomIndex(AttackIdleIndexes);
                break;
            default:
                intRandom = 0;
                break;
        }

        Log(string.Format("Random Generator: {0} Value: {1}", strParam, intRandom));
        base.SetInt(strParam, intRandom);
    }

    public int GetRandomIndex(int intMax)
    {
        return Random.Range(0, intMax);
    }

    private void AddTransformRefs(Transform t)
    {
        if (t.GetComponent<Collider>() != null && t.GetComponent<RootTransformRefEntity>() == null)
        {
            var root = t.gameObject.AddComponent<RootTransformRefEntity>();
            root.RootTransform = transform;
        }

        foreach (Transform tran in t) AddTransformRefs(tran);
    }

    private void AddTagRecursively(Transform trans, string tag)
    {
        // If the objects are untagged, then tag them, otherwise ignore setting the tag.
        if (trans.gameObject.tag.Contains("Untagged"))
            if (trans.name.ToLower().Contains("head"))
                trans.gameObject.tag = "E_BP_Head";
            else
                trans.gameObject.tag = tag;


        // Log("Transoform Tag: " + trans.name + " : " + trans.tag);
        foreach (Transform t in trans)
            AddTagRecursively(t, tag);
    }

    protected new void assignBodyParts()
    {
        if (bipedTransform == null)
        {
            Log("assignBodyParts: GraphicsTransform is null!");
            return;
        }

        Log("Mapping Body Parts");
        this.head = FindTransform(bipedTransform, bipedTransform, "Head");
        this.rightHand = FindTransform(this.bipedTransform, this.bipedTransform, RightHand);
        Log("Body Parts Mapped");
    }

}