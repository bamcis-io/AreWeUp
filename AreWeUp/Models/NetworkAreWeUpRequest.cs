namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// An abstract class for network based health checks
    /// </summary>
    public abstract class NetworkAreWeUpRequest : AreWeUpRequest
    {
        #region Public Properties

        /// <summary>
        /// The timeout to use for attempting connections, in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// For TCP or UDP checks, the port number to test against
        /// </summary>
        public int Port { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a generic network based health check using the specified port and protocol, which allows
        /// inherited classes to utilize a default constructor when their default port is associated with the specified
        /// protocol
        /// </summary>
        /// <param name="port">The port to use</param>
        /// <param name="protocol">The protocol of the health check</param>
        protected NetworkAreWeUpRequest(int port, Protocol protocol) : base(protocol)
        {
            this.Port = port;
            this.Timeout = 500;
        }

        /// <summary>
        /// Creates a generic network based health check using the specified path, port, and protocl
        /// </summary>
        /// <param name="path">The IP address, DNS host name, or URL to check</param>
        /// <param name="port">The port to use</param>
        /// <param name="protocol">The protocol of the health check</param>
        protected NetworkAreWeUpRequest(string path, int port, Protocol protocol) : base(path, protocol)
        {
            this.Port = port;
            this.Timeout = 500;
        }

        /// <summary>
        /// Creates a generic network based health check using the specified protocol, which allows
        /// inherited classes to utilize a default constructor with their specified protocol
        /// </summary>
        /// <param name="protocol">The protocol of the health check</param>
        protected NetworkAreWeUpRequest(Protocol protocol) : base(protocol)
        {
            this.Timeout = 500;
        }

        #endregion
    }
}
