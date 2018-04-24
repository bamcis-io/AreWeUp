using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// The list of available protocols to perform health checks with
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Protocol
    {
        /// <summary>
        /// Standard HTTP 
        /// </summary>
        HTTP,

        /// <summary>
        /// HTTPS (i.e. HTTP using SSL)
        /// </summary>
        HTTPS,

        /// <summary>
        /// Standard TCP
        /// </summary>
        TCP,

        /// <summary>
        /// Standard UDP
        /// </summary>
        UDP,

        /// <summary>
        /// Standard ICMP echo for "ping"
        /// </summary>
        ICMP
    }
}
