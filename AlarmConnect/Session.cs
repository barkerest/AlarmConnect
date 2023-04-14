using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AlarmConnect.Models;
using AlarmConnect.Models.Infrastructure;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace AlarmConnect
{
    public class Session : ISession 
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:92.0) Gecko/20100101 Firefox/92.0";

        /// <inheritdoc />
        public string UserName { get; }

        /// <inheritdoc />
        public string Host { get; } = "www.alarm.com";
        
        /// <inheritdoc />
        public ILogger Logger { get; }
        
        // the password to connect with.
        private readonly string _pass;

        private readonly HttpClient                 _client;
        private readonly HttpClientHandler          _handler;
        private readonly CookieContainer            _cookies;
        
        private readonly Uri                        _rootUri;
        private          Thread                     _keepAliveThread;
        private          DateTime                   _lastRequest = DateTime.MinValue;
        private          string                     _identity    = "nobody";
        private          bool                       _requiresMfa;
        private          bool?                      _supportAuthApp;
        private readonly Dictionary<string, string> _state = new Dictionary<string, string>();

        public Session(ILoginCredentials credentials, ILogger<Session> logger)
        {
            if (credentials is null) throw new ArgumentNullException(nameof(credentials));
            if (string.IsNullOrWhiteSpace(credentials.UserName)) throw new ArgumentException("user name cannot be blank", nameof(credentials));
            if (string.IsNullOrWhiteSpace(credentials.Password)) throw new ArgumentException("password cannot be blank", nameof(credentials));
            UserName = credentials.UserName;
            _pass    = credentials.Password;
            Logger   = logger ?? throw new ArgumentNullException(nameof(logger));
            _cookies = new CookieContainer();
            _handler = new HttpClientHandler { CookieContainer = _cookies };
            _client  = new HttpClient(_handler);
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            _rootUri = new Uri($"https://{Host}");
        }

        private Cookie IsLoggedInCookie => _cookies.GetCookies(_rootUri)["adc_e_loggedInAsSubscriber"];

        private Cookie AjaxRequestUniqueKey => _cookies.GetCookies(_rootUri)["afg"];

        private Cookie MfaIdCookie => _cookies.GetCookies(_rootUri)["twoFactorAuthenticationId"];

        public event EventHandler MfaRequired;

        /// <summary>
        /// Returns true if the last request indicated MFA was required.
        /// </summary>
        public bool RequiresMFA => _requiresMfa;

        /// <inheritdoc />
        public string GetStateValue(string name)
        {
            return _state.TryGetValue(name, out var ret) ? ret : "";
        }

        /// <inheritdoc />
        public void   SetStateValue(string name, string value)
        {
            _state[name] = value ?? "";
        }

        /// <summary>
        /// Is the session logged in?
        /// </summary>
        public bool IsLoggedIn
        {
            get
            {
                var cookie = IsLoggedInCookie;
                if (cookie is null) return false;
                if (cookie.Expired) return false;
                return cookie.Value == "1";
            }
        }

        /// <summary>
        /// Login the session.
        /// </summary>
        /// <param name="automaticKeepAlive">If true, a background thread will keep the session alive.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<bool> Login(bool automaticKeepAlive = true)
        {
            if (IsLoggedIn)
            {
                this.LogInformation("Already logged in.");
                return true;
            }

            try
            {
                // Initialize a session with the application.
                var req = new HttpRequestMessage(HttpMethod.Get, $"https://{Host}/login.aspx");
                this.LogDebug($"GET: {req.RequestUri}");
                var resp = await _client.SendAsync(req);
                resp.EnsureSuccessStatusCode();

                // use the HTML form sent to us to provide session information.
                var content = await resp.Content.ReadAsStringAsync();
                var html    = new HtmlDocument();
                html.LoadHtml(content);

                // get the form from the HTML document.
                var form = html.DocumentNode.Descendants("form").First()
                           ?? throw new InvalidOperationException("Failed to locate login form.");

                // build the form data.
                var data    = new MultipartFormDataContent();
                var setUser = false;
                var setPw   = false;

                foreach (var item in form.Descendants("input"))
                {
                    var name = item.Attributes["name"]?.Value ?? "";
                    if (name.EndsWith("username", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (setUser) throw new InvalidOperationException("Multiple user name field candidates located.");
                        data.Add(new StringContent(UserName), name);
                        setUser = true;
                    }
                    else if (name.EndsWith("password", StringComparison.OrdinalIgnoreCase) &&
                             item.Attributes["type"]?.Value == "password")
                    {
                        if (setPw) throw new InvalidOperationException("Multiple password field candidates located.");
                        data.Add(new StringContent(_pass), name);
                        setPw = true;
                    }
                    else if (name.StartsWith("__"))
                    {
                        data.Add(new StringContent(item.GetAttributeValue("value", "")), name);
                    }
                }

                if (!setUser) throw new InvalidOperationException("Failed to locate user name field.");
                if (!setPw) throw new InvalidOperationException("Failed to locate password field.");

                // flags.
                data.Add(new StringContent("1"), "IsFromNewSite");
                data.Add(new StringContent("1"), "JavaScriptTest");

                // POST /web/Default.aspx
                req = new HttpRequestMessage(HttpMethod.Post, $"https://{Host}/web/Default.aspx")
                {
                    Content = data
                };
                this.LogDebug($"POST: {req.RequestUri}");
                resp = await _client.SendAsync(req);
                resp.EnsureSuccessStatusCode();

                if (IsLoggedIn)
                {
                    this.LogInformation($"Successfully logged in as {UserName}.");
                    if (automaticKeepAlive)
                    {
                        _lastRequest     = DateTime.Now;
                        _keepAliveThread = new Thread(KeepAliveThread) { IsBackground = true };
                        _keepAliveThread.Start(this);
                    }

                    var idCol = this.GetIdentities();
                    if (idCol is null ||
                        idCol.Count < 1)
                    {
                        this.LogError($"Failed to locate identity for {UserName}.");
                        return false;
                    }

                    if (idCol.Count > 1)
                    {
                        this.LogError($"Found more than one identity for {UserName}.");
                        return false;
                    }

                    _identity = idCol[0].Id;
                    this.LogInformation($"Identity for {UserName} is {_identity}.");

                    return true;
                }

                this.LogError($"Failed to login as {UserName}.");
                return false;
            }
            catch (HttpRequestException e)
            {
                this.LogError(e, "Failed to login.");
                return false;
            }
        }

        private static void KeepAliveThread(object state)
        {
            var session = (Session)state;

            while (!session._disposing)
            {
                if (!session.IsLoggedIn) return;

                var ts = DateTime.Now.Subtract(session._lastRequest);

                // less than ten seconds remaining??
                if (ts.TotalSeconds >= 30)
                {
                    session.LogDebug("Performing keep-alive at 30 seconds since last request.");
                    var result = session.KeepAlive().Result;
                    if (!result)
                    {
                        session.LogWarning("Failed to automatically keep-alive.");
                    }
                }

                for (var i = 0; i < 100; i++)
                {
                    if (session._disposing) return;
                    Thread.Sleep(1);
                }
            }
        }

        private HttpRequestMessage NewJsonRequest(HttpMethod method, string url)
        {
            var ret = new HttpRequestMessage(method, url);
            ret.Headers.Accept.ParseAdd("application/vnd.api+json");
            var key = AjaxRequestUniqueKey?.Value;

            if (!string.IsNullOrEmpty(key))
            {
                ret.Headers.Add("AjaxRequestUniqueKey", key);
            }

            return ret;
        }

        /// <summary>
        /// Send a keep-alive request.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> KeepAlive()
        {
            if (!IsLoggedIn)
            {
                this.LogError("Not logged in.");
                return false;
            }

            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, $"https://{Host}/web/KeepAlive.aspx?timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
                this.LogDebug($"POST: {req.RequestUri}");
                var resp = await _client.SendAsync(req);
                resp.EnsureSuccessStatusCode();
                var content = await resp.Content.ReadAsStringAsync();
                var data    = JsonSerializer.Deserialize<Dictionary<string, string>>(content);

                if (data["status"] != "Keep Alive")
                {
                    this.LogWarning($"Status is not Keep Alive ({data["status"]}).");
                    return false;
                }

                _lastRequest = DateTime.Now;

                return true;
            }
            catch (HttpRequestException e)
            {
                this.LogError(e, "Failed to keep-alive.");
                return false;
            }
        }

        async Task<string> ISession.ApiPost(string endpoint, object data, string id, string command, string[] query, bool reqMfa)
        {
            RetryApiPost:
            if (!IsLoggedIn)
            {
                this.LogError("Not logged in.");
                return null;
            }

            if (reqMfa && RequiresMFA)
            {
                this.LogError("MFA required.");
                return null;
            }

            try
            {
                var url = new StringBuilder($"https://{Host}/web/api/{endpoint}");
                var sep = '?';

                // append ID.
                if (!string.IsNullOrEmpty(id))
                {
                    url.Append('/').Append(HttpUtility.UrlEncode(id));
                }

                // then command.
                if (!string.IsNullOrEmpty(command))
                {
                    url.Append('/').Append(HttpUtility.UrlEncode(command));
                }

                // and finally query params. 
                if (query != null)
                {
                    if ((query.Length % 2) != 0) throw new ArgumentException("Query list must contain an even number of entries.");
                    for (var i = 0; i < query.Length; i += 2)
                    {
                        var n = query[i];
                        var v = query[i + 1] ?? "";
                        if (string.IsNullOrEmpty(n)) throw new ArgumentException("Query value names cannot be blank.");
                        url.Append(sep).Append(HttpUtility.UrlEncode(n)).Append('=').Append(HttpUtility.UrlEncode(v));
                        sep = '&';
                    }
                }

                var req = NewJsonRequest(HttpMethod.Post, url.ToString());

                if (!(data is null))
                {
                    req.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
                }
                
                this.LogDebug($"POST: {url}");
                var resp = await _client.SendAsync(req);
                resp.CheckForMultiFactorRequirement();
                resp.EnsureSuccessStatusCode();

                _lastRequest = DateTime.Now;

                var result = await resp.Content.ReadAsStringAsync();

                return result;
            }
            catch (MfaRequiredException)
            {
                this.LogError("Requires multi-factor authentication.");
                _requiresMfa = true;
                MfaRequired?.Invoke(this, EventArgs.Empty);
                if (!_requiresMfa)
                {
                    goto RetryApiPost;
                }
                return null;
            }
            catch (HttpRequestException e)
            {
                this.LogError(e, "Failed to post item.");
                return null;
            }
            catch (JsonException e)
            {
                this.LogError(e, "Failed to encode item.");
                return null;
            }
        }

        async Task<string> ISession.ApiGet(string endpoint, string id, string command, string[] query, bool reqMfa)
        {
            RetryApiGet:
            if (!IsLoggedIn)
            {
                this.LogError("Not logged in.");
                return null;
            }

            if (reqMfa && RequiresMFA)
            {
                this.LogError("MFA required.");
                return null;
            }

            try
            {
                var url = new StringBuilder($"https://{Host}/web/api/{endpoint}");
                var sep = '?';

                // append ID.
                if (!string.IsNullOrEmpty(id))
                {
                    url.Append('/').Append(HttpUtility.UrlEncode(id));
                }

                // then command.
                if (!string.IsNullOrEmpty(command))
                {
                    url.Append('/').Append(HttpUtility.UrlEncode(command));
                }

                // and finally query params. 
                if (query != null)
                {
                    if ((query.Length % 2) != 0) throw new ArgumentException("Query list must contain an even number of entries.");
                    for (var i = 0; i < query.Length; i += 2)
                    {
                        var n = query[i];
                        var v = query[i + 1] ?? "";
                        if (string.IsNullOrEmpty(n)) throw new ArgumentException("Query value names cannot be blank.");
                        url.Append(sep).Append(HttpUtility.UrlEncode(n)).Append('=').Append(HttpUtility.UrlEncode(v));
                        sep = '&';
                    }
                }

                var req = NewJsonRequest(HttpMethod.Get, url.ToString());

                this.LogDebug($"GET: {url}");
                var resp = await _client.SendAsync(req);
                resp.CheckForMultiFactorRequirement();
                resp.EnsureSuccessStatusCode();

                _lastRequest = DateTime.Now;

                var result = await resp.Content.ReadAsStringAsync();

                return result;
            }
            catch (MfaRequiredException)
            {
                this.LogError("Requires multi-factor authentication.");
                _requiresMfa = true;
                MfaRequired?.Invoke(this, EventArgs.Empty);
                if (!_requiresMfa)
                {
                    goto RetryApiGet;
                }
                return null;
            }
            catch (HttpRequestException e)
            {
                this.LogError(e, "Failed to get item.");
                return null;
            }
        }
        
        private IDataObject GetTwoFactorAuthenticationSettings()
            => this.ApiGetOneRaw("engines/twoFactorAuthentication/twoFactorAuthentications", _identity, reqMfa: false);

        /// <summary>
        /// Determines if the current identity is configured for an authenticator app.
        /// </summary>
        /// <returns></returns>
        public bool IsTwoFactorAuthenticatorAppSupported()
        {
            if (_supportAuthApp.HasValue) return _supportAuthApp.Value;

            var mfaData = GetTwoFactorAuthenticationSettings();

            if (mfaData is null)
            {
                _supportAuthApp = false;
            }
            else
            {
                _supportAuthApp = mfaData.GetInt32Attribute("twoFactorType") == 1;
            }

            return _supportAuthApp.Value;
        }

        /// <summary>
        /// Verifies the two factor authentication with a code from an authenticator app.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<bool> VerifyTwoFactorViaAuthenticatorApp(string code)
        {
            if (!IsTwoFactorAuthenticatorAppSupported()) return false;

            var data = new Dictionary<string, object>
            {
                { "code", code },
                { "typeOf2FA", 1 }
            };

            var result = await ((ISession)this).ApiPost(
                             "engines/twoFactorAuthentication/twoFactorAuthentications",
                             data,
                             _identity,
                             "verifyTwoFactorCode",
                             reqMfa: false
                         );
            
            // we could process result to get the "device name", but no need as long as the result was successful.
            if (result is null) return false;
            
            _requiresMfa = false;
            
            return true;
        }
        
        private bool _disposing;

        /// <inheritdoc />
        public void Dispose()
        {
            _disposing = true;
            
            while (_keepAliveThread?.IsAlive ?? false)
                Thread.Sleep(0);
            _keepAliveThread = null;
            
            _client.Dispose();
            _handler.Dispose();
        }
    }
}
