using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;
using FMODUnity;
using Utils.Extensions;
using UnityEngine;
using UnityEngine.Assertions;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace AudioSystem
{
    public class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager instance;
        
        private Dictionary<string, EventInstance> eventInstances = new();
        
        private List<EventInstance> activeMusicInstances = new();

        /// <summary>
        /// Initializes the singleton instance. Ensures only one instance exists and persists across scenes.
        /// </summary>
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Stops the music with the specified event path and resumes the previous music if available.
        /// </summary>
        /// <param name="_eventPath">The FMOD event path of the music to stop.</param>
        public void StopMusic(string _eventPath)
        {
            foreach (var _m in activeMusicInstances.ToList())
            {
                _m.getDescription(out EventDescription _desc);
                _desc.getPath(out string _path);
                
                if (_path == _eventPath)
                {
                    _m.stop(STOP_MODE.ALLOWFADEOUT);
                    _m.release();
                    activeMusicInstances.Remove(_m);
                }
            }
            
            if (activeMusicInstances.Count > 0)
            {
                activeMusicInstances[^1].start();
            }
        }
        
        /// <summary>
        /// Plays music with the specified event path. Stops the currently playing music if any.
        /// </summary>
        /// <param name="_eventPath">The FMOD event path of the music to play.</param>
        public void PlayMusic(string _eventPath)
        {
            if (string.IsNullOrEmpty(_eventPath))
            {
                return;
            }
            
            EventReference _eventReference = RuntimeManager.PathToEventReference(_eventPath);
            EventInstance _newMusicInstance = RuntimeManager.CreateInstance(_eventReference);

            Assert.IsTrue(_newMusicInstance.isValid(), "the new music instance is not valid with path: " + _eventPath);
            
            if (activeMusicInstances.Count != 0)
            {
                activeMusicInstances[^1].stop(STOP_MODE.ALLOWFADEOUT);
            }
            
            _newMusicInstance.start();
            activeMusicInstances.Add(_newMusicInstance);
        }

        /// <summary>
        /// Plays a one-shot sound effect at the specified position using an EventReference.
        /// </summary>
        /// <param name="_eventReference">The FMOD event reference to play.</param>
        /// <param name="_position">The 3D position where the sound should be played. Defaults to zero.</param>
        public void PlayOneShot(EventReference _eventReference, Vector3 _position = default)
        {
            PlayOneShot(_eventReference.GetPath(), _position);
        }
        
        /// <summary>
        /// Plays a one-shot sound effect at the specified position using an event path.
        /// </summary>
        /// <param name="_eventPath">The FMOD event path to play.</param>
        /// <param name="_position">The 3D position where the sound should be played. Defaults to zero.</param>
        public void PlayOneShot(string _eventPath, Vector3 _position = default)
        {
            if (string.IsNullOrEmpty(_eventPath))
            {
                return;
            }
            
            RuntimeManager.PlayOneShot(_eventPath, _position);
        }
        
        /// <summary>
        /// Creates and plays an event instance with the specified reference and associates it with a key for later management.
        /// </summary>
        /// <param name="_eventReference">The FMOD event reference to play.</param>
        /// <param name="_instanceKey">A unique key to identify and manage this instance.</param>
        /// <param name="_position">The 3D position where the sound should be played. Defaults to zero.</param>
        /// <returns>The created EventInstance, or null if the event path is empty.</returns>
        public EventInstance? PlayEventInstance(EventReference _eventReference, string _instanceKey, Vector3 _position = default)
        {
            return PlayEventInstance(_eventReference.GetPath(), _instanceKey, _position);
        }
        
        /// <summary>
        /// Creates and plays an event instance with the specified path and associates it with a key for later management.
        /// </summary>
        /// <param name="_eventPath">The FMOD event path to play.</param>
        /// <param name="_instanceKey">A unique key to identify and manage this instance.</param>
        /// <param name="_position">The 3D position where the sound should be played. Defaults to zero.</param>
        /// <returns>The created EventInstance, or null if the event path is empty.</returns>
        public EventInstance? PlayEventInstance(string _eventPath, string _instanceKey, Vector3 _position = default)
        {
            if (string.IsNullOrEmpty(_eventPath))
            {
                return null;
            }
            
            EventReference _eventReference = RuntimeManager.PathToEventReference(_eventPath);
            EventInstance _instance = RuntimeManager.CreateInstance(_eventReference);
            
            _instance.getDescription(out EventDescription _desc);
            _desc.is3D(out bool _is3D);
            if (_is3D)
            {
                _instance.set3DAttributes(_position.To3DAttributes());
            }
            
            _instance.start();

            if (eventInstances.ContainsKey(_instanceKey))
            {
                StopEventInstance(_instanceKey);
            }
                
            eventInstances.Add(_instanceKey, _instance);
            return _instance;
        }
        
        /// <summary>
        /// Updates the 3D position of an active event instance.
        /// </summary>
        /// <param name="_instanceKey">The unique key of the instance to update.</param>
        /// <param name="_position">The new 3D position for the sound.</param>
        public void UpdateEventInstancePosition(string _instanceKey, Vector3 _position)
        {
            if (string.IsNullOrEmpty(_instanceKey))
            {
                return;
            }
            
            if (eventInstances.TryGetValue(_instanceKey, out EventInstance _instance))
            {
                if (_instance.isValid())
                {
                    _instance.set3DAttributes(RuntimeUtils.To3DAttributes(_position));
                }
            }
        }
        

        /// <summary>
        /// Stops and releases an active event instance by its key.
        /// </summary>
        /// <param name="_instanceKey">The unique key of the instance to stop.</param>
        public void StopEventInstance(string _instanceKey)
        {
            if (string.IsNullOrEmpty(_instanceKey))
            {
                return;
            }
            
            if (eventInstances.TryGetValue(_instanceKey, out EventInstance _instance))
            {
                if (_instance.isValid())
                {
                    _instance.stop(STOP_MODE.ALLOWFADEOUT);
                    _instance.release();
                }
                eventInstances.Remove(_instanceKey);
            }
        }

        /// <summary>
        /// Cleans up all active music and event instances when the GameObject is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            foreach (var _music in activeMusicInstances)
            {
                if (_music.isValid())
                {
                    _music.stop(STOP_MODE.ALLOWFADEOUT);
                    _music.release();
                }
            }
            activeMusicInstances.Clear();
            
            foreach (var _eventInstance in eventInstances.Values)
            {
                if (_eventInstance.isValid())
                {
                    _eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
                    _eventInstance.release();
                }
            }
            eventInstances.Clear();
        }
    }
}
