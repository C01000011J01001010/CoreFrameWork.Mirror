using System;
using System.Collections.Generic;

namespace CoreEditor.GameData.Validation
{
    public static class CsvSchemaParser
    {
        public static CsvColumnSchema ParseHeader(int columnIndex, string rawHeader)
        {
            // 1. 양끝의 괄호 { } 제거
            string content = rawHeader.Substring(1, rawHeader.Length - 2).Trim();

            // 2. '/'로 분할하되, DEFAULT(A/B) 처럼 괄호 '()' 내부의 '/'는 무시하는 스마트 스플릿
            List<string> tokens = SplitIgnoringParentheses(content, '/');
            if (tokens.Count == 0) throw new Exception("잘못된 헤더 포맷입니다.");

            var schema = new CsvColumnSchema
            {
                ColumnIndex = columnIndex,
                FieldName = tokens[0].Trim() // 첫 번째 토큰은 무조건 필드 이름
            };

            // 3. 제약조건 분석 (대소문자 및 내부 공백 무시)
            for (int i = 1; i < tokens.Count; i++)
            {
                string token = tokens[i].Trim();
                string upperToken = token.Replace(" ", "").ToUpper(); // 띄어쓰기 제거 후 대문자화

                if (string.IsNullOrEmpty(upperToken)) continue;

                if (upperToken == "PK") schema.IsPK = true;
                else if (upperToken == "UNIQUE") schema.IsUnique = true;
                else if (upperToken == "NOTNULL") schema.IsNotNull = true;
                else if (upperToken.StartsWith("DEFAULT("))
                {
                    schema.HasDefault = true;
                    // 값 자체는 원본(대소문자, 공백)을 그대로 유지해야 하므로 원본 token을 넘김
                    schema.DefaultValue = ExtractInsideParentheses(token);
                }
                else if (upperToken.StartsWith("CHECKIN("))
                {
                    // 파이썬 툴의 다중 값 구분자인 파이프(|) 기호로 분리
                    var values = ExtractInsideParentheses(token).Split('|');
                    foreach (var v in values) schema.InValues.Add(v.Trim());
                }
                else if (upperToken.StartsWith("CHECKNOTIN("))
                {
                    var values = ExtractInsideParentheses(token).Split('|');
                    foreach (var v in values) schema.NotInValues.Add(v.Trim());
                }
                else if (upperToken.StartsWith("CHECK("))
                {
                    schema.CheckExpressions.Add(ExtractInsideParentheses(token));
                }
                else
                {
                    // 위 키워드에 해당하지 않는 토큰(예: int, float, string[] 등 자료형 선언)은
                    // C#에서 리플렉션으로 실제 필드 타입을 찾아 매핑하므로 에러를 띄우지 않고 자연스럽게 무시합니다.
                }
            }

            return schema;
        }

        /// <summary>
        /// 괄호 내부의 문자열만 깔끔하게 추출합니다. (예: DEFAULT(Hello) -> Hello)
        /// </summary>
        private static string ExtractInsideParentheses(string token)
        {
            int start = token.IndexOf('(');
            int end = token.LastIndexOf(')');

            if (start == -1 || end == -1 || start >= end)
                throw new Exception($"괄호 포맷이 잘못되었습니다: {token}");

            return token.Substring(start + 1, end - start - 1).Trim();
        }

        /// <summary>
        /// 문자열을 separator 기준으로 나누되, 괄호 '()'로 묶인 내부의 문자는 무시합니다.
        /// </summary>
        private static List<string> SplitIgnoringParentheses(string text, char separator)
        {
            List<string> result = new List<string>();
            int bracketDepth = 0;
            int lastSplitIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(')
                {
                    bracketDepth++;
                }
                else if (text[i] == ')')
                {
                    bracketDepth--;
                }
                else if (text[i] == separator && bracketDepth == 0)
                {
                    result.Add(text.Substring(lastSplitIndex, i - lastSplitIndex));
                    lastSplitIndex = i + 1;
                }
            }

            result.Add(text.Substring(lastSplitIndex)); // 마지막 남은 토큰 추가
            return result;
        }
    }
}