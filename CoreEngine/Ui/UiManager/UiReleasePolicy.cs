using UnityEngine;

namespace CoreEngine.UI
{
    /// <summary>
    /// AddressableUi에서 사용
    /// </summary>
    public enum UiReleasePolicy
    {
        Keep = -1,
        Immediately = 0,

        AfterSecond05 = 5,
        AfterSecond10 = 10,
        AfterSecond15 = 15,
        AfterSecond30 = 30,
        AfterSecond45 = 45,

        AfterMinute01 = 60,
        AfterMinute02 = 120,
        AfterMinute03 = 180,
        AfterMinute05 = 300,
        AfterMinute10 = 600,
        AfterMinute15 = 900,
        AfterMinute20 = 1200,
        AfterMinute30 = 1800,
        AfterMinute45 = 2700,

        AfterHour01 = 3600,
        AfterHour02 = 7200,
        AfterHour03 = 10800,
        AfterHour06 = 21600,
        AfterHour12 = 43200,
        AfterHour24 = 86400,
    }
}

