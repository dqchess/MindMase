#if ADMOB
using GoogleMobileAds.Api;
#endif
using UnityEngine;

public class RewardedVideoCallBack : MonoBehaviour {

    private void Start()
    {
        Timer.Schedule(this, 0.1f, AddEvents);
    }

    private void AddEvents()
    {
#if (UNITY_ANDROID || UNITY_IOS) && ADMOB
        if (AdmobController.instance.rewardBasedVideo != null)
        {
            AdmobController.instance.rewardBasedVideo.OnAdRewarded += HandleRewardBasedVideoRewarded;
        }
#endif
    }

    private const string ACTION_NAME = "rewarded_video";
#if ADMOB
    public void HandleRewardBasedVideoRewarded(object sender, Reward args)
    {
        
    }
#endif

    private void OnDestroy()
    {
#if (UNITY_ANDROID || UNITY_IOS) && ADMOB
        if (AdmobController.instance.rewardBasedVideo != null)
        {
            AdmobController.instance.rewardBasedVideo.OnAdRewarded -= HandleRewardBasedVideoRewarded;
        }
#endif
    }
}
