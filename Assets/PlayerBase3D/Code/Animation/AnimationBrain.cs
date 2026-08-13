using System.Collections.Generic;
using UnityEngine;

namespace nakatimat.Animation
{
    // Generic layer over the Animator, reusable across Player, NPCs and enemies.
    // Switches states via Animator.CrossFade instead of Animator Controller
    // parameters/transitions, so the controller stays a flat list of states and
    // the switching logic lives in code (e.g. PlayerAnimationHandler).
    public class AnimationBrain : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _defaultTransitionDuration = 0.15f;

        private readonly Dictionary<string, int> _stateHashCache = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _layerIndexCache = new Dictionary<string, int>();

        public Animator Animator => _animator;

        private void Awake()
        {
            if (_animator == null) { _animator = GetComponent<Animator>(); }
        }

        public int GetLayerIndex(string layerName)
        {
            if (_layerIndexCache.TryGetValue(layerName, out int cached)) { return cached; }
            int index = _animator != null ? _animator.GetLayerIndex(layerName) : -1;
            _layerIndexCache[layerName] = index;
            return index;
        }

        public void SetLayerWeight(string layerName, float weight)
        {
            int index = GetLayerIndex(layerName);
            if (index < 0) { return; }
            SetLayerWeight(index, weight);
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            if (_animator == null || layerIndex < 0) { return; }
            _animator.SetLayerWeight(layerIndex, weight);
        }

        // Guards against restarting the animation if it's already the current
        // (or in-transition) target state, since this gets called every frame.
        public void CrossFade(string stateName, float transitionDuration = -1f, int layer = 0)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName)) { return; }

            int hash = GetStateHash(stateName);
            if (IsAlreadyTargeting(hash, layer)) { return; }

            float duration = transitionDuration >= 0f ? transitionDuration : _defaultTransitionDuration;
            _animator.CrossFade(hash, duration, layer);
        }

        public void CrossFadeOnLayer(string layerName, string stateName, float transitionDuration = -1f)
        {
            CrossFade(stateName, transitionDuration, GetLayerIndex(layerName));
        }

        public bool IsInState(string stateName, int layer = 0)
        {
            if (_animator == null) { return false; }
            int hash = GetStateHash(stateName);
            return _animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == hash;
        }

        private bool IsAlreadyTargeting(int stateHash, int layer)
        {
            if (_animator.IsInTransition(layer))
            {
                return _animator.GetNextAnimatorStateInfo(layer).shortNameHash == stateHash;
            }
            return _animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == stateHash;
        }

        private int GetStateHash(string stateName)
        {
            if (_stateHashCache.TryGetValue(stateName, out int cached)) { return cached; }
            int hash = Animator.StringToHash(stateName);
            _stateHashCache[stateName] = hash;
            return hash;
        }
    }
}
