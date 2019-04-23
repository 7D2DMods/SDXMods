using System;
using SDX.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.Collections.Generic;

public class AntiNerdPole : IPatcherMod
{

    // Debug Logging
    private bool DebugLog = true;

    public bool Patch(ModuleDefinition module)
    {
        Log("=== " + this.GetType().Name.ToString() + " ===");
        var myClass = module.Types.First(d => d.Name == "EntityPlayerLocal");
        var myField = myClass.Fields.First(d => d.Name == "isAttachedToLadder");
        SetFieldToPublic(myField);
        return true;

    }


    private void SetFieldToPublic(FieldDefinition field)
    {
        if(field == null)
            return;
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        var myClass = gameModule.Types.First(d => d.Name == "Block");
        var myMethod = myClass.Methods.First(d => d.Name == "PlaceBlock");

        var AntiNerdPoleClass = modModule.Types.First(d => d.Name == "AntiNerdPoll");
        var NerdPoleCheck = gameModule.Import(AntiNerdPoleClass.Methods.First(d => d.Name == "NerdPoleCheck"));
        var instructions = myMethod.Body.Instructions;
        var pro = myMethod.Body.GetILProcessor();

        // Grab the first line so we can use it for our brfalse.
        var FirstInstruction = instructions[0];

        // Since we are adding it the begining, the call stack is inserted reverse to what it'll look like in dnSpy
        instructions.Insert(0, Instruction.Create(OpCodes.Ret));
        instructions.Insert(0, Instruction.Create(OpCodes.Brfalse_S, FirstInstruction));
        instructions.Insert(0, Instruction.Create(OpCodes.Call, NerdPoleCheck));
        instructions.Insert(0, Instruction.Create(OpCodes.Ldarg_3));
        
        

        return true;
    }


    private void Log(String strLogMessage)
    {
        if (this.DebugLog == true)
            SDX.Core.Logging.LogInfo(this.GetType().Name.ToString() + ": " + strLogMessage);

    }
}