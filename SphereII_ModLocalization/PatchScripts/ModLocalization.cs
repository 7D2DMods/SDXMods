using System;
using SDX.Compiler;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.Collections.Generic;
using SDX.Core;

public class ModLocalization : IPatcherMod
{
  

    // Debug Logging
    private bool DebugLog = true;

    public bool Patch(ModuleDefinition module)
    {
        Log("=== Mod Localization ===");
        var gm = module.Types.First(d => d.Name == "Localization");
        var field = gm.Fields.First(d => d.Name == "mDictionary");
        SetFieldToPublic(field);
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

    private void Log( String strLogMessage )
    {
        if (this.DebugLog == true)
            SDX.Core.Logging.LogInfo( this.GetType().Name.ToString() + ": " + strLogMessage);

    }
}