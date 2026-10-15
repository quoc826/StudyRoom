using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class StudyRoomCharacterActionsBuilder
{
    [MenuItem("StudyRoom/Rebuild Character Idle And Celebration")]
    public static void Rebuild()
    {
        StudyRoomSitAnimationBuilder.Bake("KA_Idle01_breathing", "IdleBreathing", true);
        StudyRoomSitAnimationBuilder.Bake("KA_Idle16_WaveHands", "AfkWave", false);
        StudyRoomSitAnimationBuilder.Bake("KA_Idle02_LookLeftAndRight", "AfkLook", false);
        StudyRoomSitAnimationBuilder.Bake("KA_Idle36_Yay", "QuestCelebrate", false);
        StudyRoomSitAnimationBuilder.Bake("KA_Idle20_TriplePose", "ClickTriplePose", false);
        StudyRoomSitAnimationBuilder.Bake("KA_Idle27_Angry", "ClickAngry", false);
        StudyRoomSitAnimationBuilder.Bake("KA_Idle08_ComeUpWithAnIdea", "ClickIdea", false);
        StudyRoomSitAnimationBuilder.Bake("KA_Idle04_LookAtFeet", "ClickLookAtFeet", false);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            "Assets/ThirdParty/Characters/Chibi_Girl/Animations/CuteGirlAnimator.controller");
        SetMotion(controller, "Idle", "IdleBreathing");
        SetMotion(controller, "AfkWave", "AfkWave");
        SetMotion(controller, "AfkLook", "AfkLook");
        SetMotion(controller, "Celebrate", "QuestCelebrate");
        SetMotion(controller, "ClickTriplePose", "ClickTriplePose");
        SetMotion(controller, "ClickAngry", "ClickAngry");
        SetMotion(controller, "ClickIdea", "ClickIdea");
        SetMotion(controller, "ClickLookAtFeet", "ClickLookAtFeet");
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static void SetMotion(AnimatorController controller, string stateName, string clipName)
    {
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName)
            ?? machine.AddState(stateName);
        state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/StudyRoom/Animations/" + clipName + ".anim");
    }
}
