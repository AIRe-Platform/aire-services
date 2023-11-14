using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aire.Helpers
{
    public static class JsonExtensions
    {
        public static JsonSerializerSettings Settings { get; set; } = new()
        {
			Error = delegate (object sender, ErrorEventArgs args) {
				args.ErrorContext.Handled = true;
			}
		};

        public static T JsonToObject<T>(this string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString, Settings);
        }

        public static string ObjectToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, Settings);
        }
    }
}