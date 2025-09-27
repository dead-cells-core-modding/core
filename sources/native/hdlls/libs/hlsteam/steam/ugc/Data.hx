package steam.ugc;

enum abstract EFileType(Int) from Int to Int {
	var Community              =  0;	// normal Workshop item that can be subscribed to
	var Microtransaction       =  1;	// Workshop item that is meant to be voted on for the purpose of selling in-game
	var Collection             =  2;	// a collection of Workshop or Greenlight items
	var Art                    =  3;	// artwork
	var Video                  =  4;	// external video
	var Screenshot             =  5;	// screenshot
	var Game                   =  6;	// Greenlight game entry
	var Software               =  7;	// Greenlight software entry
	var Concept                =  8;	// Greenlight concept
	var WebGuide               =  9;	// Steam web guide
	var IntegratedGuide        = 10;	// application integrated guide
	var Merch                  = 11;	// Workshop merchandise meant to be voted on for the purpose of being sold
	var ControllerBinding      = 12;	// Steam Controller bindings
	var SteamWorksAccessInvite = 13;	// internal
	var SteamVideo             = 14;	// Steam video
	var GameManagedItem        = 15;	// managed completely by the game, not the user, and not shown on the web
}

enum abstract EQueryType(Int) from Int to Int {
	var RankedByVote                                  = 0;
	var RankedByPublicationDate                       = 1;
	var AcceptedForGameRankedByAcceptanceDate         = 2;
	var RankedByTrend                                 = 3;
	var FavoritedByFriendsRankedByPublicationDate     = 4;
	var CreatedByFriendsRankedByPublicationDate       = 5;
	var RankedByNumTimesReported                      = 6;
	var CreatedByFollowedUsersRankedByPublicationDate = 7;
	var NotYetRated                                   = 8;
	var RankedByTotalVotesAsc                         = 9;
	var RankedByVotesUp                               = 10;
	var RankedByTextSearch                            = 11;
	var RankedByTotalUniqueSubscriptions              = 12;
	var RankedByPlaytimeTrend                         = 13;
	var RankedByTotalPlaytime                         = 14;
	var RankedByAveragePlaytimeTrend                  = 15;
	var RankedByLifetimeAveragePlaytime               = 16;
	var RankedByPlaytimeSessionsTrend                 = 17;
	var RankedByLifetimePlaytimeSessions              = 18;
}

enum abstract EMatchingType(Int) from Int to Int {
	/**both mtx items and ready-to-use items**/
	var Items:Int			= 0;
	var Items_Mtx:Int		= 1;
	var Items_ReadyToUse	= 2;
	var Collections:Int		= 3;
	var Artwork:Int			= 4;
	var Videos:Int			= 5;
	var Screenshots:Int		= 6;
	/**both web quides and integrated guides**/
	var AllGuides:Int		= 7;
	var WebGuides:Int		= 8;
	var IntegratedGuides	= 9;
	/**ready-to-use items and integrated guides**/
	var UsableInGame:Int	= 10;
	var ControllerBindings	= 11;
	/**game managed items (not managed by users)**/
	var GameManagedItems	= 12;
	/**return everything**/
	var All:Int				= ~0;
}

