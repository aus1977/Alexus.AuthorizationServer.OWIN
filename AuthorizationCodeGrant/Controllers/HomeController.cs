﻿using System;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Mvc;
using Constants;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;

namespace AuthorizationCodeGrant.Controllers
{
    public class HomeController : Controller
    {
        private WebServerClient _webServerClient;

        public ActionResult Index()
        {
            ViewBag.AccessToken = Request.Form["AccessToken"] ?? "";
            ViewBag.RefreshToken = Request.Form["RefreshToken"] ?? "";
            ViewBag.Action = "";
            ViewBag.ApiResponse = "";

            InitializeWebServerClient();
            string accessToken = Request.Form["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                IAuthorizationState authorizationState = _webServerClient.ProcessUserAuthorization(Request);
                if (authorizationState != null)
                {
                    ViewBag.AccessToken = authorizationState.AccessToken;
                    ViewBag.RefreshToken = authorizationState.RefreshToken;
                    ViewBag.Action = Request.Path;
                }
            }

            if (!string.IsNullOrEmpty(Request.Form.Get("submit.Authorize")))
            {
                OutgoingWebResponse userAuthorization =
                    _webServerClient.PrepareRequestUserAuthorization(new[] { ClaimTypes.AuthorizationDecision, "Get_Access_to_device_and_app_history", ClaimTypes.Role, "END_USER" });
                userAuthorization.Send(HttpContext);
                Response.End();
            }
            else if (!string.IsNullOrEmpty(Request.Form.Get("submit.Refresh")))
            {
                var state = new AuthorizationState
                {
                    AccessToken = Request.Form["AccessToken"],
                    RefreshToken = Request.Form["RefreshToken"]
                };
                if (_webServerClient.RefreshAuthorization(state))
                {
                    ViewBag.AccessToken = state.AccessToken;
                    ViewBag.RefreshToken = state.RefreshToken;
                }
            }
            else if (!string.IsNullOrEmpty(Request.Form.Get("submit.CallApi")))
            {
                var resourceServerUri = new Uri(Paths.ResourceServerBaseAddress);
                var client = new HttpClient(_webServerClient.CreateAuthorizingHandler(accessToken));
                string body = client.GetStringAsync(new Uri(resourceServerUri, Paths.IdentityPath)).Result;
                ViewBag.ApiResponse = body;
            }

            return View();
        }

        private void InitializeWebServerClient()
        {
            var authorizationServerUri = new Uri(Paths.AuthorizationServerBaseAddress);
            var authorizationServer = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri(authorizationServerUri, Paths.AuthorizePath),
                TokenEndpoint = new Uri(authorizationServerUri, Paths.TokenPath)
            };
            _webServerClient = new WebServerClient(authorizationServer, "1", "alexus-secret");
        }
    }
}