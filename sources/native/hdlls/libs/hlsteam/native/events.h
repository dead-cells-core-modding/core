// common
EVENT_DECL( PersonaChange, PersonaStateChange_t )
EVENT_DECL( OverlayActivated, GameOverlayActivated_t )
EVENT_DECL( AuthSessionTicketResponse, GetAuthSessionTicketResponse_t )

// matchmaking
EVENT_DECL( LobbyData, LobbyDataUpdate_t )
EVENT_DECL( LobbyChatUpdate, LobbyChatUpdate_t )
EVENT_DECL( LobbyChatMsg, LobbyChatMsg_t )
EVENT_DECL( LobbyJoinRequest, GameLobbyJoinRequested_t )

// networking
EVENT_DECL( P2PSessionRequest, P2PSessionRequest_t )
EVENT_DECL( P2PSessionConnectionFail, P2PSessionConnectFail_t )

// ugc
EVENT_DECL(DownloadItem, DownloadItemResult_t)
EVENT_DECL(ItemInstalled, ItemInstalled_t)

// -------------- OLD EVENTS -----------------------
#ifndef GLOBAL_EVENTS

// stats
EVENT_DECL(UserStatsReceived, UserStatsReceived_t )
EVENT_DECL(UserStatsStored, UserStatsStored_t)
EVENT_DECL(AchievementStored, UserAchievementStored_t )

// gamepad
EVENT_DECL(GamepadTextInputDismissed, GamepadTextInputDismissed_t)

#endif // GLOBAL_EVENTS

// end
#undef EVENT_DECL
