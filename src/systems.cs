// -------------------------------------------------------------------------------------
// The MIT License
// Extended systems for LeoECS Lite https://github.com/Leopotam/ecslite-extendedsystems
// Copyright (c) 2021-2022 Leopotam <leopotam@gmail.com>
// -------------------------------------------------------------------------------------

using System;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Leopotam.EcsLite.ExtendedSystems {
    public struct EcsGroupSystemState<T> where T: IComparable {
        public T Name;
        public bool State;
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public static class Extensions {
        public static EcsSystems AddGroup<T> (this EcsSystems systems, T groupName, bool defaultState, string eventWorldName, params IEcsSystem[] nestedSystems) where T: IComparable {
            return systems.Add (new EcsGroupSystem<T> (groupName, defaultState, eventWorldName, nestedSystems));
        }

        public static EcsSystems AddGroup (this EcsSystems systems, string groupName, bool defaultState, string eventWorldName, params IEcsSystem[] nestedSystems) {
            return systems.AddGroup<string>(groupName, defaultState, eventWorldName, nestedSystems);
        }

        public static EcsSystems DelHere<T> (this EcsSystems systems, string worldName = null) where T : struct {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (systems.GetWorld (worldName) == null) { throw new System.Exception ($"Requested world \"{(string.IsNullOrEmpty (worldName) ? "[default]" : worldName)}\" not found."); }
#endif
            return systems.Add (new DelHereSystem<T> (systems.GetWorld (worldName)));
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class DelHereSystem<T> : IEcsRunSystem where T : struct {
        readonly EcsFilter _filter;
        readonly EcsPool<T> _pool;

        public DelHereSystem (EcsWorld world) {
            _filter = world.Filter<T> ().End ();
            _pool = world.GetPool<T> ();
        }

        public void Run (EcsSystems systems) {
            foreach (var entity in _filter) {
                _pool.Del (entity);
            }
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsGroupSystem<T> :
        IEcsPreInitSystem,
        IEcsInitSystem,
        IEcsRunSystem,
        IEcsDestroySystem,
        IEcsPostDestroySystem
        where T: IComparable {
        readonly IEcsSystem[] _allSystems;
        readonly IEcsRunSystem[] _runSystems;
        readonly int _runSystemsCount;
        readonly string _eventsWorldName;
        readonly T _name;
        EcsFilter _filter;
        EcsPool<EcsGroupSystemState<T>> _pool;
        bool _state;

        public EcsGroupSystem (T name, bool defaultState, string eventsWorldName, params IEcsSystem[] systems) {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
            if (name == null || name is string nameStr && string.IsNullOrEmpty (nameStr)) { throw new System.Exception ("Group name cant be null or empty."); }
            if (systems == null || systems.Length == 0) { throw new System.Exception ("Systems list cant be null or empty."); }
#endif
            _name = name;
            _state = defaultState;
            _eventsWorldName = eventsWorldName;
            _allSystems = systems;
            _runSystemsCount = 0;
            _runSystems = new IEcsRunSystem[_allSystems.Length];
            for (var i = 0; i < _allSystems.Length; i++) {
                if (_allSystems[i] is IEcsRunSystem runSystem) {
                    _runSystems[_runSystemsCount++] = runSystem;
                }
            }
        }

        public void PreInit (EcsSystems systems) {
            var world = systems.GetWorld (_eventsWorldName);
            _pool = world.GetPool<EcsGroupSystemState<T>> ();
            _filter = world.Filter<EcsGroupSystemState<T>> ().End ();
            for (var i = 0; i < _allSystems.Length; i++) {
                if (_allSystems[i] is IEcsPreInitSystem preInitSystem) {
                    preInitSystem.PreInit (systems);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = systems.CheckForLeakedEntities ();
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {preInitSystem.GetType ().Name}.PreInit()."); }
#endif
                }
            }
        }

        public void Init (EcsSystems systems) {
            for (var i = 0; i < _allSystems.Length; i++) {
                if (_allSystems[i] is IEcsInitSystem initSystem) {
                    initSystem.Init (systems);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = systems.CheckForLeakedEntities ();
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {initSystem.GetType ().Name}.Init()."); }
#endif
                }
            }
        }

        public void Run (EcsSystems systems) {
            foreach (var entity in _filter) {
                ref var evt = ref _pool.Get (entity);
                if (evt.Name.Equals (_name)) {
                    _state = evt.State;
                    _pool.Del (entity);
                }
            }

            if (_state) {
                for (var i = 0; i < _runSystemsCount; i++) {
                    _runSystems[i].Run (systems);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = systems.CheckForLeakedEntities ();
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {_runSystems[i].GetType ().Name}.Run()."); }
#endif
                }
            }
        }

        public void Destroy (EcsSystems systems) {
            for (var i = _allSystems.Length - 1; i >= 0; i--) {
                if (_allSystems[i] is IEcsDestroySystem destroySystem) {
                    destroySystem.Destroy (systems);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = systems.CheckForLeakedEntities ();
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {destroySystem.GetType ().Name}.Destroy()."); }
#endif
                }
            }
        }

        public void PostDestroy (EcsSystems systems) {
            for (var i = _allSystems.Length - 1; i >= 0; i--) {
                if (_allSystems[i] is IEcsPostDestroySystem postDestroySystem) {
                    postDestroySystem.PostDestroy (systems);
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                    var worldName = systems.CheckForLeakedEntities ();
                    if (worldName != null) { throw new System.Exception ($"Empty entity detected in world \"{worldName}\" after {postDestroySystem.GetType ().Name}.PostDestroy()."); }
#endif
                }
            }
        }
    }
}

#if ENABLE_IL2CPP
// Unity IL2CPP performance optimization attribute.
namespace Unity.IL2CPP.CompilerServices {
    enum Option {
        NullChecks = 1,
        ArrayBoundsChecks = 2
    }

    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    class Il2CppSetOptionAttribute : Attribute {
        public Option Option { get; private set; }
        public object Value { get; private set; }

        public Il2CppSetOptionAttribute (Option option, object value) { Option = option; Value = value; }
    }
}
#endif