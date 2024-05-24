using System.Threading.Tasks;
using Unity.BossRoom.UnityServices.Lobbies;
using Unity.Multiplayer.Samples.Utilities;
using UnityEngine.SceneManagement;
using UUnity.BossRoom.ConnectionManagement;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    class ClientHostMigrationState : OfflineState
    {
        const string k_LobbySceneName = "CharSelect";
        readonly int waitTime = 500;

        [Inject]
        protected LocalLobbyUser m_LocalUser;

        public override async void Enter()
        {
            if (SceneManager.GetActiveScene().name != k_LobbySceneName)
            {
                SceneLoaderWrapper.Instance.LoadScene(k_LobbySceneName, useNetworkSceneManager: false);
            }

            // Start new relay connections for host and clients
            await HandleHostMigration();
        }

        async Task HandleHostMigration()
        {
            // Lobby needs some time to select new host...
            while (m_LobbyServiceFacade.HostChanged == false) await Task.Delay(waitTime);

            if (m_LobbyServiceFacade.CurrentUnityLobby.HostId == m_LocalUser.ID)
            {
                m_LocalUser.IsHost = true;
                StartHostLobby(m_LocalUser.DisplayName);
            }
            else
            {
                bool waitingForNewRelayCode = true;

                do
                {
                    // Try to reconnect when host allocated new connection and new join code has been propagated to the lobby
                    if (m_LobbyServiceFacade.CurrentUnityLobby.Data["RelayJoinCode"].Value != m_LobbyServiceFacade.RelayCodeUsedForConnection)
                    {
                        m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientReconnecting);
                        waitingForNewRelayCode = false;
                    }
                    else
                    {
                        await Task.Delay(waitTime);
                    }

                } while (waitingForNewRelayCode);
            }

            m_LobbyServiceFacade.EndMigration();
        }

        public override void Exit()
        {

        }
    }
}
