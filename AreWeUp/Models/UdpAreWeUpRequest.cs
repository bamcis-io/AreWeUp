using System;

namespace BAMCIS.AreWeUp.Models
{
    /// <summary>
    /// Represents a UDP health check request. These health checks are subject to false negative and false positive results
    /// since UDP is a connectionless and stateless protocol
    /// </summary>
    public class UdpAreWeUpRequest : NetworkAreWeUpRequest
    {
        #region Private Fields

        private UInt32 _ReceiveBufferSize;

        #endregion

        #region Public Properties

        /// <summary>
        /// A base64 encoded payload that will be converted to a byte array to be sent in the 
        /// request to the destination
        /// </summary>
        public string Payload { get; private set; }

        /// <summary>
        /// The size in bytes of the receive buffer to use. This defaults to 512 bytes.
        /// </summary>
        public UInt32 ReceiveBufferSize { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a generic UDP health check
        /// </summary>
        public UdpAreWeUpRequest() : base(Protocol.UDP) { }

        /// <summary>
        /// Creates a UDP health check using the specified path and port
        /// </summary>
        /// <param name="path">The IP address or DNS host name to check</param>
        /// <param name="port">The port to use</param>
        public UdpAreWeUpRequest(string path, int port) : base(path, port, Protocol.UDP) { }

        /// <summary>
        /// Creates a UDP health check using the specified path and port as well as the specified payload to deliver in the check
        /// </summary>
        /// <param name="path">The IP address or DNS host name to check</param>
        /// <param name="port">The port to use</param>
        /// <param name="payload">The payload to include in the sent packets. This should be supplied as a base64 encoded string.</param>
        public UdpAreWeUpRequest(string path, int port, string payload) : base(path, port, Protocol.UDP)
        {
            if (String.IsNullOrEmpty(payload))
            {
                throw new ArgumentNullException("payload", "The base64 encoded payload cannot be null or empty.");
            }

            this.Payload = payload;
            this.ReceiveBufferSize = 512;
        }

        #endregion
    }
}
