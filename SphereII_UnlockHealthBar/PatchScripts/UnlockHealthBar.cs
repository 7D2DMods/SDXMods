using System;
using SDX.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;

public class UnlockEnemyHealthBar : IPatcherMod
{
    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("== XUiC_TargetBar ==");
        var gm = module.Types.First(d => d.Name == "XUiC_TargetBar");
        var method = gm.Methods.First(d => d.Name == "Update");

        var instructions = method.Body.Instructions;
        var processor = method.Body.GetILProcessor();
        processor.Remove(instructions.First(d => d.OpCode == OpCodes.Ret));

        return true;
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
