using UnityEngine;

public class Constants
{
    // 현실 1초 = 게임 1분 / 현실 60초 = 게임 1시간
    public const int REAL_SECONDS_PER_GAME_MINUTE = 1;
    public const int REAL_SECONDS_PER_GAME_HOUR = 60;

    #region Layer Name
    public const string LAYER_PlayerCharacter = "PlayerCharacter";
    #endregion    

    #region LayerMask
    public static LayerMask LAYERMASK_PlayerCharacter = GetLayer(Layer.PlayerCharacter);
    #endregion

    #region 태그
    public const string TAG_StartPoint = "StartPoint";
    public const string TAG_PlayerCharacter = "PlayerChracter";
    #endregion

    private static LayerMask GetLayer(params Layer[] index)
    {
        LayerMask result = 0;
        foreach(Layer i in index)
        {
            result |= 1 << (int)i;
        }
        return result;
    }
}

public enum Layer
{
    PlayerCharacter
}
