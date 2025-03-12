# Login-google
## To set up Google Sign-In for Android in Unity, you will need to create two client IDs:
* Android Client ID – Generated with the correct SHA-1 key.
* Web Client ID – Used in the plugin (the Android client ID will be automatically retrieved).
### This setup will allows you to verify users token id using your web client id.
# Add Packages/manifest.json
* "com.sye.universalgsignin":"https://github.com/TeamAcMong/login-google.git#1.0.2"
