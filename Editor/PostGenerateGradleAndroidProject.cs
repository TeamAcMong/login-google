using System.IO;
using UnityEngine;
using UnityEditor.Android;
namespace SyE.UI4GS.Editor {
    public class PostGenerateGradleAndroidProject : IPostGenerateGradleAndroidProject {
        public int callbackOrder => 0;
        public void OnPostGenerateGradleAndroidProject (string path) {
            string gradleFilePath = Path.Join (path, "build.gradle");
            if (File.Exists (gradleFilePath)) {
                string gradleContent = File.ReadAllText (gradleFilePath);
                if (!gradleContent.Contains ("implementation \"com.google.android.gms:play-services-auth:21.2.0\"")) {
                    string dependenciesBlock = "dependencies {";
                    int dependenciesIndex = gradleContent.IndexOf (dependenciesBlock);
                    if (dependenciesIndex >= 0) {
                        int insertIndex = dependenciesIndex + dependenciesBlock.Length;
                        string dependency = "\n    implementation \"com.google.android.gms:play-services-auth:21.2.0\"";
                        gradleContent = gradleContent.Insert (insertIndex, dependency);
                        File.WriteAllText (gradleFilePath, gradleContent);
                        Debug.Log ("Added implementation \"com.google.android.gms:play-services-auth:21.2.0\" to mainTemplate.gradle");
                    } else {
                        Debug.LogError ("dependencies block not found in build.gradle");
                    }
                } else {
                    Debug.Log ("biometric dependency already exists in build.gradle");
                }
            } else {
                Debug.LogError ("build.gradle file not found: " + gradleFilePath);
            }

            string propertiesFilePath = Path.Combine (path, "../gradle.properties");
            if (File.Exists (propertiesFilePath)) {
                string propertiesContent = File.ReadAllText (propertiesFilePath);
                if (!propertiesContent.Contains ("android.useAndroidX=true")) {
                    propertiesContent += "\nandroid.useAndroidX=true";
                    File.WriteAllText (propertiesFilePath, propertiesContent);
                    Debug.Log ("Added android.useAndroidX=true to gradleTemplate.properties");
                }
            } else {
                Debug.LogError ("gradle.properties file not found: " + propertiesFilePath);
            }
        }
    }

}