using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	private void OnGUI()
	{
		GUILayout.BeginArea(new Rect(10, 10, 300, 300));
		if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
		{
			StartButtons();
		}
		else
		{
			StatusLabels();
		}

		GUILayout.EndArea();
	}

	private void StartButtons()
	{
		if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
		if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
		if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
	}

	private void StatusLabels()
	{
		GUILayout.Label("Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
		GUILayout.Label("Mode: " + (NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client"));
	}
}
