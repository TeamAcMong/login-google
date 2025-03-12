#if UNITY_WEBGL
using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SyE.UI4GS {
    public static class UniversalGSignInWebGL {
        private static Action<UniversalGSignIn.GrantOfflineAccessResponse> _grantOfflineAccessCallback;
        private static Action<UniversalGSignIn.GoogleUser> _signInCallback;
        private static Action _onInitedCallback;
        private static bool isInited = false;
        [DllImport ("__Internal")]
        private static extern void InitGoolgeSignInJS (string clientId, Action onInited);
        [DllImport ("__Internal")]
        private static extern void GrantOfflineAccessJS (Action<string> callback);
        [DllImport ("__Internal")]
        private static extern void SignInJS (Action<string> callback);
        [DllImport ("__Internal")]
        private static extern void SignOutJS ();
        [DllImport ("__Internal")]
        private static extern void DisconnectJS ();
        [DllImport ("__Internal")]
        private static extern bool IsSignedInJS ();
        [DllImport ("__Internal")]
        private static extern string GetCurrentUserBasicProfileJS ();
        [DllImport ("__Internal")]
        private static extern string GetCurrentUserAuthResponseJS ();

        public static void Init (string clientId, Action onInited) {
            if (isInited) {
                Debug.LogWarning ("Google Sign In already inited!");
                return;
            }
            _onInitedCallback = onInited;
            InitGoolgeSignInJS (clientId, OnInitedInnerCallback);
            isInited = true;
        }
        public static void GrantOfflineAccess (Action<UniversalGSignIn.GrantOfflineAccessResponse> callback) {
            _grantOfflineAccessCallback = callback;
            GrantOfflineAccessJS (GrantOfflineAccessInnerCallback);
        }
        public static void SignIn (Action<UniversalGSignIn.GoogleUser> callback) {
            _signInCallback = callback;
            SignInJS (SignInInnerCallback);
        }
        public static void SignOut () {
            SignOutJS ();
        }
        public static void Disconnect () {
            DisconnectJS ();
        }
        public static bool IsSignedIn () {
            return IsSignedInJS ();
        }
        public static UniversalGSignIn.BasicProfile GetCurrentUserBasicProfile () {
            return JsonUtility.FromJson<UniversalGSignIn.BasicProfile> (GetCurrentUserBasicProfileJS ());
        }
        public static UniversalGSignIn.AuthResponse GetCurrentUserAuthResponse () {
            InnerAuthResponse innerResponse = JsonUtility.FromJson<InnerAuthResponse> (GetCurrentUserAuthResponseJS ());
            if (string.IsNullOrEmpty(innerResponse.error))
                return new UniversalGSignIn.AuthResponse () {
                    expiresAt = DateTimeOffset.FromUnixTimeMilliseconds (innerResponse.expires_at).UtcDateTime,
                    expiresIn = TimeSpan.FromSeconds (innerResponse.expires_in),
                    firstIssuedAt = DateTimeOffset.FromUnixTimeMilliseconds (innerResponse.first_issued_at).UtcDateTime,
                    idToken = innerResponse.id_token,
                    idpId = innerResponse.idpId,
                    loginHint = innerResponse.login_hint,
                    tokenType = innerResponse.token_type
                };
            return new UniversalGSignIn.AuthResponse () { error = innerResponse.error };
            }
        [Serializable]
        class InnerAuthResponse {
            public long expires_at;
            public int expires_in;
            public long first_issued_at;
            public string id_token;
            public string idpId;
            public string login_hint;
            public string token_type;
            public string error;
        }

        [MonoPInvokeCallback (typeof (Action))]
        private static void OnInitedInnerCallback () {
            _onInitedCallback?.Invoke ();
        }
        [MonoPInvokeCallback (typeof (Action<string>))]
        private static void GrantOfflineAccessInnerCallback (string message) {
            _grantOfflineAccessCallback?.Invoke (JsonUtility.FromJson<UniversalGSignIn.GrantOfflineAccessResponse>(message));
        }
        [MonoPInvokeCallback (typeof (Action<string>))]
        private static void SignInInnerCallback (string message) {
            UniversalGSignIn.GoogleUser user = JsonUtility.FromJson<UniversalGSignIn.GoogleUser> (message);
            if (string.IsNullOrEmpty (user.error)) {
                user.basicProfile = GetCurrentUserBasicProfile ();
                user.authResponse = GetCurrentUserAuthResponse ();
            }
            _signInCallback?.Invoke (user);
        }
    }
}
#endif