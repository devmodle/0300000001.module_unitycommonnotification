using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if LOCAL_NOTI_MODULE_ENABLE
#if UNITY_IOS
using Unity.Notifications.iOS;
#elif UNITY_ANDROID
using Unity.Notifications.Android;
#endif			// #if UNITY_IOS

//! 로컬 알림 관리자
public class CLocalNotiManager : CSingleton<CLocalNotiManager> {
	//! 매개 변수
	public struct STParams {
#if UNITY_IOS
		public AuthorizationOption m_eAuthOpts;
#elif UNITY_ANDROID
		public Importance m_eImportance;
#endif			// #if UNITY_IOS
	}

	#region 변수
	private System.Action<CLocalNotiManager, bool> m_oInitCallback = null;
	#endregion			// 변수

	#region 프로퍼티
	public bool IsInit { get; private set; } = false;
	#endregion			// 프로퍼티

	#region 함수
	//! 초기화
	public virtual void Init(STParams a_stParams, System.Action<CLocalNotiManager, bool> a_oCallback) {
#if UNITY_IOS
		CAccess.Assert(a_stParams.m_eAuthOpts.ExIsValid());
#elif UNITY_ANDROID
		CAccess.Assert(a_stParams.m_eImportance != Importance.None);
#endif			// #if UNITY_IOS

		CFunc.ShowLog("CLocalNotiManager.Init", KCDefine.B_LOG_COLOR_PLUGIN);

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
			a_oCallback?.Invoke(this, true);
		} else {
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
			var oNotiChannel = new AndroidNotificationChannel(KCDefine.U_GROUP_ID_LOCAL_NOTI,
				KCDefine.U_GROUP_NAME_LOCAL_NOTI, KCDefine.U_GROUP_DESC_LOCAL_NOTI, a_stParams.m_eImportance);

			AndroidNotificationCenter.RegisterNotificationChannel(oNotiChannel);
			this.OnInit();
#endif			// #if UNITY_IOS
		}
#else
		a_oCallback?.Invoke(this, false);
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}

	//! 로컬 알림을 추가한다
	public void AddLocalNoti(string a_oKey, STLocalNotiInfo a_stNotiInfo) {
		CAccess.Assert(a_oKey.ExIsValid());

		CFunc.ShowLog("CLocalNotiManager.AddLocalNoti: {0}, {1}, {2}", 
			KCDefine.B_LOG_COLOR_PLUGIN, a_oKey, a_stNotiInfo.m_oTitle, a_stNotiInfo.m_oMsg);

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			var oTrigger = new iOSNotificationCalendarTrigger() {
				Repeats = a_stNotiInfo.m_bIsRepeat,

				Year = a_stNotiInfo.m_stNotiTime.Year,
				Month = a_stNotiInfo.m_stNotiTime.Month,
				Day = a_stNotiInfo.m_stNotiTime.Day,

				Hour = a_stNotiInfo.m_stNotiTime.Hour,
				Minute = a_stNotiInfo.m_stNotiTime.Minute,
				Second = a_stNotiInfo.m_stNotiTime.Second
			};

			iOSNotificationCenter.ScheduleNotification(new iOSNotification() {
				Identifier = a_oKey,
				Trigger = oTrigger,

				Title = a_stNotiInfo.m_oTitle,
				Body = a_stNotiInfo.m_oMsg
			});
#else
			var oNoti = new AndroidNotification(a_stNotiInfo.m_oTitle, 
				a_stNotiInfo.m_oMsg, a_stNotiInfo.m_stNotiTime);
				
			// 반복 모드 일 경우
			if(a_stNotiInfo.m_bIsRepeat) {
				oNoti.RepeatInterval = new System.TimeSpan(1, 
					KCDefine.B_ZERO_VALUE_INT, KCDefine.B_ZERO_VALUE_INT, KCDefine.B_ZERO_VALUE_INT);
			}

			int nID = this.MakeLocalNotiID(a_oKey);
			string oGroupID = KCDefine.U_GROUP_ID_LOCAL_NOTI;

			AndroidNotificationCenter.SendNotificationWithExplicitID(oNoti, oGroupID, nID);
#endif			// #if UNITY_IOS
		}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}

	//! 로컬 알림을 제거한다
	public void RemoveLocalNoti(string a_oKey) {
		CAccess.Assert(a_oKey.ExIsValid());

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.RemoveScheduledNotification(a_oKey);
			iOSNotificationCenter.RemoveDeliveredNotification(a_oKey);
#else
			int nID = this.MakeLocalNotiID(a_oKey);			

			AndroidNotificationCenter.CancelNotification(nID);
			AndroidNotificationCenter.CancelScheduledNotification(nID);
			AndroidNotificationCenter.CancelDisplayedNotification(nID);
#endif			// #if UNITY_IOS
		}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	}

	//! 로컬 알림 식별자를 생성한다
	private int MakeLocalNotiID(string a_oKey) {
		int nID = KCDefine.B_ZERO_VALUE_INT;
		CAccess.Assert(int.TryParse(a_oKey, out nID));

		return nID;
	}
	#endregion			// 함수

	#region 조건부 함수
#if UNITY_IOS || UNITY_ANDROID
	//! 초기화 되었을 경우
	private void OnInit() {
		this.IsInit = true;
		m_oInitCallback?.Invoke(this, this.IsInit);
	}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	#endregion			// 조건부 함수
}
#endif			// #if LOCAL_NOTI_MODULE_ENABLE
