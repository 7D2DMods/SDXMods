using System;
using SDX.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;


public class AnimationChange : IPatcherMod
{

    public bool DebugLog = true;

    // Inorder to update the GetWalk Type, we'll need to mark the GetWalkType to be virtual, so we can over-ride it.
    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("== Animation Patcher===");
        var gm = module.Types.First(d => d.Name == "LegacyAvatarController");
        var field = gm.Fields.First(d => d.Name == "animSyncWaitTime");
        SetFieldToPublic(field);

        field = gm.Fields.First(d => d.Name == "hitDuration");
        SetFieldToPublic(field);
        return true;
    }

    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {

        Log("Linking");
        var myClass = gameModule.Types.First(d => d.Name == "EModelBase");
        var myMethod = myClass.Methods.First(d => d.Name == "Init");
        UpdateEmodelBase(myClass, myMethod);

        myMethod = myClass.Methods.First(d => d.Name == "InitFromPrefab");
        UpdateEmodelBase(myClass, myMethod);

        return true;
    }
    public bool UpdateEmodelBase( TypeDefinition myClass, MethodDefinition myMethod )
    { 
        Log("Found EmodelBase and Init");

        var myAvatarController = myClass.Methods.First(d => d.Name == "createAvatarController");
        Log("Found the AvatarController");

        var pro = myMethod.Body.GetILProcessor();
        var instructions = myMethod.Body.Instructions;
        Instruction insertAfterThis = null;
        bool updateNext = false;

        //The native animator gets turned off, so we want to keep it on
        foreach (var i in instructions.Reverse())
        {
            if (updateNext)
            {
                i.OpCode = OpCodes.Ldc_I4_1;
                break;
            }
            if (i.OpCode == OpCodes.Callvirt && i.Operand.ToString().Contains("set_enabled"))
            {
                updateNext = true;
            }
        }

        //set our avatar as correct.
        foreach (var i in instructions.Reverse())
        {
            if (i.OpCode == OpCodes.Blt_S)
            {
                Log("Adding after: " + i.ToString());
                insertAfterThis = i;
                break;
            }
        }

        if (instructions != null)
        {
            Log("Inserting Avatar Hooks");
            pro.InsertAfter(insertAfterThis, Instruction.Create(OpCodes.Callvirt, myAvatarController));
            pro.InsertAfter(insertAfterThis, Instruction.Create(OpCodes.Ldloc_0));
            pro.InsertAfter(insertAfterThis, Instruction.Create(OpCodes.Ldarg_0));


        }
        return true;
    }


    // Helper functions to allow us to access and change variables that are otherwise unavailable.
    private void SetMethodToVirtual(MethodDefinition meth)
    {
        meth.IsVirtual = true;
    }

    private void SetFieldToPublic(FieldDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
    private void SetMethodToPublic(MethodDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }

    private void Log(String strLogMessage)
    {
        if (this.DebugLog == true)
            SDX.Core.Logging.LogInfo(this.GetType().Name.ToString() + ": " + strLogMessage);

    }
}
