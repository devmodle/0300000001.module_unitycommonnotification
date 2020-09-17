using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if LOCAL_NOTI_MODULE_ENABLE
//! 로컬 알림 관리자
public class CLocalNotiManager : CSingleton<CLocalNotiManager> {
	#region 변수
	private System.Action<CLocalNotiManager, bool> m_oInitCallback = null;
	#endregion			// 변수

	#region 프로퍼티
	public bool IsInit { get; private set; } = false;
	#endregion			// 프로퍼티

	#region 함수
	//! 초기화
	public virtual void Init(System.Action<CLocalNotiManager, bool> a_oCallback) {
		CFunc.ShowLog("CLocalNotiManager.Init", KCDefine.B_LOG_COLOR_PLUGIN);

#if UNITY_IOS || UNITY_ANDROID
		// 초기화 가능 할 경우
		if(!this.IsInit && CAccess.IsMobilePlatform()) {
			this.IsInit = true;
		}
#endif			// #if UNITY_IOS || UNITY_ANDROID

		a_oCallback?.Invoke(this, this.IsInit);
	}
	#endregion			// 함수
}
#endif			// #if LOCAL_NOTI_MODULE_ENABLE
