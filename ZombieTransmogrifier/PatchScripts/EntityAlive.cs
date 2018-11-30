using System;
using SDX.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;


public class EntityAliveChange : IPatcherMod
{
   

    // Inorder to update the GetWalk Type, we'll need to mark the GetWalkType to be virtual, so we can over-ride it.
    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("==EntityAlive Patcher===");
        var gm = module.Types.First(d => d.Name == "EntityAlive");
        var method = gm.Methods.First(d => d.Name == "GetWalkType");
        SetMethodToVirtual(method);

        method = gm.Methods.First(d => d.Name == "Attack");
        SetMethodToVirtual(method);

        gm = module.Types.First(d => d.Name == "EntityMoveHelper");
        method = gm.Methods.First(d => d.Name == "UpdateMoveHelper");
        SetMethodToPublic(method);
        SetMethodToVirtual(method);

        //gm = module.Types.First(d => d.Name == "LegacyAvatarController");
        //var field = gm.Fields.First(d => d.Name == "anim");
        //SetFieldToPublic(field);


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
