// Retarget the KAWAII humanoid clip to CuteGirl's Generic rig without changing its idle/walk rig.
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Playables;
using UnityEngine.Animations;
public class StudyRoomSitAnimationBuilder { [MenuItem("StudyRoom/Rebuild Chair Sit Animation")] public static void Rebuild() { Debug.Log(Run());
}
public static string Run() => Bake("KA_Sit_PutHandBetweens_Loop", "ChairSitLoop", true);
public static string Bake(string sourceName, string outputName, bool loop){var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StudyRoom/Prefabs/Characters/CuteGirl.prefab");var go=Object.Instantiate(original);
go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
try {var all=go.GetComponentsInChildren<Transform>();
var parents=all.Select(t=>t.parent).ToArray();
var paths=all.Select(t=>AnimationUtility.CalculateTransformPath(t,go.transform)).ToArray();
string[] names={"DEF-spine","DEF-spine.001","DEF-spine.003","DEF-spine.004","DEF-spine.006","DEF-thigh.L","DEF-shin.L","DEF-foot.L","DEF-toe.L","DEF-thigh.R","DEF-shin.R","DEF-foot.R","DEF-toe.R","DEF-shoulder.L","DEF-upper_arm.L","DEF-forearm.L","DEF-hand.L","DEF-shoulder.R","DEF-upper_arm.R","DEF-forearm.R","DEF-hand.R"};
string[] human={"Hips","Spine","Chest","Neck","Head","LeftUpperLeg","LeftLowerLeg","LeftFoot","LeftToes","RightUpperLeg","RightLowerLeg","RightFoot","RightToes","LeftShoulder","LeftUpperArm","LeftLowerArm","LeftHand","RightShoulder","RightUpperArm","RightLowerArm","RightHand"};
foreach(var pair in new[]{new[]{"DEF-thigh.L","DEF-spine"},new[]{"DEF-thigh.R","DEF-spine"},new[]{"DEF-shoulder.L","DEF-spine.003"},new[]{"DEF-shoulder.R","DEF-spine.003"},new[]{"DEF-upper_arm.L","DEF-shoulder.L"},new[]{"DEF-upper_arm.R","DEF-shoulder.R"},new[]{"head","DEF-spine.006"}}) all.First(t=>t.name==pair[0]).SetParent(all.First(t=>t.name==pair[1]),true);
var sourceGo=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/StudyRoom/Animations/KAWAII_ANIMATIOMS_100/Assets/Meshes/DummyDoll.FBX"));
sourceGo.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
var animator=sourceGo.GetComponent<Animator>();
if(!animator) animator=sourceGo.AddComponent<Animator>();
animator.avatar=AssetDatabase.LoadAssetAtPath<Avatar>("Assets/StudyRoom/Animations/KAWAII_ANIMATIOMS_100/Assets/Meshes/DummyDoll.FBX");
animator.runtimeAnimatorController=null;
animator.applyRootMotion=false;
animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
animator.Rebind();
var source=AssetDatabase.LoadAllAssetsAtPath("Assets/StudyRoom/Animations/KAWAII_ANIMATIOMS_100/Assets/Animations/@" + sourceName + ".FBX").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
var sourceBones=human.Select(n=>animator.GetBoneTransform((HumanBodyBones)System.Enum.Parse(typeof(HumanBodyBones),n))).ToArray();
var targetBones=names.Select(n=>all.First(t=>t.name==n)).ToArray();
var sourceRest=sourceBones.Select(t=>t.rotation).ToArray();
var targetRest=targetBones.Select(t=>t.rotation).ToArray();
var alignment=go.transform.rotation*Quaternion.Euler(0,180,0);
var sourceHips=sourceBones[0].position;
var targetHips=targetBones[0].position;
float ratio=(targetBones[0].position.y-go.transform.position.y)/(sourceHips.y-sourceGo.transform.position.y);
var graph=PlayableGraph.Create("Bake Chair Sit");
var playable=AnimationClipPlayable.Create(graph,source);
var output=AnimationPlayableOutput.Create(graph,"Pose",animator);
output.SetSourcePlayable(playable);
graph.Play();
var curves=new AnimationCurve[all.Length,7];
for(int i=1;i<all.Length;i++) for(int j=0;j<7;j++) curves[i,j]=new AnimationCurve();
// Remove a short tail from one-shot actions before blending back to the native idle.
float duration = Mathf.Max(1f / 30f, source.length - (loop ? 0f : 0.15f));
int frames=Mathf.CeilToInt(duration*30);
for(int frame=0;frame<=frames;frame++){float time=duration*frame/frames;
playable.SetTime(time);
graph.Evaluate(0);
targetBones[0].position=targetHips+(alignment*(sourceBones[0].position-sourceHips))*ratio;
for(int b=0;b<targetBones.Length;b++) targetBones[b].rotation=alignment*sourceBones[b].rotation*Quaternion.Inverse(sourceRest[b])*Quaternion.Inverse(alignment)*targetRest[b];
for(int i=1;i<all.Length;i++){var t=all[i];
var parent=parents[i];
Vector3 pos=parent.InverseTransformPoint(t.position);
Quaternion rot=Quaternion.Inverse(parent.rotation)*t.rotation;
float[] values={pos.x,pos.y,pos.z,rot.x,rot.y,rot.z,rot.w};
for(int j=0;j<7;j++) curves[i,j].AddKey(time,values[j]);}}
graph.Destroy();
Object.DestroyImmediate(sourceGo);
var clip=new AnimationClip{name="CuteGirl_" + outputName,frameRate=30};
string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
for(int i=1;i<all.Length;i++) for(int j=0;j<7;j++) AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(paths[i],typeof(Transform),props[j]),curves[i,j]);
clip.EnsureQuaternionContinuity();
var settings=AnimationUtility.GetAnimationClipSettings(clip);
settings.loopTime=loop;
AnimationUtility.SetAnimationClipSettings(clip,settings);
string outputPath="Assets/StudyRoom/Animations/" + outputName + ".anim";
var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
if (!existing) { existing=new AnimationClip(); AssetDatabase.CreateAsset(existing,outputPath); }
EditorUtility.CopySerialized(clip,existing);
EditorUtility.SetDirty(existing);
AssetDatabase.SaveAssets();
Object.DestroyImmediate(clip);
AssetDatabase.ImportAsset(outputPath,ImportAssetOptions.ForceUpdate);
return "Baked "+frames+" frames; human="+existing.humanMotion+" curves="+AnimationUtility.GetCurveBindings(existing).Length;}
finally{Object.DestroyImmediate(go);}}}
