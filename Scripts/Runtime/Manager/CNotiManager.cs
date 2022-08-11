using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if NOTI_MODULE_ENABLE
#if UNITY_IOS
using Unity.Notifications.iOS;
#elif UNITY_ANDROID
using Unity.Notifications.Android;
#endif			// #if UNITY_IOS

/** 알림 관리자 */
public partial class CNotiManager : CSingleton<CNotiManager> {
	/** 식별자 */
	private enum EKey {
		NONE = -1,
		IS_INIT,
		[HideInInspector] MAX_VAL
	}

	/** 콜백 */
	public enum ECallback {
		NONE = -1,
		INIT,
		[HideInInspector] MAX_VAL
	}

	/** 매개 변수 */
	public partial struct STParams {
		public Dictionary<ECallback, System.Action<CNotiManager, bool>> m_oCallbackDict;
	}

	#region 변수
	private Dictionary<EKey, bool> m_oBoolDict = new Dictionary<EKey, bool>();
	#endregion			// 변수

	#region 상수
#if UNITY_IOS
	private const PresentationOption OPTS_PRESENTATION = PresentationOption.Alert | PresentationOption.Sound;
	private const AuthorizationOption OPTS_AUTHORIZATION = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
#endif			// #if UNITY_IOS
	#endregion			// 상수

	#region 프로퍼티
	public STParams Params { get; private set; }
	public bool IsInit => m_oBoolDict.GetValueOrDefault(EKey.IS_INIT);
	#endregion			// 프로퍼티

