using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace InsuranceIntegration.Api.Services.Schemas;

public sealed class JsonSchemaService : IJsonSchemaService
{
    public object GenerateSchema<T>()
    {
        var definitions = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
        var visited = new HashSet<Type>();
        var root = CreateSchema(typeof(T), definitions, visited, true);

        var document = new Dictionary<string, object?>
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = typeof(T).Name,
            ["type"] = root["type"],
            ["properties"] = root.TryGetValue("properties", out var properties) ? properties : new Dictionary<string, object>(),
            ["additionalProperties"] = false
        };

        if (root.TryGetValue("required", out var required))
        {
            document["required"] = required;
        }

        if (definitions.Count > 0)
        {
            document["$defs"] = definitions;
        }

        return document;
    }

    private Dictionary<string, object> CreateSchema(Type type, IDictionary<string, Dictionary<string, object>> definitions, ISet<Type> visited, bool isRoot = false)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType == typeof(string))
        {
            return new Dictionary<string, object> { ["type"] = "string" };
        }

        if (actualType == typeof(Guid))
        {
            return new Dictionary<string, object> { ["type"] = "string", ["format"] = "uuid" };
        }

        if (actualType == typeof(DateTime))
        {
            return new Dictionary<string, object> { ["type"] = "string", ["format"] = "date-time" };
        }

        if (actualType == typeof(DateOnly))
        {
            return new Dictionary<string, object> { ["type"] = "string", ["format"] = "date" };
        }

        if (actualType == typeof(JsonElement))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["additionalProperties"] = true
            };
        }

        if (actualType == typeof(bool))
        {
            return new Dictionary<string, object> { ["type"] = "boolean" };
        }

        if (IsInteger(actualType))
        {
            return new Dictionary<string, object> { ["type"] = "integer" };
        }

        if (IsNumber(actualType))
        {
            return new Dictionary<string, object> { ["type"] = "number" };
        }

        if (actualType.IsEnum)
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["enum"] = actualType.GetEnumNames()
            };
        }

        if (TryGetCollectionElementType(actualType, out var elementType))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = CreateSchema(elementType!, definitions, visited)
            };
        }

        if (!isRoot)
        {
            var definitionName = actualType.Name;
            if (!definitions.ContainsKey(definitionName) && !visited.Contains(actualType))
            {
                visited.Add(actualType);
                definitions[definitionName] = BuildObjectSchema(actualType, definitions, visited);
                visited.Remove(actualType);
            }

            return new Dictionary<string, object>
            {
                ["$ref"] = $"#/$defs/{definitionName}"
            };
        }

        return BuildObjectSchema(actualType, definitions, visited);
    }

    private Dictionary<string, object> BuildObjectSchema(Type type, IDictionary<string, Dictionary<string, object>> definitions, ISet<Type> visited)
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);
        var required = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod is null)
            {
                continue;
            }

            var propertyName = ToCamelCase(property.Name);
            properties[propertyName] = CreateSchema(property.PropertyType, definitions, visited);

            if (property.GetCustomAttribute<RequiredAttribute>() is not null || IsRequiredMember(property))
            {
                required.Add(propertyName);
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    private static bool IsRequiredMember(MemberInfo member)
    {
        return member.CustomAttributes.Any(attribute => attribute.AttributeType.Name == "RequiredMemberAttribute");
    }

    private static bool TryGetCollectionElementType(Type type, out Type? elementType)
    {
        elementType = null;

        if (type == typeof(string))
        {
            return false;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return elementType is not null;
        }

        var enumerableType = type
            .GetInterfaces()
            .Concat([type])
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType is null)
        {
            return false;
        }

        elementType = enumerableType.GetGenericArguments()[0];
        return true;
    }

    private static bool IsInteger(Type type)
    {
        return type == typeof(byte)
            || type == typeof(short)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(sbyte)
            || type == typeof(ushort)
            || type == typeof(uint)
            || type == typeof(ulong);
    }

    private static bool IsNumber(Type type)
    {
        return type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
