using System;

namespace SyE.UI4GS {
    /// <summary>
    /// Provides methods for integrating Google Sign-In functionality across different platforms in Unity.
    /// </summary>
    public static class UniversalGSignIn {
        /// <summary>
        /// Initializes the Google Sign-In process with the specified client ID.
        /// </summary>
        /// <param name="clientId">The client ID obtained from Google Developer Console.</param>
        /// <param name="onInited">An optional callback to be invoked once initialization is complete.</param>
        /// <param name="urlScheme">The URL scheme used for deep linking. Default is "com.test".</param>
        /// <param name="loopbackLink">The loopback link used during the authentication process. Default is "http://localhost:3000/".</param>
        /// <param name="clientSecret">An optional client secret used for loopback authentication on certain platforms.</param>
        public static void Init (string clientId, Action onInited = null, string urlScheme = "com.test", string loopbackLink = "http://localhost:3000/", string clientSecret = null) {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            UniversalGSignInLoopback.Init (clientId, clientSecret, loopbackLink, onInited);
#elif UNITY_ANDROID
            UniversalGSignInAndroid.Init (clientId, onInited);
#elif UNITY_WEBGL
            UniversalGSignInWebGL.Init (clientId, onInited);
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            UniversalGSignInDeeplink.Init (clientId, urlScheme, onInited);
#endif
        }

        /// <summary>
        /// Grants offline access and returns an authorization code that can be exchanged for a refresh token.
        /// </summary>
        /// <param name="callback">A callback that receives the response containing the authorization code or an error.</param>
        public static void GrantOfflineAccess (Action<GrantOfflineAccessResponse> callback) {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            UniversalGSignInLoopback.GrantOfflineAccess (callback);
#elif UNITY_ANDROID
            UniversalGSignInAndroid.GrantOfflineAccess (callback);
#elif UNITY_WEBGL
            UniversalGSignInWebGL.GrantOfflineAccess (callback);
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            UniversalGSignInDeeplink.GrantOfflineAccess (callback);
#else
            callback?.Invoke(new GrantOfflineAccessResponse () {
                error = "platform_not_supported"
            });
#endif
        }

        /// <summary>
        /// Signs the user in and returns a <see cref="GoogleUser"/> object containing user information and authentication details.
        /// </summary>
        /// <param name="callback">A callback that receives the signed-in user information or an error.</param>
        public static void SignIn (Action<GoogleUser> callback) {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            UniversalGSignInLoopback.SignIn (callback);
#elif UNITY_ANDROID
            UniversalGSignInAndroid.SignIn (callback);
#elif UNITY_WEBGL
            UniversalGSignInWebGL.SignIn (callback);
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            UniversalGSignInDeeplink.SignIn (callback);
#else
            callback?.Invoke(new GoogleUser () {
                error = "platform_not_supported"
            });
#endif
        }

        /// <summary>
        /// Signs the user out from the current session.
        /// </summary>
        public static void SignOut () {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            UniversalGSignInLoopback.SignOut ();
#elif UNITY_ANDROID
            UniversalGSignInAndroid.SignOut ();
#elif UNITY_WEBGL
            UniversalGSignInWebGL.SignOut ();
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            UniversalGSignInDeeplink.SignOut ();
#endif
        }

        /// <summary>
        /// Disconnects the user's Google account, revoking access tokens and sign-in credentials.
        /// </summary>
        public static void Disconnect () {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            UniversalGSignInLoopback.Disconnect ();
#elif UNITY_ANDROID
            UniversalGSignInAndroid.Disconnect ();
#elif UNITY_WEBGL
            UniversalGSignInWebGL.Disconnect ();
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            UniversalGSignInDeeplink.Disconnect ();
#endif
        }

        /// <summary>
        /// Checks if the user is currently signed in.
        /// </summary>
        /// <returns>Returns true if the user is signed in; otherwise, false.</returns>
        public static bool IsSignedIn () {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return UniversalGSignInLoopback.IsSignedIn ();
#elif UNITY_ANDROID
            return UniversalGSignInAndroid.IsSignedIn ();
#elif UNITY_WEBGL
            return UniversalGSignInWebGL.IsSignedIn ();
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            return UniversalGSignInDeeplink.IsSignedIn ();
#else
            return false;
#endif
        }

