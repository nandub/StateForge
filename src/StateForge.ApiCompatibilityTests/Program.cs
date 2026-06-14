using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StateForge.ApiCompatibilityTests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length != 3)
                {
                    throw new ArgumentException(
                        "Usage: StateForge.ApiCompatibilityTests <assembly-path> <baseline-path> <verify|update>");
                }

                string assemblyPath = Path.GetFullPath(args[0]);
                string baselinePath = Path.GetFullPath(args[1]);
                string mode = args[2];

                if (!File.Exists(assemblyPath))
                {
                    throw new FileNotFoundException("Assembly not found.", assemblyPath);
                }

                AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs eventArgs)
                {
                    string name = new AssemblyName(eventArgs.Name).Name + ".dll";
                    string candidate = Path.Combine(Path.GetDirectoryName(assemblyPath), name);
                    return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
                };

                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                string content = CreateBaseline(assembly);

                if (string.Equals(mode, "update", StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(baselinePath));
                    File.WriteAllText(baselinePath, content, new UTF8Encoding(false));
                    Console.WriteLine("UPDATED: {0}", baselinePath);
                    return 0;
                }

                if (!string.Equals(mode, "verify", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Mode must be verify or update.");
                }

                if (!File.Exists(baselinePath))
                {
                    throw new FileNotFoundException("API baseline not found.", baselinePath);
                }

                string expected = Normalize(File.ReadAllText(baselinePath));
                string actual = Normalize(content);
                if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    ReportDifference(expected, actual);
                    throw new InvalidOperationException(
                        "Public API changed. Review the change and run Test-StateForgeApiCompatibility.ps1 -UpdateBaseline to approve it.");
                }

                Console.WriteLine("PASS: {0}", assembly.GetName().Name);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static string CreateBaseline(Assembly assembly)
        {
            List<string> lines = new List<string>();
            lines.Add("# " + assembly.GetName().Name);

            Type[] types = assembly.GetExportedTypes()
                .OrderBy(type => GetTypeName(type), StringComparer.Ordinal)
                .ToArray();

            foreach (Type type in types)
            {
                lines.Add(string.Empty);
                lines.Add(FormatType(type));

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .OrderBy(FormatField, StringComparer.Ordinal))
                {
                    lines.Add("  " + FormatField(field));
                }

                foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .OrderBy(FormatConstructor, StringComparer.Ordinal))
                {
                    lines.Add("  " + FormatConstructor(constructor));
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .OrderBy(FormatProperty, StringComparer.Ordinal))
                {
                    lines.Add("  " + FormatProperty(property));
                }

                foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .OrderBy(FormatEvent, StringComparer.Ordinal))
                {
                    lines.Add("  " + FormatEvent(eventInfo));
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(method => !method.IsSpecialName)
                    .OrderBy(FormatMethod, StringComparer.Ordinal))
                {
                    lines.Add("  " + FormatMethod(method));
                }
            }

            return string.Join("\n", lines.ToArray()) + "\n";
        }

        private static string FormatType(Type type)
        {
            string kind;
            if (type.IsEnum)
            {
                kind = "enum";
            }
            else if (type.IsInterface)
            {
                kind = "interface";
            }
            else if (typeof(MulticastDelegate).IsAssignableFrom(type.BaseType))
            {
                kind = "delegate";
            }
            else if (type.IsValueType)
            {
                kind = "struct";
            }
            else
            {
                kind = "class";
            }

            List<string> modifiers = new List<string> { "public" };
            if (type.IsAbstract && type.IsSealed)
            {
                modifiers.Add("static");
            }
            else
            {
                if (type.IsAbstract && !type.IsInterface)
                {
                    modifiers.Add("abstract");
                }

                if (type.IsSealed && !type.IsValueType && kind != "delegate")
                {
                    modifiers.Add("sealed");
                }
            }

            modifiers.Add(kind);
            modifiers.Add(GetTypeName(type));

            List<string> inheritance = new List<string>();
            if (type.BaseType != null &&
                type.BaseType != typeof(object) &&
                type.BaseType != typeof(ValueType) &&
                type.BaseType != typeof(Enum) &&
                type.BaseType != typeof(MulticastDelegate))
            {
                inheritance.Add(GetTypeName(type.BaseType));
            }

            inheritance.AddRange(type.GetInterfaces()
                .Select(GetTypeName)
                .OrderBy(value => value, StringComparer.Ordinal));

            string line = string.Join(" ", modifiers.ToArray());
            if (inheritance.Count > 0)
            {
                line += " : " + string.Join(", ", inheritance.Distinct().ToArray());
            }

            return line + FormatGenericConstraints(type.GetGenericArguments());
        }

        private static string FormatField(FieldInfo field)
        {
            string prefix = field.IsLiteral ? "const" : (field.IsStatic ? "static field" : "field");
            string line = prefix + " " + GetTypeName(field.FieldType) + " " + field.Name;
            if (field.IsLiteral)
            {
                line += " = " + FormatConstant(field.GetRawConstantValue());
            }

            return line;
        }

        private static string FormatConstructor(ConstructorInfo constructor)
        {
            return "constructor " + constructor.DeclaringType.Name.Split('`')[0] +
                "(" + FormatParameters(constructor.GetParameters()) + ")";
        }

        private static string FormatProperty(PropertyInfo property)
        {
            MethodInfo accessor = property.GetGetMethod() ?? property.GetSetMethod();
            string prefix = accessor != null && accessor.IsStatic ? "static property" : "property";
            string accessors = string.Empty;
            if (property.GetGetMethod() != null)
            {
                accessors += "get;";
            }

            if (property.GetSetMethod() != null)
            {
                accessors += "set;";
            }

            ParameterInfo[] indexParameters = property.GetIndexParameters();
            string name = indexParameters.Length == 0
                ? property.Name
                : property.Name + "[" + FormatParameters(indexParameters) + "]";
            return prefix + " " + GetTypeName(property.PropertyType) + " " + name + " { " + accessors + " }";
        }

        private static string FormatEvent(EventInfo eventInfo)
        {
            MethodInfo accessor = eventInfo.GetAddMethod();
            string prefix = accessor != null && accessor.IsStatic ? "static event" : "event";
            return prefix + " " + GetTypeName(eventInfo.EventHandlerType) + " " + eventInfo.Name;
        }

        private static string FormatMethod(MethodInfo method)
        {
            List<string> modifiers = new List<string>();
            if (method.IsStatic)
            {
                modifiers.Add("static");
            }

            if (method.IsAbstract)
            {
                modifiers.Add("abstract");
            }
            else if (method.IsVirtual && method.GetBaseDefinition() == method && !method.IsFinal)
            {
                modifiers.Add("virtual");
            }

            modifiers.Add("method");
            modifiers.Add(GetTypeName(method.ReturnType));

            string name = method.Name;
            if (method.IsGenericMethodDefinition)
            {
                name += "<" + string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name).ToArray()) + ">";
            }

            string line = string.Join(" ", modifiers.ToArray()) + " " + name +
                "(" + FormatParameters(method.GetParameters()) + ")";
            return line + FormatGenericConstraints(method.GetGenericArguments());
        }

        private static string FormatParameters(ParameterInfo[] parameters)
        {
            return string.Join(", ", parameters.Select(parameter =>
            {
                Type parameterType = parameter.ParameterType;
                string modifier = string.Empty;
                if (parameterType.IsByRef)
                {
                    modifier = parameter.IsOut ? "out " : "ref ";
                    parameterType = parameterType.GetElementType();
                }

                return modifier + GetTypeName(parameterType) + " " + parameter.Name;
            }).ToArray());
        }

        private static string FormatGenericConstraints(Type[] arguments)
        {
            List<string> constraints = new List<string>();
            foreach (Type argument in arguments.Where(argument => argument.IsGenericParameter))
            {
                List<string> values = new List<string>();
                GenericParameterAttributes attributes = argument.GenericParameterAttributes;
                if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                {
                    values.Add("class");
                }

                if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                {
                    values.Add("struct");
                }

                values.AddRange(argument.GetGenericParameterConstraints().Select(GetTypeName));
                if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                    (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                {
                    values.Add("new()");
                }

                if (values.Count > 0)
                {
                    constraints.Add(" where " + argument.Name + " : " + string.Join(", ", values.ToArray()));
                }
            }

            return string.Concat(constraints.ToArray());
        }

        private static string GetTypeName(Type type)
        {
            if (type.IsByRef)
            {
                return GetTypeName(type.GetElementType()) + "&";
            }

            if (type.IsArray)
            {
                return GetTypeName(type.GetElementType()) + "[]";
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (type.IsGenericType)
            {
                string name = type.GetGenericTypeDefinition().FullName;
                name = name.Substring(0, name.IndexOf('`')).Replace('+', '.');
                return name + "<" + string.Join(", ", type.GetGenericArguments().Select(GetTypeName).ToArray()) + ">";
            }

            return (type.FullName ?? type.Name).Replace('+', '.');
        }

        private static string FormatConstant(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string)
            {
                return "\"" + ((string)value).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            }

            if (value is char)
            {
                return "'" + value.ToString().Replace("'", "\\'") + "'";
            }

            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string Normalize(string value)
        {
            return value.Replace("\r\n", "\n").TrimEnd() + "\n";
        }

        private static void ReportDifference(string expected, string actual)
        {
            string[] expectedLines = expected.Split('\n');
            string[] actualLines = actual.Split('\n');
            int maximum = Math.Max(expectedLines.Length, actualLines.Length);
            for (int index = 0; index < maximum; index++)
            {
                string expectedLine = index < expectedLines.Length ? expectedLines[index] : "<missing>";
                string actualLine = index < actualLines.Length ? actualLines[index] : "<missing>";
                if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine("First difference at line {0}.", index + 1);
                    Console.Error.WriteLine("Expected: {0}", expectedLine);
                    Console.Error.WriteLine("Actual:   {0}", actualLine);
                    return;
                }
            }
        }
    }
}
