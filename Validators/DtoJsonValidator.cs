using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace MyAzureFunctionApp.Validators
{
    public static class DtoJsonValidator
    {
        public static bool IsValidJsonStructure<T>(string json)
        {
            try
            {
                var element = JsonSerializer.Deserialize<JsonElement>(json);
                var properties = typeof(T).GetProperties();
                var expectedProperties = new HashSet<string>(properties.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
                var jsonProperties = new HashSet<string>(element.EnumerateObject().Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

                // Check if all expected properties are present and no extra properties exist
                if (!expectedProperties.SetEquals(jsonProperties))
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
