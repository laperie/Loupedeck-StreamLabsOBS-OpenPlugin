using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loupedeck;
using Newtonsoft.Json;
using SLOBSharp.Client.Requests;
using SLOBSharp.Client.Responses;

namespace SLOBSharp.Domain.Services
{
    public interface ISlobsService
    {
        SlobsRpcResponse ExecuteRequest(ISlobsRequest request);

        Task<SlobsRpcResponse> ExecuteRequestAsync(ISlobsRequest request);

        IEnumerable<SlobsRpcResponse> ExecuteRequests(IEnumerable<ISlobsRequest> requests);

        IEnumerable<SlobsRpcResponse> ExecuteRequests(params ISlobsRequest[] requests);

        Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(IEnumerable<ISlobsRequest> requests);

        Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(params ISlobsRequest[] requests);

        Task<Boolean> IsWarmingUpConnectionSucceeded();

        #region Subscription

        void InitSubscriptionPipe();

        void ExecuteSubscription(ISlobsRequest request);

        void ExecuteSubscription(string jsonRequest);

        Task StartListeningSubscriptionResponseAsync();
        
        void StartListeningSubscriptionResponse();
        
        void CloseSubscriptionPipe();

        void DisposeSubscriptionPipe();

        event SlobsPipeService.SubscriptionResponseReadedHandler SubscriptionResponseReaded;

        #endregion
    }
    public class ResponseEventArgs : EventArgs
    {
        public string Response { get; private set; }

        public ResponseEventArgs(string response)
        {
            Response = response;
        }
    }

    public class SlobsPipeService : ISlobsService
    {
        private readonly string pipeName;

        #region Pipe for Events and Async Method responses

        private NamedPipeClientStream _pipe;
        private StreamReader _reader;
        private StreamWriter _writer;

        public delegate void SubscriptionResponseReadedHandler(ResponseEventArgs eventArgs);
        public event SubscriptionResponseReadedHandler SubscriptionResponseReaded;

        public void InitSubscriptionPipe()
        {
            //if( _pipe != null)
            //{
            //    Tracer.Error($"--- ---> pipe != null  pipe.IsConnected: {_pipe.IsConnected},  Closing Subscription Pipe" );
            //    CloseSubscriptionPipe();
            //}

            if (_pipe == null)
            {
                _pipe = new NamedPipeClientStream(this.pipeName);

                for (var i = 1; i < 11; i++)
                {
                    var isConnected = AttemptToConnectForSubscription();

                    Tracer.Trace($"--- ---> [ {i} ], connected for subscription: {isConnected} ");

                    if( isConnected)
                    {
                        break;
                    }

                    Thread.Sleep(50);
                }

                _reader = new StreamReader(_pipe);
                _writer = new StreamWriter(_pipe) { NewLine = "\n" };
            }

            Tracer.Trace($"--- ---> pipe != null : {_pipe != null},   client connected: {_pipe.IsConnected}");
        }

        private Boolean AttemptToConnectForSubscription()
        {
            try
            {
                _pipe.Connect(1000);
            }
            catch (Exception ex)
            {
                Tracer.Error(ex, ex.Message);
                return false;
            }

            return true;
        }

        public void ExecuteSubscription(ISlobsRequest request)
        {
            _writer.WriteLine(request.ToJson());
            _writer.Flush();

            _pipe?.WaitForPipeDrain();                      // causes the server to block until all the data has been read from the pipe.
        }

        public void ExecuteSubscription(string jsonRequest)
        {
            _writer.WriteLine(jsonRequest);
            _writer.Flush();

            _pipe?.WaitForPipeDrain();                      // causes the server to block until all the data has been read from the pipe.
        }

