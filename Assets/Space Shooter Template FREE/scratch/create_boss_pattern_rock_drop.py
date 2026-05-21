import os

prefab_path = r"c:\Users\admin\Desktop\플레이 엑스포\playx4_game\Assets\Space Shooter Template FREE\Prefabs\Enemies\BossPattern_RockDrop.prefab"
meta_path = prefab_path + ".meta"

os.makedirs(os.path.dirname(prefab_path), exist_ok=True)

prefab_content = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2}
  - component: {fileID: 3}
  m_Layer: 0
  m_Name: BossPattern_RockDrop
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &2
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &3
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 9185d4a0236b83449897bc363b27b089, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  rockPrefab: {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e001, type: 3}
  rockCount: 2
  throwRate: 0.8
  duration: 4.5
  cooldown: 2.5
  repeatPattern: 0
  bossAnimator: {fileID: 0}
  attackAnimTrigger: Attack
"""

meta_content = """fileFormatVersion: 2
guid: 7b5e8a9d4f0c4a169b32c84dbef7e002
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

with open(prefab_path, "w", encoding="utf-8") as f:
    f.write(prefab_content)
print(f"Created {prefab_path}")

with open(meta_path, "w", encoding="utf-8") as f:
    f.write(meta_content)
print(f"Created {meta_path}")
