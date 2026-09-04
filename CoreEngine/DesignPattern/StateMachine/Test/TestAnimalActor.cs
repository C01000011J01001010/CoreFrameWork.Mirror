using CoreEngine;
using CoreEngine.Actor;
using UnityEngine;

namespace CoreEngine.DesignPattern.StateMachine.Test
{
    public class TestAnimalActor : BaseActor, ITickable, IActorHost
    {
        [SerializeField]
        private TestAnimalStateController _stateController;
        public TestAnimalStateController StateController => _stateController;

        private void Awake()
        {
            _stateController = new();
            _stateController.Initialize(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();    
            _stateController.StartState();
        }

        public TickGroup TickGroup => TickGroup.Character;

        public void Tick(float deltaTime)
        {
            _stateController.Tick(deltaTime);
        }

        bool IActorHost.TryGetFeature<T>(out T feature)
        {
            feature = null;
            return false;
        }
    }
}

