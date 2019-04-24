using System;
using SDX.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;


public class ItemActionsChange : IPatcherMod
{
    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("==ItemActions Patcher Patcher===");
        var gm = module.Types.First(d => d.Name == "ItemActionUseOther");
        var method = gm.NestedTypes.First(d => d.Name == "FeedInventoryData");
        method.IsNestedPublic = true;
        method.IsPublic = true;
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
