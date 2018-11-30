﻿/*
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

internal class MecanimSDX : AvatarController
{
    public float CheckDelay = 5f;
    private float ActionTime;

    // Animator reference
    protected Animator anim;

    // Stores our hashes as ints, since that's what the Animator wants
    private readonly HashSet<int> AttackHash;

    // Determines what the light threshold of being in the dark is
    private byte LightThreshold = 14;

    // The following are the number of indexes we are getting, populating from the XML files.
    private readonly int AttackIndexes;


    //// Maintain a list of strings of the same animation
    private readonly List<string> AttackStrings = new List<string>();

    public Transform bipedTransform;

    // If the displayLog is true, verbosely log out put. Disable in production!
    private readonly bool blDisplayLog = false;

    // for the particle system
    protected int specialAttackTicks;
    protected float timeSpecialAttackPlaying;
    protected float idleTime;

    private readonly int CrouchIndexes;

    // Animator's current state
    protected AnimatorStateInfo currentBaseState;
    private readonly int DeathIndexes;
    private int EatingIndexes;
    private readonly int ElectrocutionIndexes;

    // Our character references
    protected EntityAlive entityAlive;
    protected int Forward = Animator.StringToHash("Forward");
    private readonly int HarvestIndexes;
    private readonly int IdleIndexes;

    protected int IdleTime = Animator.StringToHash("IdleTime");
    protected int IsBreakingBlocks = Animator.StringToHash("IsBreakingBlocks");

    // These are animator hashes that we are using in the Update, to reduce the load on the animator
    protected int IsBreakingDoors = Animator.StringToHash("IsBreakingDoors");

    protected bool isInDeathAnim;
    protected int IsMoving = Animator.StringToHash("IsMoving");
    private readonly int JumpIndexes;

    // If the entity is visible
    protected bool m_bVisible =false;
    public Transform modelTransform;
    protected int MovementState = Animator.StringToHash("MovementState");

    // interval between changing the indexes in the LateUpdate
    private float nextCheck = 0.0f;
    private readonly int PainIndexes;
    private readonly int RagingIndexes;
    private readonly int RandomIndexes;

    // Support for letting entity shoot weapons
    private string RightHand = "RightHand";
    protected Animator rightHandAnimator;
    private Transform rightHandItemTransform;
    private Transform rightHand;
    private readonly int RunIndexes;
    private readonly int SleeperIndexes;
    private readonly int SpecialAttackIndexes;
    private readonly int SpecialSecondIndexes;
    protected int Strafe = Animator.StringToHash("Strafe");
    private readonly int StunIndexes;
    private readonly int WalkIndexes;
    private readonly int AttackIdleIndexes;

    // encroached legacy logic
    private float lastPlayerX;
    private float lastPlayerZ;
    private float lastDistance;
    private float DoesntSeemToDoAnything;
    private bool isAlwaysWalk;


    private bool CriticalError = false;

    private bool IsElectrocuting = false;
    private bool IsHarvesting = false;
    // Flag to determine if we are eating or not
    protected bool isEating = false;

    protected Dictionary<int, AnimParamData> ChangedAnimationParameters = new Dictionary<int, AnimParamData>();
    protected int jumpState;
    protected int fpvJumpState;
    protected int jumpTag;

    private bool Jumping = false;
    private MecanimSDX()
    {
            entityAlive = transform.gameObject.GetComponent<EntityAlive>();
        var entityClass = EntityClass.list[entityAlive.entityClass];


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

        this.jumpTag = Animator.StringToHash("Jump");
    }

    // Triggers the Eating animation state
    public override void StartEating()
    {
        if (!isEating)
        {
            SetRandomIndex("EatingIndex");
            anim.SetBool("IsEating", true);
            anim.SetTrigger("IsEatingTrigger");
            isEating = true;
        }
    }

    // Ends the eating animation state
    public override void StopEating()
    {
        if (isEating)
        {
            anim.SetBool("IsEating", false);
            isEating = false;
        }
    }

    // Enables the entity, and checks if we have root motion enabled or not. 
    public override void SwitchModelAndView(string _modelName, bool _bFPV, bool _bMale)
    {
        Log("Running Switch and Model View");
        // If Root MOtion is enabled on this entity, initialize it.
        //if (entityAlive.RootMotion)
        //{
        //    Log("Enabling Root Motion...");

        //    var avatarRootMotion = bipedTransform.GetComponent<AvatarRootMotion>();
        //    if (avatarRootMotion == null) avatarRootMotion = bipedTransform.gameObject.AddComponent<AvatarRootMotion>();

        //    avatarRootMotion.Init((AvatarController)this, anim);

        //    Log("Enabled Root Motion");
        //}

        anim.SetBool("IsDead", entityAlive.IsDead());
        anim.SetBool("IsAlive", entityAlive.IsAlive());

        assignBodyParts();

        //// Add in support for animations that can fire weapons
        if (rightHandItemTransform != null)
        {
            Log("Reading Right hand Items to equipment weapons");
            Debug.Log("Setting Right hand position");
            //Debug.Log("RIGHTHAND ITEM TRANSFORM");
            rightHandItemTransform.parent = rightHandItemTransform;
            var position = AnimationGunjointOffsetData.AnimationGunjointOffset[entityAlive.inventory.holdingItem.HoldType.Value].position;
            var rotation = AnimationGunjointOffsetData.AnimationGunjointOffset[entityAlive.inventory.holdingItem.HoldType.Value].rotation;
            rightHandItemTransform.localPosition = position;
            rightHandItemTransform.localEulerAngles = rotation;
            SetInRightHand(this.rightHandItemTransform);

        }
    }

    // Check if the Animation attack is still playing.
    public override bool IsAnimationAttackPlaying()
    {
        return  !anim.IsInTransition(0) && AttackHash.Contains(currentBaseState.fullPathHash);
    }

    // Picks a random attack index, and then fires off the attack trigger. 
    public override void StartAnimationAttack()
    {
        if (!entityAlive.isEntityRemote) ActionTime = 1f;


        // Randomly set the index for the AttackIndex, which allows us different attacks
        SetRandomIndex("AttackIndex");
        anim.SetTrigger("Attack");
        SetRandomIndex("AttackIdleIndex");
    }

    // Check if the Using animation state isplaying.
    public override bool IsAnimationUsePlaying()
    {
        return false;
    }

    // Starts the Use animation
    public override void StartAnimationUse()
    {
    }

    // Checks if the Special Attack animation is playing
    public override bool IsAnimationSpecialAttackPlaying()
    {
        return IsAnimationAttackPlaying();
    }

    // starts any special attack that we have
    public override void StartAnimationSpecialAttack(bool _b)
    {
        if (_b)
        {
            Log("Firing Special attack");
            SetRandomIndex("SpecialAttackIndex");
            anim.SetTrigger("SpecialAttack");
            this.idleTime = 0f;
            this.specialAttackTicks = 3;
            this.timeSpecialAttackPlaying = 0.8f;
        }
    }


    public override bool IsAnimationSpecialAttack2Playing()
    {
        return IsAnimationAttackPlaying();
    }

    public override void StartAnimationSpecialAttack2()
    {
        Log("Firing Second Special attack");
        SetRandomIndex("SpecialSecondAttack");
        anim.SetTrigger("SpecialSecondAttack");
    }

    // Checks if any of the Raging Animations are playing
    public override bool IsAnimationRagingPlaying()
    {
        return false;
    }

    public override void StartAnimationRaging()
    {
        SetRandomIndex("RagingIndex");
        anim.SetTrigger("Raging");
    }

    public override bool IsAnimationElectrocutedPlaying()
    {
        return IsElectrocuting;

    }

    public override void StartAnimationElectrocuted()
    {
        if (IsAnimationElectrocutedPlaying())
            return;

        IsElectrocuting = true;
        SetRandomIndex("ElectrocutionIndex");
        anim.SetTrigger("Electrocution");
    }

    public override bool IsAnimationHarvestingPlaying()
    {
        return IsHarvesting;
       
    }

    // Starts a Harvest trigger, if there is one
    public override void StartAnimationHarvesting()
    {
        if (IsAnimationHarvestingPlaying())
            return;
        IsHarvesting = true;
        SetRandomIndex("HarvestIndex");
        anim.SetTrigger("Harvest");
    }

  
    // Wakes up the entity and sets it alive
    public override void SetAlive()
    {
        anim.SetBool("IsAlive", true);
        anim.SetBool("IsDead", false);
        anim.SetTrigger("Alive");
    }


    // Determines and triggers whether we fire off the drunk animation
    public override void SetDrunk(float _numBeers)
    {
        if (_numBeers > 3)
        {
            SetRandomIndex("DrunkIndex");
            anim.SetTrigger("Drunk");
        }
    }

    // No implemented
    public override void SetMinibikeAnimation(string _animSuffix, bool _isPlaying)
    {
    }

    public override void SetMinibikeAnimation(string _animSuffix, float _amount)
    {
    }

    public override void SetHeadAngles(float _nick, float _yaw)
    {
    }

    public override void SetArmsAngles(float _rightArmAngle, float _leftArmAngle)
    {
    }

    public override void SetAiming(bool _bEnable)
    {
    }

    public override void SetWalkingSpeed(float _f)
    {
    }

// Determines if the entity is crouching or not  
    public override void SetCrouching(bool _bEnable)
    {
        if (_bEnable) SetRandomIndex("CrouchIndex");

        anim.SetBool("IsCrouching", _bEnable);
    }

    // Token: 0x0600121A RID: 4634 RVA: 0x00082614 File Offset: 0x00080814
    public override void SetVisible(bool _b)
    {
        if (m_bVisible != _b)
        {
            m_bVisible = _b;

            var transform = bipedTransform;
            if (transform != null)
            {
                var componentsInChildren = transform.GetComponentsInChildren<Renderer>();
                for (var i = 0; i < componentsInChildren.Length; i++)
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

    // starts the jumping animation
    public override void StartAnimationJumping()
    {
        SetRandomIndex("JumpIndex");
        anim.SetTrigger("Jump");
    }

    // The shooting animation
    public override void StartAnimationFiring()
    {
    }

    // ANimation for when the entity gets hit
    public override void StartAnimationHit(EnumBodyPartHit _bodyPart, int _dir, int _hitDamage, bool _criticalHit,
        int _movementState, float _random)
    {
        SetRandomIndex("PainIndex");
        anim.SetTrigger("Pain");
    }

    // Animation check to see for when the entity gets hit while running
    public override bool IsAnimationHitRunning()
    {
        return false;
    }

    // Starts the death animation
    public override void StartDeathAnimation(EnumBodyPartHit _bodyPart, int _movementState, float _random)
    {
        SetRandomIndex("DeathIndex");
        anim.SetBool("IsDead", true);
    }

    // Code from Mortelentus, that allows us to set an animator in the right hand for weapons
    public override void SetInRightHand(Transform _transform)
    {
       if (this.rightHandItemTransform == null || _transform == null )
            return;

        Log("Setting Right Hand: " + rightHandItemTransform.name.ToString());

        this.idleTime = 0f;
    
        Log("Setting Right Hand Transform");
        this.rightHandItemTransform = _transform;
        if (this.rightHandItemTransform == null)
        {
            Log("Right Hand Animator is Null");
            return;
        }
        else
        {
            // Animator is important to show the particle
            Log("Right Hand Animator is NOT NULL ");
            this.rightHandAnimator = rightHandItemTransform.GetComponent<Animator>();
        }
        
        if (this.rightHandItemTransform != null)
        {
            Utils.SetLayerRecursively(this.rightHandItemTransform.gameObject, 0);
        }

        Log("Done with SetInRightHand");

    }

    // Reutrns the righthand transform
    public override Transform GetRightHandTransform()
    {
        return rightHandItemTransform;
    }

    public override Transform GetActiveModelRoot()
    {
        return !modelTransform ? bipedTransform : modelTransform;
    }

    public override void SetLookPosition(Vector3 _pos)
    {
    }

    public override void CrippleLimb(EnumBodyPartHit _bodyPart, bool restoreState)
    {
    }

    public override void TurnIntoCrawler(bool restoreState)
    {
    }

    // starts the Stun Animation
    public override void BeginStun(EnumEntityStunType stun, EnumBodyPartHit _bodyPart, Utils.EnumHitDirection _hitDirection, bool _criticalHit,
        float random)
    {
        SetRandomIndex("StunIndex");
        anim.SetBool("IsStunned", true);
    }

    // Ends the stunned animation
    public override void EndStun()
    {
        anim.SetBool("IsStunned", false);
    }

    public override void PlayPlayerFPRevive()
    {
    }

    public override void SetArchetypeStance(NPCInfo.StanceTypes stance)
    {
    }

    public override void NotifyAnimatorMove(Animator instigator)
    {
        Log("Notify Animator Move!");
        entityAlive.NotifyRootMotion(instigator);
    }


    // Sets the Sleeper index
    public override void TriggerSleeperPose(int pose)
    {
        if (anim != null)
        {
            anim.SetInteger("SleeperPose", pose);
            anim.SetTrigger("SleeperTrigger");
        }
    }

    public override void RemoveLimb(EnumBodyPartHit _bodyPart, bool restoreState)
    {
        switch (_bodyPart)
        {
            case EnumBodyPartHit.Head:
                if (!headDismembered)
                {
                    headDismembered = true;
                    neck.localScale = Vector3.zero;
                    SpawnLimbGore(neckGore, "Prefabs/HeadGore", restoreState);
                }

                break;
            case EnumBodyPartHit.LeftUpperArm:
                if (!leftUpperArmDismembered)
                {
                    leftUpperArmDismembered = true;
                    leftUpperArm.localScale = Vector3.zero;
                    SpawnLimbGore(leftUpperArmGore, "Prefabs/UpperArmGore", restoreState);
                }

                break;
            case EnumBodyPartHit.RightUpperArm:
                if (!rightUpperArmDismembered)
                {
                    rightUpperArmDismembered = true;
                    rightUpperArm.localScale = Vector3.zero;
                    SpawnLimbGore(rightUpperArmGore, "Prefabs/UpperArmGore", restoreState);
                }

                break;
            case EnumBodyPartHit.LeftUpperLeg:
                if (!leftUpperLegDismembered)
                {
                    leftUpperLegDismembered = true;
                    leftUpperLeg.localScale = Vector3.zero;
                    SpawnLimbGore(leftUpperLegGore, "Prefabs/UpperLegGore", restoreState);
                }

                break;
            case EnumBodyPartHit.RightUpperLeg:
                if (!rightUpperLegDismembered)
                {
                    rightUpperLegDismembered = true;
                    rightUpperLeg.localScale = Vector3.zero;
                    SpawnLimbGore(rightUpperLegGore, "Prefabs/UpperLegGore", restoreState);
                }

                break;
            case EnumBodyPartHit.LeftLowerArm:
                if (!leftLowerArmDismembered)
                {
                    leftLowerArmDismembered = true;
                    leftLowerArm.localScale = Vector3.zero;
                    SpawnLimbGore(leftLowerArmGore, "Prefabs/LowerArmGore", restoreState);
                }

                break;
            case EnumBodyPartHit.RightLowerArm:
                if (!rightLowerArmDismembered)
                {
                    rightLowerArmDismembered = true;
                    rightLowerArm.localScale = Vector3.zero;
                    SpawnLimbGore(rightLowerArmGore, "Prefabs/LowerArmGore", restoreState);
                }

                break;
            case EnumBodyPartHit.LeftLowerLeg:
                if (!leftLowerLegDismembered)
                {
                    leftLowerLegDismembered = true;
                    leftLowerLeg.localScale = Vector3.zero;
                    SpawnLimbGore(leftLowerLegGore, "Prefabs/LowerLegGore", restoreState);
                }

                break;
            case EnumBodyPartHit.RightLowerLeg:
                if (!rightLowerLegDismembered)
                {
                    rightLowerLegDismembered = true;
                    rightLowerLeg.localScale = Vector3.zero;
                    SpawnLimbGore(rightLowerLegGore, "Prefabs/LowerLegGore", restoreState);
                }

                break;
        }
    }

    private void Log(string strLog)
    {
        if (blDisplayLog)
        {
            if ( this.modelTransform == null )
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
                Log("Adding Attack Hash: " + strAnimationState);
                hash.Add(Animator.StringToHash(strAnimationState.Trim()));
            }
        }
        return hash;
    }


    // Token: 0x060011F6 RID: 4598 RVA: 0x00081E3C File Offset: 0x0008003C
    private void Awake()
    {
        Log("Method: " + MethodBase.GetCurrentMethod().Name);
        Log("Initializing " + entityAlive.name);
        try
        {
            Log("Checking For Graphics Transform...");
            bipedTransform = transform.Find("Graphics");
            if (bipedTransform == null)
            {
                Log(" !! Graphics Transform null!");
                return;
            }

            Log("Checking For Model Transform...");
            if (bipedTransform.Find("Model") == null)
            {
                Log("!! No Model found in GraphicsTransoform");
                return;
            }

            Log("Biped Transform: " + bipedTransform.childCount);
            Log("Model name: " + bipedTransform.Find("Model").name + " with " +
                bipedTransform.FindChild("Model").childCount);

            modelTransform = bipedTransform.Find("Model").GetChild(0);
            if (modelTransform == null)
            {
                Log(" !! Model Transform is null!");
                return;
            }


            //this bit is important for SDXers! It adds the component that links each collider with the Entity class so hits can be registered.
            Log("Adding Colliders");
            AddTransformRefs(modelTransform);

            //if you're using A14 or haven't set specific tags for the collision in Unity un-comment this and it will set them all to being body contacts
            //using this method means things like head shot multiplers won't work but it will enable basic collision
            Log("Tagging the Body");
            AddTagRecursively(modelTransform, "E_BP_Body");

            // Searchs for the animator
            Log("Searching for Animator");
            anim = modelTransform.GetComponent<Animator>();
            if (anim == null)
            {
                Log("*** Animator Not Found! Invalid Class");
                CriticalError = true;
                throw new Exception("Animator Not Found! Wrong class is being used! Try AnimationSDX instead...");
            }

            Log("Animator Found");

            anim.enabled = true;


            if (this.anim.runtimeAnimatorController)
            {
                Log("My Animator Controller has: " + this.anim.runtimeAnimatorController.animationClips.Length + " Animations");
                foreach (var animation in this.anim.runtimeAnimatorController.animationClips)
                {
                    Log("Animation Clip: " + animation.name.ToString());
                    
                }
            }
            else
            {
                Log(string.Format("{0} : My Animator Controller is null!", this.modelTransform.name));
                CriticalError = true;
                throw (new Exception("Animator Controller is null!"));
            }

            Log("Searching for possible Right Hand Joint");
            var entityClass = EntityClass.list[entityAlive.entityClass];

            // Find the right hand joint, if it's set.
            if (entityClass.Properties.Values.ContainsKey("RightHandJointName"))
            {
                this.RightHand = entityClass.Properties.Values["RightHandJointName"];
                this.rightHandItemTransform = FindTransform(this.bipedTransform, this.bipedTransform, RightHand);
                if (this.rightHandItemTransform)
                    Log("Right Hand Item Transform: " + this.rightHandItemTransform.name.ToString());
                else
                    Log("Right Hand Item Transform: Could not find Transofmr: " + RightHand);
            }

        }
        catch (Exception ex)
        {
            Log("Exception thrown in Awake() " + ex);
        }
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

    // Triggers a generic Trigger
    public void StartCustomTrigger(string strTrigger)
    {
        anim.SetTrigger(strTrigger);
    }

    public void SetCustomBool(string strTrigger, bool blValue)
    {
        anim.SetBool(strTrigger, blValue);
    }

    private void UpdateBaseState()
    {
        currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
    }

    // Sets the current animator State
    private void UpdateCurrentState()
    {
        currentBaseState = anim.GetCurrentAnimatorStateInfo(0);
    }


    // The Update route
    protected virtual void LateUpdate()
    {
        try
        {

            if (this.entityAlive == null)
            {
                Log("The entity is null!");
                CriticalError = true;
            }

            // If a critical error is thrown in this method, flag it to be true, so we don't get into a spaming loop.
            if (CriticalError)
            {
                
                return;
            }

            // Delay the updating of the indexes by the CheckDelay, which is 5seconds by default.
            if (nextCheck == 0.0f || nextCheck < Time.time)
            {
                nextCheck = Time.time + CheckDelay;
                SetRandomIndex("RandomIndex");

                SetRandomIndex("WalkIndex");
                SetRandomIndex("RunIndex");
                SetRandomIndex("IdleIndex");


                Vector3i v = new Vector3i(this.entityAlive.position);
                if (v.x < 0) v.x -= 1;
                if (v.z < 0) v.z -= 1;
                byte lightLevel = GameManager.Instance.World.ChunkClusters[0].GetLight(v, Chunk.LIGHT_TYPE.SUN);

                
                Log("Sun Light Level: " + lightLevel);

                // If the current light is above the threshold, it means it's too bright out.
                if (lightLevel < LightThreshold)
                {
                    anim.SetBool("IsDark", true);
                }
                else
                {
                    anim.SetBool("IsDark", false);
                }

             

                // Read the entitie's stats and inform the state machine.
                anim.SetFloat("statTemperature", this.entityAlive.Stats.CoreTemp.Value);
                anim.SetFloat("statHealth", this.entityAlive.Stats.Health.Value);
                anim.SetFloat("statStamina", this.entityAlive.Stats.Stamina.Value);
                
                anim.SetFloat("statTemperature", this.entityAlive.Stats.CoreTemp.Value);

                // determines the level of food and water the entity has
                anim.SetFloat("statWater", this.entityAlive.Stats.Water.Value);
              //  anim.SetFloat("statFood", this.entityAlive.Stats.Food.Value);

                // Determines if the entity is in the shade or not
                anim.SetBool("statShaded", this.entityAlive.Stats.Shaded);


            }



            ActionTime -= Time.deltaTime;

            if (bipedTransform == null || !bipedTransform.gameObject.activeInHierarchy)
            {
                Log("Biped Transform was null!");
                CriticalError = true;
                return;
            }

            if (anim == null || anim.avatar == null)
            {
                Log(string.Format("Animator or Animator Avatar is Null for {0}", modelTransform.name));
                CriticalError = true;
                return;
            }
            if (anim.avatar.isValid && anim.enabled)
            {
                if (entityAlive.IsBreakingBlocks && !anim.GetBool(IsBreakingBlocks))
                    anim.SetBool(IsBreakingBlocks, true);
                else
                    anim.SetBool(IsBreakingBlocks, false);

                if (entityAlive.IsBreakingDoors && !anim.GetBool(IsBreakingDoors))
                    anim.SetBool(IsBreakingDoors, true);
                else
                    anim.SetBool(IsBreakingDoors, false);

                UpdateCurrentState();

                //// Test code for the State OneShot
                //Log("Animation State Machine Current State: " + anim.GetCurrentAnimatorStateInfo(0).);
                //if (this.anim.GetCurrentAnimatorStateInfo(0).IsName("OneShot"))
                //{
                //    anim.SetBool("OneShot", true);
                //}
                //else
                //{
                //    anim.SetBool("OneShot", false);
                //}

                if (this.timeSpecialAttackPlaying > 0f)
                {
                    // it's playing special attack
                    this.timeSpecialAttackPlaying -= Time.deltaTime;
                    return;
                }

                // Test condition while we evaluate the effectiveness of using encroachment for state machine triggers.
                if (true)
                {

                    if (this.timeSpecialAttackPlaying > 0f)
                    {
                        // it's playing special attack
                        this.timeSpecialAttackPlaying -= Time.deltaTime;
                        return;
                    }

                    Log("Last Distance: " + this.lastDistance);
                    float playerDistanceX = 0.0f;
                    float playerDistanceZ = 0.0f;
                    float encroached = this.lastDistance;


                    // Calculates how far away the entity is
                    playerDistanceX = Mathf.Abs(this.entityAlive.position.x - this.entityAlive.lastTickPos[0].x) * 6f;
                    playerDistanceZ = Mathf.Abs(this.entityAlive.position.z - this.entityAlive.lastTickPos[0].z) * 6f;

                    if (!this.entityAlive.isEntityRemote)
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

                    Log("LastPlayerX: " + this.lastPlayerX);
                    Log("LastPlayerZ: " + this.lastPlayerZ);
                    Log("Last Distance:" + this.lastDistance);
                    Log("Encroachment: " + encroached);


                    // if the entity is in water, flag it, so we'll do the swiming conditions before the movement.
                    this.anim.SetBool("IsInWater", entityAlive.IsInWater());
                    Log("Entity is in Water: " + entityAlive.IsInWater());
                    
                    Log("Movement State on Animation: " + this.anim.GetInteger("MovementState"));
                   // this.entityAlive.world.IsMaterialInBounds(BoundsUtils.ExpandBounds(this.entityAlive.boundingBox, -0.1f, -0.4f, -0.1f), MaterialBlock.water);
                    

                    if ( entityAlive.Electrocuted)
                        StartAnimationElectrocuted();
                    else
                        this.IsElectrocuting = false;

                    if (entityAlive.HarvestingAnimation)
                        StartAnimationHarvesting();
                    else
                        this.IsHarvesting = false;

                    if (entityAlive.IsEating)
                        StartEating();
                    else
                        this.isEating = false;


                    if (encroached > 0.150000005960464)
                    {

                        // Running if above 1.0
                        if (encroached > 1.0)
                        {
                            this.anim.SetInteger(MovementState, 2);
                        }
                        else
                        {
                            this.anim.SetInteger(MovementState, 1);
                        }
                    }
                    else
                    {
                        this.anim.SetInteger(MovementState, 0);

                    }
                    Log("Movement State on Animation: " + this.anim.GetInteger("MovementState"));
                }
                else
                {
                    // this code uses a newer version of logic, but seems to be inconsistent
                    var num = 0f;
                    var num2 = 0f;
                    if (!entityAlive.IsFlyMode.Value)
                    {
                        num = entityAlive.speedForward;
                        num2 = entityAlive.speedStrafe;
                    }

                    var num3 = num2;
                    if (num3 >= 1234f) num3 = 0f;

                    anim.SetFloat(Forward, num);
                    anim.SetFloat(Strafe, num3);
                    var num4 = num * num + num3 * num3;
                    if (!entityAlive.IsDead())
                        anim.SetInteger(MovementState,
                            num4 <= entityAlive.speedApproach * entityAlive.speedApproach
                                ? (num4 <= entityAlive.speedWander * entityAlive.speedWander
                                    ? (num4 <= 0.001f ? 0 : 1)
                                    : 2)
                                : 3);



                    if (Mathf.Abs(num) <= 0.01f && Mathf.Abs(num2) <= 0.01f)
                    {
                        anim.SetBool(IsMoving, false);
                    }
                    else
                    {
                        idleTime = 0f;

                        anim.SetBool(IsMoving, true);
                    }
                }

                var flag = false;
                if (anim != null && isInDeathAnim && !anim.IsInTransition(0)
                ) // this.DeathHash.Contains(this.currentBaseState.fullPathHash) )
                    flag = true;

                if (anim != null && isInDeathAnim && flag &&
                    (currentBaseState.normalizedTime >= 1f || anim.IsInTransition(0)))
                {
                    isInDeathAnim = false;
                    if (entityAlive.HasDeathAnim) entityAlive.emodel.DoRagdoll(DamageResponse.New(true), 999999f);
                }

                anim.SetFloat(IdleTime, idleTime);
                idleTime += Time.deltaTime;
            }
        }
        catch (Exception ex)
        {
            // Because errors in the LateUpdate are pretty fatal, we'll flag it as Critical so things won't get stuck in a spam update, and
            // the debug log will display all the time.
            Debug.Log(string.Format("Error in LateUpdate for {0} : {1}", this.modelTransform.name, ex.ToString()));
            CriticalError = true;
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
        anim.SetInteger(strParam, intRandom);
    }

    public int GetRandomIndex(int intMax)
    {
        return Random.Range(0, intMax);
    }

    // Token: 0x06001231 RID: 4657 RVA: 0x00082ABC File Offset: 0x00080CBC
    private EntityClass GetAvailableTriggers()
    {
        return EntityClass.list[entityAlive.entityClass];
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

    protected void SpawnLimbGore(Transform parent, string path, bool restoreState)
    {
        if (parent != null)
        {
            var original = ResourceWrapper.Load1P(path) as GameObject;
            var gameObject =
                Instantiate(original, Vector3.zero, Quaternion.identity) as GameObject;
            gameObject.transform.parent = parent;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = parent.localScale;
            var component = gameObject.GetComponent<GorePrefab>();
            if (component != null) component.restoreState = restoreState;
        }
    }

    protected void assignBodyParts()
    {
        if (bipedTransform == null)
        {
            Log("assignBodyParts: GraphicsTransform is null!");
            return;
        }

        Log("Mapping Body Parts");
        //Debug.Log("FIND Head");
        head = FindTransform(bipedTransform, bipedTransform, "Head");
        //Debug.Log("FIND NECK");
        neck = FindTransform(bipedTransform, bipedTransform, "Neck");
        //this.entityLiving.GetRightHandTransformName()
        //Debug.Log("LOOKING FOR " + rightHandStr);
        this.rightHand = FindTransform(this.bipedTransform, this.bipedTransform, RightHand);
        //if (this.rightHand != null) Debug.Log("Right HAND = " + this.rightHand.name);
        //Debug.Log("FIND LEGS");
        leftUpperLeg = FindTransform(bipedTransform, bipedTransform, "LeftUpLeg");
        leftLowerLeg = FindTransform(bipedTransform, bipedTransform, "LeftLeg");
        rightUpperLeg = FindTransform(bipedTransform, bipedTransform, "RightUpLeg");
        rightLowerLeg = FindTransform(bipedTransform, bipedTransform, "RightLeg");
        //Debug.Log("FIND ARMS");
        leftUpperArm = FindTransform(bipedTransform, bipedTransform, "LeftArm");
        leftLowerArm = FindTransform(bipedTransform, bipedTransform, "LeftForeArm");
        rightUpperArm = FindTransform(bipedTransform, bipedTransform, "RightArm");
        rightLowerArm = FindTransform(bipedTransform, bipedTransform, "RightForeArm");
        Log("Body Parts Mapped");
        try
        {
            Log("Searching For Gore Tags");
            //Debug.Log("FIND GOORES");
            neckGore = GameUtils.FindTagInChilds(bipedTransform, "L_HeadGore");
            leftUpperArmGore = GameUtils.FindTagInChilds(bipedTransform, "L_LeftUpperArmGore");
            leftLowerArmGore = GameUtils.FindTagInChilds(bipedTransform, "L_LeftLowerArmGore");
            rightUpperArmGore = GameUtils.FindTagInChilds(bipedTransform, "L_RightUpperArmGore");
            rightLowerArmGore = GameUtils.FindTagInChilds(bipedTransform, "L_RightLowerArmGore");
            leftUpperLegGore = GameUtils.FindTagInChilds(bipedTransform, "L_LeftUpperLegGore");
            leftLowerLegGore = GameUtils.FindTagInChilds(bipedTransform, "L_LeftLowerLegGore");
            rightUpperLegGore = GameUtils.FindTagInChilds(bipedTransform, "L_RightUpperLegGore");
            rightLowerLegGore = GameUtils.FindTagInChilds(bipedTransform, "L_RightLowerLegGore");
            Log("Gore Tag Search complete");
        }
        catch (Exception)
        {
            Debug.Log("NO GORE FOR YOU, NO SIREE");
        }
    }

    // Token: 0x06001F7C RID: 8060 RVA: 0x000D30BC File Offset: 0x000D12BC
    public override void SetTrigger(string _property)
    {
        this.SetTrigger(Animator.StringToHash(_property));
    }

    // Token: 0x06001F7D RID: 8061 RVA: 0x000D30CC File Offset: 0x000D12CC
    public override void ResetTrigger(string _property)
    {
        this.ResetTrigger(Animator.StringToHash(_property));
    }

    // Token: 0x06001F7E RID: 8062 RVA: 0x000D30DC File Offset: 0x000D12DC
    public override void SetFloat(string _property, float _value)
    {
        this.SetFloat(Animator.StringToHash(_property), _value);
    }

    // Token: 0x06001F7F RID: 8063 RVA: 0x000D30EC File Offset: 0x000D12EC
    public override void SetBool(string _property, bool _value)
    {
        this.SetBool(Animator.StringToHash(_property), _value);
    }

    // Token: 0x06001F80 RID: 8064 RVA: 0x000D30FC File Offset: 0x000D12FC
    public override void SetInt(string _property, int _value)
    {
        this.SetInt(Animator.StringToHash(_property), _value);
    }

    // Token: 0x06001F81 RID: 8065 RVA: 0x000D310C File Offset: 0x000D130C
    public override void SetTrigger(int _propertyHash)
    {
        if (this.anim != null)
        {
            this.anim.SetTrigger(_propertyHash);
        }
        if (this.rightHandAnimator != null && this.rightHandAnimator.runtimeAnimatorController != null)
        {
            this.rightHandAnimator.SetTrigger(_propertyHash);
        }
        if (!this.entityAlive.isEntityRemote)
        {
            this.ChangedAnimationParameters[_propertyHash] = new AnimParamData(_propertyHash, AnimatorControllerParameterType.Trigger, true);
        }
    }

    // Token: 0x06001F82 RID: 8066 RVA: 0x000D3188 File Offset: 0x000D1388
    public override void ResetTrigger(int _propertyHash)
    {
        if (this.anim != null)
        {
            this.anim.ResetTrigger(_propertyHash);
        }
        if (this.rightHandAnimator != null && this.rightHandAnimator.runtimeAnimatorController != null)
        {
            this.rightHandAnimator.ResetTrigger(_propertyHash);
        }
        if (!this.entityAlive.isEntityRemote)
        {
            this.ChangedAnimationParameters[_propertyHash] = new AnimParamData(_propertyHash, AnimatorControllerParameterType.Trigger, false);
        }
    }

    // Token: 0x06001F83 RID: 8067 RVA: 0x000D3204 File Offset: 0x000D1404
    public override void SetFloat(int _propertyHash, float _value)
    {
        if (this.anim != null)
        {
            this.anim.SetFloat(_propertyHash, _value);
        }
        if (this.rightHandAnimator != null && this.rightHandAnimator.runtimeAnimatorController != null)
        {
            this.rightHandAnimator.SetFloat(_propertyHash, _value);
        }
        if (!this.entityAlive.isEntityRemote)
        {
            this.ChangedAnimationParameters[_propertyHash] = new AnimParamData(_propertyHash, AnimatorControllerParameterType.Float, _value);
        }
    }

    // Token: 0x06001F84 RID: 8068 RVA: 0x000D3280 File Offset: 0x000D1480
    public override void SetBool(int _propertyHash, bool _value)
    {
        if (this.anim != null)
        {
            this.anim.SetBool(_propertyHash, _value);
        }
        if (this.rightHandAnimator != null && this.rightHandAnimator.runtimeAnimatorController != null)
        {
            this.rightHandAnimator.SetBool(_propertyHash, _value);
        }
        if (!this.entityAlive.isEntityRemote && !_propertyHash.Equals(Animator.StringToHash("IsFPV")))
        {
            this.ChangedAnimationParameters[_propertyHash] = new AnimParamData(_propertyHash, AnimatorControllerParameterType.Bool, _value);
        }
    }

    // Token: 0x06001F85 RID: 8069 RVA: 0x000D3310 File Offset: 0x000D1510
    public override void SetInt(int _propertyHash, int _value)
    {
        if (this.anim != null)
        {
            this.anim.SetInteger(_propertyHash, _value);
        }
        if (this.rightHandAnimator != null && this.rightHandAnimator.runtimeAnimatorController != null)
        {
            this.rightHandAnimator.SetInteger(_propertyHash, _value);
        }
        if (!this.entityAlive.isEntityRemote)
        {
            this.ChangedAnimationParameters[_propertyHash] = new AnimParamData(_propertyHash, AnimatorControllerParameterType.Int, _value);
        }
    }

    // Token: 0x06001F86 RID: 8070 RVA: 0x000D338C File Offset: 0x000D158C
    public override  bool TryGetTrigger(string _property, out bool _value)
    {
        return this.TryGetTrigger(Animator.StringToHash(_property), out _value);
    }

    // Token: 0x06001F87 RID: 8071 RVA: 0x000D339C File Offset: 0x000D159C
    public override bool TryGetFloat(string _property, out float _value)
    {
        return this.TryGetFloat(Animator.StringToHash(_property), out _value);
    }

    // Token: 0x06001F88 RID: 8072 RVA: 0x000D33AC File Offset: 0x000D15AC
    public override bool TryGetBool(string _property, out bool _value)
    {
        return this.TryGetBool(Animator.StringToHash(_property), out _value);
    }

    // Token: 0x06001F89 RID: 8073 RVA: 0x000D33BC File Offset: 0x000D15BC
    public override  bool TryGetInt(string _property, out int _value)
    {
        return this.TryGetInt(Animator.StringToHash(_property), out _value);
    }

    // Token: 0x06001F8A RID: 8074 RVA: 0x000D33CC File Offset: 0x000D15CC
    public override  bool TryGetTrigger(int _propertyHash, out bool _value)
    {
        if (this.anim == null)
        {
            _value = false;
            return false;
        }
        _value = this.anim.GetBool(_propertyHash);
        return true;
    }

    // Token: 0x06001F8B RID: 8075 RVA: 0x000D33F0 File Offset: 0x000D15F0
    public override  bool TryGetFloat(int _propertyHash, out float _value)
    {
        if (this.anim == null)
        {
            _value = 0f;
            return false;
        }
        _value = this.anim.GetFloat(_propertyHash);
        return true;
    }

    // Token: 0x06001F8C RID: 8076 RVA: 0x000D3418 File Offset: 0x000D1618
    public override  bool TryGetBool(int _propertyHash, out bool _value)
    {
        if (this.anim == null)
        {
            _value = false;
            return false;
        }
        _value = this.anim.GetBool(_propertyHash);
        return true;
    }

    // Token: 0x06001F8D RID: 8077 RVA: 0x000D343C File Offset: 0x000D163C
    public override  bool TryGetInt(int _propertyHash, out int _value)
    {
        if (this.anim == null)
        {
            _value = 0;
            return false;
        }
        _value = this.anim.GetInteger(_propertyHash);
        return true;
    }
  
    // Token: 0x06001F66 RID: 8038 RVA: 0x000D24E4 File Offset: 0x000D06E4
    public override  void StartAnimationJump(AnimJumpMode jumpMode)
    {
        this.idleTime = 0f;
        if (!(this.bipedTransform == null) && this.bipedTransform.gameObject.activeInHierarchy)
        {
            this.Jumping = true;
            if (this.anim != null)
            {
                if (jumpMode == AnimJumpMode.Start)
                {
                    this.SetTrigger("JumpStart");
                }
                else
                {
                    this.SetTrigger("JumpLand");
                }
            }
            return;
        }
    }

  
    // Token: 0x06001F67 RID: 8039 RVA: 0x000D2550 File Offset: 0x000D0750
    public override bool IsAnimationJumpRunning()
    {
        return this.Jumping || this.jumpTag == this.currentBaseState.tagHash;
    }

    public override Animator GetAnimator()
    {
        return this.anim;
    }
    #region Tags Zombie;

    protected bool isCrippled;
    protected bool isCrawler;
    protected bool suppressPainLayer;
    protected float crawlerTime;
    protected bool headDismembered;
    protected bool leftUpperArmDismembered;
    protected bool leftLowerArmDismembered;
    protected bool rightUpperArmDismembered;
    protected bool rightLowerArmDismembered;
    protected bool leftUpperLegDismembered;
    protected bool leftLowerLegDismembered;
    protected bool rightUpperLegDismembered;
    protected bool rightLowerLegDismembered;
    protected Transform head;
    protected Transform neck;
    protected Transform leftUpperArm;
    protected Transform leftLowerArm;
    protected Transform rightLowerArm;
    protected Transform rightUpperArm;
    protected Transform leftUpperLeg;
    protected Transform leftLowerLeg;
    protected Transform rightLowerLeg;
    protected Transform rightUpperLeg;
    protected Transform neckGore;
    protected Transform leftUpperArmGore;
    protected Transform leftLowerArmGore;
    protected Transform rightUpperArmGore;
    protected Transform rightLowerArmGore;
    protected Transform leftUpperLegGore;
    protected Transform leftLowerLegGore;
    protected Transform rightLowerLegGore;
    protected Transform rightUpperLegGore;

    #endregion;
}