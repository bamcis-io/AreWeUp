using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace BAMCIS.AreWeUp.Serde
{
    /// <summary>
    /// A custom contract resolver that allows JsonConvert serialization to use the private setters of properties
    /// </summary>
    internal class PrivateSetterResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty Prop = base.CreateProperty(member, memberSerialization);

            if (!Prop.Writable)
            {
                PropertyInfo Property = member as PropertyInfo;
                if (Property != null)
                {
                    bool HasPrivateSetter = Property.GetSetMethod(true) != null;
                    Prop.Writable = HasPrivateSetter;
                }
            }

            return Prop;
        }
    }
}
