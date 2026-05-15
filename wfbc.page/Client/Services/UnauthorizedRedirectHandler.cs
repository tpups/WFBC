using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace WFBC.Client.Services
{
    /// <summary>
    /// Delegating handler that detects 401 Unauthorized responses from the API
    /// and triggers a silent re-authentication by redirecting to the login page
    /// with the current URL as the return target.
    ///
    /// This handles the case where the cached access token has expired and
    /// silent refresh has failed (e.g. refresh token also expired/revoked).
    /// Without this, users see broken UI when their session quietly expires.
    /// </summary>
    public class UnauthorizedRedirectHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigation;
        private readonly SignOutSessionStateManager _signOutManager;

        public UnauthorizedRedirectHandler(
            NavigationManager navigation,
            SignOutSessionStateManager signOutManager)
        {
            _navigation = navigation;
            _signOutManager = signOutManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Capture the page the user was on so we return to it after login.
                var returnUrl = Uri.EscapeDataString(_navigation.Uri);
                _navigation.NavigateTo($"authentication/login?returnUrl={returnUrl}", forceLoad: true);
            }

            return response;
        }
    }
}