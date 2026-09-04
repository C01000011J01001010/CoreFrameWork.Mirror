using CoreEngine.Data;
using System;
using System.IO;
using System.Linq;
using CoreEngine.Extensions;
using CoreEditor.Extensions;
using UnityEditor;
using UnityEngine;


namespace CoreEditor.EditorTools
{
    public abstract class BaseCsvConverter : BaseEditorWindow<CSVConverter_Setting>
    {
        protected abstract string ConverterTarget { get; }
        protected abstract Type TargetType { get; }
        protected const string defaultMenu = Constants.ToolRootCsv + "CSV to SO Converter -> ";

        protected string saveDirectory { get => editorSetting.saveDirectory; set => editorSetting.saveDirectory = value; }
        protected string TypeName { get => editorSetting.typeName; set => editorSetting.typeName = value; }
        protected int attributeCount { get => editorSetting.attributeCount; set => editorSetting.attributeCount = value; }
        TextAsset selectedCsv;
        protected bool isFileValid;
        protected bool isPathValid;

        protected string[] headers;



        protected virtual void OnEnable()
        {
            LoadSavedSettings($"{ConverterTarget}CsvConverter_SetData");

            // 마지막으로 선택했던 타입 불러오기
            //if (string.IsNullOrWhiteSpace(TypeName)) 
            //{
            //    TypeName = TargetType?.Name;
            //    UpdateSetting();
            //}
        }

        private void OnGUI()
        {
            GUILayout.Label($"For {ConverterTarget}", EditorStyles.boldLabel);
            GUILayout.Space(10);

            //DrawDropDown();
            //GUILayout.Space(10);

            ValidateCsvFile();
            GUILayout.Space(10);

            CheckTypeNameForReflection();

            CheckColCount();
            GUILayout.Space(10);

            CheckDirectory();
            GUILayout.Space(20);

            Execute();
        }

        private void DrawDropDown()
        {
            // ---데이터 타입 드롭다운 선택 ---
            //EditorGUILayout.BeginVertical(GUI.skin.box);
            //using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            //{
            //    EditorGUI.BeginChangeCheck();
            //    selectedType = (DataType)EditorGUILayout.EnumPopup("변환할 데이터 타입", selectedType);

            //    // 타입이 바뀌면 자동으로 저장 경로도 변경해줍니다!
            //    if (EditorGUI.EndChangeCheck())
            //    {
            //        EditorPrefs.SetInt(SelectedTypePrefsKey, (int)selectedType);
            //    }
            //}
            //EditorGUILayout.EndVertical();
        }

        private void ValidateCsvFile()
        {
            // --- CSV 파일 선택(유효성 검사 포함) ---
            selectedCsv = Selection.activeObject as TextAsset;
            isFileValid = false;
            string fileName = "선택된 파일 없음";

            if (selectedCsv != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedCsv); // Assets 내에서 경로를 
                                                                            //Debug.Log($"파일 {selectedCsv.name}의 경로 : {assetPath}");
                string temp = Path.GetExtension(assetPath); // 확장자만 걸러내는 함수
                                                            //Debug.Log($"Path.GetExtension({assetPath}) 결과 : {temp}");
                string extension = Path.GetExtension(assetPath).ToLower();

                // 확장자로 걸러내기 (csv 파일 확장자는 둘중 하나)
                if (extension == ".csv" || extension == ".txt")
                {
                    // 파일 이름 초기화, 유효성 검사 확인
                    fileName = selectedCsv.name + extension;
                    isFileValid = true;
                }
            }

