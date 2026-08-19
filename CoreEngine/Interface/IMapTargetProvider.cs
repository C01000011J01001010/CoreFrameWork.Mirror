using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CoreEngine.Interface
{
    // 미니맵이 추적할 타겟의 데이터를 제공하는 인터페이스
    public interface IMapTargetProvider
    {
        Vector3 WorldPosition { get; }
    }
}
