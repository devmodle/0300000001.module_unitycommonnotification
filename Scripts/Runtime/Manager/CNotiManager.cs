using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#if NOTI_MODULE_ENABLE
#if UNITY_IOS
using Unity.Notifications.iOS;
#elif UNITY_ANDROID
using Unity.Notifications.Android;
#endif			// #if UNITY_IOS

//! 알림 관리자
public class CNotiManager : CSingleton<CNotiManager> {
	#region 타입
	//! 매개 변수
	public struct STParams {
#if UNITY_IOS
		public AuthorizationOption m_eAuthOpts;
		public PresentationOption m_ePresentOpts;
#elif UNITY_ANDROID
		public Importance m_eImportance;
#endif			// #if UNITY_IOS
	}
	#endregion			// 타입

	#region 변수
	private STParams m_stParams;
	private System.Action<CNotiManager, bool> m_oInitCallback = null;

#if UNITY_ANDROID
	private List<string> m_oNotiGroupIDList = new List<string>();
#endif			// #if UNITY_ANDROID
	#endregion			// 변수

	#region 프로퍼티
	public bool IsInit { get; private set; } = false;
	#endregion			// 프로퍼티

	#region 함수
	//! 초기화
	public virtual void Init(STParams a_stParams, System.Action<CNotiManager, bool> a_oCallback) {
#if UNITY_IOS
		CAccess.Assert(a_stParams.m_eAuthOpts.ExIsValidAuthOpts());
#elif UNITY_ANDROID
		CAccess.Assert(a_stParams.m_eImportance != Importance.None);
#endif			// #if UNITY_IOS

		CFunc.ShowLog("CNotiManager.Init", KCDefine.B_LOG_COLOR_PLUGIN);
		
#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
			a_oCallback?.Invoke(this, true);
		} else {
			m_stParams = a_stParams;
			m_oInitCallback = a_oCallback;

#if UNITY_IOS
			var oRequest = new AuthorizationRequest(a_stParams.m_eAuthOpts, false);

			this.ExRepeatCallFunc(KCDefine.U_DELTA_TIME_LOCAL_NM_REQUEST_CHECK, 
				KCDefine.U_MAX_DELTA_TIME_LOCAL_NM_REQUEST_CHECK, (a_oComponent, a_oParams, a_bIsComplete) => 
			{
				// 요청이 완료 되었을 경우
				if(a_bIsComplete) {
					this.OnInit();
				}

				return !oRequest.ExIsCompleteRequest();
			});
#else
			this.AddNotiGroup(KCDefine.U_DEF_GROUP_ID_NOTI, 
				KCDefine.U_DEF_GROUP_NAME_NOTI, KCDefine.U_DEF_GROUP_DESC_NOTI, a_stParams.m_eImportance);

			this.ExLateCallFunc((a_oSender, a_oParams) => this.OnInit());
#endif			// #if UNITY_IOS
		}
#else
		a_oCallback?.Invoke(this, false);
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}

	//! 알림을 추가한다
	public void AddNoti(string a_oKey, STNotiInfo a_stNotiInfo) {
		this.AddNoti(a_oKey, KCDefine.U_DEF_GROUP_ID_NOTI, a_stNotiInfo);
	}

	//! 알림을 추가한다
	public void AddNoti(string a_oKey, string a_oGroupID, STNotiInfo a_stNotiInfo) {
		CAccess.Assert(a_oKey.ExIsValid() && a_oGroupID.ExIsValid());

		CFunc.ShowLog("CNotiManager.AddNoti: {0}, {1}, {2}", 
			KCDefine.B_LOG_COLOR_PLUGIN, a_oKey, a_oGroupID, a_stNotiInfo.m_stNotiTime);

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.ScheduleNotification(new iOSNotification() {
				ShowInForeground = a_stNotiInfo.m_bIsShowForeground,
				ForegroundPresentationOption = m_stParams.m_ePresentOpts,

				Title = a_stNotiInfo.m_oTitle,
				Subtitle = a_stNotiInfo.m_oSubTitle,
				Body = a_stNotiInfo.m_oMsg,

				Identifier = a_oKey,
				CategoryIdentifier = a_oGroupID,
				ThreadIdentifier = Thread.CurrentThread.Name,

				Trigger = this.CreateNotiTrigger(a_stNotiInfo)
			});
#else
			var oNoti = new AndroidNotification(a_stNotiInfo.m_oTitle, 
				a_stNotiInfo.m_oMsg, a_stNotiInfo.m_stNotiTime);
				
			// 반복 모드 일 경우
			if(a_stNotiInfo.m_bIsRepeat) {
				oNoti.RepeatInterval = new System.TimeSpan(1, 
					KCDefine.B_VALUE_INT_0, KCDefine.B_VALUE_INT_0, KCDefine.B_VALUE_INT_0);
			}

			int nID = this.MakeNotiID(a_oKey);
			AndroidNotificationCenter.SendNotificationWithExplicitID(oNoti, a_oGroupID, nID);
#endif			// #if UNITY_IOS
		}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}

	//! 알림을 제거한다
	public void RemoveNoti(string a_oKey) {
		CAccess.Assert(a_oKey.ExIsValid());
		CFunc.ShowLog("CNotiManager.RemoveNoti: {0}", KCDefine.B_LOG_COLOR_PLUGIN, a_oKey);

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.RemoveScheduledNotification(a_oKey);
			iOSNotificationCenter.RemoveDeliveredNotification(a_oKey);
#else
			int nID = this.MakeNotiID(a_oKey);			

			AndroidNotificationCenter.CancelNotification(nID);
			AndroidNotificationCenter.CancelScheduledNotification(nID);
			AndroidNotificationCenter.CancelDisplayedNotification(nID);
#endif			// #if UNITY_IOS
		}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}
	#endregion			// 함수

	#region 조건부 함수
