using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BAMCIS.AreWeUp.Serde
{
    /// <summary>
    /// Represents a custom JsonConverter that converts a KeyValuePair&ltstring, string&gt object directly to a JSON object that enables the JSON like
    /// {"myKey" : "aValue" } to be read as a KeyValuePair and also allows a KeyValuePair&ltstring, string&gt to be written the same way
    /// </summary>
    public class KeyValueConverter : JsonConverter
    {
        #region Public Properties

        public override bool CanWrite => true;

        public override bool CanRead => true;


        #endregion

        #region Public Methods

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(KeyValuePair<string, string>));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            KeyValuePair<string, string> Temp = (KeyValuePair<string, string>)value;
            writer.WriteRawValue($"{{\"{Temp.Key}\":\"{Temp.Value}\"}}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject Obj = JObject.Load(reader);
            JProperty Prop = Obj.Properties().First();
            return new KeyValuePair<string, string>(Prop.Name, Prop.Value.ToObject<string>());
        }

        #endregion
    }
}
