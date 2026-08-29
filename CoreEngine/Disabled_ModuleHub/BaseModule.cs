using System.Collections;
using UnityEngine;
using CoreEngine;
using CoreEngine.Extentions;
public abstract class BaseModule<TOnwer> //: IModule
    where TOnwer : MonoBehaviour, IModuleHub
{
    public TOnwer Onwer { get; private set; }
    public bool IsActive { get; protected set; }

    public void Exit()
    {
        throw new System.NotImplementedException();
    }

    public virtual IEnumerator Initialize(IModuleHub owner)
    {
        Onwer = owner.AsOrThrow<TOnwer>();
        yield break;
    }

    public virtual void SetActive(bool active)
    {
        IsActive = active;
    }
}