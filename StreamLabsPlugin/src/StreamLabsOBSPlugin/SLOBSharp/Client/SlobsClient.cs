using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SLOBSharp.Client.Requests;
using SLOBSharp.Client.Responses;
using SLOBSharp.Domain.Services;

namespace SLOBSharp.Client
{
    public enum SlobsClientType
    {
        Pipe,
        WebSocket
    }

    public interface ISlobsClient
    {
        /// <summary>
        /// Executes a single request synchronously.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        SlobsRpcResponse ExecuteRequest(ISlobsRequest request);

        /// <summary>
        /// Executes a single request asynchronously.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        Task<SlobsRpcResponse> ExecuteRequestAsync(ISlobsRequest request);

        /// <summary>
        /// Executes multiple requests synchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        IEnumerable<SlobsRpcResponse> ExecuteRequests(params ISlobsRequest[] requests);

        /// <summary>
        /// Executes multiple requests synchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        IEnumerable<SlobsRpcResponse> ExecuteRequests(IEnumerable<ISlobsRequest> requests);

        /// <summary>
        /// Executes multiple requests asynchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(params ISlobsRequest[] requests);

        /// <summary>
        /// Executes multiple requests asynchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(IEnumerable<ISlobsRequest> requests);

        #region Subscriptions

        /// <summary>
        /// Init pipe for Event or Async Method subscription. 
        /// </summary>
        void InitSubscriptionPipe();

        /// <summary>
        /// Executes Event or Async Method subscription. 
        /// </summary>
        /// <param name="request">Subscription request</param>
        void ExecuteSubscription(ISlobsRequest request);

        /// <summary>
        /// Executes Event or Async Method subscription. 
        /// </summary>
        /// <param name="request"></param>
        void ExecuteSubscription(string jsonRequest);

        /// <summary>
        /// Start listening response for Event or Async Method. Use only in BackgroundTask!!! 
        /// </summary>
        void StartListeningSubscriptionResponse();

        Task StartListeningSubscriptionResponseAsync();

        /// <summary>
        /// Close pipe for Event or Async Method subscription.
        /// </summary>
        void CloseSubscriptionPipe();

        /// <summary>
        /// Dispose pipe for Event or Async Method subscription.
        /// </summary>
        void DisposeSubscriptionPipe();

        event SlobsPipeService.SubscriptionResponseReadedHandler SubscriptionResponseReaded;

        #endregion
    }

    public abstract class SlobsClient : ISlobsClient
    {
        private readonly ISlobsService slobsService;

        internal SlobsClient(ISlobsService slobsService)
        {
            this.slobsService = slobsService;
            slobsService.SubscriptionResponseReaded += (response)=> SubscriptionResponseReaded(response);
        }

        public static ISlobsClient NewPipeClient() => new SlobsPipeClient();

        public static ISlobsClient NewPipeClient(string pipeName) => new SlobsPipeClient(pipeName);

        /// <summary>
        /// Executes a single request synchronously.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public SlobsRpcResponse ExecuteRequest(ISlobsRequest request)
        {
            return this.slobsService.ExecuteRequest(request);
        }

        /// <summary>
        /// Executes a single request asynchronously.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public async Task<SlobsRpcResponse> ExecuteRequestAsync(ISlobsRequest request)
        {
            return await this.slobsService.ExecuteRequestAsync(request).ConfigureAwait(false);
        }

        public async Task<Boolean> IsWarmingUpConnectionSucceeded()
        {
            return await this.slobsService.IsWarmingUpConnectionSucceeded(); 
        }

        /// <summary>
        /// Executes multiple requests synchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        public IEnumerable<SlobsRpcResponse> ExecuteRequests(params ISlobsRequest[] requests)
        {
            return this.slobsService.ExecuteRequests(requests);
        }

        /// <summary>
        /// Executes multiple requests synchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        public IEnumerable<SlobsRpcResponse> ExecuteRequests(IEnumerable<ISlobsRequest> requests)
        {
            return this.slobsService.ExecuteRequests(requests);
        }

        /// <summary>
        /// Executes multiple requests asynchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        public async Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(params ISlobsRequest[] requests)
        {
            return await this.slobsService.ExecuteRequestsAsync(requests).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes multiple requests asynchronously.
        /// </summary>
        /// <param name="requests">The requests.</param>
        /// <returns></returns>
        public async Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(IEnumerable<ISlobsRequest> requests)
        {
            return await this.slobsService.ExecuteRequestsAsync(requests).ConfigureAwait(false);
        }

        public event SlobsPipeService.SubscriptionResponseReadedHandler SubscriptionResponseReaded;

        public void InitSubscriptionPipe()
        {
            slobsService.InitSubscriptionPipe();
        }

        public void ExecuteSubscription(ISlobsRequest request)
        {
            slobsService.ExecuteSubscription(request);
        }

        public void ExecuteSubscription(string jsonRequest)
        {
            slobsService.ExecuteSubscription(jsonRequest);
        }

        public async Task StartListeningSubscriptionResponseAsync()
        {
            await slobsService.StartListeningSubscriptionResponseAsync();
        }

        public void StartListeningSubscriptionResponse()
        {
            slobsService.StartListeningSubscriptionResponse();
        }

        public void CloseSubscriptionPipe()
        {
            slobsService.CloseSubscriptionPipe();
        }

        public void DisposeSubscriptionPipe() 
        {
            slobsService.DisposeSubscriptionPipe();
        }
    }
}
