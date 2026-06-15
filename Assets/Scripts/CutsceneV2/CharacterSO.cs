using UnityEngine;
using UnityEngine.Localization;

namespace XR23.Phishing.Cutscene.Data
{
    [CreateAssetMenu(menuName = "XR23/Cutscene/Character")]
    public sealed class CharacterSO : ScriptableObject
    {
        [field: SerializeField] public string CharacterId { get; private set; }
        [field: SerializeField] public string DisplayName { get; private set; }
        [field: SerializeField] public LocalizedString CharacterName { get; private set; }
        [field: SerializeField] public Sprite CharacterSprite { get; private set; }
        [field: SerializeField] public Sprite CharacterMaleSprite { get; private set; }
        [field: SerializeField] public Sprite CharacterFemaleSprite { get; private set; }
    }
}