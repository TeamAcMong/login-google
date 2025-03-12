#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace SyE.UI4GS {
    public static class UniversalGSignInLoopback {
        private static Action<UniversalGSignIn.GrantOfflineAccessResponse> _grantOfflineAccessCallback;
        private static Action<UniversalGSignIn.GoogleUser> _signInCallback;
        private static bool isInited = false;
        private static string clientId;
        private static string clientSecret;
        private static string loopbackLink;
        private static System.Net.HttpListener httpListener;
        private static UniversalGSignIn.AuthResponse cachedAuthResponse;
        private static UniversalGSignIn.BasicProfile cachedBasicProfile;
        public static void Init (string clientId, string clientSecret, string loopbackLink, Action onInited) {
            if (isInited) {
                Debug.LogWarning ("Google Sign In already inited!");
                return;
            }
            if (string.IsNullOrEmpty (clientSecret)) {
                Debug.LogWarning ("Google Sign In Loopback should be initialized with client secret");
            }
            UniversalGSignInLoopback.clientId = clientId;
            UniversalGSignInLoopback.clientSecret = clientSecret;
            UniversalGSignInLoopback.loopbackLink = loopbackLink;
            isInited = true;
            var goolgeUser = UniversalGSignInUtils.GetSavedGoogleUser (clientId);
            if (goolgeUser == null) {
                onInited?.Invoke ();
                return;
            }
            cachedAuthResponse = goolgeUser.authResponse;
            cachedBasicProfile = goolgeUser.basicProfile;
            if (cachedAuthResponse.expiresAt < DateTime.UtcNow && !string.IsNullOrEmpty (cachedAuthResponse.refreshToken)) {
                UniversalGSignInUtils.RefreshToken (cachedAuthResponse, clientId, clientSecret, (authResponse) => {
                    if (!string.IsNullOrEmpty (authResponse.error))
                        return;
                    cachedAuthResponse = authResponse;
                    UniversalGSignInUtils.GetUserInfo (authResponse, (basicProfile) => {
                        if (!string.IsNullOrEmpty (basicProfile.error))
                            return;
                        cachedBasicProfile = basicProfile;
                        UniversalGSignInUtils.SaveGoogleUser (clientId, new UniversalGSignIn.GoogleUser () {
                            authResponse = authResponse,
                            basicProfile = basicProfile
                        });
                    });
                });
            }
            onInited?.Invoke ();
        }
        public static void GrantOfflineAccess (Action<UniversalGSignIn.GrantOfflineAccessResponse> callback) {
            if (!isInited) {
                callback.Invoke (new UniversalGSignIn.GrantOfflineAccessResponse () { error = "not_initialized" });
                return;
            }
            _grantOfflineAccessCallback = callback;
            _signInCallback = null;
            StartHttpServer ();
            var authorizationRequest = UniversalGSignInUtils.GenerateOfflineAccessURL (clientId, loopbackLink);
            Application.OpenURL (authorizationRequest);
        }
        public static void SignIn (Action<UniversalGSignIn.GoogleUser> callback) {
            if (!isInited) {
                callback.Invoke (new UniversalGSignIn.GoogleUser () { error = "not_initialized" });
                return;
            }
            _grantOfflineAccessCallback = null;
            _signInCallback = callback;
            StartHttpServer ();
            var authorizationRequest = UniversalGSignInUtils.GenerateSignInURL (clientId, loopbackLink);
            Application.OpenURL (authorizationRequest);
        }
        public static void SignOut () {
            cachedAuthResponse = null;
            cachedBasicProfile = null;
            UniversalGSignInUtils.DeleteSavedGoogleUser (clientId);
        }
        public static void Disconnect () {
            if (cachedAuthResponse != null) {
                UniversalGSignInUtils.RevokeAccessToken (cachedAuthResponse);
            }
            SignOut ();
        }
        public static bool IsSignedIn () {
            return cachedBasicProfile != null;
        }
        public static UniversalGSignIn.AuthResponse GetCurrentUserAuthResponse () {
            if (cachedAuthResponse == null)
                return new UniversalGSignIn.AuthResponse () { error = "no_user" };
            return cachedAuthResponse;
        }
        public static UniversalGSignIn.BasicProfile GetCurrentUserBasicProfile () {
            if (cachedBasicProfile == null)
                return new UniversalGSignIn.BasicProfile () { error = "no_user" };
            return cachedBasicProfile;
        }
        public static void PageRequestedHandler (string url) {
            url = url.Replace (":///", ":/");
            var parameters = UniversalGSignInUtils.ParseQueryString (url);

            var error = parameters.GetValueOrDefault ("error", null);
            if (error != null) {
                _grantOfflineAccessCallback?.Invoke (new UniversalGSignIn.GrantOfflineAccessResponse () {
                    error = error
                });
                _signInCallback?.Invoke (new UniversalGSignIn.GoogleUser () {
                    error = error
                });
                return;
            }

            var code = parameters.GetValueOrDefault ("code", null);
            _grantOfflineAccessCallback?.Invoke (new UniversalGSignIn.GrantOfflineAccessResponse () {
                code = code
            });
            if (_signInCallback != null) {
                UniversalGSignInUtils.GetAuthResponse (code, loopbackLink, clientId, clientSecret, (authResponse) => {
                    cachedAuthResponse = authResponse;
                    UniversalGSignInUtils.GetUserInfo (authResponse, (basicProfile) => {
                        cachedBasicProfile = basicProfile;
                        var googleUser = new UniversalGSignIn.GoogleUser () {
                            authResponse = authResponse,
                            basicProfile = basicProfile
                        };
                        UniversalGSignInUtils.SaveGoogleUser (clientId, googleUser);
                        _signInCallback?.Invoke (googleUser);
                    });
                });
            }
        }
        private static void StartHttpServer () {
            if (httpListener != null) {
                return;
            }
            httpListener = new System.Net.HttpListener ();

            httpListener.Prefixes.Add (loopbackLink);
            httpListener.Start ();

            var context = System.Threading.SynchronizationContext.Current;
            var asyncResult = httpListener.BeginGetContext (result => context.Send (HandleHttpListenerCallback, result), httpListener);

            if (!Application.runInBackground) {
                Debug.LogWarning ("HttpListener is blocking the main thread. To avoid this, set [Run In Background] from [Player Settings].");

                // Block the thread when background mode is not supported to serve HTTP response while the application is not in focus.
                if (!asyncResult.AsyncWaitHandle.WaitOne (TimeSpan.FromSeconds (30))) {
                    Debug.LogWarning ("No response received.");
                }
            }
        }
        private static void HandleHttpListenerCallback (object state) {
            var result = (IAsyncResult) state;
            var context = httpListener.EndGetContext (result);

            // Send an HTTP response to the browser to notify the user to close the browser.
            var response = context.Response;
            var buffer = Encoding.UTF8.GetBytes ("<script language='javascript'>window.close()</script>");

            response.ContentEncoding = Encoding.UTF8;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            var output = response.OutputStream;

            output.Write (buffer, 0, buffer.Length);
            output.Close ();
            httpListener.Close ();
            httpListener = null;
            PageRequestedHandler (context.Request.Url.AbsoluteUri);
        }
    }
}
#endif