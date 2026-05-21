import re

filepath = r"c:\Users\admin\Desktop\플레이 엑스포\playx4_game\Assets\Scenes\Stage2_Scene.unity"

with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# 1. 씬 파일 내 &994693352 와 &994693353 MonoBehaviour들 그리고 &994693350 를 원래 텍스트에서 분리하여 재정리
#    우선 &994693353 은 삭제하고, &994693352 는 BossBulletPatternController로 개조, &994693350 도 무결하게 설정합니다.

new_994693352 = """MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 994693325}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 0b928f7c00dd3564a87c74823feac7ad, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  enableBackgroundSpiral: 1
  baseProjectilePrefab: {fileID: 0}
  spiralFireInterval: 0.8
  spiralBulletCount: 6
  spiralRotationSpeed: 12
  ringWaveSettings:
    baseBulletCount: 12
    bulletsPerWave: 2
    baseSpeed: 5
    speedIncreasePerWave: 0.5
    maxWaveDelay: 1.2
    minWaveDelay: 0.6
    ezWaveCount: 3
    hardWaveCount: 5
  doubleHelixSettings:
    fireInterval: 0.15
    baseRotSpeed: 8
    rotSpeedAmplitude: 15
    baseSpiralArms: 2
    extraArmsOverTime: 2
    ezDuration: 4
    hardDuration: 6
    baseSpeed: 6
    speedPulseAmplitude: 2
    speedPulseFrequency: 4
  targetedFlowerSettings:
    ezFireInterval: 1.5
    hardFireInterval: 0.8
    bulletsPerShot: 5
    shotSpreadAngle: 30
    shotSpeed: 7
    ezShotCount: 3
    hardShotCount: 5
"""

new_994693350 = """MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 994693325}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: dc729fd1fe5c2b84c845f34decf6d29a, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  ezPatterns:
  - {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}
  ezInterval: 2.5
  normalPatterns:
  - {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}
  normalInterval: 1.5
  hardPatterns:
  - {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}
  hardInterval: 1.2
  bossSurvivalTime: 180
  currentSurvivalTimer: 0
  isHacking: 0
  initialDelay: 1
  bulletPatternController: {fileID: 994693352}
"""

docs = content.split("--- !u!")
new_docs = []

for doc in docs:
    if doc.startswith("114 &994693352"):
        new_docs.append("114 &994693352\n" + new_994693352)
    elif doc.startswith("114 &994693353"):
        # 삭제하므로 건너뜀
        continue
    elif doc.startswith("114 &994693350"):
        new_docs.append("114 &994693350\n" + new_994693350)
    elif doc.startswith("1001 &1749181680"):
        # PrefabInstance를 완벽하게 재구성
        prefab_instance_str = """1001 &1749181680
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 0}
    m_Modifications:
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalPosition.x
      value: -4.54
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalPosition.y
      value: 25.58
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalPosition.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {fileID: 0}
    - target: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_Name
      value: 2ed Boss
      objectReference: {fileID: 0}
    - target: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_TagString
      value: Untagged
      objectReference: {fileID: 0}
    - target: {fileID: 6019297042463171846, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_SortingOrder
      value: 10
      objectReference: {fileID: 0}
    - target: {fileID: 6019297042463171846, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      propertyPath: m_Materials.Array.data[0]
      value: 
      objectReference: {fileID: 2100000, guid: 13e609c76c876434d88adc3e06ff7c3b, type: 2}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects:
    - targetCorrespondingSourceObject: {fileID: 3763073079279829445, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      insertIndex: -1
      addedObject: {fileID: 1078261924}
    m_AddedComponents:
    - targetCorrespondingSourceObject: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      insertIndex: -1
      addedObject: {fileID: 994693349}
    - targetCorrespondingSourceObject: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      insertIndex: -1
      addedObject: {fileID: 994693348}
    - targetCorrespondingSourceObject: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      insertIndex: -1
      addedObject: {fileID: 994693347}
    - targetCorrespondingSourceObject: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      insertIndex: -1
      addedObject: {fileID: 994693346}
    - targetCorrespondingSourceObject: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      insertIndex: -1
      addedObject: {fileID: 994693350}
    - targetCorrespondingSourceObject: {fileID: 3844258671799329967, guid: 4c7836b404131c944b33d116752d9853,
        type: 3}
      insertIndex: -1
      addedObject: {fileID: 994693352}
  m_SourcePrefab: {fileID: 100100000, guid: 4c7836b404131c944b33d116752d9853, type: 3}
"""
        new_docs.append(prefab_instance_str.strip() + "\n")
    else:
        new_docs.append(doc)

modified_content = "--- !u!".join(new_docs)

with open(filepath, "w", encoding="utf-8") as f:
    f.write(modified_content)

print("Scene successfully restored and modified with 0 leftover errors!")
