using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Text.Json;

namespace Track
{
    public class APIResponse<T>
    {
        public string message { get; set; }
        public T data { get; set; }
        public static APIResponse<T> Empty {
            get {
                return new APIResponse<T> { message = "Empty" };
            }
        }

    }

        class HttpService : IDisposable
    {
    private static HttpService singleton;
        public static HttpService i {
            get {
                return singleton ?? (singleton = new HttpService());
            }
        }
        private readonly HttpClient _http;
        private bool disposedValue;
        //private readonly string baseUrl = "http://localhost:8000/api/v1/";
        private readonly string baseUrl = "https://proman.ginalne.host/api/v1/";

        public HttpService() {
            if (singleton != null) {
                singleton.Dispose(true);
            }
            singleton = this;
            _http = new HttpClient();
        }
        public async Task<APIResponse<T>> postSimple<T>(string path, IEnumerable<KeyValuePair<string, string>> data = null) {
            Trace.WriteLine("Request POST [Simple] : " + baseUrl + path +" with Param " + (data != null ? await new FormUrlEncodedContent(data).ReadAsStringAsync() : "?"), "Http");
            var response = await _http.PostAsync(baseUrl + path, data == null ? null : new FormUrlEncodedContent(data));
            if (response.StatusCode == HttpStatusCode.OK) {
                var text = await response.Content.ReadAsStringAsync();
                Trace.WriteLine(text);
                APIResponse<T> body = JsonSerializer.Deserialize<APIResponse<T>>(text);
                return body;
            } else {
                var text = await response.Content.ReadAsStringAsync();
                Trace.WriteLine(text);
                return null;
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                }

                disposedValue = true;
            }
        }
        void IDisposable.Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
