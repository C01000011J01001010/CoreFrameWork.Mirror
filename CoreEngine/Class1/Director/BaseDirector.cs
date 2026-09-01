using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CoreEngine.DesignPattern.Singleton;

namespace CoreEngine.Director
{
    public class BaseDirector<Director> : Singleton<Director>, IDirector
        where Director : BaseDirector<Director>
    {
    }
}
