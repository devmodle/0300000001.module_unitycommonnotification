using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

#if NOTI_MODULE_ENABLE
#if UNITY_IOS
using Unity.Notifications.iOS;
#elif UNITY_ANDROID
using Unity.Notifications.Android;
#endif			// #if UNITY_IOS

/** 알림 관리자 */
public partial class CNotiManager : CSingleton<CNotiManager> {
	/** 콜백 */
	public enum ECallback {
		NONE = -1,
		INIT,
		[HideInInspector] MAX_VAL
	}

	/** 매개 변수 */
	public struct STParams {
#if UNITY_IOS
		public PresentationOption m_ePresentOpts;
		public AuthorizationOption m_eAuthorizationOpts;
#elif UNITY_ANDROID
		public Importance m_eImportance;
#endif			// #if UNITY_IOS

		public Dictionary<ECallback, System.Action<CNotiManager, bool>> m_oCallbackDict;
	}

	#region 변수
	private STParams m_stParams;

#if UNITY_ANDROID
	private List<string> m_oNotiGroupIDList = new List<string>();
#endif			// #if UNITY_ANDROID
	#endregion			// 변수

	#region 프로퍼티
	public bool IsInit { get; private set; } = false;
	#endregion			// 프로퍼티

	#region 함수
	/** 초기화 */
	public override void Awake() {
		base.Awake();

#if UNITY_ANDROID && (MSG_PACK_ENABLE || NEWTON_SOFT_JSON_MODULE_ENABLE)
		// 알림 그룹 식별자 파일이 존재 할 경우
		if(File.Exists(KCDefine.U_DATA_P_NOTI_GROUP_IDS)) {
			m_oNotiGroupIDList = this.LoadNotiGroupIDs();
		}
#endif			// #if UNITY_ANDROID && (MSG_PACK_ENABLE || NEWTON_SOFT_JSON_MODULE_ENABLE)
	}

	/** 초기화 */
	public virtual void Init(STParams a_stParams) {
		CFunc.ShowLog("CNotiManager.Init", KCDefine.B_LOG_COLOR_PLUGIN);

#if UNITY_IOS
		CAccess.Assert(a_stParams.m_eAuthorizationOpts.ExIsValidAuthOpts());
#elif UNITY_ANDROID
		CAccess.Assert(a_stParams.m_eImportance != Importance.None);
#endif			// #if UNITY_IOS

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
			a_stParams.m_oCallbackDict?.GetValueOrDefault(ECallback.INIT)?.Invoke(this, true);
		} else {
			m_stParams = a_stParams;

#if UNITY_IOS
			var oRequest = new AuthorizationRequest(a_stParams.m_eAuthorizationOpts, false);

			this.ExRepeatCallFunc((a_oSender, a_bIsComplete) => {
				// 완료 되었을 경우
				if(a_bIsComplete) {
					this.OnInit();
				}
				
				return oRequest != null && !oRequest.IsFinished;
			}, KCDefine.U_DELTA_T_NOTI_M_REQUEST_CHECK, KCDefine.U_MAX_DELTA_T_NOTI_M_REQUEST_CHECK);
#else
			// 알림 그룹이 없을 경우
			if(!m_oNotiGroupIDList.Contains(KCDefine.U_GROUP_ID_NOTI)) {
				this.AddNotiGroup(KCDefine.U_GROUP_ID_NOTI, KCDefine.U_GROUP_N_NOTI, KCDefine.U_GROUP_DESC_NOTI, a_stParams.m_eImportance);
			}

			this.ExLateCallFunc((a_oSender) => this.OnInit());
#endif			// #if UNITY_IOS
		}
#else
		a_stParams.m_oCallbackDict?.GetValueOrDefault(ECallback.INIT)?.Invoke(this, false);
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}

	/** 알림을 추가한다 */
	public void AddNoti(string a_oKey, STNotiInfo a_stNotiInfo) {
		this.AddNoti(a_oKey, KCDefine.U_GROUP_ID_NOTI, a_stNotiInfo);
	}

	/** 알림을 추가한다 */
	public void AddNoti(string a_oKey, string a_oGroupID, STNotiInfo a_stNotiInfo) {
		CFunc.ShowLog($"CNotiManager.AddNoti: {a_oKey}, {a_oGroupID}, {a_stNotiInfo.m_oMsg}, {a_stNotiInfo.m_stNotiTime}", KCDefine.B_LOG_COLOR_PLUGIN);
		CAccess.Assert(a_oKey.ExIsValid() && a_oGroupID.ExIsValid());

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.ScheduleNotification(new iOSNotification() {
				Title = a_stNotiInfo.m_oTitle,
				Subtitle = a_stNotiInfo.m_oSubTitle,
				Body = a_stNotiInfo.m_oMsg,

				Identifier = a_oKey,
				CategoryIdentifier = a_oGroupID,
				ThreadIdentifier = $"{Thread.CurrentThread.ManagedThreadId}",

				Trigger = this.CreateNotiTrigger(a_stNotiInfo),
				ShowInForeground = a_stNotiInfo.m_bIsShowForeground,
				ForegroundPresentationOption = m_stParams.m_ePresentOpts
			});
#else
			var oNoti = new AndroidNotification(a_stNotiInfo.m_oTitle, a_stNotiInfo.m_oMsg, a_stNotiInfo.m_stNotiTime);
			oNoti.RepeatInterval = a_stNotiInfo.m_bIsRepeat ? new System.TimeSpan(KCDefine.B_VAL_1_INT, KCDefine.B_VAL_0_INT, KCDefine.B_VAL_0_INT, KCDefine.B_VAL_0_INT) : null;

			AndroidNotificationCenter.SendNotificationWithExplicitID(oNoti, a_oGroupID, this.MakeNotiID(a_oKey));
#endif			// #if UNITY_IOS
		}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}

	/** 알림을 제거한다 */
	public void RemoveNoti(string a_oKey) {
		CFunc.ShowLog($"CNotiManager.RemoveNoti: {a_oKey}", KCDefine.B_LOG_COLOR_PLUGIN);
		CAccess.Assert(a_oKey.ExIsValid());

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.RemoveScheduledNotification(a_oKey);
#else
			AndroidNotificationCenter.CancelScheduledNotification(this.MakeNotiID(a_oKey));
#endif			// #if UNITY_IOS
		}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}
	#endregion			// 함수

	#region 조건부 함수
