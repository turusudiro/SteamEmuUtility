using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace SteamEmuUtility.Common.Serialization
{
    public class FilterEmptyStringsResolver : DefaultContractResolver
    {
        public static readonly FilterEmptyStringsResolver Instance = new FilterEmptyStringsResolver();
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType == typeof(string))
            {
                property.ShouldSerialize = instance =>
                {
                    var value = property.ValueProvider.GetValue(instance) as string;
                    return !string.IsNullOrEmpty(value);
                };
            }

            return property;
        }
    }
}
