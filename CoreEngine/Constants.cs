using UnityEngine;

public class Constants
{
    // 현실 1초 = 게임 1분 / 현실 60초 = 게임 1시간
    public const int REAL_SECONDS_PER_GAME_MINUTE = 1;
    public const int REAL_SECONDS_PER_GAME_HOUR = 60;

    //#region Resources 디렉터리
    //public const string DIRECTORY_Prefabs_Characters = "Prefabs/Characters";
    //public const string DIRECTORY_Prefabs_Controllers = "Prefabs/Controllers";
    //public const string DIRECTORY_Prefabs_Uis = "Prefabs/UIs";
    //#endregion

    #region Addressable Label
    public const string LABEL_GlobalPoolling = "GlobalPoolling";
    public const string LABEL_ScenedPoolling = "ScenedPoolling";

    public const string LABEL_CropData = "CropData";
    public const string LABEL_ItemData = "ItemData";
    public const string LABEL_QuestData = "QuestData";
    public const string LABEL_MagicalEggData = "MagicalEggData";

    public const string LABEL_ScenedUi = "ScenedUi";
    public const string LABEL_GlobalUi = "GlobalUi";



    #endregion

    #region Scene이름
    public const string SCENE_GlobalScene = "GlobalScene";
    public const string SCENE_TitleScene = "TitleScene";
    public const string SCENE_SampleScene = "SampleScene";
    #endregion


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