enum abstract EResult(Int) from Int to Int {
	var OK = 1;                             // success
	var Fail = 2;                           // generic failure
	var NoConnection = 3;                   // no/failed network connection
	//var NoConnectionRetry = 4;            // OBSOLETE - removed
	var InvalidPassword = 5;                // password/ticket is invalid
	var LoggedInElsewhere = 6;              // same user logged in elsewhere
	var InvalidProtocolVer = 7;             // protocol version is incorrect
	var InvalidParam = 8;                   // a parameter is incorrect
	var FileNotFound = 9;                   // file was not found
	var Busy = 10;                          // called method busy - action not taken
	var InvalidState = 11;                  // called object was in an invalid state
	var InvalidName = 12;                   // name is invalid
	var InvalidEmail = 13;                  // email is invalid
	var DuplicateName = 14;                 // name is not unique
	var AccessDenied = 15;                  // access is denied
	var Timeout = 16;                       // operation timed out
	var Banned = 17;                        // VAC2 banned
	var AccountNotFound = 18;               // account not found
	var InvalidSteamID = 19;                // steamID is invalid
	var ServiceUnavailable = 20;            // The requested service is currently unavailable
	var NotLoggedOn = 21;                   // The user is not logged on
	var Pending = 22;                       // Request is pending (may be in process; or waiting on third party)
	var EncryptionFailure = 23;             // Encryption or Decryption failed
	var InsufficientPrivilege = 24;         // Insufficient privilege
	var LimitExceeded = 25;                 // Too much of a good thing
	var Revoked = 26;                       // Access has been revoked (used for revoked guest passes)
	var Expired = 27;                       // License/Guest pass the user is trying to access is expired
	var AlreadyRedeemed = 28;               // Guest pass has already been redeemed by account; cannot be acked again
	var DuplicateRequest = 29;              // The request is a duplicate and the action has already occurred in the past; ignored this time
	var AlreadyOwned = 30;                  // All the games in this guest pass redemption request are already owned by the user
	var IPNotFound = 31;                    // IP address not found
	var PersistFailed = 32;                 // failed to write change to the data store
	var LockingFailed = 33;                 // failed to acquire access lock for this operation
	var LogonSessionReplaced = 34;
	var ConnectFailed = 35;
	var HandshakeFailed = 36;
	var IOFailure = 37;
	var RemoteDisconnect = 38;
	var ShoppingCartNotFound = 39;          // failed to find the shopping cart requested
	var Blocked = 40;                       // a user didn't allow it
	var Ignored = 41;                       // target is ignoring sender
	var NoMatch = 42;                       // nothing matching the request found
	var AccountDisabled = 43;
	var ServiceReadOnly = 44;               // this service is not accepting content changes right now
	var AccountNotFeatured = 45;            // account doesn't have value; so this feature isn't available
	var AdministratorOK = 46;               // allowed to take this action; but only because requester is admin
	var ContentVersion = 47;                // A Version mismatch in content transmitted within the Steam protocol.
	var TryAnotherCM = 48;                  // The current CM can't service the user making a request; user should try another.
	var PasswordRequiredToKickSession = 49; // You are already logged in elsewhere; this cached credential login has failed.
	var AlreadyLoggedInElsewhere = 50;      // You are already logged in elsewhere; you must wait
	var Suspended = 51;                     // Long running operation (content download) suspended/paused
	var Cancelled = 52;                     // Operation canceled (typically by user: content download)
	var DataCorruption = 53;                // Operation canceled because data is ill formed or unrecoverable
	var DiskFull = 54;                      // Operation canceled - not enough disk space.
	var RemoteCallFailed = 55;              // an remote call or IPC call failed
	var PasswordUnset = 56;                 // Password could not be verified as it's unset server side
	var ExternalAccountUnlinked = 57;       // External account (PSN; Facebook...) is not linked to a Steam account
	var PSNTicketInvalid = 58;              // PSN ticket was invalid
	var ExternalAccountAlreadyLinked = 59;  // External account (PSN; Facebook...) is already linked to some other account; must explicitly request to replace/delete the link first
	var RemoteFileConflict = 60;            // The sync cannot resume due to a conflict between the local and remote files
	var IllegalPassword = 61;               // The requested new password is not legal
	var SameAsPreviousValue = 62;           // new value is the same as the old one ( secret question and answer )
	var AccountLogonDenied = 63;            // account login denied due to 2nd factor authentication failure
	var CannotUseOldPassword = 64;          // The requested new password is not legal
	var InvalidLoginAuthCode = 65;          // account login denied due to auth code invalid
	var AccountLogonDeniedNoMail = 66;      // account login denied due to 2nd factor auth failure - and no mail has been sent
	var HardwareNotCapableOfIPT = 67;       //
	var IPTInitError = 68;                  //
	var ParentalControlRestricted = 69;     // operation failed due to parental control restrictions for current user
	var FacebookQueryError = 70;            // Facebook query returned an error
	var ExpiredLoginAuthCode = 71;          // account login denied due to auth code expired
	var IPLoginRestrictionFailed = 72;
	var AccountLockedDown = 73;
	var AccountLogonDeniedVerifiedEmailRequired = 74;
	var NoMatchingURL = 75;
	var BadResponse = 76;                   // parse failure; missing field; etc.
	var RequirePasswordReEntry = 77;        // The user cannot complete the action until they re-enter their password
	var ValueOutOfRange = 78;               // the value entered is outside the acceptable range
	var UnexpectedError = 79;               // something happened that we didn't expect to ever happen
	var Disabled = 80;                      // The requested service has been configured to be unavailable
	var InvalidCEGSubmission = 81;          // The set of files submitted to the CEG server are not valid !
	var RestrictedDevice = 82;              // The device being used is not allowed to perform this action
	var RegionLocked = 83;                  // The action could not be complete because it is region restricted
	var RateLimitExceeded = 84;             // Temporary rate limit exceeded; try again later; different from var LimitExceeded which may be permanent
	var AccountLoginDeniedNeedTwoFactor = 85; // Need two-factor code to login
	var ItemDeleted = 86;                   // The thing we're trying to access has been deleted
	var AccountLoginDeniedThrottle = 87;    // login attempt failed; try to throttle response to possible attacker
	var TwoFactorCodeMismatch = 88;         // two factor code mismatch
	var TwoFactorActivationCodeMismatch = 89; // activation code for two-factor didn't match
	var AccountAssociatedToMultiplePartners = 90; // account has been associated with multiple partners
	var NotModified = 91;                   // data not modified
	var NoMobileDevice = 92;                // the account does not have a mobile device associated with it
	var TimeNotSynced = 93;                 // the time presented is out of range or tolerance
	var SmsCodeFailed = 94;                 // SMS code failure (no match; none pending; etc.)
	var AccountLimitExceeded = 95;          // Too many accounts access this resource
	var AccountActivityLimitExceeded = 96;  // Too many changes to this account
	var PhoneActivityLimitExceeded = 97;    // Too many changes to this phone
	var RefundToWallet = 98;                // Cannot refund to payment method; must use wallet
	var EmailSendFailure = 99;              // Cannot send an email
	var NotSettled = 100;                   // Can't perform operation till payment has settled
	var NeedCaptcha = 101;                  // Needs to provide a valid captcha
	var GSLTDenied = 102;                   // a game server login token owned by this token's owner has been banned
	var GSOwnerDenied = 103;                // game server owner is denied for other reason (account lock; community ban; vac ban; missing phone)
	var InvalidItemType = 104;              // the type of thing we were requested to act on is invalid
	var IPBanned = 105;                     // the ip address has been banned from taking this action
	var GSLTExpired = 106;                  // this token has expired from disuse; can be reset for use

}

enum abstract EItemVisibility(Int) from Int to Int {
	var Public      = 0;
	var FriendsOnly = 1;
	var Private     = 2;
}


enum abstract EUserList(Int) from Int to Int {
	var Published:Int		= 0;
	var VotedOn:Int			= 1;
	var VotedUp:Int			= 2;
	var VotedDown:Int		= 3;
	var WillVoteLater:Int	= 4;
	var Favorited:Int		= 5;
	var Subscribed:Int		= 6;
	var UsedOrPlayed:Int	= 7;
	var Followed:Int		= 8;
}

enum abstract EUserListSortOrder(Int) from Int to Int {
	var CreationOrderDesc:Int		= 0;
	var CreationOrderAsc:Int		= 1;
	var TitleAsc:Int				= 2;
	var LastUpdatedDesc:Int			= 3;
	var SubscriptionDateDesc:Int	= 4;
	var VoteScoreDesc:Int			= 5;
	var ForModeration:Int			= 6;
}
