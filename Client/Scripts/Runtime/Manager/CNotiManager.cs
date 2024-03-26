using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if NOTI_MODULE_ENABLE
using System.IO;
using System.Threading;
using System.Globalization;

#if UNITY_IOS
using Unity.Notifications.iOS;
#elif UNITY_ANDROID
using Unity.Notifications.Android;
#endif // #if UNITY_IOS

/** 알림 관리자 */
public partial class CNotiManager : CSingleton<CNotiManager>
{
	/** 콜백 */
	public enum ECallback
	{
		NONE = -1,
		INIT,
		[HideInInspector] MAX_VAL
	}

	/** 매개 변수 */
	public struct STParams
	{
		public Dictionary<ECallback, System.Action<CNotiManager, bool>> m_oCallbackDict;
	}

	#region 프로퍼티
	public STParams Params { get; private set; }
	public bool IsInit { get; private set; } = false;
	#endregion // 프로퍼티

	#region 함수
	/** 초기화 */
	public virtual void Init(STParams a_stParams)
	{
		CFunc.ShowLog("CNotiManager.Init", KCDefine.B_LOG_COLOR_PLUGIN);

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
		// 초기화되었을 경우
		if(this.IsInit) {
			a_stParams.m_oCallbackDict?.ExGetVal(ECallback.INIT)?.Invoke(this, this.IsInit);
		} else {
			this.Params = a_stParams;

#if UNITY_IOS
			var oRequest = new AuthorizationRequest(KCDefine.G_NOTI_M_OPTS_AUTHORIZATION, false);

			this.ExRepeatCallFunc((a_oSender, a_bIsComplete) => {
				// 완료되었을 경우
				if(a_bIsComplete) {
					this.OnInit();
				}
				
				return oRequest != null && !oRequest.IsFinished;
			}, KCDefine.G_NOTI_M_INTERVAL_CHECK_REQUEST, KCDefine.G_NOTI_M_INTERVAL_CHECK_REQUEST_MAX);
#else
			AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(Application.identifier, KCDefine.G_NOTI_M_NAME_GROUP_NOTI, KCDefine.G_NOTI_M_DESC_GROUP_NOTI, Importance.Default));
			this.ExLateCallFunc((a_oSender) => this.OnInit());
#endif // #if UNITY_IOS
		}
#else
		a_stParams.m_oCallbackDict?.ExGetVal(ECallback.INIT)?.Invoke(this, false);
#endif // #if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
	}

	/** 알림을 추가한다 */
	public void AddNoti(string a_oKey, STNotiInfo a_stNotiInfo)
	{
		CFunc.ShowLog($"CNotiManager.AddNoti: {a_oKey}, {a_stNotiInfo.m_oMsg}, {a_stNotiInfo.m_stNotiTime}", KCDefine.B_LOG_COLOR_PLUGIN);
		CFunc.Assert(a_oKey.ExIsValid());

#if UNITY_IOS || UNITY_ANDROID
		// 초기화되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.ScheduleNotification(this.MakeiOSNoti(a_oKey, a_stNotiInfo));
#else
			AndroidNotificationCenter.SendNotificationWithExplicitID(this.MakeAndroidNoti(a_stNotiInfo), Application.identifier, this.MakeNotiID(a_oKey));
#endif // #if UNITY_IOS
		}
#endif // #if UNITY_IOS || UNITY_ANDROID
	}

	/** 알림을 제거한다 */
	public void RemoveNoti(string a_oKey)
	{
		CFunc.ShowLog($"CNotiManager.RemoveNoti: {a_oKey}", KCDefine.B_LOG_COLOR_PLUGIN);
		CFunc.Assert(a_oKey.ExIsValid());

#if UNITY_IOS || UNITY_ANDROID
		// 초기화되었을 경우
		if(this.IsInit) {
#if UNITY_IOS
			iOSNotificationCenter.RemoveScheduledNotification(a_oKey);
#else
			AndroidNotificationCenter.CancelScheduledNotification(this.MakeNotiID(a_oKey));
#endif // #if UNITY_IOS
		}
#endif // #if UNITY_IOS || UNITY_ANDROID
	}
	#endregion // 함수

	#region 조건부 함수
#if UNITY_IOS || UNITY_ANDROID
	// 초기화되었을 경우
	private void OnInit() {
		CFunc.ShowLog("CNotiManager.OnInit", KCDefine.B_LOG_COLOR_PLUGIN);

		CScheduleManager.Inst.AddCallback(KCDefine.G_NOTI_M_KEY_CALLBACK_INIT, () => {
#if UNITY_IOS
			iOSNotificationCenter.RemoveAllDeliveredNotifications();
#else
			AndroidNotificationCenter.CancelAllDisplayedNotifications();
#endif // #if UNITY_IOS

			this.IsInit = true;
			this.Params.m_oCallbackDict?.ExGetVal(ECallback.INIT)?.Invoke(this, true);
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
			Body = a_stNotiInfo.m_oMsg,
			Title = a_stNotiInfo.m_oTitle,
			Subtitle = a_stNotiInfo.m_oSubTitle,

			CategoryIdentifier = Application.identifier,
			ThreadIdentifier = $"{Thread.CurrentThread.ManagedThreadId}",

			ShowInForeground = true,
			ForegroundPresentationOption = KCDefine.G_NOTI_M_OPTS_PRESENTATION,

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
#endif // #if UNITY_IOS

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
#endif // #if UNITY_ANDROID
#endif // #if UNITY_IOS || UNITY_ANDROID
	#endregion // 조건부 함수
}

/** 알림 관리자 - 팩토리 */
public partial class CNotiManager : CSingleton<CNotiManager>
{
	#region 클래스 함수
	/** 매개 변수를 생성한다 */
	public static STParams MakeParams(Dictionary<ECallback, System.Action<CNotiManager, bool>> a_oCallbackDict = null)
	{
		return new STParams()
		{
			m_oCallbackDict = a_oCallbackDict ?? new Dictionary<ECallback, System.Action<CNotiManager, bool>>()
		};
	}
	#endregion // 클래스 함수
}
#endif // #if NOTI_MODULE_ENABLE
