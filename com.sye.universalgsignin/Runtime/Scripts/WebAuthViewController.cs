#if UNITY_IOS
using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.iOS;

namespace SyE.UI4GS {
    public static class WebAuthViewController {
        private delegate void AuthCallbackInner ([MarshalAs (UnmanagedType.LPUTF8Str)] string url, [MarshalAs (UnmanagedType.LPUTF8Str)] string error);
        
        [DllImport ("__Internal")]
        static extern void openURL (string url, string callbackScheme, AuthCallbackInner callback);

        [DllImport ("__Internal")]
        static extern void dismiss ();

        public delegate void AuthCallback (string url, string error);
        private static AuthCallback _callback;
        public static void OpenURL (string url, string callbackScheme, AuthCallback callback) {
            if (Version.Parse (Device.systemVersion).Major >= 11) {
                _callback = callback;
                openURL (url, callbackScheme, MessageHandler);
            } else {
                Application.OpenURL (url);
            }
        }

        public static void Close () {
            if (Version.Parse (UnityEngine.iOS.Device.systemVersion).Major >= 11) {
                dismiss ();
            }
        }
        [MonoPInvokeCallback (typeof (AuthCallbackInner))]
        private static void MessageHandler ([MarshalAs (UnmanagedType.LPUTF8Str)] string url, [MarshalAs (UnmanagedType.LPUTF8Str)] string error) {
            _callback?.Invoke (url, error);
        }
    }
}
#endif