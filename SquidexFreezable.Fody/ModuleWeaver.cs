﻿using System.Collections.Generic;
using System.Linq;
using Fody;

public class ModuleWeaver:BaseModuleWeaver
{
    public override void Execute()
    {
        var freezableTypeFinder = new FreezableTypeFinder(ModuleDefinition, ResolveAssembly);
        freezableTypeFinder.Execute();
        var typeFinder = new TypeFinder(ModuleDefinition, FindTypeDefinition);
        typeFinder.Execute();

        var volatileFieldFixer = new VolatileFieldFixer(typeFinder);
        var fieldInjector = new FieldInjector(typeFinder, TypeSystem, volatileFieldFixer);
        var checkIsFrozenBuilder = new CheckIsFrozenBuilder(TypeSystem, typeFinder);
        var freezeCheckerInjector = new FreezeCheckerInjector(ModuleDefinition, fieldInjector, checkIsFrozenBuilder);

        var typeResolver = new TypeResolver();
        var implementsInterfaceFinder = new ImplementsInterfaceFinder(typeResolver);

        var classes = ModuleDefinition.GetTypes()
            .Where(x => x.IsClass)
            .ToList();
        var assemblyProcessor = new AssemblyProcessor(freezeCheckerInjector, implementsInterfaceFinder, WriteInfo);
        assemblyProcessor.Execute(classes);

        volatileFieldFixer.Execute(classes);
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        return Enumerable.Empty<string>();
    }

    public override bool ShouldCleanReference => true;
}