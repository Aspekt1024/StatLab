using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StatLab.Core.Stats;

namespace StatLab.Data.Json;

// Reads the fields of one JSON object, adding a LoadError for anything missing, of the wrong type or unknown,
// so a loader can keep going and report every problem in one pass. Call ReportUnknownFields once all fields are read.
internal sealed class JsonFields
{
    private readonly JObject _json;
    private readonly string _file;
    private readonly List<LoadError> _errors;
    private readonly HashSet<string> _read = [];

    public JsonFields(JObject json, string file, List<LoadError> errors)
    {
        _json = json;
        _file = file;
        _errors = errors;
    }

    // Parses a whole file, adding an error with its line if it isn't a valid JSON object.
    public static JObject? Parse(string file, string text, List<LoadError> errors)
    {
        try
        {
            return JObject.Parse(text);
        }
        catch (JsonReaderException exception)
        {
            errors.Add(new LoadError(file, $"line {exception.LineNumber}", $"Not valid JSON: {exception.Message}"));
            return null;
        }
    }

    public static string Location(JToken token)
    {
        var path = token.Path.Length > 0 ? token.Path : "(root)";
        return token is IJsonLineInfo info && info.HasLineInfo() ? $"{path} (line {info.LineNumber})" : path;
    }

    public void Error(JToken token, string message)
    {
        _errors.Add(new LoadError(_file, Location(token), message));
    }

    public void Error(string message)
    {
        Error(_json, message);
    }

    // An error located at one of this object's fields, or at the object if the field is absent.
    public void FieldError(string name, string message)
    {
        Error(_json[name] ?? _json, message);
    }

    // Marks a field as known without reading it (e.g. "$schema").
    public void Allow(string name)
    {
        _read.Add(name);
    }

    public double? Number(string name)
    {
        JToken? token = Field(name, required: true);
        return token == null ? null : AsNumber(token);
    }

    public double? OptionalNumber(string name)
    {
        JToken? token = Field(name, required: false);
        return token == null ? null : AsNumber(token);
    }

    public string? Text(string name)
    {
        JToken? token = Field(name, required: true);
        if (token == null)
        {
            return null;
        }

        if (token.Type != JTokenType.String)
        {
            Error(token, $"'{name}' must be a string.");
            return null;
        }

        return token.Value<string>();
    }

    // A stat by its exact enum name, e.g. "MaxHealth". Case and numbers aren't accepted.
    public StatId? Stat(string name)
    {
        var text = Text(name);
        if (text == null)
        {
            return null;
        }

        if (!Enum.TryParse(text, ignoreCase: false, out StatId stat) || stat.ToString() != text)
        {
            Error(_json[name]!, $"Unknown stat '{text}'.");
            return null;
        }

        return stat;
    }

    public JsonFields? Object(string name)
    {
        JToken? token = Field(name, required: true);
        if (token == null)
        {
            return null;
        }

        if (token is not JObject json)
        {
            Error(token, $"'{name}' must be an object.");
            return null;
        }

        return new JsonFields(json, _file, _errors);
    }

    // Each element of an array of objects. A missing optional array is empty.
    public List<JsonFields> Objects(string name, bool required)
    {
        List<JsonFields> items = [];
        JToken? token = Field(name, required);
        if (token == null)
        {
            return items;
        }

        if (token is not JArray array)
        {
            Error(token, $"'{name}' must be an array.");
            return items;
        }

        foreach (JToken element in array)
        {
            if (element is JObject json)
            {
                items.Add(new JsonFields(json, _file, _errors));
            }
            else
            {
                Error(element, "Each entry must be an object.");
            }
        }

        return items;
    }

    public void ReportUnknownFields()
    {
        foreach (JProperty property in _json.Properties())
        {
            if (!_read.Contains(property.Name))
            {
                Error(property, $"Unknown field '{property.Name}'.");
            }
        }
    }

    private JToken? Field(string name, bool required)
    {
        _read.Add(name);
        JToken? token = _json[name];
        if (token == null && required)
        {
            Error($"Missing field '{name}'.");
        }

        return token;
    }

    private double? AsNumber(JToken token)
    {
        if (token.Type is not (JTokenType.Integer or JTokenType.Float))
        {
            Error(token, "Must be a number.");
            return null;
        }

        return token.Value<double>();
    }
}
