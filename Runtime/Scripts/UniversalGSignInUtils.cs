using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace SyE.UI4GS {
    public static class UniversalGSignInUtils {
        private const string GAuthEndPoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string GTokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string GUserInfoEndpoint = "https://openidconnect.googleapis.com/v1/userinfo";
        private const string GRevocationEndpoint = "https://oauth2.googleapis.com/revoke";
        public static string GenerateOfflineAccessURL (string clientId, string redirectUrl) {
            redirectUrl = Uri.EscapeDataString (redirectUrl);
            return $"{GAuthEndPoint}?access_type=offline&response_type=code&scope=openid%20profile%20email&redirect_uri={redirectUrl}&client_id={clientId}";
        }
        public static string GenerateSignInURL (string clientId, string redirectUrl) {
            redirectUrl = Uri.EscapeDataString (redirectUrl);
            return $"{GAuthEndPoint}?response_type=code&scope=openid%20profile%20email&redirect_uri={redirectUrl}&client_id={clientId}";
        }
        public static void GetAuthResponse (string code, string redirect_uri, string clientId, string clientSecret, Action<UniversalGSignIn.AuthResponse> callback) {
            var formFields = new Dictionary<string, string>
                {
                    { "code", code },
                    { "redirect_uri", redirect_uri },
                    { "client_id", clientId },
                    { "grant_type", "authorization_code" }
                };
            if (!string.IsNullOrEmpty (clientSecret))
                formFields.Add ("client_secret", clientSecret);
            var request = UnityWebRequest.Post (GTokenEndpoint, formFields);

            request.SendWebRequest ().completed += _ => {
                if (request.error != null) {
                    callback?.Invoke (new UniversalGSignIn.AuthResponse () { error = request.error });
                } else {
                    InnerAuthResponse innerResponse = JsonUtility.FromJson<InnerAuthResponse> (request.downloadHandler.text);
                    callback?.Invoke (new UniversalGSignIn.AuthResponse () {
                        expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds (innerResponse.expires_in),
                        expiresIn = TimeSpan.FromSeconds (innerResponse.expires_in),
                        firstIssuedAt = DateTime.UtcNow,
                        idToken = innerResponse.id_token,
                        idpId = innerResponse.idpId,
                        loginHint = innerResponse.login_hint,
                        accessToken = innerResponse.access_token,
                        refreshToken = innerResponse.refresh_token,
                        tokenType = innerResponse.token_type
                    });
                }
            };
        }
        public static void GetUserInfo (UniversalGSignIn.AuthResponse authResponse, Action<UniversalGSignIn.BasicProfile> callback) {
            var request = UnityWebRequest.Get (GUserInfoEndpoint);

            request.SetRequestHeader ("Authorization", $"{authResponse.tokenType} {authResponse.accessToken}");
            request.SendWebRequest ().completed += _ => {
                if (request.error != null) {
                    callback?.Invoke (new UniversalGSignIn.BasicProfile () { error = request.error });
                } else {
                    InnerBasicProfile innerResponse = JsonUtility.FromJson<InnerBasicProfile> (request.downloadHandler.text);
                    callback?.Invoke (new UniversalGSignIn.BasicProfile () { 
                        id = innerResponse.sub,
                        name = innerResponse.name,
                        givenName = innerResponse.given_name,
                        familyName = innerResponse.family_name,
                        imageUrl = innerResponse.picture,
                        email = innerResponse.email
                    });
                }
            };
        }
        public static void RefreshToken (UniversalGSignIn.AuthResponse authResponse, string clientId, string clientSecret, Action<UniversalGSignIn.AuthResponse> callback) {
            var formFields = new Dictionary<string, string>
                {
                    { "refresh_token", authResponse.refreshToken },
                    { "client_id", clientId },
                    { "grant_type", "refresh_token" },
                   
                };
            if (!string.IsNullOrEmpty(clientSecret))
                formFields.Add ("client_secret", clientSecret);
            var request = UnityWebRequest.Post (GTokenEndpoint, formFields);

            request.SendWebRequest ().completed += _ => {
                if (request.error != null) {
                    callback?.Invoke (new UniversalGSignIn.AuthResponse () { error = request.error });
                } else {
                    InnerAuthResponse innerResponse = JsonUtility.FromJson<InnerAuthResponse> (request.downloadHandler.text);
                    callback?.Invoke (new UniversalGSignIn.AuthResponse () {
                        expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds (innerResponse.expires_in),
                        expiresIn = TimeSpan.FromSeconds (innerResponse.expires_in),
                        firstIssuedAt = DateTime.UtcNow,
                        idToken = innerResponse.id_token,
                        idpId = innerResponse.idpId,
                        loginHint = innerResponse.login_hint,
                        accessToken = innerResponse.access_token,
                        refreshToken = innerResponse.refresh_token,
                        tokenType = innerResponse.token_type
                    });
                }
            };
        }
        public static void RevokeAccessToken (UniversalGSignIn.AuthResponse authResponse) {
            var request = UnityWebRequest.PostWwwForm ($"{GRevocationEndpoint}?token={authResponse.accessToken}", "");
            request.SendWebRequest ();
        }
        public static void SaveGoogleUser (string clientId, UniversalGSignIn.GoogleUser goolgeUser) {
            InnerGoogleUser innerGoogleUser = new InnerGoogleUser () {
                authResponse = new InnerAuthResponse() {
                    expires_at = goolgeUser.authResponse.expiresAt.ToFileTimeUtc (),
                    expires_in = (int) goolgeUser.authResponse.expiresIn.TotalSeconds,
                    first_issued_at = goolgeUser.authResponse.firstIssuedAt.ToFileTimeUtc (),
                    id_token = goolgeUser.authResponse.idToken,
                    idpId = goolgeUser.authResponse.idpId,
                    login_hint = goolgeUser.authResponse.loginHint,
                    access_token = goolgeUser.authResponse.accessToken,
                    refresh_token = goolgeUser.authResponse.refreshToken,
                    token_type = goolgeUser.authResponse.tokenType,
                },
                basicProfile = new InnerBasicProfile() {
                    sub = goolgeUser.basicProfile.id,
                    name = goolgeUser.basicProfile.name,
                    given_name = goolgeUser.basicProfile.givenName,
                    family_name = goolgeUser.basicProfile.familyName,
                    picture = goolgeUser.basicProfile.imageUrl,
                    email = goolgeUser.basicProfile.email,
                }
            };
            PlayerPrefs.SetString (clientId, JsonUtility.ToJson(innerGoogleUser));
        }
        public static UniversalGSignIn.GoogleUser GetSavedGoogleUser (string clientId) {
            string json = PlayerPrefs.GetString (clientId, null);
            if (string.IsNullOrEmpty (json))
                return null;
            InnerGoogleUser inner = JsonUtility.FromJson<InnerGoogleUser> (json);
            return new UniversalGSignIn.GoogleUser () {
                authResponse = new UniversalGSignIn.AuthResponse () {
                    expiresAt = DateTime.FromFileTimeUtc (inner.authResponse.expires_at),
                    expiresIn = TimeSpan.FromSeconds(inner.authResponse.expires_in),
                    firstIssuedAt = DateTime.FromFileTimeUtc(inner.authResponse.first_issued_at),
                    idToken = inner.authResponse.id_token,
                    idpId = inner.authResponse.idpId,
                    loginHint = inner.authResponse.login_hint,
                    accessToken = inner.authResponse.access_token,
                    refreshToken = inner.authResponse.refresh_token,
                    tokenType = inner.authResponse.token_type,
                },
                basicProfile = new UniversalGSignIn.BasicProfile () {
                    id = inner.basicProfile.sub,
                    name = inner.basicProfile.name,
                    givenName = inner.basicProfile.given_name,
                    familyName = inner.basicProfile.family_name,
                    imageUrl = inner.basicProfile.picture,
                    email = inner.basicProfile.email,
                }
            };
        }
        public static void DeleteSavedGoogleUser (string clientId) {
            PlayerPrefs.DeleteKey (clientId);
        }
        [Serializable]
        class InnerAuthResponse {
            public long expires_at;
            public int expires_in;
            public long first_issued_at;
            public string id_token;
            public string idpId;
            public string login_hint;
            public string access_token;
            public string refresh_token;
            public string token_type;
            public string error;
        }
        [Serializable]
        class InnerBasicProfile {
            public string sub;
            public string name;
            public string given_name;
            public string family_name;
            public string picture;
            public string email;
            public string error;
        }
        [Serializable]
        class InnerGoogleUser {
            public InnerAuthResponse authResponse;
            public InnerBasicProfile basicProfile;
        }
        public static Dictionary<string, string> ParseQueryString (string url) {
            var result = new Dictionary<string, string> ();
            foreach (Match match in Regex.Matches (url, @"(?<key>\w+)=(?<value>[^&#]+)")) {
                result.Add (match.Groups["key"].Value, Uri.UnescapeDataString (match.Groups["value"].Value));
            }
            return result;
        }

        /// <summary>
        /// Encodes and Decodes strings using Base64 URL encoding.
        /// https://jonathancrozier.com/blog/base64-url-encoding-using-c-sharp
        /// </summary>
        public static class Base64UrlEncoder {
            #region Constants

            private const char Base64Character62 = '+';
            private const char Base64Character63 = '/';
            private const string Base64DoublePadCharacter = "==";
            private const char Base64PadCharacter = '=';
            private const char Base64UrlCharacter62 = '-';
            private const char Base64UrlCharacter63 = '_';

            #endregion

            #region Methods

            /// <summary>
            /// Converts the specified Base64 URL encoded string to a UTF8 string.
            /// </summary>
            /// <param name="s">The Base64 URL encoded string to convert</param>
            /// <returns>A UTF8 string</returns>
            public static string Decode (string s) {
                return Encoding.UTF8.GetString (DecodeBytes (s));
            }

            /// <summary>
            /// Converts the specified Base64 URL encoded string to a byte array.</summary>
            /// <param name="s">The Base64 URL encoded string to convert</param>
            /// <returns>A byte array</returns>
            public static byte[] DecodeBytes (string s) {
                if (s == null) throw new ArgumentNullException (nameof (s));

                // Replace - with +
                s = s.Replace (Base64UrlCharacter62, Base64Character62);

                // Replace _ with /
                s = s.Replace (Base64UrlCharacter63, Base64Character63);

                // Check padding.
                switch (s.Length % 4) {
                    case 0: // No pad characters.
                        break;
                    case 2: // Two pad characters.
                        s += Base64DoublePadCharacter;
                        break;
                    case 3: // One pad character.
                        s += Base64PadCharacter;
                        break;
                    default:
                        throw new FormatException ("Invalid Base64 URL encoding.");
                }

                return Convert.FromBase64String (s);
            }

            /// <summary>
            /// Converts the specified UTF8 string into a Base64 URL encoded string.
            /// </summary>
            /// <param name="s">The UTF8 string to convert</param>
            /// <returns>A Base64 URL encoded string</returns>
            public static string Encode (string s) {
                if (s == null) throw new ArgumentNullException (nameof (s));

                return Encode (Encoding.UTF8.GetBytes (s));
            }

            /// <summary>
            /// Converts the specified byte array to a Base64 URL encoded string.
            /// </summary>
            /// <param name="bytes">The byte array to convert</param>
            /// <returns>A Base64 URL encoded string</returns>
            public static string Encode (byte[] bytes) {
                if (bytes == null) throw new ArgumentNullException (nameof (bytes));

                string s = Convert.ToBase64String (bytes, 0, bytes.Length);
                s = s.Split (Base64PadCharacter)[0];                     // Remove trailing padding i.e. = or ==
                s = s.Replace (Base64Character62, Base64UrlCharacter62); // Replace + with -
                s = s.Replace (Base64Character63, Base64UrlCharacter63); // Replace / with _

                return s;
            }

            /// <summary>
            /// Converts the specified byte array to a Base64 URL encoded string.
            /// </summary>
            /// <param name="bytes">The byte array to convert</param>
            /// <param name="offset">The byte array offset</param>
            /// <param name="length">The number of elements in the byte array to convert</param>
            /// <returns>A Base64 URL encoded string</returns>
            public static string Encode (byte[] bytes, int offset, int length) {
                if (bytes == null) throw new ArgumentNullException (nameof (bytes));

                string s = Convert.ToBase64String (bytes, offset, length);
                s = s.Split (Base64PadCharacter)[0];                     // Remove trailing padding i.e. = or ==
                s = s.Replace (Base64Character62, Base64UrlCharacter62); // Replace + with -
                s = s.Replace (Base64Character63, Base64UrlCharacter63); // Replace / with _

                return s;
            }

            #endregion
        }
    }
}