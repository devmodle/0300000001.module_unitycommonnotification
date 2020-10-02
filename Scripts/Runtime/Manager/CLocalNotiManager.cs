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
		public AuthorizationOption m_eNotiOptions;
#elif UNITY_ANDROID
		public Importance m_eImportance;

		public string m_oGroupID;
		public string m_oGroupName;
		public string m_oGroupDesc;
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
		CFunc.ShowLog("CLocalNotiManager.Init", KCDefine.B_LOG_COLOR_PLUGIN);

		// 초기화가 필요 없을 경우
		if(this.IsInit || !CAccess.IsMobile()) {
			a_oCallback?.Invoke(this, this.IsInit);
		} else {
			m_oInitCallback = a_oCallback;

#if UNITY_IOS
			var oRequest = new AuthorizationRequest(a_stParams.m_eNotiOptions, false);

			float fDeltaTime = KCDefine.U_DELTA_TIME_LOCAL_NM_REQUEST_CHECK;
			float fMaxDeltaTime = KCDefine.U_MAX_DELTA_TIME_LOCAL_NM_REQUEST_CHECK;

			this.ExRepeatCallFunc(fDeltaTime, fMaxDeltaTime, (a_oComponent, a_oParams, a_bIsComplete) => {
				// 요청이 완료 되었을 경우
				if(a_bIsComplete) {
					this.OnInit();
				}

				return !oRequest.ExIsCompleteRequest();
			});
#elif UNITY_ANDROID
			var oNotiChannel = new AndroidNotificationChannel(a_stParams.m_oGroupID,
				a_stParams.m_oGroupName, a_stParams.m_oGroupDesc, a_stParams.m_eImportance);
			
			AndroidNotificationCenter.RegisterNotificationChannel(oNotiChannel);
			this.OnInit();
#endif			// #if UNITY_IOS
		}
	}

	//! 로컬 알림을 추가한다
	public void AddLocalNoti(string a_oKey, STLocalNotiInfo a_stNotiInfo) {
		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			var oNoti = new iOSNotification() {
				Identifier = a_oKey,
				Title = a_stNotiInfo.m_oTitle,
				Body = a_stNotiInfo.m_oMsg
			};

			iOSNotificationCenter.ScheduleNotification(oNoti);
#elif UNITY_ANDROID
			var oNoti = new AndroidNotification(a_stNotiInfo.m_oTitle, 
				a_stNotiInfo.m_oMsg, a_stNotiInfo.m_stNotiTime);
				
			AndroidNotificationCenter.SendNotificationWithExplicitID(oNoti, a_oKey, a_stNotiInfo.m_nID);
#endif			// #if UNITY_IOS
		}
	}

	//! 로컬 알림을 제거한다
	public void RemoveLocalNoti(string a_oKey) {
		CAccess.Assert(a_oKey.ExIsValid());

		// 초기화 되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.RemoveScheduledNotification(a_oKey);
			iOSNotificationCenter.RemoveDeliveredNotification(a_oKey);
#elif UNITY_ANDROID
			int nID = 0;
			CAccess.Assert(int.TryParse(a_oKey, out nID));

			AndroidNotificationCenter.CancelNotification(nID);
			AndroidNotificationCenter.CancelScheduledNotification(nID);
			AndroidNotificationCenter.CancelDisplayedNotification(nID);
#endif			// #if UNITY_IOS
		}
	}
	#endregion			// 함수

	#region 조건부 함수
#if UNITY_IOS || UNITY_ANDROID
	//! 초기화 되었을 경우
	private void OnInit() {
		this.IsInit = true;
		m_oInitCallback?.Invoke(this, true);
	}
#endif			// #if UNITY_IOS || UNITY_ANDROID
	#endregion			// 조건부 함수
}
#endif			// #if LOCAL_NOTI_MODULE_ENABLE