	#region 함수
	/** 초기화 */
	public virtual void Init(STParams a_stParams) {
		CFunc.ShowLog("CNotiManager.Init", KCDefine.B_LOG_COLOR_PLUGIN);

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
		// 초기화 되었을 경우
		if(m_oBoolDict.GetValueOrDefault(EKey.IS_INIT)) {
			a_stParams.m_oCallbackDict?.GetValueOrDefault(ECallback.INIT)?.Invoke(this, m_oBoolDict.GetValueOrDefault(EKey.IS_INIT));
		} else {
			this.Params = a_stParams;

#if UNITY_IOS
			var oRequest = new AuthorizationRequest(OPTS_AUTHORIZATION, false);

			this.ExRepeatCallFunc((a_oSender, a_bIsComplete) => {
				// 완료 되었을 경우
				if(a_bIsComplete) {
					this.OnInit();
				}
				
				return oRequest != null && !oRequest.IsFinished;
			}, KCDefine.U_DELTA_T_NOTI_M_REQUEST_CHECK, KCDefine.U_MAX_DELTA_T_NOTI_M_REQUEST_CHECK);
#else
			AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(Application.identifier, KCDefine.U_GROUP_N_NOTI, KCDefine.U_GROUP_DESC_NOTI, Importance.Default));
			this.ExLateCallFunc((a_oSender) => this.OnInit());
#endif			// #if UNITY_IOS
		}
#else
		a_stParams.m_oCallbackDict?.GetValueOrDefault(ECallback.INIT)?.Invoke(this, false);
#endif			// #if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
	}

	/** 알림을 추가한다 */
	public void AddNoti(string a_oKey, STNotiInfo a_stNotiInfo) {
		CFunc.ShowLog($"CNotiManager.AddNoti: {a_oKey}, {a_stNotiInfo.m_oMsg}, {a_stNotiInfo.m_stNotiTime}", KCDefine.B_LOG_COLOR_PLUGIN);
		CAccess.Assert(a_oKey.ExIsValid());

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 되었을 경우
		if(m_oBoolDict.GetValueOrDefault(EKey.IS_INIT)) {
#if UNITY_IOS
			iOSNotificationCenter.ScheduleNotification(this.MakeiOSNoti(a_oKey, a_stNotiInfo));
#else
			AndroidNotificationCenter.SendNotificationWithExplicitID(this.MakeAndroidNoti(a_stNotiInfo), Application.identifier, this.MakeNotiID(a_oKey));
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
		if(m_oBoolDict.GetValueOrDefault(EKey.IS_INIT)) {
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
		CFunc.ShowLog("CNotiManager.OnInit", KCDefine.B_LOG_COLOR_PLUGIN);

		CScheduleManager.Inst.AddCallback(KCDefine.U_KEY_NOTI_M_INIT_CALLBACK, () => {
#if UNITY_IOS
			iOSNotificationCenter.RemoveAllDeliveredNotifications();
#else
			AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif			// #if UNITY_IOS

			m_oBoolDict.ExReplaceVal(EKey.IS_INIT, true);
			this.Params.m_oCallbackDict?.GetValueOrDefault(ECallback.INIT)?.Invoke(this, true);
		});
	}

	/** 알림 식별자를 생성한다 */
	private int MakeNotiID(string a_oKey) {
		return int.TryParse(a_oKey, NumberStyles.Any, null, out int nNotiID) ? nNotiID : KCDefine.B_VAL_0_INT;
	}

#if UNITY_IOS
	/** iOS 알림을 생성한다 */
	private iOSNotification MakeiOSNoti(string a_oKey, STNotiInfo a_stNotiInfo) {
		return new iOSNotification($"{this.MakeNotiID(a_oKey)}") {
			Title = a_stNotiInfo.m_oTitle,
			Subtitle = a_stNotiInfo.m_oSubTitle,
			Body = a_stNotiInfo.m_oMsg,

			CategoryIdentifier = Application.identifier,
			ThreadIdentifier = $"{Thread.CurrentThread.ManagedThreadId}",

			ShowInForeground = true,
			ForegroundPresentationOption = OPTS_PRESENTATION,

			Trigger = new iOSNotificationCalendarTrigger() {
				UtcTime = true,
				Repeats = a_stNotiInfo.m_bIsRepeat,

				Year = a_stNotiInfo.m_stNotiTime.ToUniversalTime().Year,
				Month = a_stNotiInfo.m_stNotiTime.ToUniversalTime().Month,
				Day = a_stNotiInfo.m_stNotiTime.ToUniversalTime().Day,
				Hour = a_stNotiInfo.m_stNotiTime.ToUniversalTime().Hour,
				Minute = a_stNotiInfo.m_stNotiTime.ToUniversalTime().Minute,
				Second = a_stNotiInfo.m_stNotiTime.ToUniversalTime().Second
			}
		};
	}
#endif			// #if UNITY_IOS

#if UNITY_ANDROID
	/** 안드로이드 알림을 생성한다 */
	private AndroidNotification MakeAndroidNoti(STNotiInfo a_stNotiInfo) {
		return new AndroidNotification(a_stNotiInfo.m_oTitle, a_stNotiInfo.m_oMsg, a_stNotiInfo.m_stNotiTime, a_stNotiInfo.m_bIsRepeat ? new System.TimeSpan(KCDefine.B_VAL_1_INT, KCDefine.B_VAL_0_INT, KCDefine.B_VAL_0_INT, KCDefine.B_VAL_0_INT, KCDefine.B_VAL_0_INT) : System.TimeSpan.Zero, KCDefine.U_ICON_N_ANDROID_NOTI_SMALL) {
			GroupSummary = true,
			ShowTimestamp = true,
			ShouldAutoCancel = true,
			Group = Application.identifier,
			Style = NotificationStyle.BigTextStyle,
			GroupAlertBehaviour = GroupAlertBehaviours.GroupAlertAll,
			LargeIcon = KCDefine.U_ICON_N_ANDROID_NOTI_LARGE
		};
	}
#endif			// #if UNITY_ANDROID
#endif			// #if UNITY_IOS || UNITY_ANDROID
	#endregion			// 조건부 함수
}
#endif			// #if NOTI_MODULE_ENABLE
