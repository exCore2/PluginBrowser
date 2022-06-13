using System.Text.Json;
using System.Text.Json.Serialization;
using Reactive.Bindings;

namespace PluginBrowser.Utils;

public class ReactivePropertyConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ReactiveProperty<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var actualType = typeToConvert.GenericTypeArguments[0];
        var converter = Activator.CreateInstance(typeof(ReactivePropertyConverter<>).MakeGenericType(actualType));
        return (JsonConverter?)converter;
    }
}