        public void StartListeningSubscriptionResponse()
        {
            while (_pipe != null && _pipe.IsConnected && _reader != null)
            {
                String response = _reader.ReadLine();
                
                if (!String.IsNullOrEmpty(response))
                {
                    SubscriptionResponseReaded?.Invoke(new ResponseEventArgs(response));
                }
                else
                {
                    System.Threading.Thread.Sleep(250);
                }
            }

            Tracer.Trace($"Streamlabs OBS started closing...  StartListeningSubscriptionResponse.  pipe == null: {_pipe == null};  pipe.IsConnected: {_pipe?.IsConnected};  reader == null: {_reader == null} ");
        }

        public async Task StartListeningSubscriptionResponseAsync()
        {
            while (_pipe != null && _reader != null)
            {
                String response = await _reader.ReadLineAsync();

                if (!String.IsNullOrEmpty(response))
                {
                    SubscriptionResponseReaded?.Invoke(new ResponseEventArgs(response));
                }
            }

            Tracer.Trace($"StartListeningSubscriptionResponse_Async. ended: _pipe == null {_pipe == null}; _reader == null {_reader == null} ");
        }

        public void CloseSubscriptionPipe()
        {
            if( _pipe == null )
            {
                Tracer.Trace($"--- --->  pipe == null ");

                return;
            }   
            
            Tracer.Trace($" 1. Closing Subscr pipe... pipe.IsConnected: {_pipe.IsConnected}");

            if (_pipe.IsConnected)
            {
                Tracer.Trace($" 1.1. closing Subscr pipe... ");

                try
                {
                    _writer?.Close();
                }
                catch (Exception ex)                                                        // occurs when pipe is not connected or broken, so added if(_pipe.IsConnected) check above
                {
                    // - cannot access a closed pipe  
                    // - pipe is broken
                    Tracer.Warning(ex, ex.Message);
                }
            }
            
            Tracer.Trace($" 2. closing subscr pipe... ");

            try
            {
                _reader?.BaseStream?.Close();
                _reader?.BaseStream?.Dispose();
                _reader?.Close();

                _pipe?.Close();
            }
            catch( Exception ex )
            {
                Tracer.Warning(ex, ex.Message);
            }
            finally
            {
                _pipe?.Close();
            }

            Tracer.Trace($"closing subscr pipe. Completed");
        }

        public void DisposeSubscriptionPipe()
        {
            DisposeWriter();
            DisposeReader();
            DisposePipe();
        }

        private void DisposeWriter()
        {
            try
            {
                _writer?.Dispose();
            }
            catch(Exception ex)
            {
                Tracer.Warning(ex, ex.Message);
            }
            finally
            {
                _writer = null;
            }
        }
        private void DisposeReader()
        {
            try
            {
                _reader?.Dispose();
            }
            catch (Exception ex)
            {
                Tracer.Warning(ex, ex.Message);
            }
            finally
            {
                _reader = null;
            }
        }
        private void DisposePipe()
        {
            try
            {
                _pipe?.Dispose();
            }
            catch (Exception ex)
            {
                Tracer.Warning(ex, ex.Message);
            }
            finally
            {
                _pipe = null;
            }
        }



        #endregion

        //public SlobsPipeService() : this("slobs"){}

        public SlobsPipeService(string pipeName)
        {
            this.pipeName = pipeName;
        }

        public SlobsRpcResponse ExecuteRequest(ISlobsRequest request)
        {
            return this.ExecuteRequests(request)?.FirstOrDefault();
        }

        public async Task<SlobsRpcResponse> ExecuteRequestAsync(ISlobsRequest request)
        {
            var response = await this.ExecuteRequestsAsync(request).ConfigureAwait(false);

            return response.FirstOrDefault();
        }

        public async Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(IEnumerable<ISlobsRequest> requests)
        {
            return await this.ExecuteRequestsAsync(requests.ToArray()).ConfigureAwait(false);
        }

