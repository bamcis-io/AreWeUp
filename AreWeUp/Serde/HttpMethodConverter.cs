using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace BAMCIS.AreWeUp.Serde
{
    /// <summary>
    /// Provides a custom converter for HttpMethod class objects to translate them from a single string like GET or POST and read them
    /// from JSON the same way
    /// </summary>
    public class HttpMethodConverter : JsonConverter
    {
        #region Public Properties

        public override bool CanRead => true;

        public override bool CanWrite => true;

        #endregion

        #region Public Methods

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(HttpMethod));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(((HttpMethod)value).Method);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.ValueType == typeof(string))
            {
                return new HttpMethod(reader.Value.ToString());
            }
            else
            {
                JObject Obj = JObject.Load(reader);
                return new HttpMethod(Obj["method"].ToString());
            }
        }

        #endregion
    }
}