            //EditorGUILayout.BeginVertical("box");
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("선택된 파일:", fileName, EditorStyles.wordWrappedLabel);
            }
            //EditorGUILayout.EndVertical();
        }



        private void CheckDirectory()
        {
            // --- 3. 저장 경로 (자동 설정되지만 수동으로도 변경 가능) ---
            isPathValid = Directory.Exists(saveDirectory);
            //EditorGUILayout.BeginHorizontal();
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                // 직접 수정할 수 없도록 TextField 대신 LabelField
                EditorGUILayout.LabelField("저장 경로", saveDirectory, EditorStyles.textField);
                if (GUILayout.Button("찾기", GUILayout.Width(50)))
                {
                    // 마지막 디렉터리를 확인
                    if (!Directory.Exists(saveDirectory))
                    {
                        // 없으면 Assets로 이동
                        Debug.LogWarning($"directory({saveDirectory})가 유효하지 않음");
                        saveDirectory = Application.dataPath;
                    }

                    string path = EditorUtility.OpenFolderPanel("저장 폴더 선택", saveDirectory, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        saveDirectory = path.ToUnityPath();
                        isPathValid = Directory.Exists(saveDirectory);
                        UpdateSetting();
                    }
                }
            }
            //EditorGUILayout.EndHorizontal();
        }

        protected virtual void CheckTypeNameForReflection() { }

        private void CheckColCount()
        {
            using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                EditorGUI.BeginChangeCheck();

                attributeCount = EditorGUILayout.IntField("속성 개수", attributeCount);
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateSetting();
                }
            }
        }

        private void Execute()
        {
            // --- 실행 버튼 ---
            bool isTypeValid = typeof(ScriptableObject).IsAssignableFrom(TargetType);
            if (!isTypeValid && TargetType != null)
            {
                Debug.LogError($"변경 대상 타입({TargetType?.Name})이 ScriptableObject가 아님");
            }
            bool valid = isFileValid && isPathValid && isTypeValid;
            //GUI.enabled = valid;
            using (new EditorGUI.DisabledGroupScope(!valid))
            {
                if (GUILayout.Button("변환 실행", GUILayout.Height(40)))
                {
                    ExecuteConvert(selectedCsv);
                }
            } // <--- 중괄호가 끝나는 순간, 알아서 원래 상태(true)로 돌아감
              // 만약 이전 상태가 false라면 그대로 false
              //GUI.enabled = true;
        }


        private void ExecuteConvert(TextAsset csvFile)
        {
            // 디렉토리가 없으면 생성
            // 그런데 이전 내용대로면 디렉토리가 반드시 존재함
            // 안전하게 하려고 넣어둠
            if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);

            string[] rows = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 스키마만 존재하는 경우
            if (rows.Length <= 1)
            {
                EditorUtility.DisplayDialog("데이터 변환 실패", "데이터가 부족합니다. (헤더와 내용 필요)", "확인");
                return;
            }

            ConvertData(rows, csvFile.name);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        private void ConvertData(string[] rows, string fileName)
        {
            int successCount = 0;

            // reflection에 사용
            string[] rawHeaders = rows[0].Split(',');//[0..attributeCount];

            // 값이 비어있지 않거나 공백이 아닌 동안만 가져와서 배열로 만
            // 중간에 빈 값("")을 만나면 그 즉시 멈추고 이전까지만 반환
            // TakeWhile : 결과가 true인 경우 데이터를 취하고 false이면 멈춤
            headers = rawHeaders.TakeWhile(h => !string.IsNullOrWhiteSpace(h)).ToArray();

            for (int i = 1; i < rows.Length; i++)
            {
                string[] cols = rows[i].Split(',');

                //2보다 크거나 같은값
                if (cols.Length < Mathf.Max(attributeCount, 2))
                {
                    Debug.LogWarning($"{i}번째 행은 데이터의 개수(원소의 개수)가 부족함");
                    continue;
                }

                try
                {
                    ConvertDataRow(i, cols, ref successCount);
                }
                catch (Exception e) { Debug.LogError($"[ItemData] {i + 1}번째 줄 실패: {e.Message}"); }
            }
            Debug.Log($"✅ [{fileName}] 총 {successCount}개의 아이템 데이터 갱신 완료!");
        }

        private void ConvertDataRow(int rowNum, string[] cols, ref int successCount)
        {
            int id;
            string nameTag;
            TryParse(rowNum, cols, out id, out nameTag);

            string targetAssetPath = GetTargetAssetPath(id, nameTag);

            // 같은 일련번호의 파일 이름이 변경되면 바꿔주기
            RenameOldFile(id, targetAssetPath);

            // 저장장치에서 메모리로 불러오기
            ScriptableObject asset = GetAsset(targetAssetPath, TargetType);

            // 메모리에 있는 인스턴스의 데이터를 바꾸고
            if (asset is BaseData asDataObject)
            {
                // 리플랙션으로 private 데이터 바꿔주기
                Type type = typeof(BaseData);
                asset.SetFieldByReflection("index", id);
                asset.SetFieldByReflection("nameTag", nameTag);
                // 세부적인 내용은 개별 정리
                ConvertDetails(asset, rowNum, cols);
            }

            // 현재 인스턴스와 실제 에셋의 데이터가 다르니 저장하라고 유니티에 요구하는 메서드
            // 이 메서드가 없으면 데이터를 바꿔도 에셋으로 저장 안됨
            EditorUtility.SetDirty(asset);
            successCount++;
        }

        private void TryParse(int rowNum, string[] cols, out int id, out string nameTag)
        {
            bool valid = true;

            // 둘다 데이터가 들어 있으면 참
            valid = !(Empty(rowNum, 0, cols) || Empty(rowNum, 1, cols));

            // 일련번호와 이름은 위치 고정
            id = -1;
            // valid가 false면 캐스팅 시도 안함
            if (valid && !int.TryParse(cols[0].Trim(), out id))
            {
                Debug.LogWarning($"{selectedCsv.name}파일의 {rowNum}번째 행의 id는 숫자가 아님");
                valid = false;
            }
            nameTag = cols[1].Trim();
            if (!valid) throw new Exception($" {rowNum}번째 행은 유효하지 않은 데이터가 포함됐음");
        }

        private void RenameOldFile(int id, string targetAssetPath)
        {
            // 기존 파일 보존 로직 (자리에 어떤 글자가 오든 문제 없음)
            // ex) id = 1이면 "1_"로 시작하고 ".asset"으로 끝나는 모든 파일이 해당
            // Directory.GetFiles는 '디렉토리 + 파일 이름 + 확장자'가 모두 합쳐진 파일의 전체 경로를 배열로 반환
            string[] existingFiles = Directory.GetFiles(saveDirectory, $"{id}_*.asset");
            if (existingFiles != null && existingFiles.Length == 1)
            {
                string oldAssetPath = existingFiles[0].ToUnityPath();

                // 기존 파일이 존재하고 target이름이 파일로 없으면 기존 파일에서 이름만 바꾼다
                if (oldAssetPath != targetAssetPath)
                {
                    // targetAssetPath에서 확장자와 경로를 제외한 '순수 파일 이름'만 추출
                    string newName = Path.GetFileNameWithoutExtension(targetAssetPath);

                    string error = AssetDatabase.RenameAsset(oldAssetPath, newName);

                    // 에러가 있다면 콘솔에 출력하여 원인 파악
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[ID: {id}] 파일 이름 변경 실패: {error}");
                    }
                }
            }
            else if (existingFiles != null && existingFiles.Length > 1)
            {
                Debug.LogWarning($"[ID: {id}]에 해당하는 이전 파일이 여러 개 존재하여 이름 변경을 취소");
            }
        }

        private ScriptableObject GetAsset(string assetPath, Type assetType)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            // 없다면 새로 만들기
            if (asset == null)
            {
                // 인스턴스로 만들고 해당 인스턴스를 에셋으로 저장
                asset = CreateInstance(assetType);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            return asset;
        }

        // 일련번호와 이름 외에 세부적인 데이터 처리
        protected virtual void ConvertDetails(ScriptableObject asset, int rowNum, string[] cols) { }

        protected bool Empty(int row, int col, string[] cols)
        {
            bool result = string.IsNullOrWhiteSpace(cols[col]);
            if (result)
            {
                Debug.LogWarning($"{row}번째 행의 {col}번째 열이 비어있음");
            }
            return result;
        }

        protected void WarningTypeError(ScriptableObject asset) => throw new Exception($"객체({asset.name})의 타입이 [{TargetType.Name}]이 아님");

        protected string GetSafeFileName(string name) => string.Concat(name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        protected string GetTargetAssetPath(int id, string name) => $"{saveDirectory}/{id}_{GetSafeFileName(name)}.asset";
    }
}
