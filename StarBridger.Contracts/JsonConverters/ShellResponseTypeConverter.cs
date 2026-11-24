using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarBridger.Contracts.Enums;
using StarBridger.Contracts.IPC.Shell;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarBridger.Contracts.JsonConverters;

internal class ShellResponseTypeConverter : JsonConverter
{
  public override bool CanConvert(Type objectType)
  {
    return typeof(ShellResponse).IsAssignableFrom(objectType);
  }

  public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
  {
    if (reader.TokenType == JsonToken.Null)
    {
      return null;
    }

    JObject json = JObject.Load(reader);

    string type = json.Value<string>("type") ?? throw new JsonSerializationException("Missing ShellResponse type.");

    if (!Enum.TryParse(type, true, out ShellResponseType responseType))
    {
      throw new JsonSerializationException($"Unknown ShellResponse type: {type}");
    }

    return responseType switch
    {
      ShellResponseType.Output => new OutputShellResponse
      {
        Message = json.Value<string>("message") ?? throw new JsonSerializationException("Missing output message.")
      },

      ShellResponseType.Completed => new CompletedShellResponse
      {
        Success = json.Value<bool?>("success") ?? throw new JsonSerializationException("Missing completion status."),
        ErrorMessage = json.Value<string>("error") ?? string.Empty
      },

      ShellResponseType.Data => new DataShellResponse
      {
        Payload =  json["payload"]?.DeepClone() ?? throw new JsonSerializationException("Missing data payload.")
      },

      _ => throw new JsonSerializationException($"Unsupported ShellResponse type: {type}")
    };
  }

  public override void WriteJson(
   JsonWriter writer,
   object? value,
   JsonSerializer serializer)
  {
    JObject json = value switch
    {
      OutputShellResponse output => new JObject
      {
        ["type"] = "output",
        ["message"] = output.Message
      },

      CompletedShellResponse completed => new JObject
      {
        ["type"] = "completed",
        ["success"] = completed.Success,
        ["error"] = completed.ErrorMessage
      },

      DataShellResponse data => new JObject
      {
        ["type"] = "data",
        ["payload"] = data.Payload != null ? JToken.FromObject(data.Payload, serializer) : null
      },

      _ => throw new JsonSerializationException(
        $"Unknown ShellResponse type: {value?.GetType().Name}")
    };

    json.WriteTo(writer);
  }
}