        //[Obsolete]
        //public IEnumerable<SlobsRpcResponse> ExecuteRequests(NamedPipeClientStream pipe, StreamReader reader, StreamWriter writer, ISlobsRequest[] requests)
        //{
        //    // We have to process 5 or less commands at a time since SLOBS can't handle more than that
        //    var skip = 5;
        //    var requestsChunk = requests.Take(5);
        //    //var response = default(SlobsRpcResponse);
        //    var slobsRpcResponses = new List<SlobsRpcResponse>(requests.Length);

        //    while (requestsChunk.Any())
        //    {
        //        foreach (var request in requestsChunk)
        //        {
        //            //var requestJson = request.ToJson();
        //            writer.WriteLine(request.ToJson());
        //        }

        //        writer.Flush();
        //        pipe?.WaitForPipeDrain();

        //        for (var i = 0; i < requestsChunk.Count(); i++)
        //        {
        //            String responseJson = reader.ReadLine();
        //            var response = JsonConvert.DeserializeObject<SlobsRpcResponse>(responseJson);
        //            response.JsonResponse = responseJson;
        //            slobsRpcResponses.Add(response);
        //        }

        //        requestsChunk = requests.Skip(skip).Take(5);
        //        skip += 5;
        //    }

        //    return slobsRpcResponses;
        //}

        public IEnumerable<SlobsRpcResponse> ExecuteRequestsSync(NamedPipeClientStream pipe, ISlobsRequest[] requests)
        {            
            var skip = 5;               // we have to process 5 or less commands at a time since SLOBS can't handle more than that
            var requestsChunk = requests.Take(5);
            var slobsRpcResponses = new List<SlobsRpcResponse>(requests.Length);

            using (var reader = new StreamReader(pipe))
            using (var writer = new StreamWriter(pipe) { NewLine = "\n" })
            {
                while (requestsChunk.Any())
                {
                    foreach (var request in requestsChunk)
                    {
                        writer.WriteLine(request.ToJson());
                    }

                    writer.Flush();
                    pipe?.WaitForPipeDrain();

                    for (var i = 0; i < requestsChunk.Count(); i++)
                    {
                        String responseJson = reader.ReadLine();

                        var response = JsonConvert.DeserializeObject<SlobsRpcResponse>(responseJson);
                        response.JsonResponse = responseJson;
                        
                        slobsRpcResponses.Add(response);
                    }

                    requestsChunk = requests.Skip(skip).Take(5);
                    skip += 5;
                }
            }

            return slobsRpcResponses;
        }

        //public IEnumerable<SlobsRpcResponse> ExecuteRequests(params ISlobsRequest[] requests)
        //{
        //    using (var pipe = new NamedPipeClientStream(this.pipeName))
        //    using (var reader = new StreamReader(pipe))
        //    using (var writer = new StreamWriter(pipe) { NewLine = "\n" })
        //    {
        //        pipe.Connect(5000);

        //        return this.ExecuteRequests(pipe, reader, writer, requests);
        //    }
        //}

        public IEnumerable<SlobsRpcResponse> ExecuteRequests(IEnumerable<ISlobsRequest> requests)
        {
            return this.ExecuteRequests(requests.ToArray());
        }

        public IEnumerable<SlobsRpcResponse> ExecuteRequests(params ISlobsRequest[] requests)
        {
            IEnumerable<SlobsRpcResponse> resp = null;

            using (var pipe = new NamedPipeClientStream(this.pipeName))
            {
                pipe.Connect(5000);

                if (pipe.IsConnected)
                {
                    resp = this.ExecuteRequestsSync(pipe, requests);
                }
            }

            return resp;
        }

        //[Obsolete]
        //public async Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(NamedPipeClientStream pipe, StreamReader reader, StreamWriter writer, ISlobsRequest[] requests)
        //{
        //    // We have to process 5 or less commands at a time since SLOBS can't handle more than that
        //    var skip = 5;
        //    var requestsChunk = requests.Take(5);
        //    var slobsRpcResponses = new List<SlobsRpcResponse>(requests.Length);
        //    while (requestsChunk.Any())
        //    {
        //        foreach (var request in requestsChunk)
        //        {
        //            await writer.WriteLineAsync(request.ToJson()).ConfigureAwait(false);
        //        }

