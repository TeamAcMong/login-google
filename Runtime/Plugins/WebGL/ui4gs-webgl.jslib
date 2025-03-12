mergeInto(LibraryManager.library, {
  InitGoolgeSignInJS: function (client_id, onInited) {
    var gScript = document.createElement("script");
    gScript.src = "https://apis.google.com/js/platform.js?onload=start";
    document.head.append(gScript);
    var client_id = UTF8ToString(client_id);
    window.start = function () {
      window.gapi.load('auth2', function() {
        window.auth2 = window.gapi.auth2.init({
          client_id: client_id,
          cookiepolicy: 'single_host_origin',
          plugin_name: "unity_google_signin"
        });
        window.auth2.then(function () {
            {{{ makeDynCall('v', 'onInited') }}} ();
        });
      });
    }
  },
  GrantOfflineAccessJS: function (callback) {
    if (window.auth2) {
      window.auth2.grantOfflineAccess().then(function (auth_results) {
        var buffer = stringToNewUTF8(JSON.stringify(auth_results));
        {{{ makeDynCall('vi', 'callback') }}} (buffer);
        _free(buffer);
      }).catch(function (results) {
        var buffer = stringToNewUTF8(JSON.stringify(results));
        {{{ makeDynCall('vi', 'callback') }}} (buffer);
        _free(buffer);
      });
    } else {
        var buffer = stringToNewUTF8(JSON.stringify({ error: "not_initialized" }));
        {{{ makeDynCall('vi', 'callback') }}} (buffer);
        _free(buffer);
    }
  },
  SignInJS: function (callback) {
    if (window.auth2) {
      window.auth2.signIn().then(function (auth_results) {
        var buffer = stringToNewUTF8(JSON.stringify({ error: null }));
        {{{ makeDynCall('vi', 'callback') }}} (buffer);
        _free(buffer);
      }).catch(function (results) {
        var buffer = stringToNewUTF8(JSON.stringify(results));
        {{{ makeDynCall('vi', 'callback') }}} (buffer);
        _free(buffer);
      });
    } else {
        var buffer = stringToNewUTF8(JSON.stringify({ error: "not_initialized" }));
        {{{ makeDynCall('vi', 'callback') }}} (buffer);
        _free(buffer);
    }
  },
  SignOutJS: function (callback) {
    if (window.auth2) {
      window.auth2.signOut();
    }
  },
  DisconnectJS: function (callback) {
    if (window.auth2) {
      window.auth2.disconnect();
    }
  },
  IsSignedInJS: function () {
    return window.auth2 && window.auth2.isSignedIn.get();
  },
  GetCurrentUserBasicProfileJS: function () {
    if (window.auth2) {
      var profile = window.auth2.currentUser.get().getBasicProfile();
      if (profile) {
        return stringToNewUTF8(JSON.stringify({ 
          id:  profile.getId(),
          name: profile.getName(),
          givenName: profile.getGivenName(),
          familyName: profile.getFamilyName(),
          imageUrl: profile.getImageUrl(),
          email: profile.getEmail()
        }));
      } else {
        return stringToNewUTF8(JSON.stringify({ error: "no_user" }));
      }
    }
    return stringToNewUTF8(JSON.stringify({ error: "not_initialized" }));
  },
  GetCurrentUserAuthResponseJS: function () {
    if (window.auth2) {
      if (window.auth2.currentUser.get().isSignedIn())
        return stringToNewUTF8(JSON.stringify(window.auth2.currentUser.get().getAuthResponse()));
      else
        return stringToNewUTF8(JSON.stringify({ error: "no_user" }));
    } else {
      return stringToNewUTF8(JSON.stringify({ error: "not_initialized" }));
    }
  }
});