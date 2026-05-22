#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 플레이엑스포 빌드 대응: Assets/Scenes 폴더 안의 모든 씬(Stage 2, 3 등)을 빌드 세팅(Build Settings)에 원클릭으로 일괄 자동 등록합니다.
/// </summary>
public static class AddScenesToBuildSettings
{
    [MenuItem("Tools/플레이엑스포/모든 씬 빌드 세팅에 추가 (Add All Scenes to Build)")]
    public static void AddAllScenes()
    {
        string scenesDir = "Assets/Scenes";
        if (!Directory.Exists(scenesDir))
        {
            Debug.LogError($"[빌드 세팅 자동화] {scenesDir} 폴더가 존재하지 않습니다.");
            return;
        }

        // Assets/Scenes 폴더 및 하위 폴더 내의 모든 .unity 파일 검색
        string[] sceneFiles = Directory.GetFiles(scenesDir, "*.unity", SearchOption.AllDirectories);
        List<EditorBuildSettingsScene> newScenesList = new List<EditorBuildSettingsScene>();

        // 1. 기존 빌드 세팅에 등록된 씬 목록을 가져와 중복 방지 캐시 구성
        HashSet<string> existingPaths = new HashSet<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            string cleanPath = scene.path.Replace("\\", "/");
            existingPaths.Add(cleanPath);
            newScenesList.Add(scene);
        }

        // 2. 검색된 씬 중 미등록된 씬만 빌드 세팅 목록에 추가
        int addedCount = 0;
        List<string> addedNames = new List<string>();

        foreach (string file in sceneFiles)
        {
            string normalizedPath = file.Replace("\\", "/");
            if (!existingPaths.Contains(normalizedPath))
            {
                newScenesList.Add(new EditorBuildSettingsScene(normalizedPath, true));
                existingPaths.Add(normalizedPath);
                addedCount++;
                addedNames.Add(Path.GetFileNameWithoutExtension(normalizedPath));
                Debug.Log($"[빌드 세팅 자동화] 빌드 세팅에 새 씬 등록 완료: {normalizedPath}");
            }
        }

        // 3. 빌드 세팅 갱신 및 결과 알림 팝업창 출력
        if (addedCount > 0)
        {
            EditorBuildSettings.scenes = newScenesList.ToArray();
            
            string addedListStr = string.Join(", ", addedNames);
            EditorUtility.DisplayDialog(
                "빌드 세팅 자동화 완료", 
                $"총 {addedCount}개의 씬이 빌드 세팅(Build Settings)에 자동으로 성공적으로 추가되었습니다!\n\n[추가된 씬 목록]\n{addedListStr}\n\n이제 허브 씬에서 스테이지 2, 3으로 정상 출격이 가능합니다.", 
                "확인"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "알림", 
                "모든 씬(Stage2_Scene, Stage3_Scene 등)이 이미 빌드 세팅(Build Settings)에 올바르게 등록되어 있습니다.", 
                "확인"
            );
        }
    }
}
#endif