#if UNITY_IOS || UNITY_ANDROID
	//! 초기화 되었을 경우
	private void OnInit() {
		this.IsInit = true;
		m_oInitCallback?.Invoke(this, this.IsInit);
	}

#if UNITY_IOS
	//! 알림 발생자를 생성한다
	private iOSNotificationTrigger CreateNotiTrigger(STNotiInfo a_stNotiInfo) {
		var stDeltaTime = a_stNotiInfo.m_stNotiTime - System.DateTime.Now;
		CAccess.Assert(stDeltaTime.ExIsValid());

		return new iOSNotificationTimeIntervalTrigger() {
			Repeats = a_stNotiInfo.m_bIsRepeat,
			TimeInterval = stDeltaTime
		};
	}
#endif			// #if UNITY_IOS

#if UNITY_ANDROID
	//! 알림 그룹을 추가한다
	public void AddNotiGroup(string a_oID, 
		string a_oName, string a_oDesc, Importance a_eImportance = Importance.Low) 
	{
		CAccess.Assert(a_oID.ExIsValid() && !m_oNotiGroupIDList.Contains(a_oID));

		CFunc.ShowLog("CNotiManager.AddNotiGroup: {0}, {1}, {2}. {3}", 
			KCDefine.B_LOG_COLOR_PLUGIN, a_oID, a_oName, a_oDesc, a_eImportance);

		// 초기화 되었을 경우
		if(this.IsInit) {
			var oNotiGroup = new AndroidNotificationChannel(a_oID, 
				a_oName, a_oDesc, a_eImportance);

			m_oNotiGroupIDList.Add(a_oID);
			AndroidNotificationCenter.RegisterNotificationChannel(oNotiGroup);
		}
	}

	//! 알림 식별자를 생성한다
	private int MakeNotiID(string a_oKey) {
		int nID = KCDefine.B_VALUE_INT_0;
		CAccess.Assert(int.TryParse(a_oKey, out nID));

		return nID;
	}
#endif			// #if UNITY_ANDROID
#endif			// #if UNITY_IOS || UNITY_ANDROID
	#endregion			// 조건부 함수
}
#endif			// #if NOTI_MODULE_ENABLE
