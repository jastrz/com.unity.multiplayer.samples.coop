using Unity.BossRoom.UnityServices.Lobbies;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the
    /// ClientReconnecting state if no reason is given, or to the Offline state.
    /// </summary>
    class ClientConnectedState : OnlineState
    {
        [Inject]
        protected LobbyServiceFacade m_LobbyServiceFacade;

        public override void Enter()
        {
            if (m_LobbyServiceFacade.CurrentUnityLobby != null)
            {
                m_LobbyServiceFacade.BeginTracking();
            }
        }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong _)
        {
            var disconnectReason = m_ConnectionManager.NetworkManager.DisconnectReason;

            try
            {
                Debug.Log($"Disconnected : {disconnectReason}");
                Debug.Log(_);
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                m_ConnectStatusPublisher.Publish(connectStatus);
                switch (connectStatus)
                {
                    case ConnectStatus.HostEndedSession:
                    case ConnectStatus.GenericDisconnect:
                        m_ConnectionManager.ChangeState(m_ConnectionManager.m_HostMigration);
                        break;
                    default:
                        m_ConnectionManager.ChangeState(m_ConnectionManager.m_ClientReconnecting);
                        break;
                }
            }
            catch
            {

                m_ConnectionManager.ChangeState(m_ConnectionManager.m_HostMigration);
            }

        }
    }
}
