using System;
using SDX.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;


public class QuestChanges : IPatcherMod
{
   

    // Inorder to update the GetWalk Type, we'll need to mark the GetWalkType to be virtual, so we can over-ride it.
    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("==Quest Patcher Patcher===");
        var gm = module.Types.First(d => d.Name == "QuestEventManager");
        var method = gm.Methods.First(d => d.Name == "HandleNewCompletedQuest");
        AddPlayerNullCheck(method, module);



        return true;
    }


    // Adds a player Null Check at the begining of the method, returning harmlessly if its not initialized.
    public void AddPlayerNullCheck( MethodDefinition myMethod, ModuleDefinition module )
    {
        var instructions = myMethod.Body.Instructions;
        var pro = myMethod.Body.GetILProcessor();

        // Finds a reference to the Unity's comparison method, so we can check !player
        var unityEngine = module.Import(module.GetTypeReferences().First(t => t.FullName == "UnityEngine.Object"));
        var op_implicit = unityEngine.Resolve().Methods.First(d => d.Name == "op_Implicit");
        var temp = module.Import(op_implicit);

        var first = myMethod.Body.Instructions[0];
        pro.InsertBefore(first, Instruction.Create(OpCodes.Ldarg_1));
        pro.InsertBefore(first, Instruction.Create(OpCodes.Call, temp));
        pro.InsertBefore(first, Instruction.Create(OpCodes.Brtrue_S, first));
        pro.InsertBefore(first, Instruction.Create(OpCodes.Ret));





    }

    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
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
}
