using System.Collections;
using System.Threading.Tasks;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UUnity.BossRoom.ConnectionManagement;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// This is the connection state that occurs when a host player disconnects from the Relay server, 
    /// causing the remaining players to also disconnect. Despite the disconnection, the lobby remains intact. 
    /// During this state, a new host is automatically selected by the lobby, which then initiates a new allocation process. 
    /// The new join code is then communicated to the other clients through the lobby. Once a client receives the new code, 
    /// they can reconnect to the server.
    /// </summary>
    class ClientHostMigrationState : OfflineState
    {
        const string k_LobbySceneName = "CharSelect";

        [Inject]
        protected LocalLobbyUser m_LocalUser;

        public override void Enter()
        {
            if (SceneManager.GetActiveScene().name != k_LobbySceneName)
            {
                SceneLoaderWrapper.Instance.LoadScene(k_LobbySceneName, useNetworkSceneManager: false);
            }

            // Start new relay connections for host and clients
            m_ConnectionManager.StartCoroutine(HostMigrationCoroutine());
        }

        IEnumerator HostMigrationCoroutine()
        {
            // Lobby needs some time to select new host...
            yield return new WaitUntil(() => m_LobbyServiceFacade.HostChanged);
            if (m_LobbyServiceFacade.CurrentUnityLobby.HostId == m_LocalUser.ID)
            {
                StartHostLobby(m_LocalUser.DisplayName);
            }
            else
            {
                // Wait until relay join code has been updated
                yield return new WaitUntil(() => m_LobbyServiceFacade.CurrentUnityLobby.Data["RelayJoinCode"].Value != m_LobbyServiceFacade.RelayJoinCodeUsedForConnection);

                // Try to reconnect
                m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientReconnecting);
            }
            m_LobbyServiceFacade.EndMigration();
        }

        public override void Exit()
        {

        }
    }
}
