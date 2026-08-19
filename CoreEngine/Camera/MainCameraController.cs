using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;



/// <summary>
/// GlobalScene에서 CameraManager의 자식객체로 사용
/// </summary>
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CinemachineBrain))]
public class MainCameraController : MonoBehaviour//, IInitializable
{
    public static Camera mainCamera { get; private set; }
    public CinemachineBrain cameraBrain {get; private set;}

    public void Exit()
    {
        mainCamera = null;
        cameraBrain = null;
    }

    public IEnumerator Initialize()
    {
        mainCamera = GetComponent<Camera>();
        cameraBrain = GetComponent<CinemachineBrain>();

        yield return null;
    }

    
}
