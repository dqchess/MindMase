#if ADMOB
using GoogleMobileAds.Api;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardedVideoButton : MonoBehaviour
{
    private const string ACTION_NAME = "rewarded_video";

    private void OnEnable()
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

    public void OnClick()
    {
#if ADMOB
#if UNITY_EDITOR
        HandleRewardBasedVideoRewarded(null, null);
#else
        if (IsAvailableToShow())
        {
            AdmobController.instance.ShowRewardBasedVideo();
        }
        else if (!IsActionAvailable())
        {
            int remainTime = (int)(GameConfig.instance.rewardedVideoPeriod - CUtils.GetActionDeltaTime(ACTION_NAME));
            Toast.instance.ShowMessage("Please wait " + remainTime + " seconds for the next ad");
        }
        else
        {
            Toast.instance.ShowMessage("Ad is not available now, please wait..");
        }
#endif
#else
        Debug.LogError("You need to install Admob plugin to use this feature");
#endif
    }

#if ADMOB
    public void HandleRewardBasedVideoRewarded(object sender, Reward args)
    {
        int amount = GameConfig.instance.rewardedVideoAmount;
        GameManager.Hints += amount;
        SudokuManager.intance.UpdateUI();
        Toast.instance.ShowMessage("You've received " + amount + " hints", 2);
        CUtils.SetActionTime(ACTION_NAME);
    }
#endif

#if ADMOB
    public bool IsAvailableToShow()
    {
        return IsActionAvailable() && IsAdAvailable();
    }
#endif

    private bool IsActionAvailable()
    {
        return CUtils.IsActionAvailable(ACTION_NAME, GameConfig.instance.rewardedVideoPeriod);
    }

#if ADMOB
    private bool IsAdAvailable()
    {
        if (AdmobController.instance.rewardBasedVideo == null) return false;
        bool isLoaded = AdmobController.instance.rewardBasedVideo.IsLoaded();
        return isLoaded;
    }
#endif

    private void OnDisable()
    {
#if (UNITY_ANDROID || UNITY_IOS) && ADMOB
        if (AdmobController.instance.rewardBasedVideo != null)
        {
            AdmobController.instance.rewardBasedVideo.OnAdRewarded -= HandleRewardBasedVideoRewarded;
        }
#endif
    }
}
