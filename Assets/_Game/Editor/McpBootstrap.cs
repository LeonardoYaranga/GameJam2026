using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NidoCero.Editor
{
    [InitializeOnLoad]
    public static class McpBootstrap
    {
        private const string ConfiguredKey = "NidoCero.MCP.Configured.v10";

        static McpBootstrap()
        {
            ApplyPreferences();
            if (!EditorPrefs.GetBool(ConfiguredKey, false))
                EditorApplication.delayCall += ConfigureDetectedClients;
            if (!Application.isBatchMode)
                EditorApplication.delayCall += StartServerAndBridge;
        }

        [MenuItem("Nido Cero/MCP/Configure All Detected Clients")]
        public static void ConfigureDetectedClients()
        {
            ApplyPreferences();
            ClientConfigurationSummary summary = MCPServiceLocator.Client.ConfigureAllDetectedClients();
            EditorPrefs.SetBool(ConfiguredKey, summary.FailureCount == 0);
            Debug.Log("[NIDO CERO][MCP] " + summary);
        }

        [MenuItem("Nido Cero/MCP/Start HTTP Server And Bridge")]
        public static async void StartServerAndBridge()
        {
            ApplyPreferences();
            bool serverStarted = MCPServiceLocator.Server.StartLocalHttpServer(quiet: true);
            bool bridgeStarted = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log("[NIDO CERO][MCP] Server=" + serverStarted + ", Bridge=" + bridgeStarted +
                      ", URL=http://127.0.0.1:8080/mcp");
        }

        [MenuItem("Nido Cero/MCP/Connect Bridge Only _F10")]
        public static async void ConnectBridgeOnly()
        {
            ApplyPreferences();
            bool bridgeStarted = await MCPServiceLocator.Bridge.StartAsync();
            var verification = await MCPServiceLocator.Bridge.VerifyAsync();
            Debug.Log("[NIDO CERO][MCP] BridgeOnly=" + bridgeStarted + ", Verified=" + verification.Success +
                      ", Detail=" + verification.Message);
        }

        public static void ConfigureForBatch()
        {
            ConfigureDetectedClients();
        }

        public static void StartInteractiveSession()
        {
            ApplyPreferences();
            ConfigureDetectedClients();
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/02_MainScene.unity");
            StartServerAndBridge();
        }

        private static void ApplyPreferences()
        {
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            EditorPrefs.SetString("MCPForUnity.UvxPath", @"C:\Users\Admin\.local\bin\uvx.exe");
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            EditorPrefs.SetBool("MCPForUnity.HttpServerLaunchConfirmed", true);
            EditorPrefs.SetBool("MCPForUnity.SetupCompleted", true);
        }
    }
}
