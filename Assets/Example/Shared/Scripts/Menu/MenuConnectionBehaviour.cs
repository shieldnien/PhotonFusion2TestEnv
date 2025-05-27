using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Menu;
using Fusion.Photon.Realtime;
using Fusion.Sockets;

#pragma warning disable 1998
#pragma warning disable 4014

namespace Example
{
	public class MenuConnectionBehaviour : FusionMenuConnectionBehaviour
	{
		public MenuUIController MenuUIController;
		public NetworkRunner    MenuRunner;

		public override string       SessionName    => _runner != null && _runner.IsRunning == true ? _runner.SessionInfo.Name : default;
		public override int          MaxPlayerCount => _runner != null && _runner.IsRunning == true ? _runner.SessionInfo.MaxPlayers : default;
		public override string       Region         => _runner != null && _runner.IsRunning == true ? _runner.SessionInfo.Region : default;
		public override string       AppVersion     => PhotonAppSettings.Global.AppSettings.AppVersion;
		public override List<string> Usernames      => default;
		public override bool         IsConnected    => _runner != null ? _runner.IsConnectedToServer : default;
		public override int          Ping           => _runner != null && _runner.IsRunning == true ? Mathf.RoundToInt((float)(_runner.GetPlayerRtt(PlayerRef.None) * 1000.0)) : default;

		private NetworkRunner _runner;

		public override async Task<List<FusionMenuOnlineRegion>> RequestAvailableOnlineRegionsAsync(FusionMenuConnectArgs connectArgs)
		{
			List<FusionMenuOnlineRegion> regions = new List<FusionMenuOnlineRegion>();
			foreach (var region in MenuUIController.Config.AvailableRegions)
			{
				regions.Add(new FusionMenuOnlineRegion { Code = region, Ping = 0 });
			}

			return regions;
		}

		protected override async Task<ConnectResult> ConnectAsyncInternal(FusionMenuConnectArgs connectionArgs)
		{
			if (string.IsNullOrEmpty(PhotonAppSettings.Global.AppSettings.AppIdFusion) == true)
			{
				await MenuUIController.PopupAsync("The Fusion AppId is missing in PhotonAppSettings. Please follow setup instructions before running the game.", "Game not configured");
				MenuUIController.Show<FusionMenuUIMain>();
				return ConnectionFail(ConnectFailReason.UserRequest);
			}

			_runner = CreateRunner();

			var appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
			appSettings.FixedRegion = connectionArgs.Region;

			var startGameArgs = new StartGameArgs()
			{
				SessionName = connectionArgs.Session,
				PlayerCount = connectionArgs.MaxPlayerCount,
				GameMode = GetGameMode(connectionArgs),
				CustomPhotonAppSettings = appSettings
			};

			if (connectionArgs.Creating == false && string.IsNullOrEmpty(connectionArgs.Session) == true)
			{
				startGameArgs.EnableClientSessionCreation = false;

				var randomJoinResult = await StartRunner(startGameArgs);
				if (randomJoinResult.Success)
					return await StartGame(connectionArgs.Scene.SceneName);

				if (randomJoinResult.FailReason == ConnectFailReason.UserRequest)
					return ConnectionFail(randomJoinResult.FailReason);

				connectionArgs.Creating = true;

				_runner = CreateRunner();

				startGameArgs.EnableClientSessionCreation = true;
				startGameArgs.SessionName = MenuUIController.Config.CodeGenerator.Create();
				startGameArgs.GameMode = GetGameMode(connectionArgs);
			}

			var result = await StartRunner(startGameArgs);
			if (result.Success)
				return await StartGame(connectionArgs.Scene.SceneName);

			await DisconnectAsync(result.FailReason);
			return ConnectionFail(result.FailReason);
		}

