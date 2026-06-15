using UnityEngine;

namespace XR23.SocialCard.Data 
{
    [CreateAssetMenu(fileName = "LearningData_0", menuName = "XR23/Learning Card Data")]
    public class LearningDataSO : ScriptableObject 
    {
        public string GameName;
        public Sprite OutcomeIconA;
        public Sprite OutcomeIconB;
        public Sprite OutcomeIconC;
        public string OutcomeA;
        public string OutcomeB;
        public string OutcomeC;
        public string BannerText;
    }
}