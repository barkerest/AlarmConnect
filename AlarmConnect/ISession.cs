using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using AlarmConnect.Models;
using AlarmConnect.Models.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AlarmConnect
{
    public interface ISession : IDisposable
    {
        /// <summary>
        /// The user we are connected as.
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// The host we are connected to.
        /// </summary>
        public string Host { get; }
        
        /// <summary>
        /// The logger used for this session.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ILogger Logger { get; }
        
        /// <summary>
        /// Gets a state value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetStateValue(string name);

        /// <summary>
        /// Sets a state value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetStateValue(string name, string value);
        
        /// <summary>
        /// Posts to an API endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint name.</param>
        /// <param name="data">The data to be encoded into JSON for the POST.</param>
        /// <param name="id">The ID for the endpoint command.</param>
        /// <param name="command">The endpoint command.</param>
        /// <param name="query">Query parameters, must be even length, even params are keys, odd params are values.</param>
        /// <param name="reqMfa">True if the API command requires MFA to have been completed.</param>
        /// <returns>Returns the contents of the response.</returns>
        internal Task<string> ApiPost(string endpoint, object data = null, string id = null, string command = null, string[] query = null, bool reqMfa = true);

        /// <summary>
        /// Gets a result from an API endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint name.</param>
        /// <param name="id">The ID for the request.</param>
        /// <param name="command">The command to perform.</param>
        /// <param name="query">Query parameters, must be even length, even params are keys, odd params are values.</param>
        /// <param name="reqMfa">True if the API command requires MFA to have been completed.</param>
        /// <returns>Returns the string content from the response.</returns>
        internal Task<string> ApiGet(string endpoint, string id = null, string command = null, string[] query = null, bool reqMfa = true);
    }
}