        //        await writer.FlushAsync().ConfigureAwait(false);
        //        pipe?.WaitForPipeDrain();

        //        for (var i = 0; i < requestsChunk.Count(); i++)
        //        {
        //            var responseJson = await reader.ReadLineAsync().ConfigureAwait(false);

        //            var response = JsonConvert.DeserializeObject<SlobsRpcResponse>(responseJson);
        //            response.JsonResponse = responseJson;

        //            slobsRpcResponses.Add(response);
        //        }

        //        requestsChunk = requests.Skip(skip).Take(5);
        //        skip += 5;
        //    }

        //    return slobsRpcResponses;
        //}

        public async Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(NamedPipeClientStream pipe, ISlobsRequest[] requests)
        {            
            var skip = 5;                   // We have to process 5 or less commands at a time since SLOBS can't handle more than that
            var requestsChunk = requests.Take(5);
            var slobsRpcResponses = new List<SlobsRpcResponse>(requests.Length);

            using (var reader = new StreamReader(pipe))
            using (var writer = new StreamWriter(pipe) { NewLine = "\n" })
            {
                while (requestsChunk.Any())
                {
                    foreach (var request in requestsChunk)
                    {
                        await writer.WriteLineAsync(request.ToJson()).ConfigureAwait(false);
                    }

                    await writer.FlushAsync().ConfigureAwait(false);

                    pipe?.WaitForPipeDrain();

                    for (var i = 0; i < requestsChunk.Count(); i++)
                    {
                        var responseJson = await reader.ReadLineAsync().ConfigureAwait(false);

                        var response = JsonConvert.DeserializeObject<SlobsRpcResponse>(responseJson);
                        response.JsonResponse = responseJson;

                        slobsRpcResponses.Add(response);
                    }

                    requestsChunk = requests.Skip(skip).Take(5);
                    skip += 5;
                }
            }

            return slobsRpcResponses;
        }

        //FIXME!!!
        public async Task<Boolean> IsWarmingUpConnectionSucceeded()
        {
            using (var pipe = new NamedPipeClientStream(this.pipeName))
            {
                //await Task.Delay(50);

                try
                {
                    //pipe.Connect(1000);
                    await pipe.ConnectAsync(5000).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Tracer.Warning($"--- ---> NO CONN : {ex.Message} ");
                    return false;
                }

                return true;
            }
        }

        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task<IEnumerable<SlobsRpcResponse>> ExecuteRequestsAsync(params ISlobsRequest[] requests)
        {
            await semaphoreSlim.WaitAsync();

            IEnumerable<SlobsRpcResponse> resp = null;

            try
            {
                resp = await OpenPipeAndExecute(requests).ConfigureAwait(false);
            }
            finally
            {
                semaphoreSlim.Release();
            }

            return resp;
        }

        private async Task<IEnumerable<SlobsRpcResponse>> OpenPipeAndExecute(ISlobsRequest[] requests)
        {
            IEnumerable<SlobsRpcResponse> response = null;

            //using (var pipe = new NamedPipeClientStream( ".", this.pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            using (var pipe = new NamedPipeClientStream(this.pipeName))         // $"Global\\{this.pipeName}"
            {
                try
                {
                    //pipe.Connect(); 
                    await pipe.ConnectAsync(5000).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Tracer.Error(ex, $"    ---> Loupedeck could not connect to Streamlabs OBS: {ex.Message} ");
                }

                if (pipe.IsConnected)
                {
                    response = await this.ExecuteRequestsAsync(pipe, requests).ConfigureAwait(false);
                }
                else
                {
                    Tracer.Error($"    ---> pipe is not connected");
                }
            }

            return response;
        }
    }
}
