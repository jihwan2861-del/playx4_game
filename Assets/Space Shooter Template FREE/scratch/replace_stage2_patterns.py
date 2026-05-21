import os
import re

scene_path = r"c:\Users\admin\Desktop\플레이 엑스포\playx4_game\Assets\Scenes\Stage2_Scene.unity"

print(f"Reading {scene_path} in binary mode...")
with open(scene_path, "rb") as f:
    data = f.read()

# 994693350 컴포넌트의 ezPatterns, normalPatterns, hardPatterns 영역 치환
# YAML 파일이므로 줄바꿈이 \r\n 또는 \n 일 수 있습니다.
# 정규식 패턴을 사용해 guid: 7b5e8a9d4f0c4a169b32c84dbef7e002 를 가진 부분을 교체합니다.

# ezPatterns
# - {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}
ez_old = b"- {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}"
ez_new = b"- {fileID: 3864356895611865114, guid: 5630e45cc126a7949a57691e95904141, type: 3}\n  - {fileID: 3663228080437020539, guid: 25a6ffe9c52ffb64496feeb7f2343ca7, type: 3}"

# normalPatterns
# - {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}
normal_new = b"- {fileID: 7964999317648650255, guid: c45f3363bd8555e4a90b93dc8c6391c0, type: 3}\n  - {fileID: 7332403248913460301, guid: 6d3c156df25975646847cf3ac2f2a38b, type: 3}"

# hardPatterns
# - {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}
hard_new = b"- {fileID: 1927208500936088772, guid: 9dd6f7a9e9073144aa862763092649d5, type: 3}\n  - {fileID: 3011148023377321845, guid: d148154532457a34f84e3fa9462fa60d, type: 3}"

# 정교하게 dc729fd1fe5c2b84c845f34decf6d29a (BossPatternController) 가 포함된 MonoBehaviour 블록을 찾아 치환합니다.
# 994693350 블록을 찾아서 치환하는 것이 가장 정확합니다.
# --- !u!114 &994693350 에서 시작해서 다음 --- 까지가 블록입니다.

pattern = re.compile(rb"(--- !u!114 &994693350.*?)(--- !u!114 &994693352)", re.DOTALL)

match = pattern.search(data)
if match:
    print("Found 994693350 component block!")
    block = match.group(1)
    
    # block 내에서 ezPatterns, normalPatterns, hardPatterns의 요소를 교체
    # ezPatterns 뒤에 처음으로 나오는 ez_old 교체
    # normalPatterns 뒤에 처음으로 나오는 ez_old 교체
    # hardPatterns 뒤에 처음으로 나오는 ez_old 교체
    
    # 텍스트 구조 분석을 위해 라인 바이 라인으로 안전하게 파싱 및 교체
    lines = block.split(b"\n")
    new_lines = []
    current_section = None
    
    for line in lines:
        line_stripped = line.strip()
        if line_stripped.startswith(b"ezPatterns:"):
            current_section = "ez"
            new_lines.append(line)
        elif line_stripped.startswith(b"normalPatterns:"):
            current_section = "normal"
            new_lines.append(line)
        elif line_stripped.startswith(b"hardPatterns:"):
            current_section = "hard"
            new_lines.append(line)
        elif line_stripped.startswith(b"- {fileID: 1, guid: 7b5e8a9d4f0c4a169b32c84dbef7e002, type: 3}"):
            # 적절한 인덴트(2칸 이상)를 맞춰줍니다.
            indent = line.split(b"-")[0]
            if current_section == "ez":
                new_lines.append(indent + b"- {fileID: 3864356895611865114, guid: 5630e45cc126a7949a57691e95904141, type: 3}")
                new_lines.append(indent + b"- {fileID: 3663228080437020539, guid: 25a6ffe9c52ffb64496feeb7f2343ca7, type: 3}")
            elif current_section == "normal":
                new_lines.append(indent + b"- {fileID: 7964999317648650255, guid: c45f3363bd8555e4a90b93dc8c6391c0, type: 3}")
                new_lines.append(indent + b"- {fileID: 7332403248913460301, guid: 6d3c156df25975646847cf3ac2f2a38b, type: 3}")
            elif current_section == "hard":
                new_lines.append(indent + b"- {fileID: 1927208500936088772, guid: 9dd6f7a9e9073144aa862763092649d5, type: 3}")
                new_lines.append(indent + b"- {fileID: 3011148023377321845, guid: d148154532457a34f84e3fa9462fa60d, type: 3}")
            else:
                new_lines.append(line)
        else:
            # 섹션이 끝나면 current_section 초기화 (인덴트가 2 미만이거나 다른 필드인 경우)
            if current_section and not line.startswith(b"  "):
                current_section = None
            new_lines.append(line)
            
    new_block = b"\n".join(new_lines)
    
    # 원본 데이터 치환
    new_data = data.replace(block, new_block)
    
    with open(scene_path, "wb") as f:
        f.write(new_data)
    print("Successfully replaced boss patterns in Stage2_Scene.unity!")
else:
    print("Could not find the target component block 994693350.")
    exit(1)