#if UNITY_IOS || UNITY_ANDROID
	// 초기화 되었을 경우
	private void OnInit() {
		CFunc.ShowLog("CNotiManager.OnInit");

		CScheduleManager.Inst.AddCallback(KCDefine.U_KEY_NOTI_M_INIT_CALLBACK, () => {
#if UNITY_IOS
			iOSNotificationCenter.RemoveAllDeliveredNotifications();
#else
			AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif			// #if UNITY_IOS

			this.IsInit = true;
			m_stParams.m_oCallbackDict?.GetValueOrDefault(ECallback.INIT)?.Invoke(this, true);
		});
	}

#if UNITY_IOS
	/** 알림 발생자를 생성한다 */
	private iOSNotificationTrigger CreateNotiTrigger(STNotiInfo a_stNotiInfo) {
		var stDeltaTime = a_stNotiInfo.m_stNotiTime - System.DateTime.Now;
		CAccess.Assert(stDeltaTime.ExIsValid());

		return new iOSNotificationTimeIntervalTrigger() {
			Repeats = a_stNotiInfo.m_bIsRepeat, TimeInterval = stDeltaTime
		};
	}
#endif			// #if UNITY_IOS

#if UNITY_ANDROID
	/** 알림 그룹을 추가한다 */
	private void AddNotiGroup(string a_oID, string a_oName, string a_oDesc, Importance a_eImportance = Importance.Low) {
		CFunc.ShowLog($"CNotiManager.AddNotiGroup: {a_oID}, {a_oName}, {a_oDesc}, {a_eImportance}", KCDefine.B_LOG_COLOR_PLUGIN);
		CAccess.Assert(a_oID.ExIsValid());

		// 그룹이 없을 경우
		if(this.IsInit && !m_oNotiGroupIDList.Contains(a_oID)) {
			this.AddNotiGroupID(a_oID);
			AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(a_oID, a_oName, a_oDesc, a_eImportance));
		}
	}

	/** 알림 그룹을 제거한다 */
	private void RemoveNotiGroup(string a_oID) {
		CFunc.ShowLog($"CNotiManager.RemoveNotiGroup: {a_oID}");
		CAccess.Assert(a_oID.ExIsValid());

		// 그룹이 존재 할 경우
		if(this.IsInit && m_oNotiGroupIDList.Contains(a_oID)) {
			this.RemoveNotiGroupID(a_oID);
			AndroidNotificationCenter.DeleteNotificationChannel(a_oID);
		}
	}

	/** 알림 그룹 식별자를 추가한다 */
	private void AddNotiGroupID(string a_oID) {
		m_oNotiGroupIDList.ExAddVal(a_oID);
		this.SaveNotiGroupIDs(m_oNotiGroupIDList);
	}

	/** 알림 그룹 식별자를 제거한다 */
	private void RemoveNotiGroupID(string a_oID) {
		m_oNotiGroupIDList.ExRemoveVal(a_oID);
		this.SaveNotiGroupIDs(m_oNotiGroupIDList);
	}

	/** 알림 그룹 식별자를 로드한다 */
	private List<string> LoadNotiGroupIDs() {
#if MSG_PACK_ENABLE
		return CFunc.ReadMsgPackObj<List<string>>(KCDefine.U_DATA_P_NOTI_GROUP_IDS);
#elif NEWTON_SOFT_JSON_MODULE_ENABLE
		return CFunc.ReadJSONObj<List<string>>(KCDefine.U_DATA_P_NOTI_GROUP_IDS);
#else
		return null;
#endif			// #if MSG_PACK_ENABLE
	}

	/** 알림 그룹 식별자를 저장한다 */
	private void SaveNotiGroupIDs(List<string> a_oNotiGroupIDList) {
#if MSG_PACK_ENABLE
		CFunc.WriteMsgPackObj<List<string>>(KCDefine.U_DATA_P_NOTI_GROUP_IDS, a_oNotiGroupIDList);
#elif NEWTON_SOFT_JSON_MODULE_ENABLE
		CFunc.WriteJSONObj<List<string>>(KCDefine.U_DATA_P_NOTI_GROUP_IDS, a_oNotiGroupIDList);
#endif			// #if MSG_PACK_ENABLE	
	}

	/** 알림 식별자를 생성한다 */
	private int MakeNotiID(string a_oKey) {
		int nID = KCDefine.B_VAL_0_INT;
		CAccess.Assert(int.TryParse(a_oKey, out nID));

		return nID;
	}
#endif			// #if UNITY_ANDROID
#endif			// #if UNITY_IOS || UNITY_ANDROID
	#endregion			// 조건부 함수
}
#endif			// #if NOTI_MODULE_ENABLE
