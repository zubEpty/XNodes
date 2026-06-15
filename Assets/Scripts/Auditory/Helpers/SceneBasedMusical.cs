using Outsiders.Auditory.Helpers.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Outsiders.Auditory.Helpers
{
    public class SceneBasedMusical : MonoBehaviour
    {
        public List<SceneBGMMapper> DataStore = new();
        private AudioManager _audioM;
        public AudioEventSO DefaultBGM;
        private void Awake()
        {
            SceneManager.sceneLoaded += OnNewSceneLoaded;
            SceneManager.sceneUnloaded += OnSubsceneUnloaded;
            _audioM = GetComponentInParent<AudioManager>();
        }
        private void OnSubsceneUnloaded(Scene arg0)
        {
            _audioM.PlayBGM(DefaultBGM);
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnNewSceneLoaded;
            SceneManager.sceneUnloaded -= OnSubsceneUnloaded;
        }
        private void OnNewSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            var data = DataStore.FirstOrDefault(name => name.SceneName.Equals(arg0.name));
            _audioM.PlayBGM(data.BGM, fadeTime: 1.5f);
        }
    }
}