		protected override async Task DisconnectAsyncInternal(int reason)
		{
			var runner = _runner;
			_runner = null;

			if (runner != null)
			{
				Scene sceneToUnload = default;

				if (runner.IsSceneAuthority == true && runner.TryGetSceneInfo(out NetworkSceneInfo sceneInfo) == true)
				{
					foreach (var sceneRef in sceneInfo.Scenes)
					{
						await runner.UnloadScene(sceneRef);
					}
				}
				else
				{
					sceneToUnload = runner.SceneManager.MainRunnerScene;
				}

				await runner.Shutdown();

				if (sceneToUnload.IsValid() == true && sceneToUnload.isLoaded == true && sceneToUnload != MenuUIController.gameObject.scene)
				{
					SceneManager.SetActiveScene(MenuUIController.gameObject.scene);
					SceneManager.UnloadSceneAsync(sceneToUnload);
				}
			}

			if (reason != ConnectFailReason.UserRequest)
			{
				await MenuUIController.PopupAsync(reason.ToString(), "Disconnected");
			}

			MenuUIController.OnGameStopped();
		}

		private GameMode GetGameMode(FusionMenuConnectArgs connectionArgs)
		{
			if (MenuUIController.SelectedGameMode == GameMode.AutoHostOrClient)
				return connectionArgs.Creating ? GameMode.Host : GameMode.Client;

			return MenuUIController.SelectedGameMode;
		}

		private NetworkRunner CreateRunner()
		{
			var runner = GameObject.Instantiate(MenuRunner);
			runner.ProvideInput = true;
			return runner;
		}

		private async Task<ConnectResult> StartRunner(StartGameArgs args)
		{
			var result = await _runner.StartGame(args);
			return new ConnectResult() { Success = _runner.IsRunning, FailReason = ConnectFailReason.Disconnect };
		}

		private async Task<ConnectResult> StartGame(string sceneName)
		{
			try
			{
				_runner.AddCallbacks(new MenuConnectionCallbacks(MenuUIController, sceneName));
				if (_runner.IsSceneAuthority)
				{
					await _runner.LoadScene(sceneName, LoadSceneMode.Additive, LocalPhysicsMode.None, true);
				}
				MenuUIController.OnGameStarted();
				return ConnectionSuccess();
			}
			catch (ArgumentException e)
			{
				Debug.LogError($"Failed to load scene. {e}.");
				await DisconnectAsync(ConnectFailReason.Disconnect);
				return ConnectionFail(ConnectFailReason.Disconnect);
			}
		}

		private static ConnectResult ConnectionSuccess() => new ConnectResult() { Success = true };
		private static ConnectResult ConnectionFail(int failReason) => new ConnectResult() { FailReason = failReason };

		private class MenuConnectionCallbacks : INetworkRunnerCallbacks
		{
			public readonly MenuUIController Controller;
			public readonly string SceneName;

			public MenuConnectionCallbacks(MenuUIController controller, string sceneName)
			{
				Controller = controller;
				SceneName = sceneName;
			}

			public async void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
			{
				if (shutdownReason == ShutdownReason.DisconnectedByPluginLogic)
				{
					Controller.OnGameStopped();
					Controller.Show<FusionMenuUIMain>();
					Controller.PopupAsync("Disconnected from the server.", "Disconnected");

					if (runner.SceneManager != null)
					{
						if (runner.SceneManager.MainRunnerScene.IsValid() == true)
						{
							SceneRef sceneRef = runner.SceneManager.GetSceneRef(runner.SceneManager.MainRunnerScene.name);
							runner.SceneManager.UnloadScene(sceneRef);
						}
					}
				}
			}

			public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}
			public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) {}
			public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {}
			public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {}
			public void OnInput(NetworkRunner runner, NetworkInput input) {}
			public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}
			public void OnConnectedToServer(NetworkRunner runner) {}
			public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) {}
			public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}
			public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}
			public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}
			public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {}
			public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {}
			public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {}
			public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) {}
			public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) {}
			public void OnSceneLoadStart(NetworkRunner runner) {}
			public void OnSceneLoadDone(NetworkRunner runner) {}
		}
	}
}

