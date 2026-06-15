using UnityEngine;

namespace Outsiders.Auditory.Helpers.Data
{
    [CreateAssetMenu(menuName = "Outsiders/Audio/Scene BGM Mapper", fileName = "BGM Mapper_")]
    public class SceneBGMMapper : ScriptableObject
    {
        public string SceneName;
        public AudioEventSO BGM;
    }
}