#if UNITY_ANDROID
using System;
using UnityEngine;
namespace SyE.UI4GS {
    public static class UniversalGSignInAndroid {
        private static AndroidJavaObject googleSignInClient;
        private static AndroidJavaObject unityActivity;
        private static Action<UniversalGSignIn.GrantOfflineAccessResponse> _grantOfflineAccessCallback;
        private static Action<UniversalGSignIn.GoogleUser> _signInCallback;
        private static bool isInited = false;
        private static string clientId;

        public static void Init (string clientId, Action onInited) {
            if (isInited) {
                Debug.LogWarning ("Google Sign In already inited!");
                return;
            }
            UniversalGSignInAndroid.clientId = clientId;
            using (var unityPlayer = new AndroidJavaClass ("com.unity3d.player.UnityPlayer")) {
                unityActivity = unityPlayer.GetStatic<AndroidJavaObject> ("currentActivity");
            }
            isInited = true;
            onInited?.Invoke ();
        }
        public static void GrantOfflineAccess (Action<UniversalGSignIn.GrantOfflineAccessResponse> callback) {
            if (!isInited) {
                callback.Invoke (new UniversalGSignIn.GrantOfflineAccessResponse () { error = "not_initialized" });
                return;
            }
            _grantOfflineAccessCallback = callback;
            _signInCallback = null;
            Debug.LogError("clientid"+ clientId);
            var googleSignInOptions = GenerateSignInOptions (clientId, new string[] { "requestServerAuthCode" });
            var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn");
            googleSignInClient = googleSignInClass.CallStatic<AndroidJavaObject> ("getClient", unityActivity, googleSignInOptions);
            var signInIntent = googleSignInClient.Call<AndroidJavaObject> ("getSignInIntent");
            unityActivity.Call ("startActivity", signInIntent);
            Application.focusChanged += ApplicationFocusChanged;
            signInIntent.Dispose ();
            googleSignInClass.Dispose ();
            googleSignInOptions.Dispose ();
        }
        public static void SignIn (Action<UniversalGSignIn.GoogleUser> callback) {
            if (!isInited) {
                callback.Invoke (new UniversalGSignIn.GoogleUser () { error = "not_initialized" });
                return;
            }
            _grantOfflineAccessCallback = null;
            _signInCallback = callback;
            var googleSignInOptions = GenerateSignInOptions (clientId, new string[] { "requestId", "requestEmail", "requestProfile", "requestIdToken" });
            var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn");
            googleSignInClient = googleSignInClass.CallStatic<AndroidJavaObject> ("getClient", unityActivity, googleSignInOptions);
            var signInIntent = googleSignInClient.Call<AndroidJavaObject>("getSignInIntent");
            unityActivity.Call ("startActivity", signInIntent);
            Application.focusChanged += ApplicationFocusChanged;
            signInIntent.Dispose ();
            googleSignInClass.Dispose ();
            googleSignInOptions.Dispose ();
        }
        public static void SignOut () {
            if (!isInited)
                return;
            var googleSignInOptions = GenerateSignInOptions (clientId, new string[] { });
            var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn");
            googleSignInClient = googleSignInClass.CallStatic<AndroidJavaObject> ("getClient", unityActivity, googleSignInOptions);
            googleSignInClient.Call<AndroidJavaObject> ("signOut");
            googleSignInClass.Dispose ();
            googleSignInOptions.Dispose ();
        }
        public static void Disconnect () {
            if (!isInited)
                return;
            var googleSignInOptions = GenerateSignInOptions (clientId, new string[] { });
            var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn");
            googleSignInClient = googleSignInClass.CallStatic<AndroidJavaObject> ("getClient", unityActivity, googleSignInOptions);
            googleSignInClient.Call<AndroidJavaObject> ("revokeAccess");
            googleSignInClass.Dispose ();
            googleSignInOptions.Dispose ();
        }
        public static bool IsSignedIn () {
            using (var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn")) {
                using (var lastSignedAccount = googleSignInClass.CallStatic<AndroidJavaObject> ("getLastSignedInAccount", unityActivity)) {
                    return lastSignedAccount != null;
                }
            }
        }
        public static UniversalGSignIn.AuthResponse GetCurrentUserAuthResponse () {
            using (var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn")) {
                using (var lastSignedAccount = googleSignInClass.CallStatic<AndroidJavaObject> ("getLastSignedInAccount", unityActivity)) {
                    if (lastSignedAccount == null)
                        return new UniversalGSignIn.AuthResponse () { error = "no_user" };
                    return new UniversalGSignIn.AuthResponse () {
                        idToken = lastSignedAccount.Call<string> ("getIdToken")
                    };
                }
            }
        }
        public static UniversalGSignIn.BasicProfile GetCurrentUserBasicProfile () {
            using (var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn")) {
                using (var lastSignedAccount = googleSignInClass.CallStatic<AndroidJavaObject> ("getLastSignedInAccount", unityActivity)) {
                    if (lastSignedAccount == null)
                        return new UniversalGSignIn.BasicProfile () { error = "no_user" };
                    string photoUrl = null;
                    var photoJavaObject = lastSignedAccount.Call<AndroidJavaObject> ("getPhotoUrl");
                    if (photoJavaObject != null) {
                        photoUrl = photoJavaObject.Call<string> ("toString");
                        photoJavaObject.Dispose ();
                    }
                    return new UniversalGSignIn.BasicProfile () {
                        id = lastSignedAccount.Call<string> ("getId"),
                        name = lastSignedAccount.Call<string> ("getDisplayName"),
                        givenName = lastSignedAccount.Call<string> ("getGivenName"),
                        familyName = lastSignedAccount.Call<string> ("getFamilyName"),
                        imageUrl = photoUrl,
                        email = lastSignedAccount.Call<string> ("getEmail")
                    };
                }
            }
        }
        private static AndroidJavaObject GenerateSignInOptions (string clientId, string[] requests) {
            var googleSignInOptionsClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignInOptions");
            var DEFAULT_GAMES_SIGN_IN = googleSignInOptionsClass.GetStatic<AndroidJavaObject> ("DEFAULT_SIGN_IN");
            var googleSignInOptionsBuilder = new AndroidJavaObject ("com.google.android.gms.auth.api.signin.GoogleSignInOptions$Builder", DEFAULT_GAMES_SIGN_IN);
            foreach (string request in requests) {
                if (request == "requestServerAuthCode" || request == "requestIdToken") {
                    googleSignInOptionsBuilder.Call<AndroidJavaObject> (request, clientId);
                } else if (request == "requestId" || request == "requestEmail" || request == "requestProfile") {
                    googleSignInOptionsBuilder.Call<AndroidJavaObject> (request);
                }
            }
            var googleSignInOptions = googleSignInOptionsBuilder.Call<AndroidJavaObject> ("build");
            googleSignInOptionsBuilder.Dispose ();
            DEFAULT_GAMES_SIGN_IN.Dispose ();
            googleSignInOptionsClass.Dispose ();
            return googleSignInOptions;
        }
        private static void ApplicationFocusChanged (bool isFocused) {
            if (isFocused) {
                Application.focusChanged -= ApplicationFocusChanged;
                var googleSignInClass = new AndroidJavaClass ("com.google.android.gms.auth.api.signin.GoogleSignIn");
                var lastSignedAccount = googleSignInClass.CallStatic<AndroidJavaObject> ("getLastSignedInAccount", unityActivity);
                Debug.Log ($"lastSignedAccount: {lastSignedAccount}");
                if (_grantOfflineAccessCallback != null) {
                    if (lastSignedAccount != null) {
                        _grantOfflineAccessCallback?.Invoke (new UniversalGSignIn.GrantOfflineAccessResponse () {
                            code = lastSignedAccount.Call<string> ("getServerAuthCode")
                        });
                    } else {
                        _grantOfflineAccessCallback?.Invoke (new UniversalGSignIn.GrantOfflineAccessResponse () {
                            error = "unknown_error"
                        });
                    }
                } else {
                    if (lastSignedAccount != null) {
                        string photoUrl = null;
                        var photoJavaObject = lastSignedAccount.Call<AndroidJavaObject> ("getPhotoUrl");
                        if (photoJavaObject != null) {
                            photoUrl = photoJavaObject.Call<string> ("toString");
                            photoJavaObject.Dispose ();
                        }
                        _signInCallback?.Invoke (new UniversalGSignIn.GoogleUser () {
                            authResponse = new UniversalGSignIn.AuthResponse () {
                                idToken = lastSignedAccount.Call<string> ("getIdToken")
                            },
                            basicProfile = new UniversalGSignIn.BasicProfile () {
                                id = lastSignedAccount.Call<string> ("getId"),
                                name = lastSignedAccount.Call<string> ("getDisplayName"),
                                givenName = lastSignedAccount.Call<string> ("getGivenName"),
                                familyName = lastSignedAccount.Call<string> ("getFamilyName"),
                                imageUrl = photoUrl,
                                email = lastSignedAccount.Call<string> ("getEmail")
                            }
                        });
                    } else {
                        _signInCallback?.Invoke (new UniversalGSignIn.GoogleUser () {
                            error = "unknown_error"
                        });
                    }
                }
                
                _grantOfflineAccessCallback = null;
                _signInCallback = null;
                lastSignedAccount?.Dispose ();
                googleSignInClass.Dispose ();
            }
        }
    }
}
#endif