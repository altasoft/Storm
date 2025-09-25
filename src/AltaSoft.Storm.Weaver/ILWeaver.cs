using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AltaSoft.Storm.Weaver;

/// <summary>
/// ILWeaver class that intercepts property setters in an assembly and modifies them.
/// </summary>
// ReSharper disable once InconsistentNaming
internal static class ILWeaver
{
    private static bool s_cancelled;

    internal static void InterceptPropertySetters(string targetAssembly, string targetFramework, string targetDir, TaskLoggingHelper logger, IEnumerable<string> references)
    {
        var tmpAssemblyPath = Path.ChangeExtension(targetAssembly, "tmp");

        var haveSymbols = File.Exists(Path.ChangeExtension(targetAssembly, ".pdb"));

        var readerParams = new ReaderParameters
        {
            ReadSymbols = haveSymbols,
            ReadWrite = true,
            ReadingMode = ReadingMode.Immediate,
            InMemory = true,
            AssemblyResolver = new CustomAssemblyResolver(targetAssembly, targetFramework, targetDir, logger, references)
        };

        var writerParameters = new WriterParameters
        {
            WriteSymbols = haveSymbols
        };

        using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(targetAssembly, readerParams))
        {
            foreach (var type in assemblyDefinition.MainModule.Types)
            {
                if (s_cancelled)
                {
                    logger.LogMessage(MessageImportance.Low, "AltaSoft.Storm: Cancelled");
                    break;
                }

                logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Processing type '{type.FullName}'.");

                if (!RequiresChangeTracking(type))
                {
                    logger.LogMessage(MessageImportance.Low, $"AltaSoft.Storm: Type '{type.FullName}' skipped.");
                    continue;
                }

                foreach (var property in type.Properties)
                {
                    if (property.DeclaringType != type)
                        continue; // Skip if the property. Is inherited

                    var setMethod = property.SetMethod;
                    if (setMethod is null)
                        continue; // Skip this property. It doesn't have a setter

                    var getMethod = property.GetMethod;
                    if (getMethod is null)
                        continue; // Skip this property. It doesn't have a getter

                    // Check if the property has setter and getter methods
                    if (!getMethod.IsSpecialName || !setMethod.IsSpecialName || !getMethod.HasBody || !getMethod.HasBody)
                    {
                        continue; // Skip this property. It doesn't have a setter or getter
                    }

                    // Check if the property has the StormColumnAttribute
                    var stormColumnAttribute = property.CustomAttributes.FirstOrDefault(
                        attr => attr.AttributeType is { Name: Constants.StormColumnAttributeName, Namespace: Constants.StormDbObjectAttributeNamespace });

                    // Check the SaveAs property of the attribute
                    var saveAsProperty = stormColumnAttribute?.Properties.FirstOrDefault(prop => prop.Name == Constants.StormColumnAttributeSaveAs).Argument.Value;
                    if (saveAsProperty is 99) // (int)SaveAs.Ignore
                    {
                        continue; // Skip this property. SaveAs = Ignore
                    }

                    //// Check if the field _isChangeTrackingActive exists
                    //var isChangeTrackingActiveField = GetFieldDefinition(type, Constants.StormIsChangeTrackingActiveFieldName);
                    //if (isChangeTrackingActiveField is null)
                    //    continue; // Skip if there is no _isChangeTrackingActive field

                    // Find the backing field
                    var backingField = type.Fields.FirstOrDefault(f =>
                        getMethod.Body.Instructions.Any(i => i.OpCode.Code == Code.Ldfld && i.Operand == f) ||
                        setMethod.Body.Instructions.Any(i => i.OpCode.Code == Code.Stfld && i.Operand == f));
                    if (backingField is null)
                        continue; // Skip if there is no backing field

                    var localMethod = type.Methods.FirstOrDefault(m => m.Name == $"__PropertySet_{property.Name}");
                    if (localMethod is null)
                        continue; // Skip if there is no __PropertySet_xxx method

                    var originalSetterMethod = CreateSetterMethod(property, setMethod);
                    type.Methods.Add(originalSetterMethod); // Add the new method to the type

                    setMethod.Body.Instructions.Clear(); // Clear the existing instructions
                    var ilProcessor = setMethod.Body.GetILProcessor();

                    // Create a variable to store the original value of the field
                    var originalValueVar = new VariableDefinition(backingField.FieldType);
                    setMethod.Body.Variables.Add(originalValueVar);

                    // Define a new local variable to hold the reference to originalValueVar
                    var originalValueVarRef = new VariableDefinition(new ByReferenceType(backingField.FieldType));
                    setMethod.Body.Variables.Add(originalValueVarRef);

                    // Store the address of originalValueVar in originalValueVarRef
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloca, originalValueVar)); // Load address of originalValueVar
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, originalValueVarRef)); // Store address in originalValueVarRef

                    // Load 'this' and then load the field value
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0)); // Load 'this'
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldfld, backingField)); // Load field value
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Stloc, originalValueVar)); // Store in local variable

                    // Load the current value of the field onto the evaluation stack
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0)); // Load 'this'
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_1)); // Load value
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Call, originalSetterMethod)); // Call original setter method

                    // Load 'this', then load the address of the field, and then load the address stored in originalValueVarRef
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0)); // Load 'this'
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldarg_0)); // Load 'this'
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldflda, backingField)); // Load the new current value of the field
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ldloc, originalValueVarRef)); // Load old value
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Call, localMethod)); // Call __PropertySet_Test
                    ilProcessor.Append(ilProcessor.Create(OpCodes.Ret)); // return
                }

                logger.LogMessage(MessageImportance.High, $"AltaSoft.Storm: '{type.FullName}' setter rewritten");
            }

            assemblyDefinition.Write(tmpAssemblyPath, writerParameters);
        }

        File.Delete(targetAssembly);
        File.Move(tmpAssemblyPath, targetAssembly);
    }

    //private static FieldDefinition? GetFieldDefinition(TypeDefinition type, string fieldName)
    //{
    //    var field = type.Fields.FirstOrDefault(f => f.Name == fieldName);
    //    if (field is not null)
    //        return field;

    //    while (true)
    //    {
    //        var baseType = type.BaseType.Resolve();
    //        if (baseType == type)
    //            break;
    //        type = baseType;

    //        field = type.Fields.FirstOrDefault(f => f.Name == fieldName);
    //        if (field is not null)
    //            return field;
    //    }
    //    return null;
    //}

    private static MethodDefinition CreateSetterMethod(MemberReference property, MethodDefinition setMethod)
    {
        // Create a new method definition
        var newMethod = new MethodDefinition($"__OriginalSet_{property.Name}",
            MethodAttributes.Private | MethodAttributes.HideBySig,
            setMethod.Module.ImportReference(typeof(void)));  // Assuming the setter does not return a value

        // The setter has one parameter, import the parameter type
        newMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, setMethod.Module.ImportReference(setMethod.Parameters[0].ParameterType)));

        // Copy the instructions and variables from the original setter to the new method
        newMethod.Body = new MethodBody(newMethod);

        foreach (var variable in setMethod.Body.Variables)
            newMethod.Body.Variables.Add(new VariableDefinition(setMethod.Module.ImportReference(variable.VariableType)));  // Import variable types

        foreach (var instruction in setMethod.Body.Instructions)
            newMethod.Body.Instructions.Add(instruction);

        return newMethod;
    }

    private static bool RequiresChangeTracking(ICustomAttributeProvider type)
    {
        if (type.CustomAttributes.Any(attr =>
            attr.AttributeType is { Name: Constants.StormTrackableObjectAttributeName, Namespace: Constants.StormDbObjectAttributeNamespace }))
        {
            return true;
        }

        var updateMode = int.MaxValue;

        foreach (var attr in type.CustomAttributes
            .Where(attr => attr.AttributeType is { Name: Constants.StormDbObjectAttributeName, Namespace: Constants.StormDbObjectAttributeNamespace }))
        {
            // Check the UpdateMode property of the attribute
            var updateModeProperty = attr.Properties.FirstOrDefault(prop => prop.Name == Constants.StormDbObjectAttributeUpdateModePropertyName).Argument.Value;

            var updateModeValue = updateModeProperty as int? ?? Constants.StormDbObjectAttributeUpdateModeChangeTrackingValue;
            if (updateModeValue < updateMode)
                updateMode = updateModeValue;
        }

        return updateMode == Constants.StormDbObjectAttributeUpdateModeChangeTrackingValue;
    }

    public static void Cancel() => s_cancelled = true;
}
