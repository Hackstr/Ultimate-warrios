namespace TacticalDuelist.Core.Models
{
    public enum TutorialStepType
    {
        TapAction,
        TapConfirm,
    }

    public class TutorialStep
    {
        public TutorialStepType Type;
        public ActionType RequiredAction;
        public string HintText;
        public string ButtonId;
    }
}
