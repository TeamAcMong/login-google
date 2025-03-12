#if UNITY_STANDALONE || UNITY_IOS || UNITY_WSA
using System;
using System.Collections.Generic;
using UnityEngine;
namespace SyE.UI4GS {
    public static class UniversalGSignInDeeplink {
        private static Action<UniversalGSignIn.GrantOfflineAccessResponse> _grantOfflineAccessCallback;
        private static Action<UniversalGSignIn.GoogleUser> _signInCallback;
        private static bool isInited = false;
        private static string clientId;
        private static string urlScheme;
        private static UniversalGSignIn.AuthResponse cachedAuthResponse;
        private static UniversalGSignIn.BasicProfile cachedBasicProfile;
        public static void Init (string clientId, string urlScheme, Action onInited) {
            if (isInited) {
                Debug.LogWarning ("Google Sign In already inited!");
                return;
            }
            UniversalGSignInDeeplink.clientId = clientId;
            UniversalGSignInDeeplink.urlScheme = urlScheme;
            isInited = true;
            isInited = true;
            var goolgeUser = UniversalGSignInUtils.GetSavedGoogleUser (clientId);
            if (goolgeUser == null) {
                onInited?.Invoke ();
                return;
            }
            cachedAuthResponse = goolgeUser.authResponse;
            cachedBasicProfile = goolgeUser.basicProfile;
            if (cachedAuthResponse.expiresAt < DateTime.UtcNow && !string.IsNullOrEmpty(cachedAuthResponse.refreshToken)) {
                UniversalGSignInUtils.RefreshToken (cachedAuthResponse, clientId, null, (authResponse) => {
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
            string redirectUrl = urlScheme + ":/oauth2/google";
            var authorizationRequest = UniversalGSignInUtils.GenerateOfflineAccessURL (clientId, redirectUrl);
            Application.deepLinkActivated += DeeplinkCallback;
#if UNITY_IOS && !UNITY_EDITOR
            WebAuthViewController.OpenURL (authorizationRequest, urlScheme, (url, error) => {
                if (!string.IsNullOrEmpty(error)) {
                    _grantOfflineAccessCallback?.Invoke (new UniversalGSignIn.GrantOfflineAccessResponse () {
                        error = "user_canceled"
                    });
                } else if (!string.IsNullOrEmpty(url)) {
                    DeeplinkCallback (url);
                }
            });
#else
            Application.OpenURL (authorizationRequest);
#endif
        }
        public static void SignIn (Action<UniversalGSignIn.GoogleUser> callback) {
            _grantOfflineAccessCallback = null;
            _signInCallback = callback;
            string redirectUrl = urlScheme + ":/oauth2/google";
            var authorizationRequest = UniversalGSignInUtils.GenerateSignInURL (clientId, redirectUrl);
            Application.deepLinkActivated += DeeplinkCallback;
#if UNITY_IOS && !UNITY_EDITOR
            WebAuthViewController.OpenURL (authorizationRequest, urlScheme, (url, error) => {
                if (!string.IsNullOrEmpty(error)) {
                    _signInCallback?.Invoke (new UniversalGSignIn.GoogleUser () {
                        error = "user_canceled"
                    });
                } else if (!string.IsNullOrEmpty(url)) {
                    DeeplinkCallback (url);
                }
            });
#else
            Application.OpenURL (authorizationRequest);
#endif
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
        public static void DeeplinkCallback (string deepLink) {
            Application.deepLinkActivated -= DeeplinkCallback;
            deepLink = deepLink.Replace (":///", ":/");

            var parameters = UniversalGSignInUtils.ParseQueryString (deepLink);
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
                string redirectUrl = urlScheme + ":/oauth2/google";
                UniversalGSignInUtils.GetAuthResponse (code, redirectUrl, clientId, null, (authResponse) => {
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
    }
}
#endif