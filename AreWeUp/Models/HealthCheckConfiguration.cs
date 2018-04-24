using System.Collections.Generic;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// The model for the health check configuration file that is read
    /// from S3
    /// </summary>
    public class HealthCheckConfiguration
    {
        /// <summary>
        /// The set of HTTP endpoints to check
        /// </summary>
        public IEnumerable<HttpAreWeUpRequest> Http { get; set; }

        /// <summary>
        /// The set of HTTPS endpoints to check
        /// </summary>
        public IEnumerable<HttpsAreWeUpRequest> Https { get; set; }

        /// <summary>
        /// The set of TCP endpoints to check
        /// </summary>
        public IEnumerable<TcpAreWeUpRequest> Tcp { get; set; }
    }
}
