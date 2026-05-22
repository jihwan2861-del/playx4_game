using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 유니티 에디터 내에서 투박하고 묵직한 철제 패링 타격 효과음(.wav)을 
/// 수학적 파형(DSP) 합성을 통해 실시간으로 생성해주는 에디터 스크립트입니다.
/// </summary>
public class ParrySoundGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Heavy Parry Sound")]
    public static void GenerateParrySound()
    {
        string folderPath = "Assets/Space Shooter Template FREE/Audio";
        string filePath = folderPath + "/parry_success_heavy.wav";

        // 오디오 폴더가 존재하지 않는 경우 자동 생성
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        float sampleRate = 44100f;
        float duration = 0.45f; // 0.45초의 임팩트 있고 묵직한 느낌
        int numSamples = (int)(sampleRate * duration);
        short[] pcmData = new short[numSamples];

        System.Random rand = new System.Random();

        for (int i = 0; i < numSamples; i++)
        {
            float t = i / sampleRate;

            // 1. Sharp Metal Clang (철제 무기가 강하게 맞부딪히는 순간의 날카로운 고주파음)
            float metalClang = Mathf.Sin(2f * Mathf.PI * 1350f * t) * 0.45f * Mathf.Exp(-55f * t)
                             + Mathf.Sin(2f * Mathf.PI * 1920f * t) * 0.35f * Mathf.Exp(-40f * t)
                             + Mathf.Sin(2f * Mathf.PI * 2580f * t) * 0.20f * Mathf.Exp(-70f * t);

            // 2. Heavy Hollow Resonance (모루나 철퇴가 묵직하게 울리는 중저주파 몸체 공명음 - '투박함'의 핵심)
            float ironResonance = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.55f * Mathf.Exp(-14f * t)
                                + Mathf.Sin(2f * Mathf.PI * 310f * t) * 0.30f * Mathf.Exp(-22f * t);

            // 3. Impact Grit & Friction Noise (부딪히는 찰나에 발생하는 마찰 파열 소음 및 먼지 튀는 입자감)
            float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.30f * Mathf.Exp(-95f * t);

            // 4. 소리 파형 믹싱 & 리미터 (소리가 깨지는 디스토션을 막고 고유의 꽉 찬 헤드룸을 가집니다)
            float combined = metalClang + ironResonance + noise;

            if (combined > 1.0f) combined = 1.0f;
            if (combined < -1.0f) combined = -1.0f;

            // 5. 끝단 페이드아웃 (사운드 재생이 끝날 때 툭 끊기는 잡음(Pop Noise) 완벽 제거)
            float fadeOut = Mathf.Clamp01((duration - t) / 0.05f); // 마지막 50ms 동안 페이드아웃
            combined *= fadeOut;

            // 16비트 정수 오디오 신호로 변환
            pcmData[i] = (short)(combined * 32767f);
        }

        // WAV 오디오 파일 표준 규격 쓰기 (Standard RIFF-WAVE PCM 16-Bit Mono Format)
        using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                int bytesPerSample = 2; // 16비트 PCM
                int numChannels = 1;    // 모노 채널
                int byteRate = (int)sampleRate * numChannels * bytesPerSample;
                int blockAlign = numChannels * bytesPerSample;

                // 1) RIFF 헤더
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + numSamples * bytesPerSample);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // 2) fmt 청크 (포맷 정보)
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // 포맷 헤더 크기 (16바이트)
                writer.Write((short)1); // PCM 오디오 포맷 코드
                writer.Write((short)numChannels);
                writer.Write((int)sampleRate);
                writer.Write(byteRate);
                writer.Write((short)blockAlign);
                writer.Write((short)(bytesPerSample * 8));

                // 3) data 청크 (실제 오디오 진폭 데이터)
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(numSamples * bytesPerSample);

                // 오디오 진폭 샘플 기록
                for (int i = 0; i < numSamples; i++)
                {
                    writer.Write(pcmData[i]);
                }
            }
        }

        // 유니티 엔진에 새로운 에셋이 생성되었음을 즉시 통보하여 자동 로딩
        AssetDatabase.Refresh();

        Debug.Log($"🔊 [ParrySoundGenerator] 성공! 투박하고 묵직한 패링 성공음이 생성되었습니다: {filePath}");
        EditorUtility.DisplayDialog("패링 사운드 생성 완료", 
            "투박하고 묵직한 메탈 패링 성공음이 성공적으로 합성되었습니다!\n\n" +
            "생성 경로: Assets/Space Shooter Template FREE/Audio/parry_success_heavy.wav\n\n" +
            "유니티 프로젝트 창에 바로 업데이트되었으니 드래그하여 적용해 보세요!", 
            "확인");
    }
}
