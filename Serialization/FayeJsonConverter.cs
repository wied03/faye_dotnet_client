#region

using Bsw.FayeDotNet.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

#endregion

namespace Bsw.FayeDotNet.Serialization
{
    internal class FayeJsonConverter
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
                                                                  {
                                                                      ContractResolver =
                                                                          new CamelCasePropertyNamesContractResolver()
                                                                  };

        public T Deserialize<T>(string message) where T : BaseFayeMessage
        {
            var array = JsonConvert.DeserializeObject<JArray>(message);
            return array[0].ToObject<T>();
        }

        public string Serialize(BaseFayeMessage message)
        {
            return JsonConvert.SerializeObject(message,
                                               Settings);
        }
    }
}