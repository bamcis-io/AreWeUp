using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BAMCIS.AreWeUp.Models;
using BAMCIS.AreWeUp.Serde;
using BAMCIS.AWSLambda.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BAMCIS.AreWeUp
{
    /// <summary>
    /// The client performs HTTP/S, TCP, UDP, and ICMP health checks as specified by the configuration file.
    /// </summary>
    public class AreWeUpClient
    {
        #region Private Fields

        /// <summary>
        /// The cookie container all of the handlers will use so they all have access to the same cookies
        /// </summary>
        private CookieContainer _Cookies = new CookieContainer();

        /// <summary>
        /// A normal handler that provides a log when SSL errors occur
        /// </summary>
        private HttpClientHandler _NormalHandler;

        /// <summary>
        /// A handler that provides a log when SSL errors occur and then ignores those errors
        /// </summary>
        private HttpClientHandler _IgnoreSslErrorHandler;

        /// <summary>
        /// A handler that provides a log when SSL errors occur, ignores those errors, and does not automatically follow redirects
        /// </summary>
        private HttpClientHandler _IgnoreSslErrorAndNoRedirectHandler;

        /// <summary>
        /// A handler that provides a log when SSL errors occurs and does not automatically follow redirects
        /// </summary>
        private HttpClientHandler _NoRedirectHandler;

        /// <summary>
        /// The standard HTTP client using the NormalHandler
        /// </summary>
        private HttpClient _NormalClient;

        /// <summary>
        /// An HTTP client that ignores SSL errors
        /// </summary>
        private HttpClient _IgnoreSslErrorClient;

        /// <summary>
        /// An HTTP client that ignores SSL errors and does not follow redirects
        /// </summary>
        private HttpClient _IgnoreSslErrorAndNoRedirectClient;

        /// <summary>
        /// An HTTP client that does not follow redirects
        /// </summary>
        private HttpClient _NoRedirectClient;

        /// <summary>
        /// SocketErrors for UDP tests that may indicate success
        /// </summary>
        private static IReadOnlyCollection<SocketError> _OkSocketErrors = new List<SocketError>()
        {
            SocketError.Success,
            SocketError.TimedOut,
            SocketError.AccessDenied,
            SocketError.IsConnected,
            SocketError.IOPending,
            SocketError.ConnectionReset
        };

        /// <summary>
        /// SocketErrors for UDP tests that indicate the server is accessible, but the UDP port is not listening
        /// </summary>
        private static IReadOnlyCollection<SocketError> _WarningSocketErrors = new List<SocketError>()
        {
            SocketError.ConnectionRefused,
            SocketError.AccessDenied
        };

        /// <summary>
        /// The SNS client config
        /// </summary>
        private static AmazonSimpleNotificationServiceConfig SNSConfig = 
            new AmazonSimpleNotificationServiceConfig();

        /// <summary>
        /// The SNS client used to send SNS messages
        /// </summary>
        private static AmazonSimpleNotificationServiceClient _SNSClient = new AmazonSimpleNotificationServiceClient(SNSConfig);

        /// <summary>
        /// The S3 client configuration
        /// </summary>
        private static AmazonS3Config _S3Config = new AmazonS3Config();

        /// <summary>
        /// The S3 Client to use to download the URLs
        /// </summary>
        private static IAmazonS3 _S3Client = new AmazonS3Client(_S3Config);

        /// <summary>
        /// The transfer utility to transfer the config file
        /// </summary>
        private static TransferUtility _XFerUtil = new TransferUtility(_S3Client);

        /// <summary>
        /// The Lambda context so all methods can write to the log
        /// </summary>
        private ILambdaContext _LambdaContext;

        /// <summary>
        /// The config to use to perform health checks
        /// </summary>
        private HealthCheckConfiguration _HealthCheckConfig;

        #endregion

        #region Public Properties

        /// <summary>
        /// The client configuration
        /// </summary>
        public AreWeUpClientConfig Configuration { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the client with the specified config
        /// </summary>
        /// <param name="config">The client configuration options</param>
        public AreWeUpClient(AreWeUpClientConfig config)
        {
            this.Configuration = config;
            this.InitializeHttpClients();

            ReadConfig(this.Configuration).Wait();
        }

        /// <summary>
        /// Initializes the client with the specified config and the ILambdaContext that will be used for logging
        /// </summary>
        /// <param name="config">The client configuration options</param>
        /// <param name="context">The ILambdaContext that will be used for logging</param>
        public AreWeUpClient(AreWeUpClientConfig config, ILambdaContext context) : this(config)
        {
            this._LambdaContext = context;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates the Client based on the config and reads the health check settings from S3
        /// </summary>
        /// <param name="config">The client configuration options</param>
        /// <returns>A new AreWeUpClient with the specified configuration</returns>
        public static async Task<AreWeUpClient> CreateClient(AreWeUpClientConfig config)
        {
            return new AreWeUpClient(config)
            {
                _HealthCheckConfig = await ReadConfig(config)
            };
        }

        /// <summary>
        /// Sets the health check configuration the client will use
        /// </summary>
        /// <param name="config">The health check configuration</param>
        public void SetHealthCheckConfiguration(HealthCheckConfiguration config)
        {
            this._HealthCheckConfig = config ?? throw new ArgumentNullException("config", "The health check configuration cannot be null.");
        }
        
        /// <summary>
        /// Sets the ILambdaContext to use for logging
        /// </summary>
        /// <param name="context">The ILambdaContext to use for logging</param>
        public void SetLambdaContext(ILambdaContext context)
        {
            if (context != null)
            {
                this._LambdaContext = context;
            }
            else
            {
                throw new ArgumentNullException("context", "The lambda context cannot be null.");
            }
        }

        /// <summary>
        /// Executes all of the checks specified in the S3 config file in the AreWeUpClientConfig. The method
        /// starts by reading the configuration file from S3 specified in the AreWeUpClientConfig and then performs 
        /// each test asynchronously and waits for all of the checks to finish.
        /// </summary>
        /// <returns>A task that can be awaited for completion of all the checks</returns>
        public async Task Execute()
        {
            try
            {
                if (this.Configuration.ForceRefresh)
                {
                    this._LambdaContext.LogInfo("Forcing refresh of health check config.");
                    await ReadConfig(this.Configuration);
                }

                await this.Execute(this._HealthCheckConfig);
            }
            catch (AggregateException e)
            {
                this._LambdaContext.LogError("Problem executing the health checks.", e);
            }
            catch (Exception e)
            {
                this._LambdaContext.LogError("Problem executing the health checks.", e);
            }
        }

        /// <summary>
        /// Executes all of the checks specified in the request parameter. The method
        /// performs each test asynchronously and waits for all of the checks to finish.
        /// </summary>
        /// <param name="request">The health check configuration to execute</param>
        /// <returns>A task that can be awaited for completion of all the checks</returns>
        public async Task Execute(HealthCheckConfiguration request)
        {
            if (request != null)
            {
                try
                {
                    this._LambdaContext.LogInfo($"CONFIG:\r\n{JsonConvert.SerializeObject(request, new HttpMethodConverter())}");

                    List<Task> Tasks = new List<Task>();

                    if (request.Http != null && request.Http.Any())
                    {
                        Tasks.AddRange(request.Http.Select(ExecuteHttpTest));
                    }

                    if (request.Https != null && request.Https.Any())
                    {
                        Tasks.AddRange(request.Https.Select(ExecuteHttpTest));
                    }

                    if (request.Tcp != null && request.Tcp.Any())
                    {
                        Tasks.AddRange(request.Tcp.Select(ExecuteTcpTest));
                    }

                    if (request.Udp != null && request.Udp.Any())
                    {
                        Tasks.AddRange(request.Udp.Select(ExecuteUdpTest));
                    }

                    if (request.Icmp != null && request.Icmp.Any())
                    {
                        Tasks.AddRange(request.Icmp.Select(ExecuteIcmpTest));
                    }

                    await Task.WhenAll(Tasks.ToArray());
                }
                catch (AggregateException e)
                {
                    this._LambdaContext.LogError("Problem executing the health checks.", e);
                }
                catch (Exception e)
                {
                    this._LambdaContext.LogError("Problem executing the health checks.", e);
                }
            }
            else
            {
                throw new ArgumentNullException("request", "The health check configuration object cannot be null.");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes all 4 HTTP handlers and clients
        /// </summary>
        private void InitializeHttpClients()
        {
            this._NormalHandler = new HttpClientHandler()
            {
                CookieContainer = _Cookies,
                ServerCertificateCustomValidationCallback = (request, cert, chain, sslPolicyErrors) =>
                {
                    //If there is an error with the SSL cert, log it, but let the request continue
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        this._LambdaContext.LogWarning($"The certificate {JsonConvert.SerializeObject(cert)} could not be validated: {sslPolicyErrors.ToString()}.");
                    }

                    return sslPolicyErrors == SslPolicyErrors.None;
                }
            };

            this._IgnoreSslErrorHandler = new HttpClientHandler()
            {
                CookieContainer = _Cookies,
                ServerCertificateCustomValidationCallback = (request, cert, chain, sslPolicyErrors) =>
                {
                    //If there is an error with the SSL cert, log it, but let the request continue
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        this._LambdaContext.LogWarning($"The certificate {JsonConvert.SerializeObject(cert)} could not be validated: {sslPolicyErrors.ToString()}.");
                    }

                    return true;
                }
            };

            this._IgnoreSslErrorAndNoRedirectHandler = new HttpClientHandler()
            {
                CookieContainer = _Cookies,
                ServerCertificateCustomValidationCallback = (request, cert, chain, sslPolicyErrors) =>
                {
                    //If there is an error with the SSL cert, log it, but let the request continue
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        this._LambdaContext.LogWarning($"The certificate {JsonConvert.SerializeObject(cert)} could not be validated: {sslPolicyErrors.ToString()}.");
                    }

                    return true;
                },
                AllowAutoRedirect = false
            };

            this._NoRedirectHandler = new HttpClientHandler()
            {
                CookieContainer = _Cookies,
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = (request, cert, chain, sslPolicyErrors) =>
                {
                    //If there is an error with the SSL cert, log it, but let the request continue
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        this._LambdaContext.LogWarning($"The certificate {JsonConvert.SerializeObject(cert)} could not be validated: {sslPolicyErrors.ToString()}.");
                    }

                    return sslPolicyErrors == SslPolicyErrors.None;
                }
            };

            this._NormalClient = new HttpClient(_NormalHandler);
            this._IgnoreSslErrorClient = new HttpClient(_IgnoreSslErrorHandler);
            this._IgnoreSslErrorAndNoRedirectClient = new HttpClient(_IgnoreSslErrorAndNoRedirectHandler);
            this._NoRedirectClient = new HttpClient(_NoRedirectHandler);
        }

        /// <summary>
        /// Reads the config file from S3
        /// </summary>
        /// <returns>The health check request configuration</returns>
        private static async Task<HealthCheckConfiguration> ReadConfig(AreWeUpClientConfig defaultConfig)
        {
            try
            {
                //Stream the content of the S3 object
                using (Stream Result = await _XFerUtil.OpenStreamAsync(new TransferUtilityOpenStreamRequest() { BucketName = defaultConfig.S3Bucket, Key = defaultConfig.S3Key }))
                {
                    //Read the stream
                    using (StreamReader Reader = new StreamReader(Result))
                    {
                        JObject JO = JObject.Parse(await Reader.ReadToEndAsync());
                        JObject NewConfig = new JObject();

                        // Iterates the keys like Http, Https, Tcp, Udp, Icmp
                        foreach (KeyValuePair<string, JToken> Item in JO)
                        {
                            JArray ItemArray = new JArray();

                            // Iterates each config in that category
                            foreach (JObject Config in ((JArray)JO[Item.Key]).Children<JObject>())
                            {
                                // Add in the default values if they weren't defined.
                                JToken CId = Config.GetValue("CustomerId", StringComparison.OrdinalIgnoreCase);

                                if (CId == null)
                                {
                                    if (!String.IsNullOrEmpty(defaultConfig.DefaultCustomerId))
                                    {
                                        Config.Add("CustomerId", defaultConfig.DefaultCustomerId);
                                    }
                                }

                                JToken CW = Config.GetValue("SendToCloudWatch", StringComparison.OrdinalIgnoreCase);

                                if (CW == null)
                                {
                                    Config.Add("SendToCloudWatch", defaultConfig.DefaultSendToCloudWatch);
                                }

                                if (Item.Key == "Https")
                                {
                                    JToken SSL = Config.GetValue("IgnoreSslErrors", StringComparison.OrdinalIgnoreCase);

                                    if (SSL == null)
                                    {
                                        Config.Add("IgnoreSslErrors", defaultConfig.DefaultIgnoreSslErrors);
                                    }
                                }

                                JToken Timeout = Config.GetValue("Timeout", StringComparison.OrdinalIgnoreCase);

                                if (Timeout == null)
                                {
                                    Config.Add("Timeout", defaultConfig.DefaultTimeout);
                                }

                                JToken SNS = Config.GetValue("SNSTopicARN", StringComparison.OrdinalIgnoreCase);

                                if (SNS == null)
                                {
                                    Config.Add("SNSTopicArn", defaultConfig.SNSTopicArn);
                                }

                                JToken Subject = Config.GetValue("Subject", StringComparison.OrdinalIgnoreCase);

                                if (Subject == null)
                                {
                                    if (!String.IsNullOrEmpty(defaultConfig.DefaultSubject))
                                    {
                                        Config.Add("Subject", defaultConfig.DefaultSubject);
                                    }
                                }

                                ItemArray.Add(Config);
                            }

                            NewConfig.Add(Item.Key, ItemArray);
                        }

                        JsonSerializer Serializer = new JsonSerializer();

                        // Be able to convert the string method value to an HttpMethod class object
                        Serializer.Converters.Add(new HttpMethodConverter());
                        Serializer.Converters.Add(new KeyValueConverter());

                        // Be able to set properties that have a private setter
                        Serializer.ContractResolver = new PrivateSetterResolver();

                        return NewConfig.ToObject<HealthCheckConfiguration>(Serializer);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error processing the S3 object { defaultConfig.S3Bucket}\\{ defaultConfig.S3Key}:\r\n{JsonConvert.SerializeObject(e)}");

                return null;
            }
        }

        /// <summary>
        /// Executes a single TCP health check
        /// </summary>
        /// <param name="request">The request parameters for the health check</param>
        /// <returns>A task that can be awaited for completion of the check</returns>
        private async Task ExecuteTcpTest(TcpAreWeUpRequest request)
        {
            bool Success = false;
            string Message = String.Empty;
            Stopwatch SW = new Stopwatch();

            if (request.Port > 0)
            {
                try
                {
                    TimeSpan Timeout = TimeSpan.FromMilliseconds(request.Timeout);
                    TaskCompletionSource<bool> CancellationCompletionSource = new TaskCompletionSource<bool>();

                    using (CancellationTokenSource TokenSource = new CancellationTokenSource(Timeout))
                    {
                        using (TcpClient Client = new TcpClient())
                        {

                            SW.Start();
                            Task Connect = Client.ConnectAsync(request.Path, request.Port);

                            using (TokenSource.Token.Register(() => CancellationCompletionSource.TrySetResult(true)))
                            {
                                if (Connect == await Task.WhenAny(Connect, CancellationCompletionSource.Task))
                                {
                                    if (Client.Connected)
                                    {
                                        SW.Stop();
                                        Success = true;
                                    }
                                }
                            }
                        }
                    }

                    if (Success)
                    {
                        Message = $"[INFO] : {request.Path} via TCP on port {request.Port} is up! ({SW.ElapsedMilliseconds.ToString()}ms latency)";
                    }
                    else
                    {
                        Message = $"[ERROR] : {request.Path} via TCP on port {request.Port} is down!";
                    }
                }
                catch (Exception e)
                {
                    Message = $"[ERROR] : {request.Path} via TCP on port {request.Port} failed with an unexpected error:\r\n{JsonConvert.SerializeObject(e)}";
                }
            }
            else
            {
                Message = $"[ERROR] : The {request.Protocol} health check for {request.Path} was not configured with a valid port: {request.Port}.";
            }

            // Don't send a datapoint if the check didn't succeed
            long Time = Success ? SW.ElapsedMilliseconds : -1;

            await this.ProcessResult(request, Success, Message, Time);
        }

        /// <summary>
        /// Executes a single UDP health check. This method can produce false positives on availability since
        /// it uses SocketErrors like TimedOut to indicate success. A timeout can occur if the server is listening, but just
        /// didn't respond or if the request never made it. This cannot be used on AWS Lambda because only TCP/IP sockets are supported and
        /// the container kernel lacks the CAP_NET_RAW capability needed to allows the use of raw sockets.
        /// </summary>
        /// <param name="request">The request parameters for the health check</param>
        /// <returns>A task that can be awaited for completion of the check</returns>
        private async Task ExecuteUdpTest(UdpAreWeUpRequest request)
        {
            bool Success = false;
            string Message = String.Empty;

            if (request.Port > 0)
            {
                try
                {
                    if (!IPAddress.TryParse(request.Path, out IPAddress Addr))
                    {
                        IPAddress[] Addresses = await Dns.GetHostAddressesAsync(request.Path);
                        Addr = Addresses[0];
                    }

                    using (UdpClient Client = new UdpClient(new IPEndPoint(IPAddress.Any, 0)))
                    {
                        Client.Client.ReceiveTimeout = request.Timeout;
                        Client.Client.Connect(Addr, request.Port);

                        byte[] Payload;

                        if (!String.IsNullOrEmpty(request.Payload))
                        {
                            Payload = Convert.FromBase64String(request.Payload);
                        }
                        else
                        {
                            Payload = new byte[1] { 0x00 };
                        }

                        int SentBytes = Client.Client.Send(Payload);
                        byte[] Temp = new byte[request.ReceiveBufferSize];
                        int ReceivedBytes = Client.Client.Receive(Temp);

                        Success = ReceivedBytes > 0;

                        if (Success)
                        {
                            Message = $"[INFO] : {request.Path} via UDP on port {request.Port} is up!";
                        }
                        else
                        {
                            Message = $"[WARNING] : {request.Path} via UDP on port {request.Port} received a zero byte response, this is being treated as down.";
                        }
                    }
                }
                catch (SocketException e)
                {
                    // 10054 - An existing connection was forcibly closed by the remote host : This means the host is not listening
                    // SocketErrorCode - ConnectionReset

                    // 10060 - A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
                    // SocketErrorCode - TimedOut

                    if (_OkSocketErrors.Contains(e.SocketErrorCode))
                    {
                        Success = true;
                        Message = $"[INFO] : {request.Path} via UDP on port {request.Port} had status {e.SocketErrorCode.ToString()}, this is being treated as a success.";
                    }
                    else if (_WarningSocketErrors.Contains(e.SocketErrorCode))
                    {
                        Message = $"[WARNING] : {request.Path} via UDP on port {request.Port} was reachable, but the port could not be connected to. This is being treated as a failure.";
                    }
                    else
                    {
                        Message = $"[ERROR] : {request.Path} via UDP on port {request.Port} failed with an error:\r\n{JsonConvert.SerializeObject(e)}";
                    }
                }
                catch (Exception e)
                {
                    Message = $"[ERROR] : {request.Path} via UDP on port {request.Port} failed with an unexpected error:\r\n{JsonConvert.SerializeObject(e)}";
                }
            }
            else
            {
                Message = $"[ERROR] : The {request.Protocol} health check for {request.Path} was not configured with a valid port: {request.Port}.";
            }

            await this.ProcessResult(request, Success, Message);
        }

        /// <summary>
        /// Executes a single ICMP ping health check. This cannot be used on AWS Lambda because only TCP/IP sockets are supported and
        /// the container kernel lacks the CAP_NET_RAW capability needed to allows the use of raw sockets.
        /// </summary>
        /// <param name="request">The request parameters for the health check</param>
        /// <returns>A task that can be awaited for completion of the check</returns>
        private async Task ExecuteIcmpTest(IcmpAreWeUpRequest request)
        {
            bool Success = false;
            string Message = String.Empty;

            using (Ping Pinger = new Ping())
            {
                try
                {
                    PingReply Reply = await Pinger.SendPingAsync(request.Path, 1000);

                    if (Reply.Status == IPStatus.Success)
                    {
                        Success = true;
                        Message = $"[INFO] : {request.Path} via PING is up!";
                    }
                    else
                    {
                        Message = $"[ERROR] : {request.Path} via PING is down with status: {Reply.Status.ToString()}.";
                    }
                }
                catch (Exception e)
                {
                    Message = $"[ERROR] : {request.Path} via PING failed with an unexpected error:\r\n{JsonConvert.SerializeObject(e)}.";
                }
            }

            await this.ProcessResult(request, Success, Message);
        }

        /// <summary>
        /// Performs a single HTTP health check
        /// </summary>
        /// <param name="webRequest">The request parameters for the health check</param>
        /// <returns>A task that can be awaited for completion of the check</returns>
        private async Task ExecuteHttpTest(WebAreWeUpRequest webRequest)
        {
            Stopwatch SW = new Stopwatch();
            HttpClient Client;
            bool IgnoreSslErrors = (webRequest.Protocol == Models.Protocol.HTTPS && ((HttpsAreWeUpRequest)webRequest).IgnoreSSLErrors);
            bool StopRedirects = webRequest.PreventAutoRedirect;

            if (IgnoreSslErrors && StopRedirects)
            {
                Client = _IgnoreSslErrorAndNoRedirectClient;
            }
            else if (IgnoreSslErrors)
            {
                Client = _IgnoreSslErrorClient;
            }
            else if (StopRedirects)
            {
                Client = _NoRedirectClient;
            }
            else
            {
                Client = _NormalClient;
            }

            bool Success = false;
            string Message = String.Empty;

            // Initialize these here so we can dispose them later
            HttpRequestMessage Request = null;
            HttpResponseMessage Response = null;

            try
            {
                SW.Start();

                // The client will automatically follow redirects, unless we specifically disable it in the handler
                Request = new HttpRequestMessage(webRequest.Method, webRequest.GetPath());

                // If the request is a PUT, POST, or PATCH and there was supplied content, add it 
                // to the request
                if (!String.IsNullOrEmpty(webRequest.Content) && (new HttpMethod[] { HttpMethod.Post, HttpMethod.Put, new HttpMethod("PATCH") }).Contains(webRequest.Method))
                {
                    Request.Content = webRequest.GetContent();
                }

                Response = await Client.SendAsync(Request);
                SW.Stop();

                bool RedirectSuccess = true;

                // If we're not auto following redirects, and a redirect response code was returned
                // check the headers (if any were provided) and then follow the redirect
                if (webRequest.PreventAutoRedirect && ((int)Response.StatusCode >= 300 && (int)Response.StatusCode <= 399))
                {
                    foreach (KeyValuePair<string, string> Item in webRequest.RedirectHeadersToValidate)
                    {
                        if (Response.Headers.TryGetValues(Item.Key, out IEnumerable<string> Values))
                        {
                            if (Values.Any())
                            {
                                if (!Values.Contains(Item.Value))
                                {
                                    Message = $"[ERROR] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} failed because the {Item.Key} header did not contain the value {Item.Value}, it contained {String.Join(",", Values)}.";
                                    RedirectSuccess = false;
                                    break;
                                }
                            }
                            else
                            {
                                Message = $"[ERROR] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} failed because the {Item.Key} header did not contain any values.";
                                RedirectSuccess = false;
                                break;
                            }
                        }
                        else
                        {
                            Message = $"[ERROR] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} failed because the {Item.Key} header was not present in the redirect response. The response contained these headers:\r\n{JsonConvert.SerializeObject(Response.Headers)}";
                            RedirectSuccess = false;
                            break;
                        }
                    }

                    if (RedirectSuccess)
                    {
                        // Use a client now that doesn't prevent redirects
                        if (IgnoreSslErrors)
                        {
                            Client = _IgnoreSslErrorClient;
                        }
                        else
                        {
                            Client = _NormalClient;
                        }

                        SW.Start();
                        // Now follow the redirect to see if the page is up
                        HttpRequestMessage NewRequest = new HttpRequestMessage(HttpMethod.Get, Response.Headers.Location);

                        Response = await Client.SendAsync(NewRequest);
                        SW.Stop();
                    }
                }

                //If the response didn't match the expected response, send an SNS notification
                if (Response.StatusCode != webRequest.ExpectedResponse)
                {
                    // If the autofollowredirects was off and we successfully validated the redirect headers
                    // then this will be true and an error message won't have been set
                    if (RedirectSuccess)
                    {
                        Message = $"[ERROR] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} did not match the expected response of {(int)webRequest.ExpectedResponse} {webRequest.ExpectedResponse}: received {(int)Response.StatusCode} {Response.StatusCode}";
                    }
                }
                else
                {
                    if (webRequest.CookiesToValidate != null && webRequest.CookiesToValidate.Any())
                    {
                        // All the cookies to validate, except those in the cookie container. If the output is 0 (aka ! Any()), then all of the cookies to
                        // validate were in the container
                        IEnumerable<string> MissingCookies = webRequest.CookiesToValidate.Except(_Cookies.GetCookies(webRequest.GetPath()).Cast<Cookie>().Select(x => x.Name));

                        if (MissingCookies.Any())
                        {
                            Message = $"[ERROR] :  {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} failed because the response was missing required cookies {String.Join(",", MissingCookies)}.";
                        }
                        else
                        {
                            Success = true;
                            Message = $"[INFO] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} is up! ({SW.ElapsedMilliseconds.ToString()}ms latency)";
                        }
                    }
                    else
                    {
                        Success = true;
                        Message = $"[INFO] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} is up! ({SW.ElapsedMilliseconds.ToString()}ms latency)";
                    }
                }
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException)
                {
                    Message = $"[ERROR] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} failed with an error: {((WebException)e.InnerException).Status} - {((WebException)e.InnerException).Message}";
                }
                else
                {
                    Message = $"[ERROR] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} failed with an error:\r\n{JsonConvert.SerializeObject(e)}";
                }
            }
            catch (Exception e)
            {
                Message = $"[ERROR] : {webRequest.GetPath().ToString()} via HTTP {webRequest.Method.Method} failed with an unexpected error:\r\n{JsonConvert.SerializeObject(e)}";
            }
            finally
            {
                if (Response != null)
                {
                    Response.Dispose();
                }

                if (Request != null)
                {
                    Request.Dispose();
                }
            }

            // Don't send a data point if the check didn't succeed
            long Time = Success ? SW.ElapsedMilliseconds : -1;

            await this.ProcessResult(webRequest, Success, Message, Time);
        }

        /// <summary>
        /// Processes the results of a health check for the supplied request
        /// </summary>
        /// <param name="request">The generic request that was executed</param>
        /// <param name="success">Whether the check was successful</param>
        /// <param name="message">The message associated with the success or failure of the test</param>
        /// <param name="latency">The amount of latency for a successful test</param>
        /// <returns>A task that can be awaited for completion of processing the result</returns>
        private async Task ProcessResult(AreWeUpRequest request, bool success, string message, long latency = -1)
        {
            this._LambdaContext.Logger.LogLine(message);

            if (request.SendToCloudWatch)
            {
                string Path = request.Path;
                if (request is WebAreWeUpRequest)
                {
                    // This removes the scheme from the path since it's indicated by the protocol dimension
                    Uri Temp = ((WebAreWeUpRequest)request).GetPath();
                    Path = $"{Temp.IdnHost}{Temp.PathAndQuery}";
                }

                // If the success code is true, send a 1 to mean the site is up
                await SendCloudWatchUpDownMetric((success == true) ? 1 : 0, request.Protocol, Path, request.CustomerId);

                if (latency >= 0)
                {
                    await SendCloudWatchLatencyMetric(latency, request.Protocol, Path, request.CustomerId);
                }
            }

            if (success == false)
            {
                try
                {
                    PublishRequest PubRequest = new PublishRequest(request.SNSTopicArn, message);

                    if (!String.IsNullOrEmpty(request.Subject))
                    {
                        PubRequest.Subject = request.Subject;
                    }

                    PublishResponse Response = await _SNSClient.PublishAsync(PubRequest);
                }
                catch (AggregateException e)
                {
                    this._LambdaContext.LogError("Problem sending SNS", e);
                }
            }
        }

        /// <summary>
        /// Sends a value to CloudWatch metrics
        /// </summary>
        /// <param name="value">Use 1 to indicate the Url was up or 0 to indicate it was down</param>
        /// <param name="protocol">The protocol used to make the request</param>
        /// <param name="path">The HTTP or TCP endpoing IP address or host name</param>
        /// <param name="customerId">The customer Id associated with this check</param>
        /// <returns>A task that can be awaited for completion of sending the metrics to CloudWatch</returns>
        private async Task SendCloudWatchUpDownMetric(int value, Models.Protocol protocol, string path, string customerId)
        {
            using (AmazonCloudWatchClient Client = new AmazonCloudWatchClient())
            {
                MetricDatum Metric = new MetricDatum()
                {
                    Dimensions = { new Dimension() { Name = "Path", Value = path }, new Dimension() { Name = "CustomerId", Value = customerId }, new Dimension() { Name = "Protocol", Value = protocol.ToString() } },
                    MetricName = "Availability",
                    StatisticValues = new StatisticSet(),
                    Timestamp = DateTime.Now,
                    Unit = StandardUnit.Count,
                    Value = value
                };

                PutMetricDataRequest MetricRequest = new PutMetricDataRequest()
                {
                    MetricData = { Metric },
                    Namespace = "AWS/AreWeUp"
                };

                PutMetricDataResponse Response = await Client.PutMetricDataAsync(MetricRequest);

                //Make sure response was successful
                if ((int)Response.HttpStatusCode < 200 || (int)Response.HttpStatusCode > 299)
                {
                    this._LambdaContext.LogError($"The CloudWatch metric publish failed with HTTP Status {(int)Response.HttpStatusCode } {Response.HttpStatusCode} : {JsonConvert.SerializeObject(Response.ResponseMetadata) } ");
                }
            }
        }

        /// <summary>
        /// Sends a latency measurement to CloudWatch metrics
        /// </summary>
        /// <param name="latency">The amount of time the request took</param>
        /// <param name="protocol">The protocol used to make the request</param>
        /// <param name="path">The HTTP or TCP endpoing IP address or host name</param>
        /// <param name="customerId">The customer Id associated with this check</param>
        /// <returns>A task that can be awaited for completion of the metrics to CloudWatch</returns>
        private async Task SendCloudWatchLatencyMetric(long latency, Models.Protocol protocol, string path, string customerId)
        {
            using (AmazonCloudWatchClient Client = new AmazonCloudWatchClient())
            {
                MetricDatum Metric = new MetricDatum()
                {
                    Dimensions = { new Dimension() { Name = "Path", Value = path }, new Dimension() { Name = "CustomerId", Value = customerId }, new Dimension() { Name = "Protocol", Value = protocol.ToString() } },
                    MetricName = "Latency",
                    StatisticValues = new StatisticSet(),
                    Timestamp = DateTime.Now,
                    Unit = StandardUnit.Milliseconds,
                    Value = latency
                };

                PutMetricDataRequest MetricRequest = new PutMetricDataRequest()
                {
                    MetricData = { Metric },
                    Namespace = "AWS/AreWeUp"
                };

                PutMetricDataResponse Response = await Client.PutMetricDataAsync(MetricRequest);

                //Make sure response was successful
                if ((int)Response.HttpStatusCode < 200 || (int)Response.HttpStatusCode > 299)
                {
                    this._LambdaContext.LogError($"The CloudWatch metric publish failed with HTTP Status {(int)Response.HttpStatusCode } {Response.HttpStatusCode} : {JsonConvert.SerializeObject(Response.ResponseMetadata) } ");
                }
            }
        }

        #endregion
    }
}