        /// <summary>
        /// Retrieves the basic profile information of the currently signed-in user.
        /// </summary>
        /// <returns>A <see cref="BasicProfile"/> object containing the user's basic profile information.</returns>
        public static BasicProfile GetCurrentUserBasicProfile () {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return UniversalGSignInLoopback.GetCurrentUserBasicProfile ();
#elif UNITY_ANDROID
            return UniversalGSignInAndroid.GetCurrentUserBasicProfile ();
#elif UNITY_WEBGL
            return UniversalGSignInWebGL.GetCurrentUserBasicProfile ();
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            return UniversalGSignInDeeplink.GetCurrentUserBasicProfile ();
#else
            return new BasicProfile() {
                error = "platform_not_supported"
            };
#endif
        }

        /// <summary>
        /// Retrieves the authentication response of the currently signed-in user.
        /// </summary>
        /// <returns>An <see cref="AuthResponse"/> object containing the user's authentication response.</returns>
        public static AuthResponse GetCurrentUserAuthResponse () {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return UniversalGSignInLoopback.GetCurrentUserAuthResponse ();
#elif UNITY_ANDROID
            return UniversalGSignInAndroid.GetCurrentUserAuthResponse ();
#elif UNITY_WEBGL
            return UniversalGSignInWebGL.GetCurrentUserAuthResponse ();
#elif UNITY_IOS || UNITY_WSA || UNITY_STANDALONE
            return UniversalGSignInDeeplink.GetCurrentUserAuthResponse ();
#else
            return new AuthResponse() {
                error = "platform_not_supported"
            };
#endif
        }

        /// <summary>
        /// Represents the response received when offline access is granted.
        /// </summary>
        [Serializable]
        public class GrantOfflineAccessResponse {
            /// <summary>
            /// The authorization code returned by Google.
            /// </summary>
            public string code;

            /// <summary>
            /// Error message if the offline access grant fails.
            /// </summary>
            public string error;
        }

        /// <summary>
        /// Represents a Google user, including authentication details and basic profile information.
        /// </summary>
        [Serializable]
        public class GoogleUser {
            /// <summary>
            /// The authentication response containing tokens and other details.
            /// </summary>
            public AuthResponse authResponse;

            /// <summary>
            /// The user's basic profile information.
            /// </summary>
            public BasicProfile basicProfile;

            /// <summary>
            /// Error message if the sign-in process fails.
            /// </summary>
            public string error;
        }

        /// <summary>
        /// Represents the authentication response, including tokens and their metadata.
        /// </summary>
        [Serializable]
        public class AuthResponse {
            /// <summary>
            /// The expiry date and time of the access token.
            /// </summary>
            public DateTime expiresAt;

            /// <summary>
            /// The duration for which the access token is valid.
            /// </summary>
            public TimeSpan expiresIn;

            /// <summary>
            /// The date and time when the token was first issued.
            /// </summary>
            public DateTime firstIssuedAt;

            /// <summary>
            /// The ID token returned by Google.
            /// </summary>
            public string idToken;

            /// <summary>
            /// The ID of the identity provider.
            /// </summary>
            public string idpId;

            /// <summary>
            /// A login hint that identifies the user.
            /// </summary>
            public string loginHint;

            /// <summary>
            /// The access token returned by Google.
            /// </summary>
            public string accessToken;

            /// <summary>
            /// The refresh token returned by Google.
            /// </summary>
            public string refreshToken;

            /// <summary>
            /// The type of token returned.
            /// </summary>
            public string tokenType;

            /// <summary>
            /// Error message if the authentication process fails.
            /// </summary>
            public string error;
        }

        /// <summary>
        /// Represents a user's basic profile information.
        /// </summary>
        [Serializable]
        public class BasicProfile {
            /// <summary>
            /// The user's unique ID.
            /// </summary>
            public string id;

            /// <summary>
            /// The user's full name.
            /// </summary>
            public string name;

            /// <summary>
            /// The user's given name.
            /// </summary>
            public string givenName;

            /// <summary>
            /// The user's family name.
            /// </summary>
            public string familyName;

            /// <summary>
            /// The URL of the user's profile image.
            /// </summary>
            public string imageUrl;

            /// <summary>
            /// The user's email address.
            /// </summary>
            public string email;

            /// <summary>
            /// Error message if fetching the profile information fails.
            /// </summary>
            public string error;
        }
    }
}
