using Newtonsoft.Json;
using System.Collections.Generic;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// The model for the health check configuration file that is read
    /// from S3
    /// </summary>
    public class HealthCheckConfiguration
    {
        #region Public Properties

        /// <summary>
        /// The set of HTTP endpoints to check
        /// </summary>
        public IEnumerable<HttpAreWeUpRequest> Http { get; }

        /// <summary>
        /// The set of HTTPS endpoints to check
        /// </summary>
        public IEnumerable<HttpsAreWeUpRequest> Https { get; }

        /// <summary>
        /// The set of TCP endpoints to check
        /// </summary>
        public IEnumerable<TcpAreWeUpRequest> Tcp { get; }

        /// <summary>
        /// The set of UDP endpoints to check
        /// </summary>
        public IEnumerable<UdpAreWeUpRequest> Udp { get; }

        /// <summary>
        /// The set of ICMP endpoints to check
        /// </summary>
        public IEnumerable<IcmpAreWeUpRequest> Icmp { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new set of health checks
        /// </summary>
        /// <param name="http">The http health checks</param>
        /// <param name="https">The https health checks</param>
        /// <param name="tcp">The tcp health checks</param>
        /// <param name="udp">The udp health checks</param>
        /// <param name="icmp">The icmp health checks</param>
        [JsonConstructor]
        public HealthCheckConfiguration(
            IEnumerable<HttpAreWeUpRequest> http,
            IEnumerable<HttpsAreWeUpRequest> https,
            IEnumerable<TcpAreWeUpRequest> tcp,
            IEnumerable<UdpAreWeUpRequest> udp,
            IEnumerable<IcmpAreWeUpRequest> icmp
            )
        {
            this.Http = http ?? new List<HttpAreWeUpRequest>();
            this.Https = https ?? new List<HttpsAreWeUpRequest>();
            this.Tcp = tcp ?? new List<TcpAreWeUpRequest>();
            this.Udp = udp ?? new List<UdpAreWeUpRequest>();
            this.Icmp = icmp ?? new List<IcmpAreWeUpRequest>();
        }

        #endregion
    }
}
