using System;
using System.Data;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CoreEditor.GameData.Validation
{
    public static class ConstraintEvaluator
    {
        // System.Data.DataTable을 활용한 초경량 수식 평가기 캐싱
        private static readonly DataTable _evaluator = new DataTable();

        /// <summary>
        /// CHECK(조건문) 문자열을 실제 논리 연산으로 평가하여 true/false를 반환합니다.
        /// </summary>
        public static bool EvaluateCheck(string expression, object value)
        {
            // 조건식이 없으면 무조건 통과
            if (string.IsNullOrWhiteSpace(expression)) return true;

            string stringValue = value != null ? value.ToString() : "null";

            // 💡 값이 문자열(string)일 경우 수식에서 문자로 인식되도록 작은따옴표로 감싸기
            if (value is string)
            {
                stringValue = $"'{stringValue}'";
            }

            // 1. 'VALUE' 키워드를 실제 값으로 치환
            // 정규식 \b(단어 경계)를 사용하여 MAX_VALUE 같은 다른 단어가 잘못 치환되는 것을 방지
            string parsedExpr = Regex.Replace(expression, @"\bVALUE\b", stringValue, RegexOptions.IgnoreCase);

            // 2. 프로그래머 친화적 연산자(C# 스타일)를 DataTable 호환 연산자(SQL 스타일)로 변환
            parsedExpr = parsedExpr.Replace("==", "=");
            parsedExpr = parsedExpr.Replace("!=", "<>");

            // 3. XOR 연산 치환 
            // A XOR B ➔ (A AND NOT B) OR (NOT A AND B)
            parsedExpr = Regex.Replace(parsedExpr, @"(.+)\s+XOR\s+(.+)", "($1 AND NOT ($2)) OR (NOT ($1) AND $2)", RegexOptions.IgnoreCase);

            try
            {
                // 💡 수식 계산 실행 (C# DataTable.Compute는 내부적으로 단락 평가(Short-circuit)를 지원함)
                object result = _evaluator.Compute(parsedExpr, string.Empty);

                // 결과가 boolean 형태이면 반환, 아니면 캐스팅 실패로 에러 처리
                return result is bool b && b;
            }
            catch (Exception ex)
            {
                throw new Exception($"CHECK 조건식 평가 실패: 완성된 수식 [{parsedExpr}] ➔ {ex.Message}");
            }
        }
    }
}