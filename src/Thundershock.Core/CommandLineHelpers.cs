using System;
using System.Reflection;

namespace Thundershock.Core
{
    public static class CommandLineHelpers
    {
        public static object[] ParseParameterList(string[] args, MethodInfo method)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var parameterList = method.GetParameters();

            if (parameterList.Length != args.Length)
                throw new InvalidOperationException(
                    $"Argument count mismatch. Expected {parameterList.Length} parameters, instead got {args.Length} parameters");

            var objectArray = new object[parameterList.Length];

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var parameter = parameterList[i];

                var type = parameter.ParameterType;

                // we have to do enums ourselves
                var result = null as object;
                if (type.IsEnum)
                {
                    result = Enum.Parse(type, arg);
                }
                else
                {
                    result = Convert.ChangeType(arg, type);
                }

                objectArray[i] = result;
            }
            
            return objectArray;
        }
    }
}